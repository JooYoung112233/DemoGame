using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// QA 자동 플레이 봇 — **Claude가 명령을 내리고 게임이 실행, 결과를 파일로 돌려주는 테스트 하네스**.
/// (docs/qa.md)
///
/// 조작은 전부 `GameInput` 가상 입력층을 통해 나간다 = 사람이 키보드를 치는 것과 같은 경로.
/// 게임 코드는 봇의 존재를 모른다(호출처 무변경). QA를 통째로 들어내도 게임은 그대로 돈다.
///
/// ── 실행 모드 ─────────────────────────────────────────────
///   game.exe -qa                      시나리오 1회 실행 후 리포트
///   game.exe -qa -qa-quit             끝나면 종료(오류 시 exit 1) — CI/무인
///   game.exe -qa-serve                **명령 대기 모드** — Claude가 qa-command.json을 넣으면 실행하고
///                                     qa-result-&lt;id&gt;.json으로 답한다. 여러 인스턴스 병렬 가능.
///   game.exe -qa-instance=A           인스턴스 이름(병렬 실행 시 파일 충돌 방지)
///   game.exe -qa-minutes=5            세션 상한(기본 5분)
///   에디터: F9 시작/중단
/// </summary>
public class QaBot : MonoBehaviour
{
    public static QaBot Instance { get; private set; }
    public static bool IsRunning => Instance != null && Instance._running;

    // ── 실행 설정 ────────────────────────────────────────────────────
    string _instance = "";          // 병렬 실행 구분(파일 접두)
    float _sessionMinutes = 5f;     // 사용자 요청: 우선 5분 세션
    bool _serveMode;
    bool _quitWhenDone;

    QaReport _rep;
    QaTelemetry _tele;
    QaContext _ctx;
    QaScenarioDef _scenario;
    System.Random _rng;

    bool _running;
    bool _analyzed;         // 지표·공간 분석 1회 보장(정상 종료/중단 어느 쪽이든)
    string _step = "boot";
    int _cycle;
    string _commandId = "";
    float _startedAt;

    PlayerInventory _inv;
    public PlayerInventory Inv
    {
        get
        {
            if (_inv == null && TopDownPlayer.Instance != null) _inv = TopDownPlayer.Instance.GetComponent<PlayerInventory>();
            return _inv;
        }
    }

    int _lootBaseline;

    // 스턱 감지
    float _stuckTimer;
    int _stuckReported;
    int _oscReported;       // 와리가리(제자리 진동) 보고 횟수
    float _pauseTimer;      // 일시정지 안전망 주기
    float _brainFlush;      // AI 기록 주기 저장(런 중 실시간 확인용)
    int _pauseSeen;         // 일시정지가 열린 횟수(보고 스팸 방지)

    // 자가 복구 — 사람이 안 붙어 있어도 런이 굴러가야 한다
    int _recoverLevel;      // 사이클당 0→1(세이브 로드)→2(사이클 처음부터)
    bool _restartStep;      // RunScenario가 보고 현재 사이클을 스텝0부터 다시
    int _cycleRestarts;     // 무한 재시작 방지

    // ── 플레이어 AI (지각 + 판단) ────────────────────────────────────
    /// <summary>봇이 **아는 것**만 담는다(전지 금지). ai.play가 이걸 보고 판단한다.</summary>
    public QaPerception Perception { get; private set; }
    /// <summary>목표 선택 + 결정 기록. 애매하면 스스로 질문을 남긴다.</summary>
    public QaBrain Brain { get; private set; }

    // ── 공간 텔레메트리 ("어디서" 데이터) ───────────────────────────
    QaHeatmap _heat;
    public QaHeatmap Heat => _heat;
    public int Cycle => _cycle;
    public string Scene => SceneManager.GetActiveScene().name;
    float _sampleTimer;
    bool _wasDead;

    // 씬별 길찾기 격자 기록 — 사람이 콘솔을 뒤지지 않아도 결과 JSON에 실린다
    readonly HashSet<string> _navSeen = new HashSet<string>();
    readonly List<QaBridge.NavGridJson> _navGrids = new List<QaBridge.NavGridJson>();

    /// <summary>현재 위치를 히트맵에 기록할 좌표(플레이어 없으면 zero).</summary>
    public Vector2 PlayerPos => TopDownPlayer.Instance != null ? (Vector2)TopDownPlayer.Instance.transform.position : Vector2.zero;

    /// <summary>스텝에서 "여기서 목표 도달 실패" 기록 — 진행 막히는 지점 클러스터링용.</summary>
    public void NoteUnreachable() => _heat?.AddUnreachable(Scene, PlayerPos, _cycle);

    // ── 부트 ─────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;
        if (Instance != null) return;
        var go = new GameObject("[QaBot]");
        DontDestroyOnLoad(go);
        var bot = go.AddComponent<QaBot>();
        bot.ParseArgs();

        if (bot._serveMode) bot.StartCoroutine(bot.ServeLoop());
        else if (HasArg("-qa")) bot.StartRun(bot._quitWhenDone);
    }

    void ParseArgs()
    {
        _instance = ArgValue("-qa-instance") ?? "";
        QaBridge.Instance = _instance;   // 막힘/세션 파일도 같은 접두를 쓰게
        _serveMode = HasArg("-qa-serve");
        _quitWhenDone = HasArg("-qa-quit");
        var m = ArgValue("-qa-minutes");
        if (!string.IsNullOrEmpty(m) && float.TryParse(m, out float mm) && mm > 0f) _sessionMinutes = mm;
    }

    static bool HasArg(string a)
    {
        foreach (var s in System.Environment.GetCommandLineArgs())
            if (string.Equals(s, a, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>-key=value 형태 인자 값.</summary>
    static string ArgValue(string key)
    {
        foreach (var s in System.Environment.GetCommandLineArgs())
            if (s != null && s.StartsWith(key + "=", System.StringComparison.OrdinalIgnoreCase))
                return s.Substring(key.Length + 1);
        return null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        // 봇이 죽어도 가상 입력이 켜진 채 남으면 **실제 키보드/마우스가 전부 먹통**이 된다.
        if (_running) { GameInput.Virtual = false; SaveManager.SuppressWrites = false; }
        if (Instance == this) { Application.logMessageReceived -= OnLog; Instance = null; }
    }

    void OnApplicationQuit()
    {
        // 중단(Play 정지·창 닫기)돼도 **여기까지의 결과는 남긴다**.
        // 안 그러면 긴 런이 끊겼을 때 아무 데이터도 안 남아 밖에서 진단할 수 없다.
        if (_running)
        {
            _rep?.Warn(_step, "INTERRUPTED", $"런이 끝나기 전 중단됨(스텝 {_step}, 사이클 {_cycle})");
            Finish();
        }
    }

    /// <summary>F9 수동 토글은 개발 환경에서만. 릴리스 빌드에선 -qa* 인자로만 켜진다.</summary>
    static bool HotkeyAllowed => Application.isEditor || Debug.isDebugBuild || HasArg("-qa-hotkey");

    void Update()
    {
        if (!HotkeyAllowed) return;
        // 봇이 도는 동안엔 GameInput이 가상이라, 시작/중단 토글은 디바이스를 직접 본다.
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        if (kb.f9Key.wasPressedThisFrame)
        {
            if (_running) Abort("사용자 F9 중단");
            else StartRun(false);
        }

        // F10 = 명령 대기 모드 토글.
        //
        // 이게 없으면 **마더가 에디터를 반복 지휘할 수 없다** — 실행 인자(-qa-serve)는 빌드에만
        // 줄 수 있어서, 에디터는 매 판 사람이 F9를 눌러야 했다. 자동 루프(qa_autoloop)는
        // "투입 → 결과 → 교정 → 재투입"을 사람 없이 돌려야 하므로 반드시 필요하다.
        // 켜두면 Play 상태로 두기만 해도 마더가 계속 시나리오를 밀어 넣는다.
        if (kb.f10Key.wasPressedThisFrame)
        {
            if (_serveMode)
            {
                _serveMode = false;
                StopCoroutine(nameof(ServeLoop));
                Debug.Log("[QA] 명령 대기 모드 **해제** (F10)");
                WriteStatus("off", "명령 대기 해제");
            }
            else
            {
                _serveMode = true;
                Debug.Log($"[QA] 명령 대기 모드 **시작** (F10) — 마더가 이 에디터에 시나리오를 투입할 수 있다\n"
                          + $"  명령 파일: {F("qa-command.json")}");
                if (!_running) StartCoroutine(ServeLoop());
            }
        }
    }

    float _hbTimer;

    void LateUpdate()
    {
        // 가상 입력의 1프레임 플래그는 모든 Update 소비자가 본 뒤 걷는다.
        if (!_running) return;
        GameInput.VEndFrame();
        NoteNavGrid();
        Perception?.Tick(Time.unscaledDeltaTime);   // 시야에 들어온 것만 '발견'으로 누적
        TrackEnemyAwareness();                      // 적이 나를 인식하는가(게임 쪽 증거 수집)

        // 지속 학습: Claude가 런 도중에 정책·사례를 고치면 **그 판부터** 반영된다.
        if (Brain != null)
        {
            if (Brain.PollReload(Time.unscaledDeltaTime))
                _rep?.Info("ai", "POLICY_RELOAD", $"정책/사례 갱신 반영 — {Brain.TrustSummary()}");

            _brainFlush += Time.unscaledDeltaTime;
            if (_brainFlush >= 10f)   // 끝나야 보이면 실시간 교정을 못 한다
            {
                _brainFlush = 0f;
                Brain.Flush(F("qa-decisions.jsonl"), F("qa-ask.json"), F("qa-learn.jsonl"));
            }
        }
        SampleSpace();

        // 안전망 — 일시정지가 열리면 timeScale=0이라 게임이 통째로 멈춘다.
        // 원인이 무엇이든(ESC 오사용·다른 경로) 런이 죽지 않게 봇이 직접 닫는다.
        _pauseTimer += Time.unscaledDeltaTime;
        if (_pauseTimer >= 0.5f)
        {
            _pauseTimer = 0f;
            if (PauseMenu.Instance != null && PauseMenu.Instance.IsShowing)
            {
                _pauseSeen++;
                if (_pauseSeen <= 3)
                    _rep?.Warn(_step, "PAUSE_OPENED",
                        $"일시정지가 열려 진행이 멈춤 — 봇이 닫는다 ({_pauseSeen}회째)");
                GameInput.VTapKey(KeyCode.Escape);
            }
        }

        // 주기적 하트비트 — 스텝 전환 때만 쓰면 긴 스텝(explore 150초) 동안 밖에서 생사 확인이 안 된다.
        _hbTimer += Time.unscaledDeltaTime;
        if (_hbTimer >= 1f) { _hbTimer = 0f; WriteStatus("running", $"사이클 {_cycle} · {_step}"); }
    }

    /// <summary>씬마다 길찾기 격자 상태를 1회 기록.
    ///
    /// 봇이 A* 방향을 받고도 제자리면 원인이 셋인데, 이 숫자 하나로 갈린다.
    ///   • 격자 없음      → 적·NPC·봇 전부 직선 이동(NavAgent가 폴백)
    ///   • 막힘 0%        → 장애물을 못 잡음 = 베이크 시점 문제
    ///   • 막힘 과다      → agentRadius 팽창이 통로를 삼킴
    ///   • 막힘 정상인데 제자리 → 격자가 아니라 **스폰/콜라이더 문제**
    /// 사람이 콘솔을 뒤지게 하지 않으려고 결과 JSON에 싣는다.</summary>
    void NoteNavGrid()
    {
        string sc = Scene;
        if (string.IsNullOrEmpty(sc) || sc == "Systems") return;
        if (!_navSeen.Add(sc)) return;

        var g = NavGrid.Instance;
        var j = new QaBridge.NavGridJson { scene = sc, present = g != null && g.Ready };

        if (!j.present)
        {
            _rep?.Warn("nav", "NO_NAVGRID",
                $"[{sc}] 길찾기 격자 없음 — 적·NPC·봇이 전부 직선 이동만 한다");
        }
        else
        {
            j.width = g.Width; j.height = g.Height; j.cellSize = g.CellSize;
            j.blockedPct = g.BlockedRatio * 100f;
            _rep?.Info("nav", "NAVGRID",
                $"[{sc}] 격자 {g.Width}×{g.Height} · 셀 {g.CellSize:0.00}m · 막힘 {j.blockedPct:0.#}%");

            if (j.blockedPct <= 0f)
                _rep?.Warn("nav", "NAVGRID_EMPTY",
                    $"[{sc}] 막힘 0% — 장애물을 하나도 못 잡음(베이크 시점 의심). 길찾기가 사실상 직선");
            else if (j.blockedPct >= 70f)
                _rep?.Warn("nav", "NAVGRID_DENSE",
                    $"[{sc}] 막힘 {j.blockedPct:0.#}% — 과다 팽창으로 통로가 막혔을 수 있음"
                    + $"(agentRadius / 셀 {g.CellSize:0.00}m 확인)");
        }
        _navGrids.Add(j);
    }

    /// <summary>주기적 위치 샘플 — 체류/이동/사망을 셀에 누적(안전가옥은 제외, 레이드 맵만).</summary>
    void SampleSpace()
    {
        if (_heat == null || TopDownPlayer.Instance == null) return;
        string sc = Scene;
        if (sc == "Safehouse" || sc == "Hideout" || sc == "Systems") return;

        _sampleTimer += Time.unscaledDeltaTime;
        if (_sampleTimer < 0.25f) return;
        _heat.Sample(sc, PlayerPos, _sampleTimer, _cycle);
        _sampleTimer = 0f;

        // 사망 지점 = 난이도 스파이크 후보
        var hp = TopDownPlayer.Instance.GetComponent<Health>();
        bool dead = hp != null && hp.IsDead;
        if (dead && !_wasDead)
        {
            _heat.AddDeath(sc, PlayerPos, _cycle);
            if (_tele?.Current != null) _tele.Current.deaths++;
            _rep?.Warn(_step, "DEATH", $"사망 — ({PlayerPos.x:0}, {PlayerPos.y:0})");
        }
        _wasDead = dead;
    }

    // ══════════════════════════════════════════════════════════════
    //  명령 대기 모드 — Claude ↔ 게임
    // ══════════════════════════════════════════════════════════════

    string F(string name) => System.IO.Path.Combine(Application.persistentDataPath,
        string.IsNullOrEmpty(_instance) ? name : $"{_instance}-{name}");

    [System.Serializable]
    class CommandJson
    {
        public string id;               // 결과 파일 이름에 쓰임
        public string note;             // Claude가 남기는 의도(리포트에 실림)
        public QaScenarioDef scenario;  // 실행할 시퀀스
    }

    /// <summary>qa-command.json이 들어오면 실행하고 결과를 쓴다. 무한 대기(인스턴스 여러 개 가능).</summary>
    IEnumerator ServeLoop()
    {
        string cmdPath = F("qa-command.json");
        // 주의: 안내 문구용 패턴('<id>')을 F()에 넣으면 안 된다 — '<', '>'는 윈도우에서
        // 경로에 못 쓰는 문자라 Path.Combine이 ArgumentException을 던진다(2026-07-28).
        string prefix = string.IsNullOrEmpty(_instance) ? "" : _instance + "-";
        Debug.Log($"[QA] 명령 대기 모드 시작 (instance='{_instance}')\n"
                  + $"  명령 투입: {cmdPath}\n"
                  + $"  결과 출력: {Application.persistentDataPath}/{prefix}qa-result-<id>.json");
        WriteStatus("idle", "명령 대기 중");

        while (true)
        {
            if (!_running && System.IO.File.Exists(cmdPath))
            {
                CommandJson cmd = null;
                try { cmd = JsonUtility.FromJson<CommandJson>(System.IO.File.ReadAllText(cmdPath)); }
                catch (System.Exception e) { Debug.LogError($"[QA] 명령 파싱 실패: {e.Message}"); }

                try { System.IO.File.Delete(cmdPath); } catch { }

                if (cmd != null)
                {
                    _commandId = string.IsNullOrEmpty(cmd.id) ? System.DateTime.Now.ToString("HHmmss") : cmd.id;
                    _scenario = (cmd.scenario != null && cmd.scenario.steps != null && cmd.scenario.steps.Length > 0)
                                ? cmd.scenario : QaScenarioDef.Load();
                    Debug.Log($"[QA] 명령 수신 id={_commandId} — {cmd.note}");
                    StartRun(false);
                    while (_running) yield return null;      // 끝날 때까지 대기
                    WriteStatus("idle", "명령 대기 중");
                }
            }
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    void WriteStatus(string state, string detail)
    {
        try
        {
            var s = new StatusJson
            {
                instance = _instance,
                state = state,
                step = _step,
                cycle = _cycle,
                detail = detail,
                scene = SceneManager.GetActiveScene().name,
                money = QaTelemetry.Money(),
                level = QaTelemetry.Level(),
                elapsedSec = _running ? Time.realtimeSinceStartup - _startedAt : 0f,
                errors = _rep != null ? _rep.ErrorCount : 0,
                warns = _rep != null ? _rep.WarnCount : 0,
                updatedAt = System.DateTime.Now.ToString("HH:mm:ss"),
                recent = RecentIssues(3),
                playerX = PlayerPos.x, playerY = PlayerPos.y,
            };
            System.IO.File.WriteAllText(F("qa-status.json"), JsonUtility.ToJson(s, true));
        }
        catch { }
    }

    [System.Serializable]
    class StatusJson
    {
        public string instance, state, step, detail, scene, updatedAt;
        public int cycle, money, level, errors, warns;
        public float elapsedSec;
        /// <summary>최근 이상 3건 — 런이 끝나기 전에도 밖에서 진단할 수 있게(리포트를 기다리지 않아도 됨).</summary>
        public string[] recent;
        public float playerX, playerY;
    }

    /// <summary>리포트의 최근 Warn/Error 몇 건을 문자열로 — 상태 파일에 실어 실시간 진단용.</summary>
    string[] RecentIssues(int n)
    {
        if (_rep == null) return new string[0];
        var list = new List<string>();
        var es = _rep.Entries;
        for (int i = es.Count - 1; i >= 0 && list.Count < n; i--)
        {
            if (es[i].level == QaReport.Level.Info) continue;
            list.Add($"[{es[i].level}] {es[i].step}/{es[i].kind}: {es[i].msg}");
        }
        return list.ToArray();
    }

    // ══════════════════════════════════════════════════════════════
    //  런 제어
    // ══════════════════════════════════════════════════════════════

    public void StartRun(bool quitWhenDone)
    {
        if (_running) return;
        _running = true;
        _analyzed = false;
        _quitWhenDone = quitWhenDone || _quitWhenDone;
        _startedAt = Time.realtimeSinceStartup;
        _cycle = 0;
        _stuckReported = 0;
        _oscReported = 0;
        _pauseTimer = 0f;
        _pauseSeen = 0;
        _recoverLevel = 0;
        _restartStep = false;
        _cycleRestarts = 0;
        _navSeen.Clear();
        _navGrids.Clear();

        _rep = new QaReport();
        _tele = new QaTelemetry();
        _heat = new QaHeatmap();

        Perception = new QaPerception();
        Brain = new QaBrain();
        Brain.Load(F("qa-policy.json"), F("qa-cases.json"));
        if (_scenario == null) _scenario = QaScenarioDef.Load();
        int seed = _scenario.seed != 0 ? _scenario.seed : System.Environment.TickCount;
        _rng = new System.Random(seed);
        _ctx = new QaContext { Bot = this, Report = _rep, Tele = _tele, Rng = _rng, Scenario = _scenario };

        Application.logMessageReceived += OnLog;
        GameInput.Virtual = true;
        // 봇이 준 테스트 아이템·비운 상자·XP가 플레이어의 진짜 세이브를 덮어쓰지 않게.
        SaveManager.SuppressWrites = true;

        _rep.Info("boot", "START", $"시나리오 '{_scenario.name}' · 시드 {seed} · {_scenario.cycles}사이클 · 상한 {_sessionMinutes}분 · 세이브 쓰기 차단");
        StartCoroutine(RunScenario());
    }

    void Abort(string why)
    {
        if (!_running) return;
        _rep?.Warn(_step, "ABORT", why);
        Finish();
    }

    IEnumerator RunScenario()
    {
        yield return WaitSec(1.5f);
        float deadline = _startedAt + _sessionMinutes * 60f;

        for (int cy = 1; cy <= Mathf.Max(1, _scenario.cycles); cy++)
        {
            _cycle = cy;
            _ctx.Cycle = cy;
            if (Time.realtimeSinceStartup > deadline)
            { _rep.Warn("session", "TIME_CAP", $"세션 상한 {_sessionMinutes}분 도달 — {cy - 1}사이클에서 중단"); break; }

            _recoverLevel = 0;   // 복구 예산은 사이클마다 새로

            // 인덱스 루프 — 복구 2단계에서 이 사이클을 스텝0부터 다시 돌려야 한다
            var steps = new List<QaStepDef>(_scenario.steps);
            for (int si = 0; si < steps.Count; si++)
            {
                var stepDef = steps[si];
                if (!_running) yield break;
                if (Time.realtimeSinceStartup > deadline)
                { _rep.Warn("session", "TIME_CAP", $"세션 상한 도달 — 사이클 {cy} 중도 종료"); break; }

                if (stepDef == null || string.IsNullOrEmpty(stepDef.op)) continue;
                if (!QaSteps.Has(stepDef.op))
                { _rep.Error("scenario", "UNKNOWN_OP", $"모르는 op '{stepDef.op}' — 오타이거나 미구현"); continue; }

                _step = stepDef.op;
                _stuckTimer = 0f;   // 스텝마다 스턱 카운터 초기화(리포트 조기 소진 방지)
                WriteStatus("running", $"사이클 {cy} · {stepDef.op}");
                yield return RunStepSafely(stepDef);

                if (_restartStep)
                {
                    _restartStep = false;
                    if (_cycleRestarts < 2)
                    {
                        _cycleRestarts++;
                        _rep.Warn("recover", "CYCLE_RESTART",
                            $"사이클 {cy}를 처음부터 다시 (재시작 {_cycleRestarts}/2)");
                        si = -1;   // 다음 증가로 0
                    }
                    else
                    {
                        _rep.Error("recover", "RESTART_CAP",
                            "사이클 재시작 상한(2회) 도달 — 다음 사이클로 넘어간다");
                        break;
                    }
                }
            }
        }

        _rep.Info("done", "END", "시나리오 완료");
        Finish();   // 사이클 마감·분석은 Finish가 한다(중단 경로도 같은 처리를 받게)
    }

    /// <summary>스텝을 예외 안전하게 실행 — 한 스텝이 터져도 런 전체가 죽지 않는다.
    /// (예외로 코루틴이 끊기면 Virtual=true가 고착돼 실제 입력이 먹통이 되고 리포트도 안 남는다.)
    /// yield는 try 밖에 있어야 하므로 열거자를 수동으로 돌린다.</summary>
    IEnumerator RunStepSafely(QaStepDef def)
    {
        IEnumerator inner;
        try { inner = QaSteps.Ops[def.op](def, _ctx); }
        catch (System.Exception e) { _rep.Error(def.op, "STEP_EXCEPTION", Trim(e.Message)); yield break; }

        while (true)
        {
            object cur;
            try
            {
                if (!inner.MoveNext()) yield break;
                cur = inner.Current;
            }
            catch (System.Exception e)
            {
                _rep.Error(def.op, "STEP_EXCEPTION", Trim(e.Message) + " | " + Trim(e.StackTrace, 160));
                yield break;
            }
            yield return cur;
        }
    }

    void Finish()
    {
        StopCoroutine(nameof(RunScenario));
        _running = false;
        GameInput.Virtual = false;
        SaveManager.SuppressWrites = false;
        DisposeNav();
        Application.logMessageReceived -= OnLog;

        // ★ 분석은 여기서 한다. 예전엔 정상 종료 경로에만 있어서, 중단된 런은
        //   스턱 핫스팟·길막힘 같은 **가장 중요한 신호가 통째로 빠진 채** 판정됐다
        //   (실제로 한 칸에서 스턱 8회인 런이 '진행 막힘 없음 PASS'로 나왔다).
        if (_rep != null && !_analyzed)
        {
            _analyzed = true;
            if (_tele.Current != null) _tele.EndCycle();   // 열린 채 중단된 사이클 마감
            _tele.Analyze(_rep);
            _heat?.Analyze(_rep);

            // 적 인식 — 사용자 관찰("적이 나를 인식 못 한다")을 수치로 남긴다.
            if (_foeSeenClose > 0)
            {
                _rep.Info("enemy", "AWARENESS", AwarenessSummary());
                _rep.Metric($"적 인식률(%)", _foeNoticedCount * 100f / _foeSeenClose);
                _rep.Metric("근접 노출 적 수", _foeSeenClose);
                if (_foeNoticedCount == 0)
                    _rep.Error("enemy", "NO_AWARENESS",
                        $"적 {_foeSeenClose}기가 {ObserveRadius:0}m 안에 들어왔는데 **한 기도 인식하지 못했다** — "
                        + "감지(detectRange·시야·소음) 배선 확인 필요");
                else if (_foeNoticedCount * 2 < _foeSeenClose)
                    _rep.Warn("enemy", "LOW_AWARENESS",
                        $"근접한 적의 절반 이상이 반응하지 않는다 — {AwarenessSummary()}");
            }

            // AI 결정 기록 — Claude가 읽고 정책·사례를 교정한다(이게 '학습'의 입력).
            if (Brain != null && Brain.DecisionCount > 0)
            {
                Brain.Flush(F("qa-decisions.jsonl"), F("qa-ask.json"), F("qa-learn.jsonl"));
                _rep.Info("ai", "DECISIONS",
                    $"결정 {Brain.DecisionCount}건 · 애매 신고 {Brain.AskCount}건 — {Brain.Summary()}");
                _rep.Info("ai", "TRUST", $"런 중 학습 결과 — {Brain.TrustSummary()}");
                if (Brain.AskCount > 0)
                    _rep.Warn("ai", "NEEDS_TEACHING",
                        $"{Brain.AskCount}건은 AI가 판단을 못 했다 — qa-ask.json 참고해 qa-cases.json에 정답을 추가할 것");
            }
        }

        if (_rep != null)
        {
            string body = _rep.Build($"QA 자동 플레이 — {_scenario?.name}") + "\n" + _tele.BuildTable()
                          + "\n" + (_heat != null ? _heat.BuildReport() : "");
            string stamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
            try { System.IO.File.WriteAllText(F($"qa-report-{stamp}.txt"), body); } catch { }
            Debug.Log(body);

            WriteResultJson();
        }
        // 막힘 대기 중 종료되면 QaBridge가 요청 파일을 지울 기회를 못 얻는다.
        // 남겨두면 마더가 죽은 차일드를 영원히 '응답 대기'로 본다.
        try
        {
            string bp = QaBridge.PathOf(QaBridge.BlockedFile);
            if (System.IO.File.Exists(bp)) System.IO.File.Delete(bp);
        }
        catch { }

        WriteStatus("done", $"오류 {_rep?.ErrorCount ?? 0} · 경고 {_rep?.WarnCount ?? 0}");

        if (_quitWhenDone)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit((_rep != null && _rep.ErrorCount > 0) ? 1 : 0);
#endif
        }
    }

    /// <summary>Claude가 읽는 기계 판독용 결과.</summary>
    void WriteResultJson()
    {
        var s = new QaBridge.SessionJson
        {
            instance = _instance,
            commandId = _commandId,
            scenario = _scenario?.name,
            seed = _scenario?.seed ?? 0,
            cyclesPlanned = _scenario?.cycles ?? 0,
            cyclesCompleted = _tele.Cycles.Count,
            durationSec = Time.realtimeSinceStartup - _startedAt,
            unityVersion = Application.unityVersion,
            platform = Application.platform.ToString(),
            isEditor = Application.isEditor,
            // 마더가 (차일드,시작시각)으로 런을 식별한다 — 비면 서로 다른 런이 한 건으로 합쳐진다.
            startedAt = System.DateTime.Now.AddSeconds(-(Time.realtimeSinceStartup - _startedAt))
                                           .ToString("yyyy-MM-dd HH:mm:ss"),
            endedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            errorCount = _rep.ErrorCount,
            warnCount = _rep.WarnCount,
            cycles = _tele.Snapshot(),
            cells = _heat != null ? _heat.Export() : new List<QaHeatmap.CellJson>(),
            navGrids = _navGrids,
        };
        foreach (var kv in _rep.Metrics)
            s.metrics.Add(new QaBridge.MetricJson { key = kv.Key, value = kv.Value });
        foreach (var e in _rep.Entries)
        {
            if (e.level == QaReport.Level.Info) continue;   // 이상만 싣는다
            s.anomalies.Add(new QaBridge.AnomalyJson
            {
                level = e.level.ToString(), step = e.step, kind = e.kind, msg = e.msg,
                atSec = e.time, scene = SceneManager.GetActiveScene().name,
                playerPos = TopDownPlayer.Instance != null ? TopDownPlayer.Instance.transform.position.ToString("0.#") : "-",
            });
        }

        BuildVerdict(s);

        try
        {
            string json = JsonUtility.ToJson(s, true);
            // latest는 매 런 덮어써진다. F9 수동 런(commandId 없음)도 타임스탬프본을 남겨야
            // 이력이 보존되고 마더의 런 목록에 쌓인다.
            string id = string.IsNullOrEmpty(_commandId)
                ? System.DateTime.Now.ToString("yyyyMMdd-HHmmss") : _commandId;
            string name = $"qa-result-{id}.json";
            System.IO.File.WriteAllText(F(name), json);
            System.IO.File.WriteAllText(F("qa-result-latest.json"), json);
            Debug.Log($"[QA] 결과 JSON: {F(name)}");
        }
        catch (System.Exception e) { Debug.LogError($"[QA] 결과 저장 실패: {e.Message}"); }
    }

    /// <summary>항목별 통과/미통과 판정 — GUI 통계의 단위. "이번 런이 뭘 증명했나"를 명시적으로 남긴다.
    /// (이상 로그만 있으면 사람이 매번 해석해야 한다. 체크로 못박아야 통계가 쌓인다.)</summary>
    void BuildVerdict(QaBridge.SessionJson s)
    {
        void Check(string name, bool ok, string detail)
            => s.checks.Add(new QaBridge.CheckJson { name = name, passed = ok, detail = detail });

        int kind(string k) { int n = 0; foreach (var a in s.anomalies) if (a.kind == k) n++; return n; }

        // 루프가 실제로 도는가
        int completedRaids = 0;
        foreach (var c in s.cycles) if (c.raidCompleted) completedRaids++;
        Check("사이클 완주", s.cyclesCompleted >= s.cyclesPlanned,
              $"{s.cyclesCompleted}/{s.cyclesPlanned} 사이클");
        Check("레이드 탈출", completedRaids > 0 && completedRaids == s.cyclesCompleted,
              $"{completedRaids}/{s.cyclesCompleted} 회 탈출 성공");

        // 루팅이 실제로 나오는가
        int lootTotal = 0; foreach (var c in s.cycles) lootTotal += c.lootValue;
        Check("루팅 획득", lootTotal > 0, $"총 루팅가치 {lootTotal}");

        // 치명 이상이 없는가
        Check("예외 없음", kind("EXCEPTION") == 0 && kind("LOG_ERROR") == 0,
              $"예외 {kind("EXCEPTION")} · 에러로그 {kind("LOG_ERROR")}");
        Check("아이템 유실 없음", kind("ITEM_LOST") == 0, $"{kind("ITEM_LOST")}건");
        Check("씬 전환 정상", kind("SCENE_TIMEOUT") == 0 && kind("EXTRACT_TIMEOUT") == 0,
              $"전환실패 {kind("SCENE_TIMEOUT") + kind("EXTRACT_TIMEOUT")}건");
        Check("진행 막힘 없음", kind("STUCK_HOTSPOT") == 0 && kind("BLOCKED_HOTSPOT") == 0 && kind("NO_EXIT") == 0,
              $"스턱핫스팟 {kind("STUCK_HOTSPOT")} · 길막힘 {kind("BLOCKED_HOTSPOT")} · 탈출구없음 {kind("NO_EXIT")}");
        Check("UI 잠김 없음", kind("UI_STUCK") == 0, $"{kind("UI_STUCK")}건");

        int failed = 0; var reasons = new List<string>();
        foreach (var c in s.checks) if (!c.passed) { failed++; reasons.Add($"{c.name}({c.detail})"); }

        s.verdict = failed == 0 ? "PASS" : "FAIL";
        s.verdictReason = failed == 0 ? "전 항목 통과" : string.Join(" / ", reasons);

        _rep.Info("verdict", s.verdict, s.verdictReason);
        Debug.Log($"[QA] ===== {s.verdict} ===== {s.verdictReason}");
    }

    void OnLog(string condition, string stack, LogType type)
    {
        if (_rep == null) return;
        if (type == LogType.Exception)
            _rep.Error(_step, "EXCEPTION", Trim(condition) + " | " + Trim(stack, 200));
        else if (type == LogType.Error || type == LogType.Assert)
        {
            if (condition != null && condition.StartsWith("[QA")) return;
            _rep.Error(_step, "LOG_ERROR", Trim(condition));
        }
    }

    static string Trim(string s, int n = 160)
        => string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s.Replace('\n', ' ') : s.Substring(0, n).Replace('\n', ' ') + "…");

    // ══════════════════════════════════════════════════════════════
    //  스텝이 쓰는 조작/유틸 (public)
    // ══════════════════════════════════════════════════════════════

    public void Tap(KeyCode key) => GameInput.VTapKey(key);

    /// <summary>이동 입력 — **키보드처럼 8방향으로 양자화**해서 넣는다.
    ///
    /// 사람은 WASD라 축 값이 -1/0/1뿐이고, 그래서 이동이 8방향으로 꺾인다.
    /// 봇이 임의 각도 벡터를 넣으면 사람이 절대 안 하는 매끄러운 사선 활강이 나온다
    /// (2026-07-28 사용자 지적: "사람이면 안 할 대각이동"). 보기에도 이상하고,
    /// 실제 플레이와 다른 이동이라 맵 밸런스(모서리 끼임·통로 폭) 검증이 틀어진다.
    /// 경계 0.383 = sin(22.5°) — 8분면 경계.</summary>
    public static void Move8(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) { GameInput.VSetMove(Vector2.zero); return; }
        dir.Normalize();
        float x = Mathf.Abs(dir.x) < 0.383f ? 0f : Mathf.Sign(dir.x);
        float y = Mathf.Abs(dir.y) < 0.383f ? 0f : Mathf.Sign(dir.y);
        if (x == 0f && y == 0f) { x = Mathf.Sign(dir.x); }   // 경계에 걸리면 한 축은 살린다
        GameInput.VSetMove(new Vector2(x, y).normalized);
    }

    /// <summary>UI를 **열려 있을 때만** ESC로 닫는다.
    ///
    /// 닫을 게 없는데 ESC를 누르면 UIManager가 일시정지 메뉴를 연다(UIManager.Update).
    /// 2026-07-28 QA: 루팅 후 무조건 ESC를 눌러 일시정지가 계속 떴다 —
    /// 상태를 안 보고 키를 누른 것이라 봇의 잘못이다. 반드시 이 함수를 쓸 것.</summary>
    public IEnumerator CloseUi(string step)
    {
        var ui = UIManager.Instance;
        if (ui == null) yield break;

        for (int i = 0; i < 5 && ui.IsAnyUIOpen(); i++)
        {
            GameInput.VTapKey(KeyCode.Escape);
            yield return WaitSec(0.25f);
        }
        if (ui.IsAnyUIOpen())
            _rep?.Warn(step, "UI_STUCK", "ESC 5회에도 UI가 안 닫힘 — 닫기 배선 확인");
    }

    public IEnumerator WaitSec(float sec)
    {
        float t = 0f;
        while (t < sec) { t += Time.unscaledDeltaTime; yield return null; }
    }

    public IEnumerator WaitUntil(System.Func<bool> cond, float timeout, System.Action<bool> onDone)
    {
        float t = 0f;
        while (t < timeout)
        {
            if (cond()) { onDone?.Invoke(true); yield break; }
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        onDone?.Invoke(false);
    }

    /// <summary>막힘 — Claude에게 물어보고 응답까지 대기(무응답이면 타임아웃 후 계속).</summary>
    public IEnumerator Blocked(string step, string kind, string msg, string tried)
    {
        _rep.Error(step, kind, msg);

        // ★ 묻기 전에 스스로 복구한다. 사람이 안 붙어 있어도 런이 끝까지 굴러가야
        //   "어디서 막히나"가 아니라 "막히고 나서 어디까지 가나"까지 데이터가 남는다.
        //   1단계: 세이브를 다시 불러 이어서 / 2단계: 사이클을 처음부터.
        if (_recoverLevel < 2)
        {
            _recoverLevel++;
            bool restart = (_recoverLevel == 2);
            _rep.Warn(step, restart ? "RECOVER_RESTART" : "RECOVER_LOAD",
                $"{kind} — 세이브 로드 후 {(restart ? "사이클을 처음부터" : "이어서 재시도")} (복구 {_recoverLevel}/2)");
            yield return ReloadFromSave();
            if (restart) _restartStep = true;
            yield break;                      // 복구했으니 마더에게 묻지 않는다
        }

        // 두 번 복구해도 안 되면 그때 사람/마더에게 묻는다
        var req = new QaBridge.BlockedJson
        {
            kind = kind, step = step, message = msg, tried = tried,
            scene = SceneManager.GetActiveScene().name,
            playerPos = TopDownPlayer.Instance != null ? TopDownPlayer.Instance.transform.position.ToString("0.#") : "-",
            cycle = _cycle, scenario = _scenario?.name, seed = _scenario?.seed ?? 0,
        };
        WriteStatus("blocked", $"{kind} — {msg}");
        yield return QaBridge.RequestHelp(req, 90f, ans =>
        {
            _rep.Info(step, "RESUME", $"action={ans.action} note={ans.note}");
            if (ans.action == "abort") Abort("Claude 지시: abort");
        });
    }

    // ── 이동 ─────────────────────────────────────────────────────────

    /// <summary>목표까지 가상 입력으로 걸어간다. 스턱·NaN도 여기서 감지.</summary>
    /// <summary>목표까지 이동. <paramref name="abortIf"/>가 참이 되면 즉시 멈추고 실패로 끝낸다.
    ///
    /// 왜 필요한가: 탐색 목적지를 멀리 잡으면서 한 번 이동에 최대 45초가 걸리는데, 그동안
    /// **아무 판단도 안 해서 적을 그냥 지나쳤다**(2026-07-28 사용자 관찰). 사람은 걷다가
    /// 적이 보이면 멈춘다 — 긴 이동일수록 중간에 끊을 수 있어야 한다.</summary>
    public IEnumerator MoveTo(Vector3 target, float arriveDist, float timeout, System.Action<bool> onDone,
                              System.Func<bool> abortIf = null)
    {
        var player = TopDownPlayer.Instance;
        if (player == null) { onDone?.Invoke(false); yield break; }

        float t = 0f, checkTimer = 0f;
        Vector3 lastCheck = player.transform.position;

        // 와리가리 감지 — 위치가 계속 바뀌므로 STUCK(정지)으로는 절대 안 잡힌다.
        // "많이 움직였는데 제자리"면 경로가 좌우로 진동하는 것.
        float oscTimer = 0f, oscPath = 0f;
        Vector3 oscAnchor = lastCheck, prevPos = lastCheck;

        // 경로 없음 지속 시간 — A*가 길을 못 찾는데도 계속 밀면 벽에 박힌 채 타임아웃까지 낭비한다.
        // (2026-07-28 화면 확인: 2.6m 옆 목표인데 사이에 벽 → NavAgent 직선 폴백 → 12초 내내 벽 밀기)
        float noPathTimer = 0f;

        while (t < timeout)
        {
            if (player == null) break;
            Vector3 pos = player.transform.position;

            if (float.IsNaN(pos.x) || float.IsNaN(pos.y))
            { _rep.Error(_step, "NAN_POS", "플레이어 좌표 NaN"); GameInput.VSetMove(Vector2.zero); onDone?.Invoke(false); yield break; }

            Vector2 to = target - pos;
            if (to.magnitude <= arriveDist) { GameInput.VSetMove(Vector2.zero); onDone?.Invoke(true); yield break; }

            // 중간에 판단이 바뀔 사유가 생기면(적 등장 등) 멈춘다 — 긴 이동을 끝까지 밀지 않는다.
            if (abortIf != null && abortIf())
            {
                GameInput.VSetMove(Vector2.zero);
                _rep?.Info(_step, "MOVE_ABORT", $"이동 중단 — 상황 변화 감지 (남은 거리 {to.magnitude:0.#}m)");
                onDone?.Invoke(false);
                yield break;
            }

            // ── 길찾기(A*) — 게임의 NavGrid/NavAgent를 그대로 쓴다(적 AI와 같은 경로 로직).
            //   직선으로만 밀면 벽 하나에 막혀 STUCK이 뜨고, 그게 맵 문제인지 봇 한계인지 구분이 안 된다.
            Vector2 dir;
            var nav = EnsureNav(player);
            bool pathing = false;
            if (nav != null && NavGrid.Instance != null && NavGrid.Instance.Ready)
            {
                nav.SetDestination(target);

                // 격자가 준비됐는데 경로가 안 나온다 = **갈 수 없는 목표**다.
                // 이때 직선으로 밀면 벽에 박히기만 한다 — 빨리 포기하고 다른 목표를 고르는 게 맞다.
                if (!nav.HasPath && to.magnitude > 2.0f)
                {
                    noPathTimer += Time.unscaledDeltaTime;
                    if (noPathTimer >= 1.5f)
                    {
                        _rep.Warn(_step, "NO_PATH",
                            $"경로 없음 — 위치({pos.x:0.#},{pos.y:0.#}) 목표({target.x:0.#},{target.y:0.#}) "
                            + $"직선 {to.magnitude:0.#}m. 벽 너머이거나 격자가 끊겼다");
                        GameInput.VSetMove(Vector2.zero);
                        _heat?.AddUnreachable(Scene, pos, _cycle);
                        onDone?.Invoke(false);
                        yield break;
                    }
                }
                else noPathTimer = 0f;
                Vector2 nd = nav.DesiredDirection;
                if (nd.sqrMagnitude > 0.0001f) { dir = nd; pathing = true; }
                else dir = to.normalized;
            }
            else dir = to.normalized;   // NavGrid 없는 씬 → 직선 폴백

            _pathingNow = pathing;
            GameInput.VSetMove(dir);
            var cam = Camera.main;
            if (cam != null) GameInput.VSetMousePos(cam.WorldToScreenPoint(pos + (Vector3)dir * 3f));

            checkTimer += Time.unscaledDeltaTime;
            if (checkTimer >= 1f)
            {
                if (Vector2.Distance(pos, lastCheck) < 0.15f)
                {
                    _stuckTimer += checkTimer;
                    if (_stuckTimer >= 3f && _stuckReported < 8)
                    {
                        _stuckReported++;
                        // 길찾기가 돌고 있었는지 함께 남긴다 — 아니면 "봇이 직선으로만 밀어서"일 수 있어 신뢰도가 다르다.
                        string how = _pathingNow ? "A*경로 추종 중" : "직선이동(길찾기 없음 — 봇 한계 가능)";
                        _rep.Warn(_step, "STUCK", $"이동 입력에도 3초 정지 [{how}] — 위치({pos.x:0.#},{pos.y:0.#}) 목표({target.x:0.#},{target.y:0.#})");
                        _heat?.AddStuck(Scene, pos, _cycle);
                        _stuckTimer = 0f;
                        // 자유도: 옆으로 빠져나가기 시도(벽 끼임 탈출)
                        yield return Nudge(dir);
                    }
                }
                else _stuckTimer = 0f;
                lastCheck = pos; checkTimer = 0f;
            }

            // ── 와리가리 판정 (6초 창) ─────────────────────────────
            oscPath += Vector2.Distance(pos, prevPos);
            prevPos = pos;
            oscTimer += Time.unscaledDeltaTime;
            if (oscTimer >= 6f)
            {
                float net = Vector2.Distance(pos, oscAnchor);
                if (oscPath >= 8f && net < 2f && _oscReported < 6)
                {
                    _oscReported++;
                    _rep.Warn(_step, "OSCILLATION",
                        $"6초간 {oscPath:0.#}m 이동했는데 순이동 {net:0.#}m — 제자리 왕복(와리가리). "
                        + $"위치({pos.x:0.#},{pos.y:0.#}) 목표({target.x:0.#},{target.y:0.#}) "
                        + $"[{(_pathingNow ? "A*경로" : "직선")}]");
                    _heat?.AddStuck(Scene, pos, _cycle);
                    if (nav != null) nav.Stop();      // 경로 버리고 다시 잡게
                    yield return Nudge(dir);
                }
                oscTimer = 0f; oscPath = 0f; oscAnchor = pos;
            }

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        GameInput.VSetMove(Vector2.zero);
        onDone?.Invoke(false);
    }

    /// <summary>세이브를 다시 불러 알려진 정상 상태(안전가옥)로 되돌린다.
    /// 레이드 중이었다면 그 레이드는 버린다 — 사람이 막혔을 때 하는 것과 같다.</summary>
    IEnumerator ReloadFromSave()
    {
        GameInput.VSetMove(Vector2.zero);
        GameInput.Virtual = false;   // 눌린 가상 키 잔재 정리(setter가 전부 해제)
        GameInput.Virtual = true;
        _stuckTimer = 0f;

        if (TitleScreen.IsShowing)
        {
            if (!TitleScreen.ContinueGame(0)) TitleScreen.StartNewGame(0);
        }
        else if (SaveManager.Instance != null)
        {
            if (!SaveManager.Instance.Load())
                _rep.Warn(_step, "RECOVER_NOSAVE", "세이브 로드 실패 — 현재 상태로 계속");
        }

        yield return new WaitForSecondsRealtime(2f);   // 씬 전환·초기화 대기
        _navSeen.Clear();                              // 씬이 바뀌었으면 격자 다시 기록
        _rep.Info(_step, "RECOVER_DONE", $"복구 후 재개 — 씬 {Scene} 위치 {PlayerPos}");
    }

    // ── 적 인식 추적 (2026-07-28) ────────────────────────────────────
    //  사용자 관찰: "적도 나를 인식 못 하는 경우가 있다".
    //  그게 사실인지, 몇 미터에서 인식하는지, 아예 못 하는지를 **수치로** 남겨야
    //  게임 결함으로 보고할 근거가 된다(추측으로는 보고하지 않는다).
    //  EnemyController.CurrentState가 Patrol/Investigate → Chase/AttackWindup으로 바뀌는 순간이 '인식'이다.
    readonly Dictionary<int, EnemyController.State> _foeState = new Dictionary<int, EnemyController.State>();
    readonly HashSet<int> _foeNoticed = new HashSet<int>();
    int _foeSeenClose;          // 플레이어가 detectRange 안에 들어간 적 수(중복 없이)
    int _foeNoticedCount;       // 그중 실제로 인식(추격 전환)한 수
    float _foeTimer;

    void TrackEnemyAwareness()
    {
        _foeTimer += Time.unscaledDeltaTime;
        if (_foeTimer < 0.25f) return;
        _foeTimer = 0f;

        var player = TopDownPlayer.Instance;
        if (player == null) return;
        Vector2 me = player.transform.position;

        foreach (var e in EnemyController.All)
        {
            if (e == null || !e.gameObject.activeInHierarchy || e.IsDead) continue;

            int id = e.GetEntityId().GetHashCode();
            var now = e.CurrentState;
            float dist = Vector2.Distance(e.transform.position, me);

            // 분모 = **관측 반경 안에 들어와 본 적 수**. 게임의 detectRange는 접근자가 없어
            // 읽을 수 없으므로 추정하지 않고, 고정 관측 반경(8m)을 기준으로 세고 그렇게 표기한다.
            if (dist <= ObserveRadius && !_foeExposed.Contains(id))
            {
                _foeExposed.Add(id);
                _foeSeenClose++;
            }

            bool had = _foeState.TryGetValue(id, out var prev);
            _foeState[id] = now;
            if (!had) continue;

            bool wasIdle = prev == EnemyController.State.Patrol || prev == EnemyController.State.Investigate;
            bool nowAware = now == EnemyController.State.Chase || now == EnemyController.State.AttackWindup
                            || now == EnemyController.State.Attack;
            if (wasIdle && nowAware && _foeNoticed.Add(id))
            {
                _foeNoticedCount++;
                _foeNoticeDistSum += dist;
                _rep?.Info("enemy", "NOTICED", $"{e.name}이(가) 플레이어를 인식 — 거리 {dist:0.#}m");
            }
        }
    }

    const float ObserveRadius = 8f;                       // 인식률 분모 기준(게임 detectRange가 아님)
    readonly HashSet<int> _foeExposed = new HashSet<int>();
    float _foeNoticeDistSum;

    /// <summary>적 인식 통계 — 리포트용. 분모는 관측 반경 8m 기준(게임 detectRange 아님).</summary>
    public string AwarenessSummary()
        => $"{ObserveRadius:0}m 안에 들어간 적 {_foeSeenClose}기 중 **{_foeNoticedCount}기가 인식**"
           + (_foeSeenClose > 0 ? $" ({_foeNoticedCount * 100f / _foeSeenClose:0}%)" : "")
           + (_foeNoticedCount > 0 ? $" · 평균 인식거리 {_foeNoticeDistSum / _foeNoticedCount:0.#}m" : "");

    public int FoeSeenClose => _foeSeenClose;
    public int FoeNoticed => _foeNoticedCount;

    /// <summary>진행 방향 코앞에 적이 있으면 **옆으로 비껴간다**.
    ///
    /// <see cref="NavGrid"/>는 Player/Enemy 레이어를 장애물에서 뺀다(적끼리 서로 막히지 않게 하려는
    /// 게임 설계). 그래서 경로가 적이 선 자리를 그대로 통과하고, 봇은 몸으로 **밀고 지나간다**
    /// (2026-07-28 사용자 지적: "밀치면 안 돼"). 사람은 돌아서 간다.
    /// 게임의 길찾기는 그대로 두고 **봇 이동에서만** 국소 회피를 한다.</summary>
    Vector2 AvoidEnemies(Vector2 pos, Vector2 dir)
    {
        EnemyController near = null; float bestD = 2.0f;   // 코앞만
        foreach (var e in EnemyController.All)
        {
            if (e == null || !e.gameObject.activeInHierarchy) continue;
            var h = e.GetComponent<Health>();
            if (h != null && h.IsDead) continue;           // 시체는 밀어도 된다(루팅 대상)
            float d = Vector2.Distance(e.transform.position, pos);
            if (d < bestD) { bestD = d; near = e; }
        }
        if (near == null) return dir;

        Vector2 toE = (Vector2)near.transform.position - pos;
        if (toE.sqrMagnitude < 0.0001f) return dir;
        // 진행 방향에 있는 적만 피한다(뒤·옆의 적까지 피하면 이동이 흔들린다)
        if (Vector2.Dot(dir, toE.normalized) < 0.3f) return dir;

        Vector2 side = new Vector2(-toE.y, toE.x).normalized;      // 적을 기준으로 직각
        if (Vector2.Dot(side, dir) < 0f) side = -side;             // 가던 쪽에 가까운 옆으로
        return (dir + side * 1.6f).normalized;
    }

    // ── 길찾기 ───────────────────────────────────────────────────────
    NavAgent _nav;
    bool _navAdded;      // 봇이 붙였으면 런 종료 시 떼어낸다(흔적 남기지 않음)
    bool _pathingNow;    // 직전 프레임에 A* 경로를 따르고 있었나(STUCK 신뢰도 표기용)

    /// <summary>플레이어에 NavAgent 확보 — 적 AI와 **같은** 길찾기를 쓴다.
    /// 원래 없으면 QA가 임시로 붙이고 Finish에서 제거한다(게임 상태 오염 방지).</summary>
    NavAgent EnsureNav(TopDownPlayer player)
    {
        if (_nav != null) return _nav;
        if (player == null) return null;
        _nav = player.GetComponent<NavAgent>();
        if (_nav == null) { _nav = player.gameObject.AddComponent<NavAgent>(); _navAdded = true; }
        return _nav;
    }

    void DisposeNav()
    {
        if (_nav != null)
        {
            _nav.Stop();
            if (_navAdded) Destroy(_nav);
        }
        _nav = null; _navAdded = false; _pathingNow = false;
    }

    /// <summary>끼임 탈출 — 직각 방향으로 잠깐 이동.</summary>
    IEnumerator Nudge(Vector2 dir)
    {
        Vector2 side = new Vector2(-dir.y, dir.x);
        if (_rng != null && _rng.Next(2) == 0) side = -side;
        GameInput.VSetMove(side);
        yield return WaitSec(0.6f);
        GameInput.VSetMove(Vector2.zero);
    }

    /// <summary>무작위 배회 — 사람 같지 않아도 "여러 방면"을 훑어 구멍·스턱을 찾는다.</summary>
    public IEnumerator Wander(QaContext ctx, float seconds)
    {
        var player = TopDownPlayer.Instance;
        if (player == null) yield break;
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
        {
            float ang = ctx.Rand01() * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            GameInput.VSetMove(dir);
            var cam = Camera.main;
            if (cam != null) GameInput.VSetMousePos(cam.WorldToScreenPoint(player.transform.position + (Vector3)dir * 3f));
            yield return WaitSec(0.5f + ctx.Rand01());
        }
        GameInput.VSetMove(Vector2.zero);
    }

    // ── 루팅 ─────────────────────────────────────────────────────────

    public List<LootContainer> NearestCrates(List<LootContainer> pool, int take)
    {
        var res = new List<LootContainer>();
        var p = TopDownPlayer.Instance;
        if (p == null) return res;
        pool.RemoveAll(x => x == null);
        pool.Sort((a, b) => Vector2.Distance(a.transform.position, p.transform.position)
                     .CompareTo(Vector2.Distance(b.transform.position, p.transform.position)));
        for (int i = 0; i < pool.Count && i < take; i++) res.Add(pool[i]);
        return res;
    }

    /// <summary>상자 내용물을 전부 가방으로. 반환 = 실제로 챙긴 개수.</summary>
    public int TakeAllFrom(LootContainer c)
    {
        if (c == null || c.Grid == null || Inv == null) return 0;
        int taken = 0;
        int gained = 0;
        foreach (var it in c.Grid.GetAll())
        {
            var inst = it.item;
            c.Grid.Remove(it);
            if (Inv.TryAutoPlaceAnywhere(inst))
            {
                taken++;
                if (inst?.data != null) gained += inst.data.sellPrice * Mathf.Max(1, inst.stackCount);
            }
            else c.Grid.TryAutoPlace(inst);   // 못 넣으면 원복(유실 금지)
        }
        // 이 좌표에서 얼마를 벌었는지 = 파밍 효율 계산의 분자
        if (taken > 0) _heat?.AddLoot(Scene, PlayerPos, gained, taken, _cycle);
        return taken;
    }

    public void MarkLootBaseline() => _lootBaseline = QaTelemetry.HeldValue();
    public int LootValueSinceBaseline() => Mathf.Max(0, QaTelemetry.HeldValue() - _lootBaseline);
}
