using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 저장 체크포인트 시스템 (세이브 스커밍 방지 + 크래시 복구).
///
/// 핵심 모델:
///  • 디스크 저장(Commit)은 "안전 맥락"에서만 일어난다 — 레이드 시작/종료, 안전가옥 이벤트, 침대 수면.
///  • 레이드 중(InRaid)의 진행 이벤트는 인메모리 스냅샷(Record)만 갱신하고 디스크엔 쓰지 않는다.
///    → 죽거나 강제 종료하면 "레이드 시작 시점"으로 되돌아간다(세이브 스커밍 차단).
///  • 단, 예외(LogType.Exception)로 크래시가 나면 인메모리 스냅샷을 1회 디스크에 커밋한다(크래시 복구).
///
/// InRaid 판정: 현재 게임플레이 씬이 레이드 씬인가.
///   = SystemsScene.IsGameplayScene(scene) && 씬이 안전 허브(Safehouse/Hideout/전당포 등)가 아님.
///
/// 공유 인터페이스 계약(트랙 B 등 외부 호출): RaidStarted/RaidEnded/BuildingEntered/BuildingExited/
///   QuestAccepted/QuestCompleted/CombatStarted/CombatEnded/BedSleepSave + bool InRaid.
///
/// 설계 문서: docs/save.md
/// </summary>
public class SaveCheckpoints : MonoBehaviour
{
    public static SaveCheckpoints Instance { get; private set; }

    /// <summary>안전(비-레이드) 맥락으로 취급할 허브·건물 씬 이름.
    /// 이 씬들에서의 이벤트는 항상 Commit(디스크 즉시 저장).</summary>
    static readonly string[] SafeHubScenes =
    {
        "Safehouse",        // 안전가옥 허브
        "Hideout",          // 은신처 실내
        // ※ Zone1 등 레이드 맵은 안전 허브 아님(세이브 스커밍 대상). 전당포는 마을(Safehouse) 안에서 걸어 들어간다.
    };

    // ── 런타임 ──
    bool _inRaid;
    string _raidSnapshotJson;   // 레이드 중 인메모리 스냅샷(디스크 X)
    bool _crashCommitted;       // 크래시 복구 커밋은 1회만

    /// <summary>현재 레이드(위험 맥락) 중인가.</summary>
    public bool InRaid => _inRaid;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;
        if (Instance != null) return;
        if (FindFirstObjectByType<SaveCheckpoints>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("[SaveCheckpoints]");
        go.AddComponent<SaveCheckpoints>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.logMessageReceived += OnLogMessage;

        // 초기 상태: 현재 활성 씬 기준으로 갱신
        RefreshInRaid(SceneManager.GetActiveScene());
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.logMessageReceived -= OnLogMessage;
        if (Instance == this) Instance = null;
    }

    // ════════════════════════════════════════
    //  씬 기반 InRaid 갱신
    // ════════════════════════════════════════

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Systems/맵툴 등 비-콘텐츠 씬 로드는 무시(게임플레이 씬만 맥락 결정)
        if (!SystemsScene.IsGameplayScene(scene)) return;
        RefreshInRaid(scene);
    }

    void RefreshInRaid(Scene scene)
    {
        _inRaid = IsRaidScene(scene);
    }

    /// <summary>레이드(위험) 씬인가 — 게임플레이 씬이며 안전 허브 목록에 없음.</summary>
    static bool IsRaidScene(Scene scene)
    {
        if (!SystemsScene.IsGameplayScene(scene)) return false;
        foreach (var n in SafeHubScenes)
            if (scene.name == n) return false;
        return true;
    }

    // ════════════════════════════════════════
    //  저장 원자 연산
    // ════════════════════════════════════════

    /// <summary>디스크 즉시 저장(체크포인트 확정).</summary>
    void Commit()
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.Save();
    }

    /// <summary>인메모리 스냅샷만 갱신(디스크 X).</summary>
    void Record()
    {
        if (SaveManager.Instance == null) return;
        _raidSnapshotJson = SaveManager.Instance.ToJson(SaveManager.Instance.BuildSaveData());
    }

    // ════════════════════════════════════════
    //  이벤트 훅 (공유 인터페이스 계약)
    // ════════════════════════════════════════

    /// <summary>레이드 시작 — 복귀 기준점 확정(Commit). 이후 InRaid=true.</summary>
    public void RaidStarted()
    {
        _inRaid = true;
        _crashCommitted = false;
        _raidSnapshotJson = null;
        Commit();   // 레이드 시작 시점 = 죽거나 강제종료 시 되돌아갈 기준점
    }

    /// <summary>레이드 종료(탈출 정산) — Commit. 스냅샷 비움.</summary>
    public void RaidEnded()
    {
        Commit();
        _raidSnapshotJson = null;
        _crashCommitted = false;
        _inRaid = false;
    }

    /// <summary>건물 진입 — 레이드 중이면 인메모리 기록, 안전 맥락이면 디스크 커밋.</summary>
    public void BuildingEntered() => RecordOrCommit();

    /// <summary>건물 퇴장 — 레이드 중이면 인메모리 기록, 안전 맥락이면 디스크 커밋.</summary>
    public void BuildingExited() => RecordOrCommit();

    /// <summary>퀘스트 수주 — 레이드 중이면 인메모리 기록, 안전 맥락이면 디스크 커밋.</summary>
    public void QuestAccepted() => RecordOrCommit();

    /// <summary>퀘스트 완료 — 레이드 중이면 인메모리 기록, 안전 맥락이면 디스크 커밋.</summary>
    public void QuestCompleted() => RecordOrCommit();

    /// <summary>전투 시작 — 항상 인메모리 스냅샷만(전투 결과를 디스크에 고정하지 않음).</summary>
    public void CombatStarted() => Record();

    /// <summary>전투 종료 — 항상 인메모리 스냅샷만.</summary>
    public void CombatEnded() => Record();

    /// <summary>침대 수면(수동 저장) — 침대는 안전가옥 시설이므로 항상 Commit.</summary>
    public void BedSleepSave() => Commit();

    /// <summary>인벤/창고/경제 변경(상점 구매·판매, F1 디버그 아이템 변경 등) —
    /// 안전 맥락이면 디스크 커밋, 레이드 중이면 인메모리 기록.</summary>
    public void InventoryChanged() => RecordOrCommit();

    /// <summary>레이드 중이면 Record(인메모리), 아니면 Commit(디스크). 이벤트성 훅 공용.</summary>
    void RecordOrCommit()
    {
        if (_inRaid) Record();
        else Commit();
    }

    // ════════════════════════════════════════
    //  크래시 / 강제 종료
    // ════════════════════════════════════════

    /// <summary>예외(크래시) 발생 시: 레이드 중 인메모리 스냅샷을 1회 디스크에 커밋(크래시 복구).
    /// 일반 종료/되돌리기와 달리, 의도치 않은 크래시에서는 진행을 잃지 않게 한다.</summary>
    void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Exception) return;
        if (_crashCommitted) return;
        if (!_inRaid) return;
        if (string.IsNullOrEmpty(_raidSnapshotJson)) return;
        if (SaveManager.Instance == null) return;

        _crashCommitted = true;
        SaveManager.Instance.WriteJson(_raidSnapshotJson);
        Debug.Log("[SaveCheckpoints] 크래시 감지 — 레이드 인메모리 스냅샷을 디스크에 복구 커밋.");
    }

    /// <summary>강제 종료/전원: 레이드 중이면 아무것도 쓰지 않는다
    /// (디스크엔 레이드 시작본만 → 다음 로드 시 레이드 시작 지점으로 복귀).
    /// 비-레이드면 이미 이벤트성 커밋이 끝나 있으므로 별도 저장하지 않는다.</summary>
    void OnApplicationQuit()
    {
        // 의도적으로 no-op. (세이브 스커밍 방지 모델의 핵심)
    }
}
