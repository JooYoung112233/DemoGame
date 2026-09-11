using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아이템 1개 상세 정보 팝업 — 시안 "ITEM NAME" 패널 레이아웃.
/// 제목태그 + 아이콘박스 + TYPE/WEIGHT/STACK + 설명 + STAT 3행 + DURABILITY 바 + SELL VALUE + EQUIP/DROP/SCRAP.
/// 컨텍스트 메뉴 "자세히"에서 호출. item.png 종이 배경 + 어두운 잉크 텍스트. 배경 클릭/Esc로 닫힘.
/// 싱글톤 자가 부트스트랩(NoteUI 패턴). 프리팹 베이크 가능(WireEvents로 버튼 onClick 재부착).
/// </summary>
public class ItemDetailUI : MonoBehaviour
{
    public static ItemDetailUI Instance { get; private set; }

    public static bool IsShowing => Instance != null && Instance.isShowing;
    public static void Hide() { if (Instance != null) Instance.Close(); }
    public static void Show(ItemInstance item) { Ensure().Open(item, null, null); }

    /// <summary>내 소지품에서 연 경우 — 장착/버리기 동작을 부른 쪽(캐릭터 패널)이 넘긴다. null이면 그 버튼을 숨긴다.</summary>
    public static void Show(ItemInstance item, System.Action onEquip, System.Action onDrop)
    { Ensure().Open(item, onEquip, onDrop); }

    bool isShowing;
    int openFrame = -1;
    ItemInstance current;
    System.Action onEquipAction, onDropAction;

    // ── 직렬화 뷰(프리팹 베이크 보존) ───────────────────────────────
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject panelRoot;
    [SerializeField] Button dimButton;          // 배경 클릭 닫기
    [SerializeField] Image iconImage;
    [SerializeField] Image iconBg;
    [SerializeField] Text iconFallback;
    [SerializeField] Text nameText;             // 제목 태그
    [SerializeField] Text typeValue, weightValue, stackValue;
    [SerializeField] Text descText;
    [SerializeField] Text stat1Label, stat1Value, stat2Label, stat2Value, stat3Label, stat3Value;
    [SerializeField] Text durValue;
    [SerializeField] Image durFill;
    [SerializeField] Text sellValue;
    [SerializeField] Button equipBtn, dropBtn, scrapBtn;
    Font koreanFont;   // 런타임 동적 OS 폰트 — 직렬화 안 함(Instantiate 후 ApplyFonts 재바인딩)

    const int SortingOrder = 112;
    const float CardW = 460f, CardH = 680f;

    // 종이 위 어두운 잉크 톤
    static readonly Color Ink     = new Color(0.15f, 0.12f, 0.09f, 1f);   // 라벨/제목(진함)
    static readonly Color InkSoft = new Color(0.28f, 0.23f, 0.17f, 1f);   // 값/보조
    static readonly Color RuleCol = new Color(0.38f, 0.31f, 0.22f, 0.6f); // 밑줄
    static readonly Color BarBg   = new Color(0.16f, 0.13f, 0.10f, 0.5f);
    static readonly Color BarFill = new Color(0.30f, 0.24f, 0.16f, 1f);

    // 새 레이아웃 마커로 판정 — 구버전 베이크 프리팹(이 필드 없음)이면 false → 재생성(아래 캔버스 정리).
    bool IsGenerated => equipBtn != null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        koreanFont = LoadKoreanFont();
        if (!IsGenerated) GenerateUI();   // 폴백: 프리팹 없이 코드로 생성
        else ApplyFonts();                // 프리팹 인스턴스: 동적 폰트 재바인딩
        WireEvents();                     // onClick은 직렬화 안 됨 → 양쪽 경로에서 재부착
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public static ItemDetailUI Ensure()
    {
        if (Instance == null)
        {
            var prefab = Resources.Load<GameObject>("UI/ItemDetailUI");
            GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("[ItemDetailUI]");
            go.name = "[ItemDetailUI]";
            if (prefab == null) go.AddComponent<ItemDetailUI>();
            DontDestroyOnLoad(go);
        }
        return Instance;
    }

    // ── 데이터 바인딩 ───────────────────────────────────────────────

    void Open(ItemInstance item, System.Action onEquip, System.Action onDrop)
    {
        if (item == null || item.data == null) return;
        if (!IsGenerated) GenerateUI();
        current = item;
        onEquipAction = onEquip;
        onDropAction = onDrop;
        // 동작이 없는 버튼은 숨긴다(상점·장비 슬롯에서 연 경우). 분해(SCRAP)는 시스템이 없어 항상 숨김.
        if (equipBtn != null) equipBtn.gameObject.SetActive(onEquipAction != null);
        if (dropBtn != null) dropBtn.gameObject.SetActive(onDropAction != null);
        if (scrapBtn != null) scrapBtn.gameObject.SetActive(false);
        var d = item.data;

        nameText.text = d.displayName;

        // 아이콘 박스 — 희귀도 어두운 tint
        iconBg.color = DarkTint(d.RarityColor);
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
        }

        // TYPE / WEIGHT / STACK
        typeValue.text   = $"{GetCategoryName(d.category)}";
        weightValue.text = $"{d.weight:F1} kg";
        stackValue.text  = item.stackCount > 1 ? $"{item.stackCount} / {d.maxStack}" : "1";

        // 설명
        descText.text = string.IsNullOrEmpty(d.description) ? "설명 없음." : d.description;

        // STAT 3행
        stat1Label.text = "무게";     stat1Value.text = $"{d.weight * Mathf.Max(1, item.stackCount):F2} kg";
        stat2Label.text = "희귀도";   stat2Value.text = GetRarityName(d.rarity);
        if (d.isUsable) { stat3Label.text = "효과"; stat3Value.text = $"{GetEffectName(d.useEffect)} ({d.effectValue:F0})"; }
        else            { stat3Label.text = "분류"; stat3Value.text = GetCategoryName(d.category); }

        // DURABILITY
        if (item.HasDurability)
        {
            durValue.text = $"{item.durability:F0} / {d.maxDurability:F0}";
            durFill.fillAmount = d.maxDurability > 0 ? Mathf.Clamp01(item.durability / d.maxDurability) : 1f;
            durFill.enabled = true;
        }
        else { durValue.text = "—"; durFill.fillAmount = 0f; durFill.enabled = false; }

        // SELL VALUE
        sellValue.text = d.sellPrice > 0 ? $"{d.sellPrice} 스크랩" : "—";

        isShowing = true;
        openFrame = Time.frameCount;
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (!isShowing) return;
        isShowing = false;
        current = null;
        onEquipAction = null;
        onDropAction = null;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Update()
    {
        if (!isShowing) return;
        if (Time.frameCount == openFrame) return;
        if (GameInput.GetKeyDown(KeyCode.Escape)) Close();   // 바깥 클릭은 Dim 버튼이 처리
    }

    // ── 버튼 동작 — 부른 쪽이 넘긴 동작을 창을 닫은 뒤 실행(확인창이 이 창 뒤에 깔리지 않게) ──
    void OnEquip() { var a = onEquipAction; Close(); a?.Invoke(); }
    void OnDrop()  { var a = onDropAction;  Close(); a?.Invoke(); }
    void OnScrap() { Close(); }   // 분해 시스템 없음 — 버튼은 Open에서 숨긴다

    void WireEvents()
    {
        Wire(dimButton, Close);
        Wire(equipBtn, OnEquip);
        Wire(dropBtn,  OnDrop);
        Wire(scrapBtn, OnScrap);
    }

    static void Wire(Button b, UnityEngine.Events.UnityAction h)
    {
        if (b == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(h);
    }

    // ── UI 생성 (시안 레이아웃) ───────────────────────────────────────

    void GenerateUI()
    {
        // 스테일 프리팹(구버전 베이크)에서 재생성 시 기존 캔버스 제거(중복 방지). 신규 베이크/폴백은 자식 없음.
        var stale = transform.Find("ItemDetailUI_Canvas");
        if (stale != null) Destroy(stale.gameObject);

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

        // 배경(클릭 시 닫기)
        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        dim.transform.SetParent(panelRoot.transform, false);
        var dimRT = dim.GetComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = Vector2.zero; dimRT.offsetMax = Vector2.zero;
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.22f);   // 팝업: 전체화면 어둠 대신 옅은 dim(바깥 클릭 닫기용)
        dimButton = dim.GetComponent<Button>();
        dimButton.transition = Selectable.Transition.None;

        // 카드 (item.png 큰 크림 종이)
        var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(panelRoot.transform, false);
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = cardRT.anchorMax = cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(CardW, CardH);
        cardRT.anchoredPosition = Vector2.zero;
        var cardImg = card.GetComponent<Image>();
        cardImg.color = new Color(0.78f, 0.72f, 0.58f, 1f);   // 폴백 종이색(스프라이트 없을 때)
        UISkin.ItemPanel(cardImg);                            // 시안: item.png

        // 제목 태그 (name.png 찢긴 종이) + 제목
        var tag = new GameObject("TitleTag", typeof(RectTransform), typeof(Image));
        tag.transform.SetParent(cardRT, false);
        var tagRT = tag.GetComponent<RectTransform>();
        tagRT.anchorMin = tagRT.anchorMax = tagRT.pivot = new Vector2(0, 1);
        tagRT.anchoredPosition = new Vector2(12, 6);
        tagRT.sizeDelta = new Vector2(238, 58);
        var tagImg = tag.GetComponent<Image>(); tagImg.color = new Color(0.74f, 0.68f, 0.54f, 1f); UISkin.Tag(tagImg);
        nameText = Txt(tag.transform, "Name", 24, FontStyle.Bold, Ink, TextAnchor.MiddleLeft, 0, 0, 0, 0);
        var nRT = (RectTransform)nameText.transform;
        nRT.anchorMin = Vector2.zero; nRT.anchorMax = Vector2.one;
        nRT.offsetMin = new Vector2(26, 4); nRT.offsetMax = new Vector2(-14, -4);

        // 아이콘 박스 (itembox.png)
        var iconBgGO = new GameObject("IconBg", typeof(RectTransform), typeof(Image));
        iconBgGO.transform.SetParent(cardRT, false);
        var ibRT = iconBgGO.GetComponent<RectTransform>();
        ibRT.anchorMin = ibRT.anchorMax = ibRT.pivot = new Vector2(0, 1);
        ibRT.anchoredPosition = new Vector2(30, -98);
        ibRT.sizeDelta = new Vector2(176, 176);
        iconBg = iconBgGO.GetComponent<Image>();
        iconBg.color = UITheme.Cell; UISkin.IconBox(iconBg);

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(iconBgGO.transform, false);
        var icRT = iconGO.GetComponent<RectTransform>();
        icRT.anchorMin = Vector2.zero; icRT.anchorMax = Vector2.one;
        icRT.offsetMin = new Vector2(16, 16); icRT.offsetMax = new Vector2(-16, -16);
        iconImage = iconGO.GetComponent<Image>();
        iconImage.preserveAspect = true; iconImage.raycastTarget = false;

        iconFallback = Txt(iconBgGO.transform, "IconFallback", 18, FontStyle.Bold, new Color(0.85f,0.82f,0.74f), TextAnchor.MiddleCenter, 0,0,0,0);
        var ifRT = (RectTransform)iconFallback.transform;
        ifRT.anchorMin = Vector2.zero; ifRT.anchorMax = Vector2.one;
        ifRT.offsetMin = new Vector2(8, 8); ifRT.offsetMax = new Vector2(-8, -8);

        // 우측 필드: TYPE / WEIGHT / STACK (라벨 + 값 + 밑줄)
        const float fx = 224f, fw = 208f;
        Txt(cardRT, "TypeL", 16, FontStyle.Bold, Ink, TextAnchor.LowerLeft, fx, 122, fw, 24).text = "TYPE";
        typeValue = Txt(cardRT, "TypeV", 16, FontStyle.Normal, InkSoft, TextAnchor.LowerRight, fx, 122, fw, 24);
        Rule(cardRT, fx, 130, fw);
        Txt(cardRT, "WeightL", 16, FontStyle.Bold, Ink, TextAnchor.LowerLeft, fx, 176, fw, 24).text = "WEIGHT";
        weightValue = Txt(cardRT, "WeightV", 16, FontStyle.Normal, InkSoft, TextAnchor.LowerRight, fx, 176, fw, 24);
        Rule(cardRT, fx, 184, fw);
        Txt(cardRT, "StackL", 16, FontStyle.Bold, Ink, TextAnchor.LowerLeft, fx, 230, fw, 24).text = "STACK";
        stackValue = Txt(cardRT, "StackV", 16, FontStyle.Normal, InkSoft, TextAnchor.LowerRight, fx, 230, fw, 24);
        Rule(cardRT, fx, 238, fw);

        // 설명 (아이콘 아래 전폭)
        const float px = 28f, pw = 404f;
        descText = Txt(cardRT, "Desc", 15, FontStyle.Normal, Ink, TextAnchor.UpperLeft, px, 292, pw, 50);
        descText.horizontalOverflow = HorizontalWrapMode.Wrap; descText.lineSpacing = 1.1f;
        Rule(cardRT, px, 350, pw);

        // STAT 3행 (라벨 좌 / 값 우)
        stat1Label = Txt(cardRT, "S1L", 15, FontStyle.Bold, Ink, TextAnchor.MiddleLeft, px, 366, 180, 24);
        stat1Value = Txt(cardRT, "S1V", 15, FontStyle.Normal, InkSoft, TextAnchor.MiddleRight, px, 366, pw, 24);
        stat2Label = Txt(cardRT, "S2L", 15, FontStyle.Bold, Ink, TextAnchor.MiddleLeft, px, 396, 180, 24);
        stat2Value = Txt(cardRT, "S2V", 15, FontStyle.Normal, InkSoft, TextAnchor.MiddleRight, px, 396, pw, 24);
        stat3Label = Txt(cardRT, "S3L", 15, FontStyle.Bold, Ink, TextAnchor.MiddleLeft, px, 426, 180, 24);
        stat3Value = Txt(cardRT, "S3V", 15, FontStyle.Normal, InkSoft, TextAnchor.MiddleRight, px, 426, pw, 24);

        // DURABILITY (라벨 + 값 + 바)
        Txt(cardRT, "DurL", 15, FontStyle.Bold, Ink, TextAnchor.MiddleLeft, px, 486, 200, 24).text = "DURABILITY";
        durValue = Txt(cardRT, "DurV", 15, FontStyle.Normal, InkSoft, TextAnchor.MiddleRight, px, 486, pw, 24);
        durFill = MakeBar(cardRT, px, 514, pw, 10);
        Rule(cardRT, px, 540, pw);

        // SELL VALUE
        Txt(cardRT, "SellL", 16, FontStyle.Bold, Ink, TextAnchor.MiddleLeft, px, 564, 220, 24).text = "SELL VALUE";
        sellValue = Txt(cardRT, "SellV", 16, FontStyle.Normal, InkSoft, TextAnchor.MiddleRight, px, 564, pw, 24);

        // EQUIP / DROP / SCRAP (btn.png, 어두운 글자)
        equipBtn = ActionButton(cardRT, "EquipBtn", "EQUIP", 30, 20, 128, 48);
        dropBtn  = ActionButton(cardRT, "DropBtn",  "DROP",  166, 20, 128, 48);
        scrapBtn = ActionButton(cardRT, "ScrapBtn", "SCRAP", 302, 20, 128, 48);

        panelRoot.SetActive(false);
    }

    /// <summary>프리팹 인스턴스화 시 모든 Text에 동적 OS 폰트 재바인딩.</summary>
    void ApplyFonts()
    {
        var f = koreanFont != null ? koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (var t in GetComponentsInChildren<Text>(true)) t.font = f;
    }

#if UNITY_EDITOR
    public void EditorBake()
    {
        if (IsGenerated) return;
        koreanFont = LoadKoreanFont();
        GenerateUI();
    }
#endif

    // ── 빌더 헬퍼 ───────────────────────────────────────────────────

    /// <summary>cardRT 기준 top-left 좌표(x, yTop=위에서 아래로 +)에 Text 배치.</summary>
    Text Txt(Transform parent, string name, int size, FontStyle style, Color color, TextAnchor anchor,
             float x, float yTop, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = koreanFont != null ? koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = anchor;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var rt = (RectTransform)t.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -yTop);
        rt.sizeDelta = new Vector2(w, h);
        return t;
    }

    Image Rule(Transform parent, float x, float yTop, float w)
    {
        var go = new GameObject("Rule", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -yTop);
        rt.sizeDelta = new Vector2(w, 2);
        var img = go.GetComponent<Image>(); img.color = RuleCol; img.raycastTarget = false;
        return img;
    }

    Image MakeBar(Transform parent, float x, float yTop, float w, float h)
    {
        var bg = new GameObject("DurBar", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(parent, false);
        var rt = bg.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -yTop);
        rt.sizeDelta = new Vector2(w, h);
        var bgImg = bg.GetComponent<Image>(); bgImg.color = BarBg; bgImg.raycastTarget = false;

        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(bg.transform, false);
        var frt = fillGO.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        var fill = fillGO.GetComponent<Image>();
        fill.color = BarFill; fill.raycastTarget = false;
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 1f;
        return fill;
    }

    Button ActionButton(Transform parent, string name, string label, float x, float yBottom, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(x, yBottom);
        rt.sizeDelta = new Vector2(w, h);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.72f, 0.66f, 0.52f, 1f); UISkin.ButtonPrimary(img);
        var btn = go.GetComponent<Button>(); btn.targetGraphic = img;
        var lbl = Txt(go.transform, "Label", 20, FontStyle.Bold, Ink, TextAnchor.MiddleCenter, 0, 0, 0, 0);
        var lrt = (RectTransform)lbl.transform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        lbl.text = label;
        return btn;
    }

    static Color DarkTint(Color c) => new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f, 1f);

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
