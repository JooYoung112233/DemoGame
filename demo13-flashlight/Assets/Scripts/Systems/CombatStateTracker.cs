using UnityEngine;

/// <summary>
/// 전투 상태 추적기 (SaveCheckpoints 전투 훅 구동).
///
/// 0.5초 폴링으로 "플레이어 교전 중"인지 판정한다:
///   • 활성 EnemyController 중 하나라도 IsEngagingPlayer(추격/예비동작/공격)면 전투중.
///
/// 에지:
///   • 비전투 → 전투 전이 시 SaveCheckpoints.CombatStarted().
///   • 교전 상태가 사라진 시점부터 CombatEndGrace초 경과 후 SaveCheckpoints.CombatEnded().
///     (그 사이 다시 교전되면 타이머 리셋)
///
/// 레이드 씬에서만 동작(SaveCheckpoints.InRaid). 안전가옥/허브에선 폴링하지 않는다.
/// </summary>
public class CombatStateTracker : MonoBehaviour
{
    public static CombatStateTracker Instance { get; private set; }

    const float PollInterval   = 0.5f;   // 폴링 주기(초)
    const float CombatEndGrace = 30f;    // 교전 종료 후 전투 종료 확정까지 유예(초)

    bool  _inCombat;
    float _pollTimer;
    float _lastEngagedTime = -999f;   // 마지막으로 교전 상태였던 시각(Time.time)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;
        if (Instance != null) return;
        if (FindFirstObjectByType<CombatStateTracker>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("[CombatStateTracker]");
        go.AddComponent<CombatStateTracker>();
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
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // 레이드 밖에선 전투 추적 안 함(전투 상태도 안전하게 해제)
        if (SaveCheckpoints.Instance == null || !SaveCheckpoints.Instance.InRaid)
        {
            if (_inCombat) _inCombat = false;   // 씬 떠남: 종료 훅 없이 상태만 초기화
            return;
        }

        _pollTimer -= Time.deltaTime;
        if (_pollTimer > 0f) return;
        _pollTimer = PollInterval;

        bool engaged = AnyEnemyEngagingPlayer();
        if (engaged) _lastEngagedTime = Time.time;

        if (!_inCombat)
        {
            // 비전투 → 전투 전이
            if (engaged)
            {
                _inCombat = true;
                SaveCheckpoints.Instance.CombatStarted();
            }
        }
        else
        {
            // 전투 중 — 교전이 끊긴 뒤 유예시간 경과하면 종료 확정
            if (!engaged && Time.time - _lastEngagedTime >= CombatEndGrace)
            {
                _inCombat = false;
                SaveCheckpoints.Instance.CombatEnded();
            }
        }
    }

    /// <summary>활성 적 중 하나라도 플레이어와 교전 중인가.</summary>
    static bool AnyEnemyEngagingPlayer()
    {
        var enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (e != null && !e.IsDead && e.IsEngagingPlayer) return true;
        }
        return false;
    }
}
