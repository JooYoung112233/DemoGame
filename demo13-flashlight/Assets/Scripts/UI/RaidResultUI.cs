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
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    /// <summary>캡처된 데이터로 UI 텍스트 갱신</summary>
    void UpdateTexts()
    {
        var s = RaidManager.LastSettlement;   // static — RaidManager 파괴 후에도 유효

        if (titleText != null && s != null)
        {
            titleText.text = s.success ? "── 귀환 정산 ──" : "── 레이드 실패 ──";
            titleText.color = s.success ? UITheme.Gold : UITheme.Negative;
        }

        if (timeText != null)
        {
            float t = s != null ? s.survivalTime : survivalTime;
            int min = (int)(t / 60);
            int sec = (int)(t % 60);
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
            // 런타임 스타일 오버라이드 — 베이크된 프리팹 rect/폰트를 3줄 XP 블록에 맞춤(재베이크 불필요).
            rewardsText.supportRichText = true;
            rewardsText.fontSize = 14;
            rewardsText.fontStyle = FontStyle.Normal;   // 전체 볼드 해제 — 강조는 <b> 인라인만
            rewardsText.alignment = TextAnchor.MiddleCenter;
            rewardsText.rectTransform.sizeDelta = new Vector2(400, 70);

            if (s == null)
            {
                rewardsText.text = "";   // 정산 레코드 없음(구버전 경로) — 표기 생략
            }
            else if (s.success)
            {
                string levelLine = s.levelAfter > s.levelBefore
                    ? $"<color=#E8C86A><b>Lv.{s.levelBefore} → Lv.{s.levelAfter}  레벨 업!</b></color>  <color=#8A8170>XP {s.xpAfter}/{s.xpToNextAfter}</color>"
                    : $"Lv.{s.levelAfter}  <color=#8A8170>XP {s.xpAfter}/{s.xpToNextAfter}</color>";
                rewardsText.text =
                    $"루팅 가치 <color=#E8C86A>+◈{s.lootValue:N0}</color>\n" +
                    $"경험치 <b>+{s.totalXp}</b>  <color=#8A8170>(킬 {s.killXp} · 탈출 {s.extractBonus} · 루팅 {s.lootXp})</color>\n" +
                    levelLine;
            }
            else
            {
                string levelLine = s.levelAfter > s.levelBefore
                    ? $"<color=#E8C86A><b>Lv.{s.levelBefore} → Lv.{s.levelAfter}  레벨 업!</b></color>"
                    : $"Lv.{s.levelAfter}  <color=#8A8170>XP {s.xpAfter}/{s.xpToNextAfter}</color>";
                rewardsText.text =
                    $"<color=#B06A5A>물자 손실 발생</color>\n" +   // 사망=가방 전체 / 시간초과=일부 (구분은 토스트가 이미 안내)
                    $"경험치 <b>+{s.totalXp}</b>  <color=#8A8170>(킬 XP의 절반 — 죽어도 배운다)</color>\n" +
                    levelLine;
            }
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

        if (showTimer > 1f && (GameInput.GetKeyDown(closeKey) || GameInput.GetKeyDown(KeyCode.Escape) || GameInput.GetMouseButtonDown(0)))
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
        UITheme.ConfigureCanvasScale(scaler);
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

        // 보상/XP 블록 (Show에서 갱신 — 내용·스타일은 UpdateTexts가 런타임 지정)
        rewardsText = MakeText(panel.transform, "Rewards", "",
            new Vector2(0, -270), new Vector2(400, 70), 14, UITheme.Positive, TextAnchor.MiddleCenter);

        // 닫기 안내
        closeHintText = MakeText(panel.transform, "CloseHint", "[ Enter / 클릭으로 닫기 ]",
            new Vector2(0, -345), new Vector2(400, 25), 14, UITheme.TextMuted, TextAnchor.MiddleCenter);

        panelRoot.SetActive(false);
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — GenerateUI를 1회 실행해 프리팹화할 계층을 만든다.</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        GenerateUI();
    }
#endif

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
