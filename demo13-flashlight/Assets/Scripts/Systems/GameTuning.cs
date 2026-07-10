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
    [Tooltip("적 스폰 마릿수 전역 배율. 1=동일. EnemySpawner가 각 SpawnZone.enemyCount에 곱함(반올림).")]
    [Range(0f, 3f)] public float enemySpawnCountMult = 1f;
    [Tooltip("적 처치 시 전리품(지상 티어 루트) 드랍 확률. 1=항상 굴림(루트 자체 확률은 별도), 0=안 떨굼. EnemyController가 읽음.")]
    [Range(0f, 1f)] public float enemyDropChance = 1f;
    [Tooltip("적 시체에 가방(컨테이너 아이템)이 통째로 들어 있을 확률(타르코프식 — 가방 안엔 지역 루트 1~2개, 가방째 가져갈 수 있음). EnemyController가 읽음.")]
    [Range(0f, 1f)] public float corpseBagChance = 0.3f;

    // ── 시야 (FOV 시야콘, 좀보이드식) ─────────────────────────────────
    [Header("시야 (FOV 시야콘)")]
    [Tooltip("시야콘 켜기. 끄면 시야 제한 없이 모든 적이 보임(디버그/비활성).")]
    public bool visionEnabled = true;
    [Tooltip("시야콘 전체 각도(도). 정면 기준 좌우로 절반씩. 150=정면 ±75°. PlayerVision이 읽음.")]
    [Range(30f, 360f)] public float visionFovDegrees = 150f;
    [Tooltip("시야 사거리(m). 이 안 + 콘 각도 안의 적만 보임.")]
    [Range(2f, 30f)] public float visionRange = 9f;
    [Tooltip("근접 인지 반경(m) — 이 안은 각도 무관 360° 보임(바로 옆 기척).")]
    [Range(0f, 6f)] public float visionNearRadius = 2.2f;
    [Tooltip("켜면 벽(솔리드 콜라이더)이 시야를 막음(LOS). 끄면 각도·사거리만.")]
    public bool visionLineOfSight = true;

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
    [Tooltip("동시 활성 최대 개수(상한). 아래 군데 수 가중치로 뽑은 값을 이 값·잠복 존 수로 클램프.")]
    public int anomalyMaxConcurrent = 1;
    [Tooltip("한 번 발생 시 동시에 몇 '군데' 나올지 가중치. [0]=1군데, [1]=2군데, [2]=3군데 … 비우거나 합 0이면 항상 1군데.")]
    public float[] anomalySpotCountWeights = new float[] { 1f };
    [Tooltip("몬스터 최소 스폰 거리(m) — 플레이어 옆 즉시 스폰 방지. (Phase 2)")]
    public float anomalyMonsterMinDist = 6f;

    // ── 경험치/레벨 (레이드 XP → 레벨업 → PP — traits.md §2) ─────────
    [Header("경험치/레벨 (레이드 정산)")]
    [Tooltip("탈출 성공 고정 보너스 XP. RaidManager가 읽음.")]
    public int xpExtractBonus = 100;
    [Tooltip("루팅 XP = 획득 아이템 sellPrice 합 × 이 값. 탈출 성공 시에만.")]
    [Range(0f, 1f)] public float xpPerLootValue = 0.02f;
    [Tooltip("실패(사망/시간초과) 시 킬 XP 획득 배율(탈출·루팅 보너스는 없음).")]
    [Range(0f, 1f)] public float xpFailMult = 0.5f;
    [Tooltip("다음 레벨 필요 XP = 이 값 × 현재 레벨 (선형). PlayerProgress가 읽음.")]
    public int xpPerLevelBase = 100;
    [Tooltip("레벨업 1회당 지급 PP. TraitManager.GrantPP.")]
    public int xpPpPerLevel = 1;

    // ── 아르바이트 (안전구역 납품 의뢰 — economy.md §아르바이트) ──────
    [Header("아르바이트 (게시판 납품)")]
    [Tooltip("게시판에 동시에 걸리는 납품 의뢰 수. ArbeitBoard가 읽음. (패널 높이상 3행까지 — 4+ 필요 시 MapSelectUI 아르바이트 패널 스크롤 대응 먼저)")]
    [Range(1, 3)] public int arbeitSlotCount = 3;
    [Tooltip("의뢰당 요구 수량 최소.")]
    public int arbeitQtyMin = 2;
    [Tooltip("의뢰당 요구 수량 최대.")]
    public int arbeitQtyMax = 5;
    [Tooltip("보수 배율 — 아이템 sellPrice × 수량 × 이 값(10 단위 올림). 직접판매(sellRate≈0.6)보다 높고 수배(×1.2)와 비슷하게.")]
    [Range(0.5f, 3f)] public float arbeitRewardMult = 1.0f;
    [Tooltip("납품 n회마다 PP +1 (0=PP 지급 안 함). TraitManager.GrantPP 호출.")]
    public int arbeitPpEvery = 3;

    // ── 상점 — 희귀도별 해금 평판 등급 (NPC 상점 기능 밸런스) ──────────
    [Header("상점 해금 평판(희귀도별)")]
    [Tooltip("Rare 아이템을 살 수 있게 되는 최소 평판 등급. ShopUI가 읽음.")]
    public ReputationTier shopTierRare = ReputationTier.D;
    [Tooltip("Epic 아이템 해금 최소 평판 등급.")]
    public ReputationTier shopTierEpic = ReputationTier.B;
    [Tooltip("Legendary 아이템 해금 최소 평판 등급.")]
    public ReputationTier shopTierLegendary = ReputationTier.A;

    // ── 스토리/대화 페이싱 (프롤로그·내레이션·대화·튜토리얼 완급) ──────
    [Header("스토리/대화 페이싱")]
    [Tooltip("새 게임 프롤로그 시작 전 대기(초). GameStartHandler가 읽음. 짧을수록 흑화면 빨리 지나감.")]
    [Range(0f, 2f)] public float prologueStartDelay = 0.2f;
    [Tooltip("스토리 JSON의 wait 노드 전역 배율. 1=원본, 0.5=절반(빠르게). StoryPlayer가 곱함. authored 값을 안 건드리고 전체 완급 조절.")]
    [Range(0.1f, 3f)] public float storyWaitScale = 0.6f;
    [Tooltip("스토리 fade_in 기본 지속(초). S-000 등이 이 값을 쓰도록 배선. 흑화면→화면 드러나는 속도.")]
    [Range(0.1f, 3f)] public float storyFadeInDuration = 0.8f;
    [Tooltip("내레이션(독백) 타이핑 속도(자당 초). 작을수록 빠름. NarrationUI가 읽음.")]
    [Range(0.005f, 0.1f)] public float narrationTypingSpeed = 0.02f;
    [Tooltip("대화(NPC) 타이핑 속도(자당 초). 작을수록 빠름. DialogueUI가 읽음.")]
    [Range(0.005f, 0.1f)] public float dialogueTypingSpeed = 0.02f;
    [Tooltip("튜토리얼 프롬프트 기본 표시 시간(초). 노드에 duration 미지정 시. StoryPlayer가 읽음.")]
    [Range(1f, 10f)] public float tutorialDefaultDuration = 4f;
}
