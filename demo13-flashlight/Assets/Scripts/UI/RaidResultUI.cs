using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 귀환 정산 UI (Canvas/uGUI).
/// UIManager 자식으로 배치.
/// 필드→안전가옥 복귀 시 자동 표시.
/// TODO: 실제 데이터 연동.
/// </summary>
public class RaidResultUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] string safehouseScene = "Safehouse";
    [SerializeField] KeyCode closeKey = KeyCode.Return;

    public bool IsShowing => isShowing;

    bool isShowing;
    float showTimer;

    // 더미 데이터
    string[] dummyItems = { "철 파이프 x2", "붕대 x3", "통조림 x1", "고철 x5", "진통제 x1" };
    int dummyExp = 120;
    int dummyMoney = 45;
    float dummySurvivalTime = 487f;

    // uGUI
    Canvas canvas;
    GameObject panelRoot;
    Image dimBg;
    Text titleText, timeText, itemsText, rewardsText, closeHintText;

    void Awake()
    {
        BuildUI();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == safehouseScene && !string.IsNullOrEmpty(SceneTransitionManager.PendingSpawnPointId))
        {
            Show();
            RefillBattery();
        }
    }

    public void Show()
    {
        isShowing = true;
        showTimer = 0;
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    /// <summary>안전가옥 귀환 시 배터리 자동 충전</summary>
    void RefillBattery()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        var flashlight = player.GetComponentInChildren<FlashlightController>();
        if (flashlight != null)
            flashlight.AddBattery(9999f); // 풀 충전 (maxBattery로 클램프됨)
    }

    public void Hide()
    {
        isShowing = false;
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void Update()
    {
        if (!isShowing) return;

        showTimer += Time.unscaledDeltaTime;

        // 닫기 안내 깜빡임
        if (closeHintText != null)
        {
            float alpha = Mathf.PingPong(Time.unscaledTime * 2f, 1f) * 0.5f + 0.5f;
            closeHintText.color = new Color(0.8f, 0.8f, 0.8f, alpha);
        }

        if (showTimer > 1f && (Input.GetKeyDown(closeKey) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0)))
            Hide();
    }

    void BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("RaidResult_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var canvasRT = canvasGO.GetComponent<RectTransform>();

        // ── 전체 루트 (숨김용) ──
        panelRoot = new GameObject("PanelRoot");
        panelRoot.transform.SetParent(canvasRT, false);
        var rootRT = panelRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        // 어두운 배경
        dimBg = panelRoot.AddComponent<Image>();
        dimBg.color = new Color(0, 0, 0, 0.85f);

        // 중앙 패널
        var panel = new GameObject("CenterPanel");
        panel.transform.SetParent(panelRoot.transform, false);
        var pRT = panel.AddComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.5f, 0.5f);
        pRT.anchorMax = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(440, 400);
        var pImg = panel.AddComponent<Image>();
        pImg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // 제목
        titleText = MakeText(panel.transform, "Title", "── 귀환 정산 ──",
            new Vector2(0, -20), new Vector2(400, 40), 24, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
        titleText.fontStyle = FontStyle.Bold;

        // 생존 시간
        int min = (int)(dummySurvivalTime / 60);
        int sec = (int)(dummySurvivalTime % 60);
        timeText = MakeText(panel.transform, "Time", $"생존 시간: {min}분 {sec}초",
            new Vector2(0, -70), new Vector2(400, 25), 16, new Color(0.7f, 1f, 0.8f), TextAnchor.MiddleCenter);
        timeText.fontStyle = FontStyle.Bold;

        // 구분선
        MakeLine(panel.transform, -100);

        // 획득 아이템
        string itemStr = "[ 획득 아이템 ]\n";
        for (int i = 0; i < dummyItems.Length; i++)
            itemStr += $"  • {dummyItems[i]}\n";
        itemsText = MakeText(panel.transform, "Items", itemStr,
            new Vector2(20, -115), new Vector2(400, 140), 15, new Color(0.9f, 0.95f, 1f), TextAnchor.UpperLeft);

        // 구분선
        MakeLine(panel.transform, -260);

        // 보상
        rewardsText = MakeText(panel.transform, "Rewards",
            $"경험치: +{dummyExp} EXP\n루디(화폐): +{dummyMoney}",
            new Vector2(0, -275), new Vector2(400, 55), 16, new Color(0.7f, 1f, 0.8f), TextAnchor.MiddleCenter);
        rewardsText.fontStyle = FontStyle.Bold;

        // 닫기 안내
        closeHintText = MakeText(panel.transform, "CloseHint", "[ Enter / 클릭으로 닫기 ]",
            new Vector2(0, -345), new Vector2(400, 25), 14, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter);

        panelRoot.SetActive(false);
    }

    Text MakeText(Transform parent, string name, string content,
        Vector2 pos, Vector2 size, int fontSize, Color color, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.color = color;
        txt.text = content;
        txt.alignment = align;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        return txt;
    }

    void MakeLine(Transform parent, float yPos)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, yPos);
        rt.sizeDelta = new Vector2(380, 1);
        var img = go.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.3f);
    }
}
