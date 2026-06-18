using UnityEngine;

/// <summary>
/// 게임 전역 튜닝 값 한 곳(밸런스/타이밍 단일 소스). `Resources/Data/GameTuning.asset`.
/// 각 시스템은 `GameTuning.Instance.X`를 읽되, 에셋이 없으면 자체 기본값으로 폴백(크래시 없음).
/// 에디터에서 'Tools ▸ TopDown ▸ Control Panel'로 보고 튜닝.
///
/// 값 추가법: 아래에 필드 하나 추가 → 쓰는 시스템에서 GameTuning.Instance.필드 읽기 → 패널에 자동 노출.
/// </summary>
[CreateAssetMenu(menuName = "BRB/Game Tuning", fileName = "GameTuning")]
public class GameTuning : ScriptableObject
{
    static GameTuning _instance;

    /// <summary>없으면 null 반환 → 각 시스템이 자체 기본값 사용. (런타임/에디터 공용)</summary>
    public static GameTuning Instance
    {
        get
        {
            if (_instance == null) _instance = Resources.Load<GameTuning>("Data/GameTuning");
            return _instance;
        }
    }

    // ── 수색 (루팅 상자 아이템 공개 속도) ──────────────────────────────
    [Header("수색 (루팅 상자)")]
    [Tooltip("아이템 공개 딜레이 배율. >1 느리게, <1 빠르게. 희귀도별 기본 딜레이에 곱함.")]
    [Range(0.25f, 5f)] public float searchSpeedMult = 1f;
    [Tooltip("Common 아이템 수색 기본 딜레이(초). searchSpeedMult가 곱해짐. 고급일수록 길게.")]
    public float searchSecCommon = 0.4f;
    [Tooltip("Uncommon 수색 기본 딜레이(초).")]
    public float searchSecUncommon = 0.6f;
    [Tooltip("Rare 수색 기본 딜레이(초).")]
    public float searchSecRare = 0.9f;
    [Tooltip("Epic 수색 기본 딜레이(초).")]
    public float searchSecEpic = 1.3f;
    [Tooltip("Legendary 수색 기본 딜레이(초). 고급 아이템일수록 늦게 공개.")]
    public float searchSecLegendary = 1.8f;

    // ── 시간 / 현상 (지역 낮·밤 길이) ─────────────────────────────────
    [Header("시간 / 현상 (지역 낮·밤, 초)")]
    [Tooltip("낮 지속 시간(초). RegionTimeManager가 읽음.")]
    public float dayDuration = 120f;
    [Tooltip("밤(짙은 현상) 지속 시간(초). 1회 현상 지속 ≈ 10분.")]
    public float nightDuration = 600f;

    // ── 레이드 ───────────────────────────────────────────────────────
    [Header("레이드")]
    [Tooltip("레이드 제한 시간(초). 0=무제한. RaidManager가 읽음.")]
    public float raidDuration = 1200f;

    // ── 드랍 ─────────────────────────────────────────────────────────
    [Header("드랍 (맵 전체)")]
    [Tooltip("지역 루트 수량 배율. 1=기본, 0.5=절반, 2=두 배. RegionLootBootstrap가 읽음.")]
    [Range(0f, 3f)] public float lootCountMult = 1f;
    [Tooltip("루트 롤이 실제로 떨어질 확률 배율(전역). 1=기존과 동일(항상 통과), <1=빈손 증가. RegionLootCatalog.Roll이 롤마다 게이트.")]
    [Range(0f, 1f)] public float lootChanceMult = 1f;
    [Tooltip("귀중품(Valuable) 카테고리 등장 가중치 배율. 1=동일, >1=귀중품 더 자주. RegionLootCatalog.Roll의 Valuable 항목 weight에 곱함.")]
    [Range(0f, 5f)] public float valuableWeightMult = 1f;
    [Tooltip("맵 아이템 스폰 총량 전역 배율. 1=동일. MapSpawnProfile 예산에 추가로 곱함.")]
    [Range(0f, 3f)] public float itemSpawnCountMult = 1f;

    // ── 생존 (수분 / 포만감 / 아사) ───────────────────────────────────
    [Header("생존 (레이드 중 차감)")]
    [Tooltip("수분 100→0까지 걸리는 시간(분, 레이드 실시간). SurvivalStats가 분→초당으로 환산해 읽음.")]
    public float survivalWaterMinutesToEmpty = 18f;
    [Tooltip("포만감 100→0까지 걸리는 시간(분, 레이드 실시간).")]
    public float survivalSatietyMinutesToEmpty = 36f;
    [Tooltip("빈 스탯(0) 1개당 초당 HP 감소(silent DoT). 둘 다 0이면 2배.")]
    public float survivalStarveHpPerSec = 0.6f;

    // ── 수면 (거처 침상 — 옵션별 회복/차감) ───────────────────────────
    [Header("수면 4시간")]
    [Tooltip("4시간 수면 시 HP 회복 비율(0~1, MaxHp 기준). SleepUI가 읽음.")]
    public float sleep4hHpPct = 0.40f;
    [Tooltip("4시간 수면 시 수분 차감량.")]
    public float sleep4hWater = 18f;
    [Tooltip("4시간 수면 시 포만감 차감량.")]
    public float sleep4hSatiety = 18f;

    [Header("수면 8시간")]
    [Tooltip("8시간 수면 시 HP 회복 비율(0~1, MaxHp 기준).")]
    public float sleep8hHpPct = 1.00f;
    [Tooltip("8시간 수면 시 수분 차감량.")]
    public float sleep8hWater = 38f;
    [Tooltip("8시간 수면 시 포만감 차감량.")]
    public float sleep8hSatiety = 38f;

    // ── 짙은현상 (구간 현상) ──────────────────────────────────────────
    [Header("짙은현상 (구간)")]
    [Tooltip("현상 활성 지속(초). 이 안에 루트 회수. AnomalyZone이 읽음.")]
    public float anomalyActiveDuration = 180f;
    [Tooltip("징조(텔레그래프) 시간(초) — 안개 모이고 예고.")]
    public float anomalyTelegraph = 8f;
    [Tooltip("종료 경고 시간(초) — 활성 끝 이만큼 전부터 '빨리 챙겨'.")]
    public float anomalyWarning = 30f;
    [Tooltip("붕괴(미회수 증발) 연출 시간(초).")]
    public float anomalyCollapse = 5f;
    [Tooltip("자동 발생 랜덤 간격 최소(초). AnomalyManager가 읽음.")]
    public float anomalyIntervalMin = 90f;
    [Tooltip("자동 발생 랜덤 간격 최대(초).")]
    public float anomalyIntervalMax = 180f;
    [Tooltip("동시 활성 최대 개수.")]
    public int anomalyMaxConcurrent = 1;
    [Tooltip("몬스터 최소 스폰 거리(m) — 플레이어 옆 즉시 스폰 방지. (Phase 2)")]
    public float anomalyMonsterMinDist = 6f;

    // ── 상점 — 희귀도별 해금 평판 등급 (NPC 상점 기능 밸런스) ──────────
    [Header("상점 해금 평판(희귀도별)")]
    [Tooltip("Rare 아이템을 살 수 있게 되는 최소 평판 등급. ShopUI가 읽음.")]
    public ReputationTier shopTierRare = ReputationTier.D;
    [Tooltip("Epic 아이템 해금 최소 평판 등급.")]
    public ReputationTier shopTierEpic = ReputationTier.B;
    [Tooltip("Legendary 아이템 해금 최소 평판 등급.")]
    public ReputationTier shopTierLegendary = ReputationTier.A;
}
