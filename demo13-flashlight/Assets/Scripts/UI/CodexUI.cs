using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 아이템 도감(Codex) 패널 — 좌 카테고리 탭 + 가운데 아이콘 격자 + 우 상세. (docs/items.md §아이템 도감)
/// 'U' 토글, ESC 닫기. 미발견 = 실루엣 + 이름 '???'(설명도 가림). 1차 = 기록·열람만(보상 없음).
/// 데이터: CodexManager(발견 set) + ItemDatabase(전체 목록).
///
/// TraitPanelUI(K)와 동일 패턴 — 프리팹 우선(Resources/UI/CodexUI.prefab), 없으면 코드 생성 폴백.
/// 키 토글이라 부팅 시 영속 인스턴스 1개(캔버스 숨김)로 U 폴링.
/// ※ N은 RaidMapUI(레이드 지도)가 선점 — 중복 폴링 시 Update 순서 미정의로 어느 쪽이 열릴지 뒤집힘.
/// </summary>
public class CodexUI : MonoBehaviour
{
    static CodexUI instance;
    static bool isShowing;
    public static bool IsShowing => isShowing;

    [SerializeField] Canvas canvas;              // 베이크된 스켈레톤 루트. null=코드생성 폴백 필요.
    [SerializeField] RectTransform tabContent;   // 좌: 카테고리 탭 컨테이너
    [SerializeField] RectTransform gridContent;  // 가운데: 아이콘 격자 컨테이너
    [SerializeField] RectTransform detailContent;// 우: 상세 컨테이너
    [SerializeField] Text headerText;            // 헤더(수집률)
    [SerializeField] Button closeBtn;

    bool IsGenerated => canvas != null;

    // 카테고리 탭 (ItemData의 실제 enum 7종)
    static readonly ItemCategory[] Cats =
    {
        ItemCategory.Weapon, ItemCategory.Medical, ItemCategory.Consumable,
        ItemCategory.Material, ItemCategory.Valuable, ItemCategory.Key, ItemCategory.Misc,
    };
    static string CatName(ItemCategory c)
    {
        switch (c)
        {
            case ItemCategory.Weapon:     return "무기";
            case ItemCategory.Medical:    return "의료";
            case ItemCategory.Consumable: return "소비";
            case ItemCategory.Material:   return "재료";
            case ItemCategory.Valuable:   return "귀중품";
            case ItemCategory.Key:        return "열쇠";
            default:                      return "잡템";
        }
    }

    int _catIndex;
    string _selectedId;   // 상세에 띄운 아이템(발견된 것만)

    static Font _kr;
    static Font KR
    {
        get
        {
            if (_kr == null)
                _kr = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Arial" }, 16);
            return _kr != null ? _kr : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;   // 맵툴 씬에선 비활성(U 폴링·DDOL 캔버스 생성 방지)
        if (instance != null) return;
        if (FindFirstObjectByType<CodexUI>(FindObjectsInactive.Include) != null) return;
        EnsureInstance();   // U 폴링용 영속 인스턴스
    }

    static void EnsureInstance()
    {
        if (instance != null) return;
        var prefab = Resources.Load<GameObject>("UI/CodexUI");
        GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("CodexUI");
        go.name = "CodexUI";
        if (prefab == null) go.AddComponent<CodexUI>();
        DontDestroyOnLoad(go);
        // 방어: 프리팹을 베이크했는데 루트에 CodexUI 컴포넌트가 없으면 Awake가 안 돌아 instance가 null → Show()에서 NRE.
        if (instance == null) instance = go.GetComponent<CodexUI>() ?? go.AddComponent<CodexUI>();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (IsGenerated)   // 프리팹 경로: 폰트·onClick 재바인딩(직렬화 안 됨) + 부팅 시 숨김
        {
            ApplyFonts();
            WireEvents();
            SetVisible(false);
        }
    }

    public static void Show()
    {
        if (isShowing) { Hide(); return; }
        EnsureInstance();
        instance.Open();
    }

    void Open()
    {
        if (!IsGenerated) BuildSkeleton();
        SetVisible(true);
        _selectedId = null;
        Rebuild();
        isShowing = true;
    }

    public static void Hide()
    {
        isShowing = false;
        if (instance != null) instance.SetVisible(false);
    }

    void SetVisible(bool v) { if (canvas != null) canvas.gameObject.SetActive(v); }

    const KeyCode ToggleKey = KeyCode.U;   // 사용자 지정. N=RaidMapUI, M=NavigationHUD, J=QuestLog, K=Trait 선점

    void Update()
    {
        if (GameInput.GetKeyDown(ToggleKey))
        {
            if (isShowing) Hide();
            else if (UIManager.Instance == null || !UIManager.Instance.IsAnyUIOpen()) Show();
        }
        if (!isShowing) return;
        if (GameInput.GetKeyDown(KeyCode.Escape)) Hide();
    }

    void ApplyFonts()
    {
        foreach (var t in GetComponentsInChildren<Text>(true))
            if (t.font == null || t.font.dynamic) t.font = KR;
    }

    void WireEvents()
    {
        if (closeBtn != null) { closeBtn.onClick.RemoveAllListeners(); closeBtn.onClick.AddListener(Hide); }
    }

    // ─────────────────────────────────────────────
    //  스켈레톤 (정적 프레임 — 동적 내용은 Rebuild)
    // ─────────────────────────────────────────────
    void BuildSkeleton()
    {
        var uiRoot = new GameObject("CodexCanvas");
        uiRoot.transform.SetParent(transform, false);
        canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 920;
        var scaler = uiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        uiRoot.AddComponent<GraphicRaycaster>();

        var dim = NewImage(uiRoot.transform, "Dim", UITheme.Backdrop);
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        var panel = NewImage(uiRoot.transform, "Panel", UITheme.Panel);
        var pRT = panel.rectTransform;
        pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f, 0.5f);
        pRT.pivot = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(1180, 780);

        headerText = MakeText(pRT, "도감 (Codex)", 24, UITheme.Gold, TextAnchor.MiddleLeft,
                              new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -34), new Vector2(700, 32));

        closeBtn = MakeButton(pRT, "✕", UITheme.Cell, new Vector2(1, 1), new Vector2(1, 1),
                              new Vector2(-52, -46), new Vector2(36, 32));
        closeBtn.onClick.AddListener(Hide);

        // 좌: 카테고리 탭
        var tabBg = NewImage(pRT, "Tabs", UITheme.PanelAlt);
        var tRT = tabBg.rectTransform;
        tRT.anchorMin = new Vector2(0, 0); tRT.anchorMax = new Vector2(0, 1); tRT.pivot = new Vector2(0, 0.5f);
        tRT.anchoredPosition = new Vector2(20, -30); tRT.sizeDelta = new Vector2(160, -100);
        tabContent = tRT;

        // 가운데: 아이콘 격자
        var gridBg = NewImage(pRT, "Grid", UITheme.PanelAlt);
        var gRT = gridBg.rectTransform;
        gRT.anchorMin = new Vector2(0, 0); gRT.anchorMax = new Vector2(0, 1); gRT.pivot = new Vector2(0, 0.5f);
        gRT.anchoredPosition = new Vector2(192, -30); gRT.sizeDelta = new Vector2(620, -100);
        gridContent = gRT;

        // 우: 상세
        var detBg = NewImage(pRT, "Detail", UITheme.PanelAlt);
        var dRT = detBg.rectTransform;
        dRT.anchorMin = new Vector2(1, 0); dRT.anchorMax = new Vector2(1, 1); dRT.pivot = new Vector2(1, 0.5f);
        dRT.anchoredPosition = new Vector2(-20, -30); dRT.sizeDelta = new Vector2(340, -100);
        detailContent = dRT;
    }

    // ─────────────────────────────────────────────
    //  동적 내용
    // ─────────────────────────────────────────────
    void Rebuild()
    {
        BuildTabs();
        BuildGrid();
        BuildDetail();
        UpdateHeader();
    }

    static List<ItemData> CatItems(ItemCategory c)
    {
        var list = ItemDatabase.GetByCategory(c);
        return list ?? new List<ItemData>();
    }

    void UpdateHeader()
    {
        if (headerText == null) return;
        var all = ItemDatabase.GetAll();
        int total = all != null ? all.Length : 0;
        int found = CodexManager.Instance != null ? CodexManager.Instance.DiscoveredCount : 0;
        headerText.text = $"도감 (Codex)    <color=#8A8073>전체 {found}/{total}</color>";
        headerText.supportRichText = true;
    }

    void ClearChildren(RectTransform rt)
    {
        if (rt == null) return;
        for (int i = rt.childCount - 1; i >= 0; i--) Destroy(rt.GetChild(i).gameObject);
    }

    void BuildTabs()
    {
        ClearChildren(tabContent);
        if (tabContent == null) return;

        for (int i = 0; i < Cats.Length; i++)
        {
            var cat = Cats[i];
            int idx = i;
            var items = CatItems(cat);
            int found = 0;
            foreach (var d in items)
                if (d != null && CodexManager.Instance != null && CodexManager.Instance.IsDiscovered(d.itemId)) found++;

            bool sel = idx == _catIndex;
            var btn = MakeButton(tabContent, "", sel ? UITheme.Accent : UITheme.Cell,
                                 new Vector2(0, 1), new Vector2(0, 1),
                                 new Vector2(8, -8 - i * 46), new Vector2(144, 40));
            MakeText(btn.GetComponent<RectTransform>(), $"{CatName(cat)}  {found}/{items.Count}", 14,
                     sel ? UITheme.TextBright : UITheme.TextMuted, TextAnchor.MiddleCenter,
                     Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            btn.onClick.AddListener(() => { _catIndex = idx; _selectedId = null; Rebuild(); });
        }
    }

    void BuildGrid()
    {
        ClearChildren(gridContent);
        if (gridContent == null) return;

        var items = CatItems(Cats[_catIndex]);
        const int cols = 8, cell = 68, gap = 6;

        for (int i = 0; i < items.Count; i++)
        {
            var d = items[i];
            if (d == null) continue;
            bool found = CodexManager.Instance != null && CodexManager.Instance.IsDiscovered(d.itemId);
            int cx = i % cols, cy = i / cols;

            var slot = MakeButton(gridContent, "", UITheme.Cell, new Vector2(0, 1), new Vector2(0, 1),
                                  new Vector2(10 + cx * (cell + gap), -10 - cy * (cell + gap)),
                                  new Vector2(cell, cell));

            // 아이콘 — 미발견은 실루엣(검게)
            if (d.icon != null)
            {
                var ic = NewImage(slot.GetComponent<RectTransform>(), "Icon", found ? Color.white : new Color(0f, 0f, 0f, 0.85f));
                ic.sprite = d.icon;
                ic.preserveAspect = true;
                ic.raycastTarget = false;
                var iRT = ic.rectTransform;
                iRT.anchorMin = Vector2.zero; iRT.anchorMax = Vector2.one;
                iRT.offsetMin = new Vector2(6, 6); iRT.offsetMax = new Vector2(-6, -6);
            }
            else
            {
                // 아이콘 에셋 없음(대부분 미제작) — 발견=이름 축약, 미발견=???
                MakeText(slot.GetComponent<RectTransform>(), found ? Short(d.displayName) : "?", 12,
                         found ? UITheme.TextBright : UITheme.TextMuted, TextAnchor.MiddleCenter,
                         Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            if (found)
            {
                string id = d.itemId;
                slot.onClick.AddListener(() => { _selectedId = id; BuildDetail(); });
            }
            // 미발견 슬롯은 클릭해도 상세 없음(이름·설명 모두 가림)
        }
    }

    static string Short(string s) => string.IsNullOrEmpty(s) ? "?" : (s.Length <= 4 ? s : s.Substring(0, 4));

    void BuildDetail()
    {
        ClearChildren(detailContent);
        if (detailContent == null) return;

        var d = string.IsNullOrEmpty(_selectedId) ? null : ItemDatabase.Get(_selectedId);
        bool found = d != null && CodexManager.Instance != null && CodexManager.Instance.IsDiscovered(d.itemId);

        if (!found)
        {
            MakeText(detailContent, "발견한 아이템을 선택하면\n정보가 표시된다.", 14, UITheme.TextMuted, TextAnchor.MiddleCenter,
                     Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return;
        }

        MakeText(detailContent, d.displayName, 20, UITheme.Gold, TextAnchor.UpperLeft,
                 new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(310, 28));

        MakeText(detailContent, $"{CatName(d.category)} · {d.rarity}", 13, UITheme.TextMuted, TextAnchor.UpperLeft,
                 new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -46), new Vector2(310, 20));

        // 설명(이상현상 아이템은 로어 조각)
        var desc = MakeText(detailContent, string.IsNullOrEmpty(d.description) ? "(설명 없음)" : d.description,
                            14, UITheme.TextBright, TextAnchor.UpperLeft,
                            new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -76), new Vector2(310, 300));
        desc.horizontalOverflow = HorizontalWrapMode.Wrap;
        desc.verticalOverflow = VerticalWrapMode.Truncate;

        MakeText(detailContent, $"무게 {d.weight:0.##}kg · 판매 {d.sellPrice}", 13, UITheme.TextMuted, TextAnchor.LowerLeft,
                 new Vector2(0, 0), new Vector2(0, 0), new Vector2(14, 14), new Vector2(310, 20));
    }

    // ── uGUI 헬퍼 (TraitPanelUI와 동일 스타일) ───────────────────────

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static Image NewImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    Text MakeText(RectTransform parent, string text, int size, Color color, TextAnchor anchor,
                  Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 sizeOrOffMax)
    {
        var go = new GameObject("T", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        if (aMin == Vector2.zero && aMax == Vector2.one)
        {
            rt.offsetMin = offMin; rt.offsetMax = sizeOrOffMax;
        }
        else
        {
            rt.pivot = aMin;
            rt.anchoredPosition = offMin; rt.sizeDelta = sizeOrOffMax;
        }
        var t = go.AddComponent<Text>();
        t.font = KR; t.fontSize = size; t.color = color; t.alignment = anchor; t.text = text;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    Button MakeButton(RectTransform parent, string label, Color col, Vector2 aMin, Vector2 aMax,
                      Vector2 anchoredPos, Vector2 size)
    {
        var img = NewImage(parent, $"Btn_{label}", col);
        var rt = img.rectTransform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = aMin;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors; colors.highlightedColor = UITheme.CellHover; colors.pressedColor = UITheme.CellPressed; btn.colors = colors;
        if (!string.IsNullOrEmpty(label))
            MakeText(rt, label, 13, UITheme.TextBright, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return btn;
    }
}
