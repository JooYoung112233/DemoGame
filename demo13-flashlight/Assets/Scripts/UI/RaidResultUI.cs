using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 귀환 정산 UI (Canvas/uGUI).
/// UIManager 자식으로 배치.
/// 필드→안전가옥 복귀 시 자동 표시.
/// PlayerInventory + RaidManager 실데이터 연동.
/// </summary>
public class RaidResultUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] string safehouseScene = "Safehouse";
    [SerializeField] KeyCode closeKey = KeyCode.Return;

    [Header("Generated UI References")]
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject panelRoot;
    [SerializeField] Image dimBg;
    [SerializeField] Text titleText;
    [SerializeField] Text timeText;
    [SerializeField] Text itemsText;
    [SerializeField] Text rewardsText;
    [SerializeField] Text closeHintText;

    public bool IsGenerated => canvas != null;
    public bool IsShowing => isShowing;

    bool isShowing;
    float showTimer;

    // 실제 데이터 (Show 시 캡처)
    List<string> itemLines = new List<string>();
    float survivalTime;
    int totalItemCount;
    float totalWeight;

    void Awake()
    {
        if (!IsGenerated) GenerateUI();
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
        // 레이드를 실제로 다녀온 경우에만 정산(부팅 직후 안전가옥 진입에선 RaidManager.PendingResult=false).
        if (scene.name == safehouseScene && RaidManager.PendingResult)
        {
            RaidManager.PendingResult = false;   // 1회 소비
            CaptureRaidData();
            RefillBattery();

            // PostRaidEvent 먼저 체크 → 이벤트 끝나면 정산 표시
            if (PostRaidEventManager.Instance != null && PostRaidEventUI.Instance != null)
            {
                var evt = PostRaidEventManager.Instance.TryGetEvent();
                if (evt != null)
                {
                    PostRaidEventUI.Instance.ShowEvent(evt, () => Show());
                    return;
                }
            }

            Show();
        }
    }

    /// <summary>정산 데이터 캡처 (씬 전환 직후, UI 표시 전)</summary>
    void CaptureRaidData()
    {
        itemLines.Clear();
        totalItemCount = 0;
        totalWeight = 0f;
        survivalTime = 0f;

        // RaidManager에서 생존 시간 가져오기
        if (RaidManager.Instance != null)
        {
            survivalTime = RaidManager.Instance.ElapsedTime;

            // 루트 추적 아이템 사용
            var looted = RaidManager.Instance.LootedItems;
            if (looted != null && looted.Count > 0)
            {
                for (int i = 0; i < looted.Count; i++)
                {
                    var item = looted[i];
                    if (item == null || item.data == null) continue;

                    string rarityColor = ColorToHex(item.data.RarityColor);
                    string line = item.stackCount > 1
                        ? $"  <color={rarityColor}>{item.data.displayName}</color> x{item.stackCount}"
                        : $"  <color={rarityColor}>{item.data.displayName}</color>";
                    itemLines.Add(line);
                    totalItemCount += item.stackCount;
                    totalWeight += item.TotalWeight;
                }
            }
        }

        // RaidManager 없으면 현재 인벤토리에서 가져오기 (폴백)
        if (itemLines.Count == 0)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var inventory = player.GetComponent<PlayerInventory>();
                if (inventory != null && inventory.Grid != null)
                {
                    var allItems = inventory.Grid.GetAll();
                    for (int i = 0; i < allItems.Count; i++)
                    {
                        var item = allItems[i].item;
                        if (item == null || item.data == null) continue;

                        string line = item.stackCount > 1
                            ? $"  {item.data.displayName} x{item.stackCount}"
                            : $"  {item.data.displayName}";
                        itemLines.Add(line);
                        totalItemCount += item.stackCount;
                        totalWeight += item.TotalWeight;
                    }
                }
            }
        }

        if (itemLines.Count == 0)
            itemLines.Add("  (획득 아이템 없음)");
    }

    string ColorToHex(Color c)
    {
        return $"#{ColorUtility.ToHtmlStringRGB(c)}";
    }

    public void Show()
    {
        isShowing = true;
        showTimer = 0;
        UpdateTexts();
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    /// <summary>캡처된 데이터로 UI 텍스트 갱신</summary>
    void UpdateTexts()
    {
        if (timeText != null)
        {
            int min = (int)(survivalTime / 60);
            int sec = (int)(survivalTime % 60);
            timeText.text = $"생존 시간: {min}분 {sec}초";
        }

        if (itemsText != null)
        {
            string header = $"[ 획득 아이템 — {totalItemCount}개, {totalWeight:F1}kg ]\n";
            itemsText.text = header + string.Join("\n", itemLines);
            itemsText.supportRichText = true;
        }

        if (rewardsText != null)
        {
            // 보상 계산: 아이템 가치 합산
            int totalValue = 0;
            if (RaidManager.Instance != null)
            {
                foreach (var item in RaidManager.Instance.LootedItems)
                {
                    if (item != null && item.data != null)
                        totalValue += item.data.sellPrice * item.stackCount;
                }
            }
            rewardsText.text = $"아이템 가치: {totalValue} 스크랩";
        }
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
            var hint = UITheme.TextMuted;
            closeHintText.color = new Color(hint.r, hint.g, hint.b, alpha);
        }

        if (showTimer > 1f && (Input.GetKeyDown(closeKey) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0)))
            Hide();
    }

    public void GenerateUI()
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
        dimBg.color = UITheme.Backdrop;

        // 중앙 패널
        var panel = new GameObject("CenterPanel");
        panel.transform.SetParent(panelRoot.transform, false);
        var pRT = panel.AddComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.5f, 0.5f);
        pRT.anchorMax = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(440, 400);
        var pImg = panel.AddComponent<Image>();
        pImg.color = UITheme.Panel;

        // 제목
        titleText = MakeText(panel.transform, "Title", "── 귀환 정산 ──",
            new Vector2(0, -20), new Vector2(400, 40), 24, UITheme.Gold, TextAnchor.MiddleCenter);
        titleText.fontStyle = FontStyle.Bold;

        // 생존 시간 (Show에서 갱신)
        timeText = MakeText(panel.transform, "Time", "생존 시간: --",
            new Vector2(0, -70), new Vector2(400, 25), 16, UITheme.Positive, TextAnchor.MiddleCenter);
        timeText.fontStyle = FontStyle.Bold;

        // 구분선
        MakeLine(panel.transform, -100);

        // 획득 아이템 (Show에서 갱신)
        itemsText = MakeText(panel.transform, "Items", "[ 획득 아이템 ]",
            new Vector2(20, -115), new Vector2(400, 140), 15, UITheme.TextBright, TextAnchor.UpperLeft);

        // 구분선
        MakeLine(panel.transform, -260);

        // 보상 (Show에서 갱신)
        rewardsText = MakeText(panel.transform, "Rewards", "",
            new Vector2(0, -275), new Vector2(400, 55), 16, UITheme.Positive, TextAnchor.MiddleCenter);
        rewardsText.fontStyle = FontStyle.Bold;

        // 닫기 안내
        closeHintText = MakeText(panel.transform, "CloseHint", "[ Enter / 클릭으로 닫기 ]",
            new Vector2(0, -345), new Vector2(400, 25), 14, UITheme.TextMuted, TextAnchor.MiddleCenter);

        panelRoot.SetActive(false);
    }

    public void ClearGeneratedUI()
    {
        var child = transform.Find("RaidResult_Canvas");
        if (child != null)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        canvas = null;
        panelRoot = null;
        dimBg = null;
        titleText = null;
        timeText = null;
        itemsText = null;
        rewardsText = null;
        closeHintText = null;
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
        img.color = UITheme.Divider;
    }
}
