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

    // ── 건물 내부 (진입 가능 비율 · 내부 루팅 예산) ─────────────────────
    // 2026-07-11: 지역1의 건물은 수십 채인데 전용 내부 씬은 12개뿐이라 나머지는 공용 내부를 돌려 쓴다.
    //   "건물을 더 열지 / 파밍을 늘릴지"는 **QA 플레이 결과로 판단**하므로 값이 아니라 **노브**로 뺀다.
    //   둘 다 씬 재빌드 없이(내부 예산) 또는 빌더 1회 재실행(진입 비율)으로 반영된다.
    [Tooltip("건물 내부가 받는 루팅 예산 배율. 내부 씬은 맵 전체가 아니라 '한 채'라 1.0이면 과다.\n" +
             "MapSpawnController(isInterior=true)가 곱한다. 예: 0.25면 맵 예산의 1/4.")]
    [Range(0.05f, 2f)] public float interiorLootBudgetMult = 0.25f;

    [Tooltip("절차 생성 건물 중 실제로 **진입 가능**하게 만들 비율(0~1). 1=전부, 0.5=절반.\n" +
             "Zone1 빌더가 결정론적으로 고른다 — 값을 바꾸면 `빌드 ▸ 지역1` 재실행 필요.")]
    [Range(0f, 1f)] public float buildingEnterRatio = 1f;
    // ── 근접 전투 (2026-07-11) ────────────────────────────────────────
    [Header("근접 전투")]
    [Tooltip("약공 3연타 콤보 사용. 2026-07-11 사용자 결정으로 **기본 OFF** — 궤적·타이밍이 매번 달라\n" +
             "타격 리듬이 안 읽혔다. 데이터(AttackComboData)는 그대로 두고 진행만 막는다(되살리기 쉽게).")]
    public bool comboEnabled = false;

    [Tooltip("구르기(Space / 패드 B) 사용. 2026-09-11 사용자 결정으로 **기본 OFF** — 구르기 모션이 없어\n" +
             "달리기 모션으로 미끄러지기만 했다. 코드·특성(dodge_iframe 등)은 그대로 두고 입력만 막는다(모션이 생기면 켠다).")]
    public bool dodgeEnabled = false;

    // ── 근접 사거리 감쇠 (2026-07-29) ──
    //   "닿기만 하면 같은 데미지"면 사거리가 긴 무기가 무조건 이득이라 거리 판단이 사라진다.
    //   품 안으로 파고들면 100%, 끝에 걸치면 farMult까지.
    [Tooltip("이 비율(사거리 대비)까지는 감쇠 없음. 0.35 = 사거리 5의 1.75m 안쪽은 100%")]
    [Range(0f, 0.9f)] public float meleeFalloffNear = 0.35f;
    [Tooltip("사거리 끝에서의 데미지 배율. 0.6 = 끝에 걸치면 60%")]
    [Range(0.1f, 1f)] public float meleeFalloffFar = 0.6f;

    [Header("적 강공(AI)")]
    [Tooltip("적이 공격을 시작할 때 **강공**을 고를 확률. 0=항상 약공.")]
    [Range(0f, 1f)] public float enemyHeavyChance = 0.3f;
    [Tooltip("약공을 이만큼 연속으로 낸 뒤엔 **반드시** 강공. 확률만 두면 영영 안 나오는 판이 생긴다.")]
    [Range(1, 8)] public int enemyHeavyForceAfter = 3;
    [Tooltip("강공의 예비동작 배율 — 길수록 '읽고 피할 수 있는' 큰 공격이 된다.")]
    [Range(1f, 4f)] public float enemyHeavyWindupMult = 1.9f;
    [Tooltip("강공의 데미지 배율.")]
    [Range(1f, 4f)] public float enemyHeavyDamageMult = 1.9f;

    // ── 총기 밴딧 (2026-09-11, docs/combat.md §총기 밴딧) — 유닛별 수치는 StatDB, 공통 타이밍만 여기 ──
    [Header("총기 밴딧(AI)")]
    [Tooltip("발사 직전 조준이 **고정**되는 시간(초). 고정된 조준선을 보고 옆으로 빠지면 피한다 — 길수록 쉽다.")]
    [Range(0f, 1f)] public float enemyAimLockTime = 0.2f;
    [Tooltip("마지막 탄 뒤 추격으로 돌아가기까지(초).")]
    [Range(0f, 2f)] public float enemyRangedRecover = 0.35f;
    [Tooltip("교전 시 총을 꺼내는 시간(초). 다 꺼내기 전엔 조준하지 않는다 — 발견 후 첫 발까지의 여유.")]
    [Range(0.1f, 2f)] public float enemyGunDrawTime = 0.6f;
    [Tooltip("적 탄의 비행 거리 = 사격 사거리 × 이 배율. 탄은 비행 거리 절반부터 55%까지 감쇠한다.")]
    [Range(1f, 2f)] public float enemyBulletRangeMult = 1.25f;

    [Tooltip("총알 거리 감쇠 시작 — 유효사거리의 이 비율까지는 데미지 그대로(플레이어·적 공용).\n" +
             "2026-09-11 Projectile 상수에서 옮김(총 수치 정리, docs/combat.md §무기 구성 결정).")]
    [Range(0f, 1f)] public float gunFalloffStart = 0.5f;
    [Tooltip("사거리 끝에서의 데미지 배율 — 감쇠 시작부터 끝까지 선형으로 이 값까지 준다.")]
    [Range(0.1f, 1f)] public float gunFalloffEndMult = 0.55f;

    // ── 총격(플레이어) (2026-09-11, docs/combat.md §총격전) — 총마다 수치는 WeaponData, 탄마다는 탄 아이템 ──
    [Header("총격(플레이어)")]
    [Tooltip("조준(우클릭) 중 카메라가 커서 쪽으로 밀리는 비율 — 플레이어→커서 거리 × 이 값.")]
    [Range(0f, 1f)] public float aimLookAhead = 0.35f;
    [Tooltip("조준 시 카메라가 밀리는 최대 거리(m).")]
    [Range(0f, 10f)] public float aimLookAheadMax = 4f;
    [Tooltip("관통탄이 몸 하나를 뚫을 때마다 남는 데미지 비율(탄 스펙 ammoPenetration과 함께).")]
    [Range(0.1f, 1f)] public float gunPierceDamageKeep = 0.6f;

    // ── 도로 장애물 / 막힌 통로 (2026-07-11) ──────────────────────────
    //   "넓은 길인데 그냥 뻥 뚫린 통로" 방지 — 도로 위 잔해로 동선을 꺾고 시야를 끊는다.
    //   밀도/통행폭은 빌더가 읽으므로 바꾸면 `빌드 ▸ 지역1` 재실행 필요.
    [Header("도로 장애물")]
    [Tooltip("도로 장애물 밀도 배율. 1=기본, 0=장애물 없음(뻥 뚫린 도로), 2=배로 빽빽.\n" +
             "Zone1 빌더가 읽는다 — 바꾸면 `빌드 ▸ 지역1` 재실행.")]
    [Range(0f, 2f)] public float roadObstacleDensity = 1f;
    [Tooltip("장애물을 놓고 **남겨 둘 최소 통행 폭(m)**. 이보다 좁아지지 않는다.\n" +
             "2.2 미만이면 플레이어가 낄 수 있으니 주의(몸 반경 ≈0.3).")]
    [Range(1.5f, 8f)] public float roadMinPassWidth = 3f;
    [Tooltip("잔해 '치우기' 채널 시간(초). BlockedPassage.Clearable.")]
    [Range(0.5f, 15f)] public float barricadeClearSeconds = 3.5f;
    [Tooltip("열쇠 없이 **강제 돌파**할 때 걸리는 시간(초). 열쇠 경로보다 확실히 비싸야 한다.")]
    [Range(1f, 30f)] public float barricadeBreachSeconds = 7f;
    [Tooltip("적 스폰 마릿수 전역 배율. 1=동일. EnemySpawner가 각 SpawnZone.enemyCount에 곱함(반올림).")]
    [Range(0f, 3f)] public float enemySpawnCountMult = 1f;
    [Tooltip("적 처치 시 전리품(지상 티어 루트) 드랍 확률. 1=항상 굴림(루트 자체 확률은 별도), 0=안 떨굼. EnemyController가 읽음.")]
    [Range(0f, 1f)] public float enemyDropChance = 1f;
    [Tooltip("적 시체에 가방(컨테이너 아이템)이 통째로 들어 있을 확률(타르코프식 — 가방 안엔 지역 루트 1~2개, 가방째 가져갈 수 있음). EnemyController가 읽음.")]
    [Range(0f, 1f)] public float corpseBagChance = 0.3f;
    [Tooltip("약탈자 회수 — 사망 시 잃은 소지품을 다음 레이드에서 나눠 가질 '약탈자' 적 최대 마리 수. ScavengerLoot가 읽음. (docs/raid.md)")]
    [Range(1, 6)] public int scavengerCount = 3;

    // ── 적 무리 / 레이드 스폰 안전 (2026-09-09) ────────────────────────
    [Header("적 무리 · 스폰 안전")]
    [Tooltip("한 '무리'로 볼 반경(m). 이 안의 적 스폰존들은 사실상 동시에 달려든다.\n" +
             "Zone1 빌더가 읽는다 — 바꾸면 `빌드3D ▸ 지역1` 재실행.")]
    [Range(4f, 30f)] public float enemyPackRadius = 12f;
    [Tooltip("무리 하나의 최대 **무게**(일반=1, 중장=2). 초과분은 빌드 시 잘린다.\n" +
             "3 = 일반 3마리 또는 일반 1 + 중장 1. 손으로 배치한 구역 존이 우선 살아남는다.")]
    [Range(1, 10)] public int enemyPackMaxWeight = 3;
    [Tooltip("레이드 진입 스폰이 확보해야 할 적 없는 반경(m). RaidSpawnDirector가 후보 스폰 중\n" +
             "이 조건을 만족하는 곳만 고른다(전부 실패하면 가장 여유 있는 곳).")]
    [Range(0f, 60f)] public float raidSpawnSafeRadius = 20f;

    // ── 무게 초과 페널티 (타르코프식 3구간 — docs/inventory.md 2026-07-10) ────────
    [Header("무게 초과 페널티")]
    [Tooltip("과적 시작 비율(현재무게/MaxWeight). 이 이상부터 스프린트 불가 + 이속 −slow1. 1.0=100%.")]
    [Range(0.5f, 2f)] public float overweightStartPct = 1.0f;
    [Tooltip("심각 시작 비율. 이 이상부터 이속 −slow2 + 스태미너 회복 배율 적용. 1.15=115%.")]
    [Range(0.5f, 2f)] public float overweightSeverePct = 1.15f;
    [Tooltip("하드컷 비율. 이 이상이 되도록은 더 담지 못함(줍기/이전 차단). 1.30=130%.")]
    [Range(0.5f, 3f)] public float overweightHardPct = 1.30f;
    [Tooltip("과적(과적~심각) 구간 이동속도 감소율. 0.15=−15%.")]
    [Range(0f, 0.9f)] public float overweightSlow1 = 0.15f;
    [Tooltip("심각(심각~하드컷 이상) 구간 이동속도 감소율. 0.30=−30%.")]
    [Range(0f, 0.95f)] public float overweightSlow2 = 0.30f;
    [Tooltip("심각 구간 스태미너 회복 배율. 0.5=회복 절반.")]
    [Range(0f, 1f)] public float overweightRegenMult = 0.5f;

    // ── 투척물 (돌 — 유인 전용) ───────────────────────────────────────
    // 소음 시스템은 2026-09-09 폐기(docs/scope-cut.md). 아래 둘은 **투척물 유인** 전용으로만 남았다.
    [Header("투척물 (돌 유인)")]
    [Tooltip("돌 최대 투척 사거리(m). 조준 원 반경 = 이 값. 커서가 밖이면 경계로 클램프.")]
    [Range(3f, 20f)] public float throwRange = 8f;
    [Tooltip("착탄 유인 반경(m). 이 안의 적이 조사하러 이동. 유인 강도.")]
    [Range(3f, 30f)] public float throwNoiseRadius = 9f;
    [Tooltip("돌 비행 속도(m/s). 착탄까지 시간 = 거리/속도(0.15~1.0s 클램프). 낮을수록 느리게 = 눈에 보이는 포물선.")]
    [Range(4f, 30f)] public float throwSpeed = 10f;
    [Tooltip("착탄 유인이 유효한 시간(초) — 이 동안 반경 안의 적이 반응한다.")]
    [Range(0.1f, 3f)] public float noisePulseDuration = 0.6f;
    [Tooltip("적이 유인 지점 도착 후 두리번거리는 시간(초). 이후 순찰 복귀.")]
    [Range(0.5f, 6f)] public float noiseInvestigateLook = 2.5f;

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

    [Tooltip("시야콘 **밖** 어둠 농도(0=끔, 1=완전 암흑). VisionDarkness 오버레이가 읽는다.\n" +
             "적이 시야 밖에서 숨겨지는 걸 '사라진 버그'가 아니라 '안 보이는 구역'으로 읽히게 하는 값.")]
    [Range(0f, 1f)] public float visionDarkAlpha = 0.72f;

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

    // ── 상점 — 희귀도별 해금 평판 등급 (NPC 상점 기능 밸런스) ──────────
    [Header("상점 해금 평판(희귀도별)")]
    [Tooltip("Rare 아이템을 살 수 있게 되는 최소 평판 등급. ShopUI가 읽음.")]
    public ReputationTier shopTierRare = ReputationTier.D;
    [Tooltip("Epic 아이템 해금 최소 평판 등급.")]
    public ReputationTier shopTierEpic = ReputationTier.B;
    [Tooltip("Legendary 아이템 해금 최소 평판 등급.")]
    public ReputationTier shopTierLegendary = ReputationTier.A;
    [Tooltip("재고 회전 — 매일 회전 슬롯 수(고정 슬롯 외 추가로 풀에서 추첨). ShopUI가 읽음.")]
    [Range(0, 8)] public int shopRotationSlots = 4;

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

    // ── 은신처 화면 (3D 디오라마 · 카메라 / 도크) ─────────────────────
    // 이 값들은 코드 상수로 흩어져 있었다. 카메라를 조금 당기고 싶을 때마다 스크립트를
    // 찾아 고치고 재컴파일해야 했다 — 화면 연출 수치라 자주 만지게 되므로 여기로 모은다.
    // 읽는 곳: HideoutDiorama(카메라·프레이밍), HideoutDockPanel(도크), DockedLayout(재배치).
    [Header("은신처 화면 — 카메라")]
    [Tooltip("방을 담는 카메라 오소 크기. 작을수록 확대(방이 크게 보인다). 기본 7.")]
    [Range(3f, 14f)] public float hideoutRoomOrtho = 7f;
    [Tooltip("기본 시야에서 방이 차지하는 가로 반지름 비율(실측 0.29 + 여유). 도크를 피해 줌아웃하는 계산의 기준. 키우면 더 물러난다.")]
    [Range(0.15f, 0.5f)] public float hideoutRoomHalfX = 0.31f;
    [Tooltip("같은 값의 세로판(실측 0.33 + 여유).")]
    [Range(0.15f, 0.5f)] public float hideoutRoomHalfY = 0.35f;
    [Tooltip("제목·안내·시설 바가 쓰는 화면 상단 띠 높이(1920×1080 기준). 방은 이 아래에 담긴다.")]
    [Range(0f, 400f)] public float hideoutTopBand = 180f;

    [Header("은신처 화면 — 도크")]
    [Tooltip("시설 UI 도크 폭(1920 기준). 넓힐수록 UI가 크고 방이 작아진다. 카메라가 자동으로 따라 물러난다.")]
    [Range(400f, 1400f)] public float hideoutDockWidth = 840f;
    [Tooltip("시설 UI 도크 높이(1080 기준).")]
    [Range(400f, 1040f)] public float hideoutDockHeight = 960f;
    [Tooltip("도크와 화면 오른쪽 사이 여백.")]
    [Range(0f, 120f)] public float hideoutDockMargin = 32f;
    [Tooltip("입양한 패널을 줄일 때의 하한. 이보다 더 줄여야 하면 스크롤을 붙인다. 작을수록 글씨가 작아진다.")]
    [Range(0.5f, 1f)] public float hideoutDockMinScale = 0.72f;
}
