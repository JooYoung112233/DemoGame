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

    // ── 공간 텔레메트리 ("어디서" 데이터) ───────────────────────────
    QaHeatmap _heat;
    public QaHeatmap Heat => _heat;
    public int Cycle => _cycle;
    public string Scene => SceneManager.GetActiveScene().name;
    float _sampleTimer;
    bool _wasDead;

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
        if (kb == null || !kb.f9Key.wasPressedThisFrame) return;
        if (_running) Abort("사용자 F9 중단");
        else StartRun(false);
    }

    float _hbTimer;

    void LateUpdate()
    {
        // 가상 입력의 1프레임 플래그는 모든 Update 소비자가 본 뒤 걷는다.
        if (!_running) return;
        GameInput.VEndFrame();
        SampleSpace();

        // 주기적 하트비트 — 스텝 전환 때만 쓰면 긴 스텝(explore 150초) 동안 밖에서 생사 확인이 안 된다.
        _hbTimer += Time.unscaledDeltaTime;
        if (_hbTimer >= 1f) { _hbTimer = 0f; WriteStatus("running", $"사이클 {_cycle} · {_step}"); }
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
        Debug.Log($"[QA] 명령 대기 모드 시작 (instance='{_instance}')\n  명령 투입: {cmdPath}\n  결과 출력: {F("qa-result-<id>.json")}");
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
        _quitWhenDone = quitWhenDone || _quitWhenDone;
        _startedAt = Time.realtimeSinceStartup;
        _cycle = 0;
        _stuckReported = 0;

        _rep = new QaReport();
        _tele = new QaTelemetry();
        _heat = new QaHeatmap();
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

            foreach (var stepDef in _scenario.steps)
            {
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
            }
        }

        // 사이클이 열린 채 끝났으면 닫는다(지표 유실 방지)
        if (_tele.Current != null) _tele.EndCycle();

        _tele.Analyze(_rep);
        _heat?.Analyze(_rep);
        _rep.Info("done", "END", "시나리오 완료");
        Finish();
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
        Application.logMessageReceived -= OnLog;

        if (_rep != null)
        {
            string body = _rep.Build($"QA 자동 플레이 — {_scenario?.name}") + "\n" + _tele.BuildTable()
                          + "\n" + (_heat != null ? _heat.BuildReport() : "");
            string stamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
            try { System.IO.File.WriteAllText(F($"qa-report-{stamp}.txt"), body); } catch { }
            Debug.Log(body);

            WriteResultJson();
        }
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
            endedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            errorCount = _rep.ErrorCount,
            warnCount = _rep.WarnCount,
            cycles = _tele.Snapshot(),
            cells = _heat != null ? _heat.Export() : new List<QaHeatmap.CellJson>(),
        };
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
            string name = string.IsNullOrEmpty(_commandId) ? "qa-result-latest.json" : $"qa-result-{_commandId}.json";
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
    public IEnumerator MoveTo(Vector3 target, float arriveDist, float timeout, System.Action<bool> onDone)
    {
        var player = TopDownPlayer.Instance;
        if (player == null) { onDone?.Invoke(false); yield break; }

        float t = 0f, checkTimer = 0f;
        Vector3 lastCheck = player.transform.position;

        while (t < timeout)
        {
            if (player == null) break;
            Vector3 pos = player.transform.position;

            if (float.IsNaN(pos.x) || float.IsNaN(pos.y))
            { _rep.Error(_step, "NAN_POS", "플레이어 좌표 NaN"); GameInput.VSetMove(Vector2.zero); onDone?.Invoke(false); yield break; }

            Vector2 to = target - pos;
            if (to.magnitude <= arriveDist) { GameInput.VSetMove(Vector2.zero); onDone?.Invoke(true); yield break; }

            Vector2 dir = to.normalized;
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
                        _rep.Warn(_step, "STUCK", $"이동 입력에도 3초 정지 — 위치({pos.x:0.#},{pos.y:0.#}) 목표({target.x:0.#},{target.y:0.#})");
                        _heat?.AddStuck(Scene, pos, _cycle);
                        _stuckTimer = 0f;
                        // 자유도: 옆으로 빠져나가기 시도(벽 끼임 탈출)
                        yield return Nudge(dir);
                    }
                }
                else _stuckTimer = 0f;
                lastCheck = pos; checkTimer = 0f;
            }

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        GameInput.VSetMove(Vector2.zero);
        onDone?.Invoke(false);
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
