using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아이템 1개 상세 정보 팝업 — 큰 아이콘 + 이름 + 설명 + 스탯.
/// 컨텍스트 메뉴 "자세히"에서 호출. 전체화면 어두운 배경 + 가운데 카드.
/// 클릭(배경)/Esc로 닫힘. 싱글톤 자가 부트스트랩(NoteUI 패턴).
/// UIManager.IsAnyUIOpen()에 포함, Esc는 다른 UI보다 먼저 이 팝업을 닫음.
/// </summary>
public class ItemDetailUI : MonoBehaviour
{
    public static ItemDetailUI Instance { get; private set; }

    public static bool IsShowing => Instance != null && Instance.isShowing;
    public static void Hide() { if (Instance != null) Instance.Close(); }
    public static void Show(ItemInstance item) { Ensure().Open(item); }

    bool isShowing;
    int openFrame = -1;

    Canvas canvas;
    GameObject panelRoot;
    Image iconImage;
    Image iconBg;
    Text iconFallback;
    Text nameText;
    Text descText;
    Text statText;
    Font koreanFont;

    const int SortingOrder = 112;   // CharacterPanel(40) 위, NoteUI(110) 부근
    const float CardW = 460f, CardH = 620f;

    bool IsGenerated => canvas != null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        koreanFont = LoadKoreanFont();
        if (!IsGenerated) GenerateUI();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public static ItemDetailUI Ensure()
    {
        if (Instance == null)
        {
            var go = new GameObject("[ItemDetailUI]");
            DontDestroyOnLoad(go);
            go.AddComponent<ItemDetailUI>();
        }
        return Instance;
    }

    void Open(ItemInstance item)
    {
        if (item == null || item.data == null) return;
        if (!IsGenerated) GenerateUI();
        var d = item.data;

        // 이름
        nameText.text = d.displayName;
        nameText.color = d.RarityColor;

        // 아이콘 / 폴백
        iconBg.color = ScaleColor(d.RarityColor, 0.22f);
        if (d.icon != null)
        {
            iconImage.sprite = d.icon;
            iconImage.enabled = true;
            iconFallback.enabled = false;
        }
        else
        {
            iconImage.enabled = false;
            iconFallback.enabled = true;
            iconFallback.text = d.displayName;
            iconFallback.color = d.RarityColor;
        }

        // 설명
        descText.text = string.IsNullOrEmpty(d.description) ? "<color=#888888>설명 없음</color>" : d.description;

        // 스탯
        var sb = new System.Text.StringBuilder();
        sb.Append($"<b>분류</b>  {GetCategoryName(d.category)} · {GetRarityName(d.rarity)}\n");
        sb.Append($"<b>크기</b>  {d.gridWidth} x {d.gridHeight}    <b>무게</b>  {d.weight:F1} kg\n");
        if (item.stackCount > 1)
            sb.Append($"<b>수량</b>  {item.stackCount} / {d.maxStack}\n");
        if (item.HasDurability)
            sb.Append($"<b>내구도</b>  {item.durability:F0} / {d.maxDurability:F0}\n");
        if (d.sellPrice > 0 || d.buyPrice > 0)
        {
            sb.Append("<b>가격</b>  ");
            if (d.buyPrice > 0) sb.Append($"구매 {d.buyPrice}  ");
            if (d.sellPrice > 0) sb.Append($"판매 {d.sellPrice}  ");
            sb.Append("스크랩\n");
        }
        if (d.isUsable)
            sb.Append($"\n<color=#88CC88><b>사용 가능</b> — {GetEffectName(d.useEffect)} ({d.effectValue:F0})</color>");
        statText.text = sb.ToString();

        isShowing = true;
        openFrame = Time.frameCount;
        panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (!isShowing) return;
        isShowing = false;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Update()
    {
        if (!isShowing) return;
        if (Time.frameCount == openFrame) return;   // 연 프레임 입력 무시
        if (GameInput.GetKeyDown(KeyCode.Escape) || GameInput.GetMouseButtonDown(0) || GameInput.GetMouseButtonDown(1))
            Close();
    }

    // ── UI 생성 ───────────────────────────────────────────────────────

    void GenerateUI()
    {
        var canvasGO = new GameObject("ItemDetailUI_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject("Root", typeof(RectTransform));
        panelRoot.transform.SetParent(canvas.transform, false);
        var rootRT = panelRoot.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero; rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero; rootRT.offsetMax = Vector2.zero;

        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(panelRoot.transform, false);
        var dimRT = dim.GetComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = Vector2.zero; dimRT.offsetMax = Vector2.zero;
        dim.GetComponent<Image>().color = UITheme.Backdrop;

        var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(panelRoot.transform, false);
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = cardRT.anchorMax = cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(CardW, CardH);
        cardRT.anchoredPosition = Vector2.zero;
        card.GetComponent<Image>().color = UITheme.Panel;

        // 이름 (상단)
        nameText = MakeText(cardRT, "Name", 26, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        var nRT = (RectTransform)nameText.transform;
        nRT.anchorMin = new Vector2(0, 1); nRT.anchorMax = new Vector2(1, 1); nRT.pivot = new Vector2(0.5f, 1f);
        nRT.offsetMin = new Vector2(24, -64); nRT.offsetMax = new Vector2(-24, -18);

        // 아이콘 박스 (큰 사각)
        var iconBgGO = new GameObject("IconBg", typeof(RectTransform), typeof(Image));
        iconBgGO.transform.SetParent(cardRT, false);
        var ibRT = iconBgGO.GetComponent<RectTransform>();
        ibRT.anchorMin = new Vector2(0.5f, 1f); ibRT.anchorMax = new Vector2(0.5f, 1f); ibRT.pivot = new Vector2(0.5f, 1f);
        ibRT.sizeDelta = new Vector2(220, 220);
        ibRT.anchoredPosition = new Vector2(0, -76);
        iconBg = iconBgGO.GetComponent<Image>();
        iconBg.color = UITheme.Cell;

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(iconBgGO.transform, false);
        var icRT = iconGO.GetComponent<RectTransform>();
        icRT.anchorMin = Vector2.zero; icRT.anchorMax = Vector2.one;
        icRT.offsetMin = new Vector2(14, 14); icRT.offsetMax = new Vector2(-14, -14);
        iconImage = iconGO.GetComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        iconFallback = MakeText(iconBgGO.transform, "IconFallback", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        var ifRT = (RectTransform)iconFallback.transform;
        ifRT.anchorMin = Vector2.zero; ifRT.anchorMax = Vector2.one;
        ifRT.offsetMin = new Vector2(8, 8); ifRT.offsetMax = new Vector2(-8, -8);

        // 설명 (아이콘 아래)
        descText = MakeText(cardRT, "Desc", 16, FontStyle.Normal, UITheme.TextBright, TextAnchor.UpperLeft);
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.lineSpacing = 1.1f;
        var dRT = (RectTransform)descText.transform;
        dRT.anchorMin = new Vector2(0, 1); dRT.anchorMax = new Vector2(1, 1); dRT.pivot = new Vector2(0.5f, 1f);
        dRT.offsetMin = new Vector2(24, -432); dRT.offsetMax = new Vector2(-24, -306);

        // 스탯 (설명 아래, 상단 기준 고정 — 설명이 길어도 겹침 최소화)
        statText = MakeText(cardRT, "Stats", 15, FontStyle.Normal, UITheme.TextMuted, TextAnchor.UpperLeft);
        statText.horizontalOverflow = HorizontalWrapMode.Wrap;
        statText.lineSpacing = 1.15f;
        var sRT = (RectTransform)statText.transform;
        sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1); sRT.pivot = new Vector2(0.5f, 1f);
        sRT.offsetMin = new Vector2(24, -588); sRT.offsetMax = new Vector2(-24, -442);

        // 닫기 힌트
        var hint = MakeText(cardRT, "Hint", 13, FontStyle.Italic, UITheme.TextDim, TextAnchor.LowerCenter);
        hint.text = "[클릭] / [Esc] 닫기";
        var hRT = (RectTransform)hint.transform;
        hRT.anchorMin = new Vector2(0, 0); hRT.anchorMax = new Vector2(1, 0); hRT.pivot = new Vector2(0.5f, 0f);
        hRT.offsetMin = new Vector2(12, 12); hRT.offsetMax = new Vector2(-12, 34);

        panelRoot.SetActive(false);
    }

    Text MakeText(Transform parent, string name, int size, FontStyle style, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = koreanFont != null ? koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = anchor;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Color ScaleColor(Color c, float a) => new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, a);

    static string GetCategoryName(ItemCategory cat)
    {
        switch (cat)
        {
            case ItemCategory.Weapon: return "무기";
            case ItemCategory.Medical: return "의료";
            case ItemCategory.Consumable: return "소비";
            case ItemCategory.Material: return "재료";
            case ItemCategory.Valuable: return "귀중품";
            case ItemCategory.Key: return "열쇠";
            case ItemCategory.Misc: return "기타";
            default: return cat.ToString();
        }
    }

    static string GetRarityName(ItemRarity r)
    {
        switch (r)
        {
            case ItemRarity.Common: return "일반";
            case ItemRarity.Uncommon: return "고급";
            case ItemRarity.Rare: return "희귀";
            case ItemRarity.Epic: return "영웅";
            case ItemRarity.Legendary: return "전설";
            default: return r.ToString();
        }
    }

    static string GetEffectName(ItemUseEffect fx)
    {
        switch (fx)
        {
            case ItemUseEffect.HealHP: return "체력 회복";
            case ItemUseEffect.HealInjury: return "부상 치료";
            case ItemUseEffect.AddBattery: return "배터리 충전";
            case ItemUseEffect.Food: return "허기/수분 회복";
            default: return fx.ToString();
        }
    }

    static Font LoadKoreanFont()
    {
        var f = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Dotum", "Batang", "Arial" }, 22);
        return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
