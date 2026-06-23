using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 특성(퍽) 런타임 코어 싱글톤. traits.md §1~§4.
/// NPCRelationshipManager / ReputationManager 패턴(싱글톤 + DontDestroyOnLoad + 상태추적)을 본떴다.
///
/// 책임:
///   - 부팅 시 Resources/Data/Traits/*.asset 로드(id → TraitData 사전).
///   - 해금 상태(unlocked) · PP 잔량(availablePP) · 부정특성 수 추적.
///   - 해금 가능성/해금 처리(PP 차감·환급) API.
///   - 효과 합성 쿼리 API: GetModifier(key) / HasFlag(key).
///   - 세이브/로드(unlocked + PP). SaveManager 패턴.
///
/// ★ 경계: 이 매니저는 "쿼리 API"만 제공한다. StatDB·전투·인벤·현상 등 말단 read-site는
///   이번 범위에서 건드리지 않는다. 각 시스템이 필요 시 아래처럼 직접 조회한다(말단 배선 TODO):
///
///   // TODO(말단 배선) 예시 — 스태미너 최대치에 trait 모디파이어 적용:
///   //   float baseMax = playerStat.staminaMax;
///   //   float finalMax = baseMax * TraitManager.Instance.GetModifier("stamina_max");
///   // TODO(말단 배선) 예시 — 능력형(flag):
///   //   if (TraitManager.Instance != null && TraitManager.Instance.HasFlag("rudi_ping")) { /* 핑 표시 */ }
///
/// 부트스트랩: Systems 씬에 배치하는 것이 정석. Systems 미빌드/맵툴 씬에서는
///   RuntimeInitializeOnLoadMethod 폴백으로 자동 스폰(GameBootstrap 수정 불필요).
/// </summary>
public class TraitManager : MonoBehaviour
{
    public static TraitManager Instance { get; private set; }

    // ───────── effectKey / op 규약 (traits.md §4 표와 동기화) ─────────
    //  op 어휘:
    //    "mul"  : 비율. value 0.15 = +15%, -0.30 = -30%. 합성은 ∏(1 + value).
    //             GetModifier가 반환하는 "최종 배수"이므로 호출부는 baseValue * GetModifier(key)로 쓴다.
    //    "add"  : 가산. value 단위 그대로 합산. GetModifierAdditive로 조회(또는 GetRawSum).
    //    "flag" : 능력 on/off. value!=0 이면 켜짐. HasFlag(key)로 조회.
    public const string OP_MUL = "mul";
    public const string OP_ADD = "add";
    public const string OP_FLAG = "flag";

    // ───────── 데이터 ─────────
    readonly Dictionary<string, TraitData> byId = new Dictionary<string, TraitData>();
    readonly List<TraitData> all = new List<TraitData>();

    // ───────── 상태 ─────────
    readonly HashSet<string> unlocked = new HashSet<string>();
    int availablePP = 0;

    // ───────── 규칙 상수 (traits.md §2) ─────────
    public const int MaxNegativeTraits = 3;   // 부정 특성 보유 상한

    /// <summary>특성 해금/PP 변동 시 발생. (UI 갱신용)</summary>
    public event System.Action OnTraitsChanged;

    // ═══════════════════════════
    //  부트스트랩
    // ═══════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadDefinitions();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Systems 미빌드/맵툴 씬 폴백 자동 스폰. (GameBootstrap 수정 불필요)</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapFallback()
    {
        // Systems 씬이 매니저를 공급하면 폴백 생략. (SystemsScene가 없는 빌드 대비 방어)
        if (SystemsScene.ProvidesSystems) return;
        if (Instance != null) return;
        if (FindFirstObjectByType<TraitManager>() != null) return;
        var go = new GameObject("[TraitManager]");
        go.AddComponent<TraitManager>();
        DontDestroyOnLoad(go);
    }

    /// <summary>정의 로드 보장. 에딧 모드(Awake 미실행)의 자가검증에서 명시 호출용. 이미 로드됐으면 무시.</summary>
    public void EnsureLoaded() => LoadDefinitions();

    void LoadDefinitions()
    {
        if (all.Count > 0) return; // 중복 로드 방지
        var loaded = Resources.LoadAll<TraitData>("Data/Traits");
        foreach (var t in loaded)
        {
            if (t == null || string.IsNullOrEmpty(t.traitId)) continue;
            if (byId.ContainsKey(t.traitId))
            {
                Debug.LogWarning($"[TraitManager] 중복 traitId 무시: {t.traitId}");
                continue;
            }
            byId[t.traitId] = t;
            all.Add(t);
        }
        Debug.Log($"[TraitManager] 특성 {all.Count}개 로드.");
    }

    // ═══════════════════════════
    //  조회
    // ═══════════════════════════

    /// <summary>로드된 전 특성(읽기 전용 사본 아님 — 수정 금지).</summary>
    public IReadOnlyList<TraitData> AllTraits => all;

    /// <summary>id로 특성 정의 조회. 없으면 null.</summary>
    public TraitData Get(string traitId)
    {
        if (string.IsNullOrEmpty(traitId)) return null;
        byId.TryGetValue(traitId, out var t);
        return t;
    }

    /// <summary>현재 PP 잔량.</summary>
    public int AvailablePP => availablePP;

    /// <summary>해금 여부.</summary>
    public bool IsUnlocked(string traitId)
        => !string.IsNullOrEmpty(traitId) && unlocked.Contains(traitId);

    /// <summary>현재 보유한 부정 특성 수.</summary>
    public int NegativeTraitCount
    {
        get
        {
            int n = 0;
            foreach (var id in unlocked)
            {
                var t = Get(id);
                if (t != null && t.tier == TraitTier.Negative) n++;
            }
            return n;
        }
    }

    // ═══════════════════════════
    //  PP
    // ═══════════════════════════

    /// <summary>PP 지급(평판 레벨업·마일스톤 등). 음수도 허용하나 0 미만으로 떨어지지 않게 클램프.</summary>
    public void GrantPP(int amount)
    {
        if (amount == 0) return;
        availablePP = Mathf.Max(0, availablePP + amount);
        OnTraitsChanged?.Invoke();
    }

    // ═══════════════════════════
    //  해금 규칙
    // ═══════════════════════════

    /// <summary>
    /// 해금 가능 여부 + 불가 사유(reason). reason은 UI 표시용 한국어.
    /// 규칙:
    ///   - 이미 해금 → 불가.
    ///   - 정의 없음 → 불가.
    ///   - 부정 특성(ppCost &lt;= 0): 보유 상한(3개) 검사. 통과 시 환급이라 항상 가능.
    ///   - 긍정 퍽: 선행 특성 해금 필요 + PP &gt;= ppCost.
    ///   - (티어/평판 게이트는 1차로 선행조건·PP로 갈음. 평판 등급 게이트는 TODO 훅만.)
    /// </summary>
    public bool CanUnlock(string traitId, out string reason)
    {
        reason = null;
        var t = Get(traitId);
        if (t == null) { reason = "존재하지 않는 특성"; return false; }
        if (IsUnlocked(traitId)) { reason = "이미 해금됨"; return false; }

        // 부정 특성(낙인): PP 환급. 보유 상한만 검사.
        if (t.tier == TraitTier.Negative)
        {
            if (NegativeTraitCount >= MaxNegativeTraits)
            {
                reason = $"부정 특성 보유 상한({MaxNegativeTraits}개) 초과";
                return false;
            }
            return true;
        }

        // 긍정 퍽: 선행조건 + PP.
        if (!string.IsNullOrEmpty(t.prereqTraitId) && !IsUnlocked(t.prereqTraitId))
        {
            var pre = Get(t.prereqTraitId);
            reason = $"선행 특성 필요: {(pre != null ? pre.displayName : t.prereqTraitId)}";
            return false;
        }

        // TODO(평판 게이트): 상위 티어는 ReputationManager.Tier 하한을 추가로 요구할 수 있음.
        //   예) if (t.tier == TraitTier.T3 && ReputationManager.Instance != null
        //          && (int)ReputationManager.Instance.Tier < requiredTier) { reason=...; return false; }

        int cost = Mathf.Max(0, t.ppCost); // 긍정 퍽은 ppCost>0
        if (availablePP < cost)
        {
            reason = $"PP 부족 (필요 {cost} / 보유 {availablePP})";
            return false;
        }
        return true;
    }

    /// <summary>
    /// 특성 해금. 성공 시 true.
    ///   - 긍정 퍽: PP 차감(ppCost).
    ///   - 부정 특성: PP 환급(|ppCost| 만큼 +). ppCost가 음수로 저장되어 있으므로 부호 반전해서 더함.
    /// </summary>
    public bool Unlock(string traitId)
    {
        if (!CanUnlock(traitId, out _)) return false;
        var t = Get(traitId);
        if (t == null) return false;

        if (t.tier == TraitTier.Negative)
        {
            // 환급: ppCost = -2 → +2 PP.
            availablePP = Mathf.Max(0, availablePP + Mathf.Abs(t.ppCost));
        }
        else
        {
            availablePP = Mathf.Max(0, availablePP - Mathf.Max(0, t.ppCost));
        }

        unlocked.Add(traitId);
        OnTraitsChanged?.Invoke();
        Debug.Log($"[TraitManager] 해금: {t.displayName} ({traitId}) · PP 잔량 {availablePP}");
        return true;
    }

    // ═══════════════════════════
    //  효과 합성 쿼리 (말단 read-site용)
    // ═══════════════════════════

    /// <summary>
    /// 해금된 특성들의 effects 중 effectKey가 일치하는 항목을 합성해 "최종 배수"를 반환.
    ///   - op=mul: ∏(1 + value)  → 호출부는 baseValue * GetModifier(key) 로 쓴다.
    ///   - op=add: (1 + Σvalue)  → 비율로 환산해 함께 곱해진다(가산 효과도 배수에 포함).
    ///   - op=flag: 무시(HasFlag로 조회).
    /// 일치 항목이 없으면 1.0(영향 없음).
    /// ※ add를 절대량으로 쓰고 싶으면 GetAdditive(key)를 사용.
    /// </summary>
    public float GetModifier(string effectKey)
    {
        if (string.IsNullOrEmpty(effectKey)) return 1f;
        float mul = 1f;
        float addSum = 0f;
        foreach (var id in unlocked)
        {
            var t = Get(id);
            if (t == null || t.effects == null) continue;
            foreach (var e in t.effects)
            {
                if (e == null || e.effectKey != effectKey) continue;
                if (e.op == OP_MUL) mul *= (1f + e.value);
                else if (e.op == OP_ADD) addSum += e.value;
            }
        }
        return mul * (1f + addSum);
    }

    /// <summary>해금된 특성의 add 항목 절대 합(예: 보존 슬롯 +1). 일치 없으면 0.</summary>
    public float GetAdditive(string effectKey)
    {
        if (string.IsNullOrEmpty(effectKey)) return 0f;
        float sum = 0f;
        foreach (var id in unlocked)
        {
            var t = Get(id);
            if (t == null || t.effects == null) continue;
            foreach (var e in t.effects)
            {
                if (e == null || e.effectKey != effectKey) continue;
                if (e.op == OP_ADD) sum += e.value;
            }
        }
        return sum;
    }

    /// <summary>op=flag 효과가 하나라도 켜져 있으면 true.</summary>
    public bool HasFlag(string effectKey)
    {
        if (string.IsNullOrEmpty(effectKey)) return false;
        foreach (var id in unlocked)
        {
            var t = Get(id);
            if (t == null || t.effects == null) continue;
            foreach (var e in t.effects)
            {
                if (e == null || e.effectKey != effectKey) continue;
                if (e.op == OP_FLAG && e.value != 0f) return true;
            }
        }
        return false;
    }

    // ═══════════════════════════
    //  세이브/로드 (ReputationManager / NPCRelationshipManager 패턴)
    // ═══════════════════════════
    //
    // TODO(말단 배선 — SaveManager 훅): SaveManager.BuildSaveData()/Load()에 아래 한 쌍을 추가하면 영속화 완성.
    //   저장:  if (TraitManager.Instance != null) data.traits = TraitManager.Instance.GetSaveData();
    //   로드:  if (TraitManager.Instance != null && data.traits != null)
    //              TraitManager.Instance.LoadSaveData(data.traits);
    //   그리고 GameSaveData에  public TraitSaveData traits;  필드 추가.
    //   (이번 작업은 기존 SaveManager/GameSaveData를 수정하지 않으므로 구조체만 제공하고 배선은 TODO로 남김.)

    /// <summary>현재 상태 → 직렬화 구조체.</summary>
    public TraitSaveData GetSaveData()
    {
        return new TraitSaveData
        {
            availablePP = availablePP,
            unlocked = new List<string>(unlocked),
        };
    }

    /// <summary>직렬화 구조체 → 상태 복원.</summary>
    public void LoadSaveData(TraitSaveData data)
    {
        if (data == null) return;
        unlocked.Clear();
        availablePP = Mathf.Max(0, data.availablePP);
        if (data.unlocked != null)
        {
            foreach (var id in data.unlocked)
            {
                // 정의가 사라진 id는 버린다(데이터 변경 대비).
                if (!string.IsNullOrEmpty(id) && byId.ContainsKey(id))
                    unlocked.Add(id);
            }
        }
        OnTraitsChanged?.Invoke();
    }
}

/// <summary>특성 세이브 구조체. GameSaveData에 필드로 얹는다(배선 TODO).</summary>
[System.Serializable]
public class TraitSaveData
{
    public int availablePP;
    public List<string> unlocked = new List<string>();
}
