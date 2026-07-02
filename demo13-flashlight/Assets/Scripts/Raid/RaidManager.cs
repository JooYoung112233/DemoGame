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
        if (GameTuning.Instance != null) raidDuration = GameTuning.Instance.raidDuration;
        remainingTime = raidDuration;
        raidStartTime = Time.time;
        raidActive = true;
        raidEnded = false;
        PendingResult = true;   // 레이드 시작 → 귀환 시 정산 표시 대상
        TryBindPlayerHealth();
        SaveCheckpoints.Instance?.RaidStarted();   // 레이드 시작 = 복귀 기준점 커밋
        Debug.Log($"[RaidManager] 레이드 시작! 제한시간: {raidDuration}초");
    }

    void OnDestroy()
    {
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
            int lost = ApplyItemLoss(inventory, deathLossRate);   // 가방+주머니 내용물(보안 제외)
            // 가방 아이템 자체도 손실 (내용물 비운 뒤 해제 → 스태시로 새지 않음)
            var equip = inventory.GetComponent<PlayerEquipment>();
            if (equip != null) equip.Unequip(EquipSlot.Backpack);
            Debug.Log($"[RaidManager] 사망 아이템 손실: {lost}개 + 가방");
        }

        // 사망 연출 (붉은 플래시 + 셰이크)
        if (ScreenEffectManager.Instance != null)
        {
            ScreenEffectManager.Instance.Flash(new Color(0.7f, 0.05f, 0.05f), 0.8f);
            ScreenEffectManager.Instance.ScreenShake(0.3f, 0.5f);
        }
        ToastManager.Show("사망 — 가방을 잃었다", ToastManager.ToastType.Warning, 3f);

        // 안전가옥에서 부활 (풀회복)
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.GetComponent<Health>()?.FullHeal();
            player.GetComponent<PlayerMedicalSystem>()?.HealAll();
        }

        // 사망 페널티 확정 커밋(부활 회복 후) — 미커밋이면 강제종료→재로드로 가방 손실 회피 가능
        SaveCheckpoints.Instance?.RaidEnded();

        // 강제 귀환
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(deathExitScene, deathSpawnPointId);
    }

    /// <summary>인벤토리에서 일부 아이템 랜덤 손실. 가방+주머니 대상, **보안 컨테이너는 면제(타르코프식)**.</summary>
    int ApplyItemLoss(PlayerInventory inventory, float lossRate)
    {
        int lostCount = 0;
        lostCount += LoseFromGrid(inventory.Grid, lossRate);          // 가방 내용물
        lostCount += LoseFromGrid(inventory.PocketsGrid, lossRate);   // 주머니 내용물
        // inventory.SecureGrid = 보존(손실 면제)
        return lostCount;
    }

    static int LoseFromGrid(InventoryGrid grid, float lossRate)
    {
        if (grid == null) return 0;
        var items = grid.GetAll();
        int n = 0;
        for (int i = items.Count - 1; i >= 0; i--)
            if (Random.value < lossRate) { grid.Remove(items[i]); n++; }
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
        if (!enableTimer || raidEnded) return;

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
