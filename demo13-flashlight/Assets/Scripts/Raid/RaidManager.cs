using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 레이드 매니저.
/// InGameScene에 배치. 레이드 타이머 + HUD + 시간초과 처리.
/// 레이드 중 획득한 아이템 추적 (정산용).
/// </summary>
public class RaidManager : MonoBehaviour
{
    public static RaidManager Instance { get; private set; }

    /// <summary>레이드가 한 번이라도 시작됐고 아직 정산 안 했는지(귀환 정산 UI 트리거용).
    /// RaidManager.Start에서 true, RaidResultUI가 정산 표시 시 false로 소비. static이라 씬 언로드 후에도 유지.
    /// (게임 부팅 직후 안전가옥 진입에서 정산창이 잘못 뜨는 것 방지)</summary>
    public static bool PendingResult;

    [Header("Raid Timer")]
    [Tooltip("레이드 제한 시간(초). 0이면 무제한")]
    [SerializeField] float raidDuration = 1200f; // 20분
    [SerializeField] bool enableTimer = true;

    [Header("Time Over")]
    [Tooltip("시간초과 시 자동 탈출할 씬")]
    [SerializeField] string failExitScene = "Safehouse";
    [SerializeField] string failSpawnPointId = "raid_return";   // 안전구역 스폰 2개로 통합(귀환 1곳)
    [Tooltip("시간초과 시 아이템 손실 비율 (0=손실없음, 1=전부)")]
    [SerializeField] float timeOverLossRate = 0.5f;

    [Header("Death (사망 페널티)")]
    [Tooltip("사망 시 가방 아이템 손실 비율 (1=전부)")]
    [SerializeField] float deathLossRate = 1f;
    [Tooltip("사망 후 복귀할 씬")]
    [SerializeField] string deathExitScene = "Safehouse";
    [SerializeField] string deathSpawnPointId = "raid_return";  // 안전구역 스폰 2개로 통합(귀환 1곳)

    [Header("HUD Style")]
    [SerializeField] int timerFontSize = 22;
    [SerializeField] Color timerNormalColor = new Color(0.9f, 0.95f, 1f);
    [SerializeField] Color timerWarningColor = new Color(1f, 0.6f, 0.1f);
    [SerializeField] Color timerCriticalColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] float warningThreshold = 120f;  // 2분 이하
    [SerializeField] float criticalThreshold = 30f;   // 30초 이하

    // ── 런타임 ──
    float remainingTime;
    bool raidActive;
    bool raidEnded;
    Health _playerHealth;
    bool _deathHandled;

    // 획득 아이템 추적
    List<ItemInstance> lootedItems = new List<ItemInstance>();
    float raidStartTime;

    // HUD
    GUIStyle timerStyle;
    GUIStyle timerBgStyle;
    Texture2D bgTex;
    bool stylesInit;

    public float RemainingTime => remainingTime;
    public float ElapsedTime => Time.time - raidStartTime;
    public bool IsRaidActive => raidActive;
    public List<ItemInstance> LootedItems => lootedItems;
    public float RaidDuration => raidDuration;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // 레이드 씬에서 바로 Play한 경우 — 씬 로드 이벤트가 이미 지나갔으므로 여기서 시작 판정.
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (IsRaidScene(SceneManager.GetSceneAt(i).name)) { BeginRaid(); break; }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsRaidScene(scene.name)) BeginRaid();
        else if (scene.name == failExitScene) StopRaid();   // 안전가옥 복귀 = 레이드 종료
    }

    /// <summary>이 씬이 레이드 맵인가(= 월드 지역 카탈로그에 등재된 씬).
    /// 건물 내부 씬은 카탈로그에 없으므로 내부를 드나들어도 레이드가 재시작되지 않는다.</summary>
    public static bool IsRaidScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        for (int i = 0; i < WorldRegionCatalog.All.Length; i++)
            if (WorldRegionCatalog.All[i].sceneName == sceneName) return true;
        return false;
    }

    /// <summary>레이드 시작. 이미 진행 중이면 무시 — 내부 씬 왕복으로 타이머·전리품이 리셋되지 않게.</summary>
    public void BeginRaid()
    {
        if (raidActive) return;

        if (GameTuning.Instance != null) raidDuration = GameTuning.Instance.raidDuration;
        remainingTime = raidDuration;
        raidStartTime = Time.time;
        raidActive = true;
        raidEnded = false;
        _deathHandled = false;
        lootedItems.Clear();
        killXp = 0;
        PendingResult = true;   // 레이드 시작 → 귀환 시 정산 표시 대상
        LastSettlement = null;  // 이전 레이드 정산 레코드 폐기
        startInvValue = CurrentInventoryValue();   // 루팅 XP 기준점 (플레이어는 영속 PlayerRig라 이 시점 존재)
        TryBindPlayerHealth();
        SaveCheckpoints.Instance?.RaidStarted();   // 레이드 시작 = 복귀 기준점 커밋
        Debug.Log($"[RaidManager] 레이드 시작! 제한시간: {raidDuration}초");
    }

    /// <summary>레이드 상태 해제(타이머·HUD 정지). 정산은 탈출/사망/시간초과에서 이미 처리됨.</summary>
    void StopRaid()
    {
        raidActive = false;
        raidEnded = true;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_playerHealth != null) _playerHealth.OnDeath -= OnPlayerDeath;
        if (Instance == this)
            Instance = null;
    }

    /// <summary>플레이어 Health 사망 이벤트 구독 (지연 바인딩 대응).</summary>
    void TryBindPlayerHealth()
    {
        if (_playerHealth != null) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        _playerHealth = player.GetComponent<Health>();
        if (_playerHealth != null)
        {
            _playerHealth.OnDeath -= OnPlayerDeath;
            _playerHealth.OnDeath += OnPlayerDeath;
        }
    }

    void Update()
    {
        if (_playerHealth == null) TryBindPlayerHealth();

        if (!raidActive || !enableTimer || raidEnded) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            OnTimeOver();
        }
    }

    // ════════════════════════════════════════
    //  아이템 추적
    // ════════════════════════════════════════

    /// <summary>아이템 획득 시 호출 (정산용 기록)</summary>
    public void TrackLoot(ItemInstance item)
    {
        if (item == null || item.data == null) return;
        lootedItems.Add(item);
        Debug.Log($"[RaidManager] 루트 기록: {item.DisplayName}");
    }

    // ════════════════════════════════════════
    //  경험치 (traits.md §2 — 킬+탈출+루팅, 실패 시 킬×배율)
    // ════════════════════════════════════════

    int killXp;          // 이번 레이드 킬 XP 누적 (EnemyController.OnDeath가 호출)
    int startInvValue;   // 레이드 시작 시 소지품 가치 스냅샷 (루팅 XP = 종료-시작 차액)

    /// <summary>레이드 정산 레코드 — RaidResultUI 표시용. static이라 씬 전환(RaidManager 파괴) 후에도 생존.
    /// SettleXp가 채우고, 다음 레이드 Start에서 초기화.</summary>
    public class RaidSettlement
    {
        public bool success;
        public float survivalTime;
        public int killXp;
        public int extractBonus;
        public int lootValue;      // 소지품 가치 순증가분(스크랩 기준)
        public int lootXp;
        public int totalXp;
        public int levelBefore;
        public int levelAfter;
        public int xpAfter;        // 레벨업 처리 후 현재 레벨 내 XP
        public int xpToNextAfter;
    }
    public static RaidSettlement LastSettlement;

    /// <summary>적 처치 시 킬 XP 누적 (유닛별 UnitStatData.expReward).</summary>
    public void TrackKillXp(int exp)
    {
        if (!raidActive || raidEnded || exp <= 0) return;
        killXp += exp;
    }

    /// <summary>소지품 총 가치(sellPrice×스택) = 가방+주머니+보안+장비.
    /// 루팅 XP는 lootedItems가 아니라 이 값의 시작/종료 **차액**으로 계산 —
    /// 픽업 경로(E키/클러스터/컨테이너 드래그)와 무관하게 전부 집계되고,
    /// 레이드에 들고 들어간 장비는 차액에서 자연 상쇄된다.</summary>
    int CurrentInventoryValue()
    {
        int v = 0;
        var inv = FindPlayerInventory();
        if (inv != null)
        {
            v += GridValue(inv.Grid);
            v += GridValue(inv.PocketsGrid);
            v += GridValue(inv.SecureGrid);
            var equip = inv.GetComponent<PlayerEquipment>();
            if (equip != null)
                foreach (var kv in equip.GetAllEquipped())
                    if (kv.Value != null) v += kv.Value.sellPrice;
        }
        return v;
    }

    static int GridValue(InventoryGrid g)
    {
        if (g == null) return 0;
        int v = 0;
        foreach (var p in g.GetAll())
            if (p?.item?.data != null) v += p.item.data.sellPrice * Mathf.Max(1, p.item.stackCount);
        return v;
    }

    /// <summary>레이드 종료 XP 정산 → PlayerProgress 지급 + 정산 레코드(LastSettlement) 기록.
    /// RaidEnded()(세이브 커밋) **전**에 호출할 것.</summary>
    void SettleXp(bool success)
    {
        var t = GameTuning.Instance;
        int total;
        int lootValue = 0;
        int extractBonus = 0;
        int lootXp = 0;
        if (success)
        {
            lootValue = Mathf.Max(0, CurrentInventoryValue() - startInvValue);   // 순증가분만
            extractBonus = t != null ? t.xpExtractBonus : 100;
            float perLoot = t != null ? t.xpPerLootValue : 0.02f;
            lootXp = Mathf.RoundToInt(lootValue * perLoot);
            total = killXp + extractBonus + lootXp;
        }
        else
        {
            // 실패(사망/시간초과) = 킬 XP × 배율만 — 탈출·루팅 보너스 없음("죽어도 배운다").
            float failMult = t != null ? t.xpFailMult : 0.5f;
            total = Mathf.RoundToInt(killXp * failMult);
        }

        var prog = PlayerProgress.Instance;
        int levelBefore = prog.Level;

        if (total > 0)
        {
            ToastManager.Show($"경험치 +{total}", ToastManager.ToastType.Info, 2.5f);   // 레벨업 토스트보다 먼저
            prog.GrantXp(total);
        }

        // 정산 레코드 — RaidResultUI가 귀환 후 표시 (static: 씬 전환 생존)
        LastSettlement = new RaidSettlement
        {
            success = success,
            survivalTime = Time.time - raidStartTime,
            killXp = killXp,
            extractBonus = extractBonus,
            lootValue = lootValue,
            lootXp = lootXp,
            totalXp = total,
            levelBefore = levelBefore,
            levelAfter = prog.Level,
            xpAfter = prog.Xp,
            xpToNextAfter = prog.XpToNext,
        };

        Debug.Log($"[RaidManager] XP 정산({(success ? "성공" : "실패")}): 킬 {killXp} + 루팅가치 {lootValue} → 총 {total}");
    }

    // ════════════════════════════════════════
    //  탈출 / 시간초과
    // ════════════════════════════════════════

    /// <summary>정상 탈출 시 호출</summary>
    public void OnExtractSuccess()
    {
        if (raidEnded) return;
        raidEnded = true;
        raidActive = false;

        float elapsed = Time.time - raidStartTime;
        Debug.Log($"[RaidManager] 탈출 성공! 생존시간: {elapsed:F0}초, 획득 아이템: {lootedItems.Count}개");

        SettleXp(true);   // XP 정산(레벨업/PP 포함) — 커밋 전에 확정

        // 고철 정산(docs/economy.md §적 고철 드랍) — 들고 나온 고철 화폐를 ◈로.
        //   XP(루팅가치)를 먼저 잰 **뒤에** 뺀다 — 순서가 바뀌면 고철이 루팅 XP에서 빠진다. 세이브 커밋 전에.
        //   현금(cash)은 퀘스트·이벤트 전용이라 정산하지 않는다(2026-09-11 재화 역할).
        var scrapInv = TopDownPlayer.Instance != null ? TopDownPlayer.Instance.GetComponent<PlayerInventory>() : null;
        int scrap = ScrapWallet.SettleFromInventory(scrapInv);
        if (scrap > 0) Debug.Log($"[RaidManager] 고철 정산 ◈{scrap}");

        // 탈출 정산 = 체크포인트 커밋(레이드 종료). 인벤/정산 결과를 디스크에 확정.
        SaveCheckpoints.Instance?.RaidEnded();

        // 스토리 트리거: 탈출 성공 → 플래그 설정
        if (StoryTriggerManager.Instance != null)
        {
            bool wasNight = false;
            if (RegionTimeManager.Instance != null)
            {
                string activeId = RegionTimeManager.Instance.ActiveRegionId;
                if (!string.IsNullOrEmpty(activeId))
                {
                    var rt = RegionTimeManager.Instance.GetRegion(activeId);
                    if (rt != null) wasNight = rt.isNight;
                }
            }
            else
            {
                var dnc = FindFirstObjectByType<DayNightCycle>();
                if (dnc != null) wasNight = dnc.IsNight;
            }

            bool hasRudi = lootedItems.Exists(i =>
                i.data != null && (i.data.itemId == "ruby_shard" || i.data.itemId == "rudi_shard" || i.data.itemId == "rudi"));

            StoryTriggerManager.Instance.OnRaidExtract(wasNight, hasRudi);
        }
    }

    /// <summary>시간초과</summary>
    void OnTimeOver()
    {
        if (raidEnded) return;
        raidEnded = true;
        raidActive = false;

        Debug.Log("[RaidManager] 시간 초과!");

        // 아이템 손실 처리
        if (timeOverLossRate > 0f)
        {
            var inventory = FindPlayerInventory();
            if (inventory != null)
            {
                int lostCount = ApplyItemLoss(inventory, timeOverLossRate);
                Debug.Log($"[RaidManager] 시간초과 아이템 손실: {lostCount}개");
            }
        }

        SettleXp(false);   // 시간초과 = 킬 XP 절반 — 커밋 전에 확정

        // 페널티 확정 커밋 — 미커밋이면 강제종료→재로드로 손실 회피 가능(세이브 스커밍)
        SaveCheckpoints.Instance?.RaidEnded();

        // 강제 귀환
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionTo(failExitScene, failSpawnPointId);
        }
    }

    /// <summary>플레이어 사망 — 레이드 실패. 가방 손실 + 안전가옥에서 부활.</summary>
    void OnPlayerDeath()
    {
        if (raidEnded || _deathHandled) return;
        _deathHandled = true;
        raidEnded = true;
        raidActive = false;

        Debug.Log("[RaidManager] 플레이어 사망 — 레이드 실패");

        // 사망 손실: 가방·주머니 내용물 전부 + 가방 아이템 자체. 보안 컨테이너는 보존(타르코프식).
        var inventory = FindPlayerInventory();
        if (inventory != null)
        {
            // 사망 손실을 삭제 대신 '약탈자 회수' 대기열로 캡처(docs/raid.md — ⑧ 대체).
            var captured = new List<ItemInstance>();
            int lost = ApplyItemLoss(inventory, deathLossRate, captured);   // 가방+주머니 내용물(보안 제외)
            // 가방 아이템 자체도 손실 (내용물 비운 뒤 해제 → 스태시로 새지 않음)
            var equip = inventory.GetComponent<PlayerEquipment>();
            if (equip != null) equip.Unequip(EquipSlot.Backpack);
            ScavengerLoot.Capture(captured);   // 다음 레이드 약탈자에게 분배(한 번의 회수 기회)
            Debug.Log($"[RaidManager] 사망 아이템 손실: {lost}개 + 가방 → 약탈자 회수 대기");
        }

        // 사망 연출 (붉은 플래시 + 셰이크)
        if (ScreenEffectManager.Instance != null)
        {
            ScreenEffectManager.Instance.Flash(new Color(0.7f, 0.05f, 0.05f), 0.8f);
            ScreenEffectManager.Instance.ScreenShake(0.3f, 0.5f);
        }
        ToastManager.Show("사망 — 소지품을 약탈자가 가져갔다. 다음 레이드에서 되찾아라", ToastManager.ToastType.Warning, 3.5f);

        // 안전가옥에서 부활 (풀회복)
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.GetComponent<Health>()?.FullHeal();
        }

        SettleXp(false);   // 사망 = 킬 XP 절반("죽어도 배운다") — 커밋 전에 확정

        // 사망 페널티 확정 커밋(부활 회복 후) — 미커밋이면 강제종료→재로드로 가방 손실 회피 가능
        SaveCheckpoints.Instance?.RaidEnded();

        // 강제 귀환
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(deathExitScene, deathSpawnPointId);
    }

    /// <summary>인벤토리에서 일부 아이템 랜덤 손실. 가방+주머니 대상, **보안 컨테이너는 면제(타르코프식)**.</summary>
    int ApplyItemLoss(PlayerInventory inventory, float lossRate, List<ItemInstance> captured = null)
    {
        int lostCount = 0;
        lostCount += LoseFromGrid(inventory.Grid, lossRate, captured);          // 가방 내용물
        lostCount += LoseFromGrid(inventory.PocketsGrid, lossRate, captured);   // 주머니 내용물
        // inventory.SecureGrid = 보존(손실 면제)
        return lostCount;
    }

    /// <summary>격자에서 lossRate 확률로 아이템 제거. captured != null이면 제거분을 거기 담는다(약탈자 회수용).</summary>
    static int LoseFromGrid(InventoryGrid grid, float lossRate, List<ItemInstance> captured = null)
    {
        if (grid == null) return 0;
        var items = grid.GetAll();
        int n = 0;
        for (int i = items.Count - 1; i >= 0; i--)
            if (Random.value < lossRate)
            {
                if (captured != null && items[i].item != null) captured.Add(items[i].item);
                grid.Remove(items[i]); n++;
            }
        return n;
    }

    PlayerInventory FindPlayerInventory()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<PlayerInventory>() : null;
    }

    // ════════════════════════════════════════
    //  HUD
    // ════════════════════════════════════════

    void OnGUI()
    {
        if (!enableTimer || raidEnded || !raidActive) return;

        InitStyles();

        // 타이머 텍스트
        int min = (int)(remainingTime / 60);
        int sec = (int)(remainingTime % 60);
        string timeStr = $"{min:D2}:{sec:D2}";

        // 색상 결정
        Color col = timerNormalColor;
        if (remainingTime <= criticalThreshold)
        {
            col = timerCriticalColor;
            // 깜빡임
            if (Mathf.Sin(Time.unscaledTime * 6f) > 0f)
                col.a = 0.5f;
        }
        else if (remainingTime <= warningThreshold)
        {
            col = timerWarningColor;
        }

        timerStyle.normal.textColor = col;

        // 위치: 화면 상단 중앙
        float w = 120f;
        float h = 40f;
        float x = (Screen.width - w) * 0.5f;
        float y = 12f;

        // 배경
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(new Rect(x - 10, y - 4, w + 20, h + 8), bgTex);
        GUI.color = Color.white;

        // 텍스트
        GUI.Label(new Rect(x, y, w, h), timeStr, timerStyle);
    }

    void InitStyles()
    {
        if (stylesInit) return;
        stylesInit = true;

        bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, Color.white);
        bgTex.Apply();

        timerStyle = new GUIStyle(GUI.skin.label);
        timerStyle.fontSize = timerFontSize;
        timerStyle.fontStyle = FontStyle.Bold;
        timerStyle.alignment = TextAnchor.MiddleCenter;
        timerStyle.normal.textColor = timerNormalColor;
    }
}
