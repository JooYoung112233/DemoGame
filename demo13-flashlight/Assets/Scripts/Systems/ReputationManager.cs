using UnityEngine;

/// <summary>
/// 플레이어 전역 평판(명성) 중앙 관리 싱글톤. quests-region1.md §9.1~9.2.
/// 평판 = 거리에서의 이름값(전역 단일 수치). 신뢰도(NPC별)는 NPCRelationshipManager가 별도 관리.
/// 등급 F~A → 상점·하이드아웃 해금 상한 + 자릿세 할인. 적립 행동값 = ReputationActions(reputation.csv 미러).
/// 등급 로직은 ReputationTiers(순수). SaveManager가 잔액 영속화.
/// </summary>
public class ReputationManager : MonoBehaviour
{
    public static ReputationManager Instance { get; private set; }

    [SerializeField] int reputation = 0;

    /// <summary>현재 평판 수치(0~9999).</summary>
    public int Reputation => reputation;
    /// <summary>현재 등급 F~A.</summary>
    public ReputationTier Tier => ReputationTiers.GetTier(reputation);
    /// <summary>현재 등급 호칭(신참·견습…이름).</summary>
    public string TierName => ReputationTiers.Name(Tier);
    /// <summary>현재 자릿세 할인 %.</summary>
    public int RentDiscountPercent => ReputationTiers.RentDiscountPercent(reputation);
    /// <summary>다음 등급까지 남은 평판(A면 0).</summary>
    public int ToNextTier => ReputationTiers.ToNextTier(reputation);

    /// <summary>평판 변동 (newRep, delta). HUD/UI가 구독.</summary>
    public event System.Action<int, int> OnReputationChanged;
    /// <summary>등급 변동 (old, new). 해금 알림·연출이 구독.</summary>
    public event System.Action<ReputationTier, ReputationTier> OnTierChanged;

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

    /// <summary>평판 증감. reason은 로그용. 등급이 바뀌면 OnTierChanged + 상승 토스트.</summary>
    public void Add(int amount, string reason = null)
    {
        if (amount == 0) return;
        var oldTier = Tier;
        reputation = Mathf.Clamp(reputation + amount, 0, ReputationTiers.Max);
        var newTier = Tier;

        Debug.Log($"[Reputation] {(amount >= 0 ? "+" : "")}{amount} ({reason ?? "행동"}) → {reputation} [{ReputationTiers.Name(newTier)} {newTier}]");
        OnReputationChanged?.Invoke(reputation, amount);

        // 변동 표시 토스트 (올라감/내려감) — 현재값·등급 함께
        ToastManager.Show($"평판 {(amount >= 0 ? "+" : "")}{amount}  ·  {reputation} [{ReputationTiers.Name(newTier)}]",
            amount >= 0 ? ToastManager.ToastType.Success : ToastManager.ToastType.Warning);

        if (newTier != oldTier)
        {
            Debug.Log($"[Reputation] 등급 변동 {oldTier} → {newTier}");
            OnTierChanged?.Invoke(oldTier, newTier);
            if ((int)newTier > (int)oldTier)
            {
                ToastManager.Show($"★ 평판 상승 — {ReputationTiers.Name(newTier)} ({newTier})", ToastManager.ToastType.Success);
                // 평판 등급업 = PP 보조 공급원 (traits.md §2 — 등급당 +1, 한 번에 여러 등급이면 등급 수만큼).
                // 등급 하락 시 회수는 안 함(이미 쓴 PP 회수 불가 — 단순화).
                int tiersUp = (int)newTier - (int)oldTier;
                if (TraitManager.Instance != null)
                {
                    TraitManager.Instance.GrantPP(tiersUp);
                    ToastManager.Show($"PP +{tiersUp} (평판 등급 상승)", ToastManager.ToastType.Success);
                }
                else Debug.LogWarning($"[Reputation] TraitManager 없음 — 등급업 PP {tiersUp} 소실");
            }
        }
    }

    /// <summary>행동 키로 적립(reputation.csv 값). 미정의 키(BD/DQ 등)는 0 → 무시.</summary>
    public void AddByAction(string actionKey, string reason = null)
    {
        int v = ReputationActions.Value(actionKey);
        if (v != 0) Add(v, reason ?? actionKey);
    }

    // ═══════════════════════════
    //  세이브/로드
    // ═══════════════════════════

    public int GetSaveData() => reputation;

    public void LoadSaveData(int saved)
    {
        reputation = Mathf.Clamp(saved, 0, ReputationTiers.Max);
        OnReputationChanged?.Invoke(reputation, 0);
    }
}
