using UnityEngine;

/// <summary>
/// 디버그 테스트 UI.
/// H키로 토글. 탭 구분으로 여러 시스템 테스트 가능.
/// 인게임에서만 사용 (빌드 시 제거 또는 #if UNITY_EDITOR).
/// </summary>
public class DebugTestUI : MonoBehaviour
{
    static DebugTestUI instance;

    [SerializeField] KeyCode toggleKey = KeyCode.H;

    [Header("치료 아이템 (테스트용)")]
    [SerializeField] MedicalItemData[] testMedicalItems;

    bool isOpen;
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
        if (FindObjectOfType<DebugTestUI>(true) != null) return;

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
        // Player 재탐색 (씬 전환 후)
        if (player == null) FindPlayer();

        if (Input.GetKeyDown(toggleKey))
            isOpen = !isOpen;
    }

    void OnGUI()
    {
        if (!isOpen) return;

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

    void DrawItemTab()
    {
        GUILayout.Label("── 아이템 (미구현) ──", headerStyle);
        GUILayout.Space(5);
        GUILayout.Label("인벤토리 시스템 구현 후 활성화", labelStyle);
        GUILayout.Space(10);
        GUILayout.Label("예정 기능:", labelStyle);
        GUILayout.Label("• 아이템 지급", labelStyle);
        GUILayout.Label("• 인벤토리 초기화", labelStyle);
        GUILayout.Label("• 치료 아이템 사용 테스트", labelStyle);
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
