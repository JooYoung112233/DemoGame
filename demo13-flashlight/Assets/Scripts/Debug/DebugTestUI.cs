using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 디버그 테스트 UI.
/// F1키로 토글. 탭 구분으로 여러 시스템 테스트 가능.
/// 인게임에서만 사용 (빌드 시 제거 또는 #if UNITY_EDITOR).
/// </summary>
public class DebugTestUI : MonoBehaviour
{
    static DebugTestUI instance;

    [SerializeField] KeyCode toggleKey = KeyCode.F1;

    [Header("치료 아이템 (테스트용)")]
    [SerializeField] MedicalItemData[] testMedicalItems;

    bool isOpen;
    // 맵툴 씬에서는 게임용 디버그 UI를 띄우지 않는다 (맵툴 자체 F1 도움말과 충돌 방지)
    bool inMapTool;
    int currentTab;
    string[] tabNames = { "의료", "전투", "아이템", "씬" };

    // 레퍼런스
    PlayerMedicalSystem medical;
    Health health;
    PlayerController player;
    FlashlightController flashlight;

    // 의료 탭 - 치료 선택
    int selectedHealPart = 0;
    int selectedHealItem = 0;

    // 드래그 이동
    Rect windowRect;
    bool windowRectInit;

    GUIStyle tabActiveStyle;
    GUIStyle tabInactiveStyle;
    GUIStyle headerStyle;
    GUIStyle btnStyle;
    GUIStyle labelStyle;
    Texture2D bgTex;
    Texture2D tabActiveTex;
    Texture2D tabInactiveTex;
    bool stylesInit;

    Vector2 scrollPos;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        if (FindFirstObjectByType<DebugTestUI>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("[DebugTestUI]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DebugTestUI>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshMapToolState();
    }

    void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RefreshMapToolState();

    void RefreshMapToolState()
    {
        inMapTool = false; // 맵빌더 제거됨
        if (inMapTool) isOpen = false;
    }

    void Start()
    {
        FindPlayer();

        // 치료 아이템 SO 자동 로드 (Assets/Resources/MedicalItems/)
        if (testMedicalItems == null || testMedicalItems.Length == 0)
            testMedicalItems = Resources.LoadAll<MedicalItemData>("MedicalItems");
    }

    void FindPlayer()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            medical = playerGO.GetComponent<PlayerMedicalSystem>();
            health = playerGO.GetComponent<Health>();
            player = playerGO.GetComponent<PlayerController>();
            flashlight = playerGO.GetComponentInChildren<FlashlightController>();
        }
    }

    void Update()
    {
        // 맵툴 씬에서는 비활성 (맵툴 F1 도움말이 대신 뜬다)
        if (inMapTool) return;

        // Player 재탐색 (씬 전환 후)
        if (player == null) FindPlayer();

        if (Input.GetKeyDown(toggleKey))
            isOpen = !isOpen;
    }

    void OnGUI()
    {
        if (inMapTool || !isOpen) return;

        InitStyles();

        float panelW = 400f;
        float panelH = 450f;

        // 초기 위치: 화면 중앙
        if (!windowRectInit)
        {
            windowRect = new Rect((Screen.width - panelW) * 0.5f, (Screen.height - panelH) * 0.5f, panelW, panelH);
            windowRectInit = true;
        }

        // 드래그 가능한 윈도우
        windowRect = GUI.Window(9999, windowRect, DrawWindow, "", GUIStyle.none);

        // 화면 밖으로 나가지 않도록 클램프
        windowRect.x = Mathf.Clamp(windowRect.x, -panelW + 50, Screen.width - 50);
        windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - 50);
    }

    void DrawWindow(int windowID)
    {
        float panelW = windowRect.width;
        float panelH = windowRect.height;

        // 배경
        GUI.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
        GUI.DrawTexture(new Rect(0, 0, panelW, panelH), bgTex);
        GUI.color = Color.white;

        // 제목 바 (드래그 영역)
        GUI.Label(new Rect(10, 5, panelW - 20, 25), "[ DEBUG TEST UI ]  (드래그로 이동 / H: 닫기)", headerStyle);
        GUI.DragWindow(new Rect(0, 0, panelW, 28));

        // 탭 버튼
        float tabY = 30;
        float tabW = panelW / tabNames.Length;
        for (int i = 0; i < tabNames.Length; i++)
        {
            GUIStyle style = (i == currentTab) ? tabActiveStyle : tabInactiveStyle;
            if (GUI.Button(new Rect(tabW * i, tabY, tabW, 28), tabNames[i], style))
                currentTab = i;
        }

        // 콘텐츠 영역
        float contentY = tabY + 35;
        float contentH = panelH - 70;
        Rect contentRect = new Rect(10, contentY, panelW - 20, contentH);

        GUILayout.BeginArea(contentRect);
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        switch (currentTab)
        {
            case 0: DrawMedicalTab(); break;
            case 1: DrawCombatTab(); break;
            case 2: DrawItemTab(); break;
            case 3: DrawSceneTab(); break;
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    #region 탭: 의료

    void DrawMedicalTab()
    {
        if (medical == null)
        {
            GUILayout.Label("PlayerMedicalSystem을 찾을 수 없음", labelStyle);
            return;
        }

        GUILayout.Label("── 부상 추가 ──", headerStyle);
        GUILayout.Space(5);

        GUILayout.Label("부위 선택 후 부상 적용:", labelStyle);
        GUILayout.Space(3);

        // 부위별 버튼
        string[] partNames = { "머리", "몸통", "양팔", "왼다리", "오른다리" };
        BodyPartType[] parts = { BodyPartType.Head, BodyPartType.Torso, BodyPartType.Arms, BodyPartType.LeftLeg, BodyPartType.RightLeg };

        for (int i = 0; i < parts.Length; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(partNames[i], labelStyle, GUILayout.Width(60));

            if (GUILayout.Button("출혈", btnStyle, GUILayout.Width(55)))
                medical.InflictInjury(parts[i], InjuryType.Bleeding, 0.7f);
            if (GUILayout.Button("골절", btnStyle, GUILayout.Width(55)))
                medical.InflictInjury(parts[i], InjuryType.Fracture, 0.8f);
            if (GUILayout.Button("통증", btnStyle, GUILayout.Width(55)))
                medical.InflictInjury(parts[i], InjuryType.Pain, 0.5f);

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        GUILayout.Label("── 일괄 처리 ──", headerStyle);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("전체 치료 (침대)", btnStyle, GUILayout.Height(30)))
            medical.HealAll();
        if (GUILayout.Button("랜덤 부상 x3", btnStyle, GUILayout.Height(30)))
        {
            medical.InflictRandomInjury(InjuryType.Bleeding, Random.Range(0.3f, 0.9f));
            medical.InflictRandomInjury(InjuryType.Pain, Random.Range(0.3f, 0.7f));
            medical.InflictRandomInjury(InjuryType.Fracture, Random.Range(0.5f, 1f));
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("── 치료 아이템 사용 ──", headerStyle);
        GUILayout.Space(5);

        if (testMedicalItems == null || testMedicalItems.Length == 0)
        {
            GUILayout.Label("testMedicalItems에 SO를 할당하세요", labelStyle);
        }
        else
        {
            // 아이템 선택
            GUILayout.BeginHorizontal();
            GUILayout.Label("아이템:", labelStyle, GUILayout.Width(50));
            for (int i = 0; i < testMedicalItems.Length; i++)
            {
                if (testMedicalItems[i] == null) continue;
                GUIStyle style = (i == selectedHealItem) ? tabActiveStyle : btnStyle;
                if (GUILayout.Button(testMedicalItems[i].displayName, style, GUILayout.Height(22)))
                    selectedHealItem = i;
            }
            GUILayout.EndHorizontal();

            // 부위 선택
            string[] healPartNames = { "머리", "몸통", "양팔", "왼다리", "오른다리" };
            GUILayout.BeginHorizontal();
            GUILayout.Label("부위:", labelStyle, GUILayout.Width(50));
            for (int i = 0; i < healPartNames.Length; i++)
            {
                GUIStyle style = (i == selectedHealPart) ? tabActiveStyle : btnStyle;
                if (GUILayout.Button(healPartNames[i], style, GUILayout.Height(22)))
                    selectedHealPart = i;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // 치료 실행
            MedicalItemData selectedItem = testMedicalItems[Mathf.Clamp(selectedHealItem, 0, testMedicalItems.Length - 1)];
            BodyPartType targetPart = (BodyPartType)selectedHealPart;
            BodyPart bp = medical.GetPart(targetPart);

            // 현재 선택 부위의 부상 표시
            if (bp != null && bp.IsInjured)
            {
                string injuries = "";
                for (int i = 0; i < bp.injuries.Count; i++)
                    injuries += $"{bp.injuries[i].type}({bp.injuries[i].severity:F1}) ";
                GUILayout.Label($"  → {healPartNames[selectedHealPart]} 부상: {injuries}", labelStyle);
            }
            else
            {
                GUILayout.Label($"  → {healPartNames[selectedHealPart]}: 정상 (부상 없음)", labelStyle);
            }

            GUILayout.Space(3);

            if (medical.IsHealing)
            {
                GUILayout.Label($"치료 진행 중... {medical.HealProgress * 100:F0}%", labelStyle);
                if (GUILayout.Button("치료 취소", btnStyle, GUILayout.Height(25)))
                    medical.CancelHealing();
            }
            else
            {
                // 치료 가능한 부상 찾아서 버튼 표시
                if (bp != null && bp.IsInjured && selectedItem != null)
                {
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < bp.injuries.Count; i++)
                    {
                        InjuryType injType = bp.injuries[i].type;
                        bool canTreat = selectedItem.CanTreat(injType);
                        GUI.enabled = canTreat;
                        if (GUILayout.Button($"치료: {injType}", btnStyle, GUILayout.Height(28)))
                        {
                            medical.StartHealing(targetPart, injType, selectedItem);
                        }
                        GUI.enabled = true;
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("── 현재 디버프 ──", headerStyle);
        GUILayout.Label($"이동속도: {medical.MoveSpeedMultiplier * 100:F0}%", labelStyle);
        GUILayout.Label($"공격속도: {medical.AttackSpeedMultiplier * 100:F0}%", labelStyle);
        GUILayout.Label($"스태미너회복: {medical.StaminaRegenMultiplier * 100:F0}%", labelStyle);
    }

    #endregion

    #region 탭: 전투

    void DrawCombatTab()
    {
        if (health == null || player == null)
        {
            GUILayout.Label("Player를 찾을 수 없음", labelStyle);
            return;
        }

        GUILayout.Label("── HP 조작 ──", headerStyle);
        GUILayout.Space(5);

        GUILayout.Label($"HP: {health.CurrentHp:F0} / {health.MaxHp:F0}", labelStyle);
        GUILayout.Space(3);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("데미지 10", btnStyle)) health.TakeDamage(10f);
        if (GUILayout.Button("데미지 30", btnStyle)) health.TakeDamage(30f);
        if (GUILayout.Button("데미지 50", btnStyle)) health.TakeDamage(50f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("힐 20", btnStyle)) health.Heal(20f);
        if (GUILayout.Button("힐 50", btnStyle)) health.Heal(50f);
        if (GUILayout.Button("풀힐", btnStyle)) health.Heal(health.MaxHp);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("── 상태 ──", headerStyle);
        GUILayout.Label($"전투 상태: {player.CurrentState}", labelStyle);
        GUILayout.Label($"스태미너: {player.StaminaCurrent:F0} / {player.StaminaMax:F0}", labelStyle);
        GUILayout.Label($"전투 활성화: {player.CombatEnabled}", labelStyle);

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("전투 ON", btnStyle)) player.CombatEnabled = true;
        if (GUILayout.Button("전투 OFF", btnStyle)) player.CombatEnabled = false;
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("── 손전등 / 배터리 ──", headerStyle);

        if (flashlight != null)
        {
            GUILayout.Label($"배터리: {flashlight.BatteryPercent * 100:F0}%  |  {(flashlight.IsOn ? "ON" : "OFF")}", labelStyle);
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("충전 25%", btnStyle)) flashlight.AddBattery(flashlight.BatteryPercent > 0 ? 22.5f : 22.5f);
            if (GUILayout.Button("충전 50%", btnStyle)) flashlight.AddBattery(45f);
            if (GUILayout.Button("풀 충전", btnStyle)) flashlight.AddBattery(9999f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("소모 25%", btnStyle)) flashlight.AddBattery(-22.5f);
            if (GUILayout.Button("소모 50%", btnStyle)) flashlight.AddBattery(-45f);
            if (GUILayout.Button("방전", btnStyle)) flashlight.AddBattery(-9999f);
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("FlashlightController를 찾을 수 없음", labelStyle);
        }
    }

    #endregion

    #region 탭: 아이템

    PlayerInventory inventory;

    void DrawItemTab()
    {
        if (inventory == null && player != null)
            inventory = player.GetComponent<PlayerInventory>();

        if (inventory == null)
        {
            GUILayout.Label("PlayerInventory를 찾을 수 없음", labelStyle);
            return;
        }

        GUILayout.Label("── 인벤토리 상태 ──", headerStyle);
        GUILayout.Label($"아이템: {inventory.Grid.ItemCount}개  |  무게: {inventory.CurrentWeight:F1} / {inventory.MaxWeight:F0} kg", labelStyle);
        GUILayout.Space(5);

        // 격자 내 아이템 목록
        var items = inventory.Grid.GetAll();
        for (int i = 0; i < items.Count; i++)
        {
            GUILayout.BeginHorizontal();
            string durInfo = items[i].item.HasDurability
                ? $" ({items[i].item.durability:F0}/{items[i].item.data.maxDurability:F0})"
                : "";
            GUILayout.Label($"  [{items[i].gridX},{items[i].gridY}] {items[i].item.DisplayName}{durInfo}", labelStyle, GUILayout.Width(250));
            if (items[i].item.data != null && items[i].item.data.isUsable)
            {
                if (GUILayout.Button("사용", btnStyle, GUILayout.Width(50)))
                    inventory.UseItem(items[i]);
            }
            if (GUILayout.Button("버리기", btnStyle, GUILayout.Width(55)))
                inventory.DropItem(items[i]);
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        GUILayout.Label("── 아이템 지급 ──", headerStyle);
        GUILayout.Space(5);

        var allItems = ItemDatabase.GetAll();
        if (allItems.Length == 0)
        {
            GUILayout.Label("ItemDatabase에 아이템이 없음 (Resources/Items/ 확인)", labelStyle);
        }
        else
        {
            for (int i = 0; i < allItems.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{allItems[i].displayName} ({allItems[i].gridWidth}x{allItems[i].gridHeight})", labelStyle, GUILayout.Width(200));
                if (GUILayout.Button("+1", btnStyle, GUILayout.Width(40)))
                {
                    var item = new ItemInstance(allItems[i], 1);
                    if (!inventory.TryPickup(item))
                        Debug.Log("[Debug] 인벤토리 공간 부족");
                }
                if (allItems[i].maxStack > 1)
                {
                    if (GUILayout.Button($"+{allItems[i].maxStack}", btnStyle, GUILayout.Width(50)))
                    {
                        var item = new ItemInstance(allItems[i], allItems[i].maxStack);
                        if (!inventory.TryPickup(item))
                            Debug.Log("[Debug] 인벤토리 공간 부족");
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.Space(10);
        if (GUILayout.Button("인벤토리 전체 비우기", btnStyle, GUILayout.Height(28)))
            inventory.Grid.Clear();

        GUILayout.Space(10);
        GUILayout.Label("── 제작 UI (CraftingUI) ──", headerStyle);
        if (UIManager.Instance != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("의료대", btnStyle, GUILayout.Height(25)))
                UIManager.Instance.ShowCrafting(CraftingStation.MedicalBench);
            if (GUILayout.Button("조리대", btnStyle, GUILayout.Height(25)))
                UIManager.Instance.ShowCrafting(CraftingStation.CookingBench);
            if (GUILayout.Button("작업대", btnStyle, GUILayout.Height(25)))
                UIManager.Instance.ShowCrafting(CraftingStation.Workbench);
            GUILayout.EndHorizontal();
        }
        else
            GUILayout.Label("UIManager 없음", labelStyle);

        GUILayout.Space(10);
        GUILayout.Label("── 지역 루트 (RegionLootCatalog) ──", headerStyle);
        string activeRegion = RegionLootCatalog.GetActiveRegionId();
        bool regionNight = RegionLootCatalog.IsNightInRegion(activeRegion);
        GUILayout.Label($"활성 지역: {activeRegion}  |  {(regionNight ? "밤" : "낮")}", labelStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("지역 상자 루트", btnStyle, GUILayout.Height(25)))
            DropRegionLoot(RegionLootTier.ContainerDay);
        if (GUILayout.Button("지역 바닥 루트", btnStyle, GUILayout.Height(25)))
            DropRegionLoot(RegionLootTier.GroundDay);
        GUILayout.EndHorizontal();
        var regionOnly = ItemDatabase.GetByPrimaryRegion(activeRegion);
        if (regionOnly.Count > 0)
            GUILayout.Label($"지역 전용 SO: {regionOnly.Count}종", labelStyle);

        GUILayout.Space(10);
        GUILayout.Label("── 월드 드롭 테스트 ──", headerStyle);
        GUILayout.Space(3);

        if (allItems.Length > 0)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("앞에 아이템 드롭 (랜덤)", btnStyle, GUILayout.Height(25)))
            {
                var data = allItems[Random.Range(0, allItems.Length)];
                var item = new ItemInstance(data, Random.Range(1, Mathf.Min(data.maxStack, 3) + 1));
                Vector3 dropPos = player.transform.position + player.transform.forward * 1.5f;
                WorldItem.Drop(item, dropPos);
            }
            if (GUILayout.Button("주변 5개 드롭", btnStyle, GUILayout.Height(25)))
            {
                for (int i = 0; i < 5; i++)
                {
                    var data = allItems[Random.Range(0, allItems.Length)];
                    var item = new ItemInstance(data, Random.Range(1, Mathf.Min(data.maxStack, 3) + 1));
                    Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
                    WorldItem.Drop(item, player.transform.position + offset);
                }
            }
            GUILayout.EndHorizontal();
        }
    }

    void DropRegionLoot(RegionLootTier tier)
    {
        if (player == null) return;
        var items = RegionLootCatalog.RollForActiveRegion(tier);
        Vector3 basePos = player.transform.position + player.transform.forward * 1.5f;
        for (int i = 0; i < items.Length; i++)
        {
            Vector3 pos = basePos + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f) + i * 0.2f);
            WorldItem.Drop(items[i], pos);
        }
        Debug.Log($"[Debug] 지역 루트 {tier} → {items.Length}개 드롭");
    }

    #endregion

    #region 탭: 씬

    void DrawSceneTab()
    {
        GUILayout.Label("── 씬 전환 ──", headerStyle);
        GUILayout.Space(5);

        if (GUILayout.Button("→ Safehouse", btnStyle, GUILayout.Height(28)))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionTo("Safehouse", "default");
        }
        if (GUILayout.Button("→ InGameScene", btnStyle, GUILayout.Height(28)))
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionTo("InGameScene", "default");
        }

        GUILayout.Space(10);
        GUILayout.Label("── 시간 ──", headerStyle);
        GUILayout.Label($"Time.timeScale: {Time.timeScale:F2}", labelStyle);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("x0.5", btnStyle)) Time.timeScale = 0.5f;
        if (GUILayout.Button("x1", btnStyle)) Time.timeScale = 1f;
        if (GUILayout.Button("x2", btnStyle)) Time.timeScale = 2f;
        if (GUILayout.Button("x5", btnStyle)) Time.timeScale = 5f;
        GUILayout.EndHorizontal();
    }

    #endregion

    #region 스타일 초기화

    void InitStyles()
    {
        if (stylesInit) return;

        bgTex = MakeTex(Color.white);
        tabActiveTex = MakeTex(new Color(0.2f, 0.4f, 0.6f));
        tabInactiveTex = MakeTex(new Color(0.15f, 0.15f, 0.2f));

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 14;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 13;
        labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.95f);

        btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 12;
        btnStyle.fontStyle = FontStyle.Bold;

        tabActiveStyle = new GUIStyle(GUI.skin.button);
        tabActiveStyle.fontSize = 13;
        tabActiveStyle.fontStyle = FontStyle.Bold;
        tabActiveStyle.normal.background = tabActiveTex;
        tabActiveStyle.normal.textColor = Color.white;

        tabInactiveStyle = new GUIStyle(GUI.skin.button);
        tabInactiveStyle.fontSize = 13;
        tabInactiveStyle.normal.background = tabInactiveTex;
        tabInactiveStyle.normal.textColor = new Color(0.6f, 0.6f, 0.7f);

        stylesInit = true;
    }

    Texture2D MakeTex(Color col)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }

    #endregion
}
