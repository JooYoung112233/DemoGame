using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 특성(퍽) 패널 — 카테고리별 특성 목록 + PP 잔량 + 클릭 해금(선행·비용·부정상한 준수).
/// 'K' 토글, ESC 닫기. 데이터: TraitManager 코어.
/// 프리팹 우선: Resources/UI/TraitPanelUI.prefab 베이크본(정적 스켈레톤)을 Instantiate → 캔버스만 토글,
/// 동적 행은 Rebuild. 프리팹 없으면 코드 생성 폴백. 키 토글이라 부팅 시 영속 인스턴스 1개(캔버스 숨김)로 K 폴링.
/// </summary>
public class TraitPanelUI : MonoBehaviour
{
    static TraitPanelUI instance;
    static bool isShowing;
    public static bool IsShowing => isShowing;

    [SerializeField] Canvas canvas;             // 베이크된 스켈레톤 루트(자식 캔버스). null=코드생성 폴백 필요.
    [SerializeField] RectTransform listContent; // 동적 행 컨테이너
    [SerializeField] Text ppText;               // PP 잔량 표시
    [SerializeField] Button debugPpBtn;         // +10 PP (디버그)
    [SerializeField] Button closeBtn;           // 닫기 ✕

    bool IsGenerated => canvas != null;

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
        if (instance != null) return;
        if (FindFirstObjectByType<TraitPanelUI>(FindObjectsInactive.Include) != null) return;
        EnsureInstance();   // 프리팹 우선(스켈레톤 보유), 없으면 bare. K 폴링용 영속 인스턴스.
    }

    static void EnsureInstance()
    {
        if (instance != null) return;
        // 프리팹 우선 — Instantiate가 Awake로 instance를 세팅. 없으면 코드 생성 폴백.
        var prefab = Resources.Load<GameObject>("UI/TraitPanelUI");
        GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("TraitPanelUI");
        go.name = "TraitPanelUI";
        if (prefab == null) go.AddComponent<TraitPanelUI>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (IsGenerated)   // 프리팹 인스턴스: 동적 폰트·onClick 재바인딩(직렬화 안 됨) + 부팅 시 숨김
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
        if (!IsGenerated) BuildSkeleton();   // 폴백: 코드로 스켈레톤 생성(+ 참조 할당)
        SetVisible(true);
        Rebuild();
        isShowing = true;
    }

    public static void Hide()
    {
        isShowing = false;
        if (instance != null) instance.SetVisible(false);
    }

    void SetVisible(bool v) { if (canvas != null) canvas.gameObject.SetActive(v); }

    void Update()
    {
        if (GameInput.GetKeyDown(KeyCode.K))
        {
            if (isShowing) Hide();
            else if (UIManager.Instance == null || !UIManager.Instance.IsAnyUIOpen()) Show();
        }
        if (!isShowing) return;
        if (GameInput.GetKeyDown(KeyCode.Escape)) Hide();
    }

    // ─────────────────────────────────────────────
    //  스켈레톤 빌드 (정적 프레임 — 동적 행은 Rebuild)
    // ─────────────────────────────────────────────
    void BuildSkeleton()
    {
        var uiRoot = new GameObject("TraitPanelCanvas");
        uiRoot.transform.SetParent(transform, false);   // 컴포넌트 GO 자식 → 프리팹에 베이크됨
        canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 920;
        var scaler = uiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        UITheme.ConfigureCanvasScale(scaler);
        uiRoot.AddComponent<GraphicRaycaster>();

        // 배경(클릭 차단)
        var dim = NewImage(uiRoot.transform, "Dim", UITheme.Backdrop);
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        // 중앙 패널
        var panel = NewImage(uiRoot.transform, "Panel", UITheme.Panel);
        var pRT = panel.rectTransform;
        pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f, 0.5f);
        pRT.pivot = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(1080, 760);

        // 헤더
        MakeText(pRT, "특성 (Traits)", 24, UITheme.Gold, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -16), new Vector2(360, 36)).fontStyle = FontStyle.Bold;

        ppText = MakeText(pRT, "PP 0", 20, UITheme.AccentBright, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(220, 32));
        ppText.fontStyle = FontStyle.Bold;

        // 디버그 +10 PP / 닫기 X (onClick은 WireEvents에서 — 프리팹 직렬화 안 됨)
        debugPpBtn = MakeButton(pRT, "+10 PP (디버그)", UITheme.Cell, new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-120, -16), new Vector2(130, 30));
        closeBtn = MakeButton(pRT, "✕", UITheme.Negative, new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-44, -16), new Vector2(30, 30));

        // 안내
        MakeText(pRT, "행을 클릭해 PP로 해금 · 부정 특성(낙인)은 PP 환급 · 부정 최대 3개 · K/ESC 닫기",
            13, UITheme.TextMuted, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -56), new Vector2(-48, 22));

        // 스크롤 리스트
        BuildScroll(pRT);

        WireEvents();
    }

    /// <summary>프리팹 인스턴스화 시 동적 OS 폰트를 직렬화된 Text들에 재바인딩.</summary>
    void ApplyFonts()
    {
        if (canvas == null) return;
        var f = KR;
        foreach (var t in canvas.GetComponentsInChildren<Text>(true)) t.font = f;
    }

    /// <summary>정적 버튼 onClick 재부착(프리팹 베이크는 onClick 직렬화 안 됨). 멱등.</summary>
    void WireEvents()
    {
        if (debugPpBtn != null)
        {
            debugPpBtn.onClick.RemoveAllListeners();
            debugPpBtn.onClick.AddListener(() =>
            {
                if (TraitManager.Instance != null) { TraitManager.Instance.GrantPP(10); Rebuild(); }
            });
        }
        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(Hide);
        }
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — 정적 스켈레톤만 1회 생성해 프리팹화. 동적 행은 런타임 Rebuild.</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        BuildSkeleton();
    }
#endif

    void BuildScroll(RectTransform parent)
    {
        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image), typeof(ScrollRect));
        viewportGO.transform.SetParent(parent, false);
        var vpRT = viewportGO.GetComponent<RectTransform>();
        vpRT.anchorMin = new Vector2(0, 0); vpRT.anchorMax = new Vector2(1, 1);
        vpRT.offsetMin = new Vector2(16, 16); vpRT.offsetMax = new Vector2(-16, -84);
        viewportGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.25f);

        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);
        listContent = contentGO.GetComponent<RectTransform>();
        listContent.anchorMin = new Vector2(0, 1); listContent.anchorMax = new Vector2(1, 1);
        listContent.pivot = new Vector2(0.5f, 1);
        var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.spacing = 4; vlg.padding = new RectOffset(8, 8, 8, 8);
        var fitter = contentGO.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewportGO.GetComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true;
        scroll.viewport = vpRT; scroll.content = listContent;
        scroll.scrollSensitivity = 30f;
        scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    // ─────────────────────────────────────────────
    //  목록 채우기
    // ─────────────────────────────────────────────
    static readonly TraitCategory[] CatOrder =
        { TraitCategory.Combat, TraitCategory.Survival, TraitCategory.Scavenging,
          TraitCategory.Stealth, TraitCategory.Social, TraitCategory.Anomaly };

    void Rebuild()
    {
        if (listContent == null) return;
        for (int i = listContent.childCount - 1; i >= 0; i--)
            Destroy(listContent.GetChild(i).gameObject);

        var tm = TraitManager.Instance;
        if (tm == null)
        {
            if (ppText != null) ppText.text = "PP -";
            AddRowLabel("TraitManager 없음 (런타임 미부팅)");
            return;
        }
        if (tm.AllTraits.Count == 0) tm.EnsureLoaded();

        if (ppText != null) ppText.text = $"PP {tm.AvailablePP}   ·   부정 {tm.NegativeTraitCount}/3";

        foreach (var cat in CatOrder)
        {
            var list = tm.AllTraits.Where(t => t.category == cat)
                .OrderBy(t => t.tier == TraitTier.Negative ? 99 : (int)t.tier)
                .ThenBy(t => t.displayName).ToList();
            if (list.Count == 0) continue;

            AddCategoryHeader(CatName(cat));
            foreach (var t in list) AddTraitRow(tm, t);
        }
    }

    void AddCategoryHeader(string name)
    {
        var go = NewImage(listContent, $"Cat_{name}", UITheme.PanelAlt);
        SetRowHeight(go.rectTransform, 26);
        var txt = MakeText(go.rectTransform, name, 15, UITheme.Header, TextAnchor.MiddleLeft,
            Vector2.zero, Vector2.one, new Vector2(10, 0), Vector2.zero);
        txt.fontStyle = FontStyle.Bold;
    }

    void AddTraitRow(TraitManager tm, TraitData t)
    {
        bool unlocked = tm.IsUnlocked(t.traitId);
        bool canUnlock = !unlocked && tm.CanUnlock(t.traitId, out _);

        Color bg = unlocked ? new Color(0.18f, 0.34f, 0.22f, 0.9f)
                 : canUnlock ? UITheme.Cell
                 : new Color(0.12f, 0.12f, 0.14f, 0.9f);
        var go = NewImage(listContent, $"Trait_{t.traitId}", bg);
        SetRowHeight(go.rectTransform, 46);

        string mark = unlocked ? "<color=#5FE08A>✔</color>" : canUnlock ? "<color=#E8E8E8>○</color>" : "<color=#888888>✖</color>";
        string tier = t.tier == TraitTier.Negative ? "낙인" : $"T{(int)t.tier}";
        string cost = t.ppCost > 0 ? $"PP {t.ppCost}" : t.ppCost < 0 ? $"환급 +{-t.ppCost}" : "무료";
        Color nameCol = unlocked ? UITheme.Positive : (t.tier == TraitTier.Negative ? UITheme.Negative : UITheme.TextBright);

        var l1 = MakeText(go.rectTransform, $"{mark}  <b>{t.displayName}</b>  <size=12><color=#9AA8B8>[{tier} · {cost}]</color></size>",
            15, nameCol, TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(12, -4), new Vector2(-24, 0));
        l1.supportRichText = true;

        MakeText(go.rectTransform, t.effectSummary ?? "", 12, UITheme.TextMuted, TextAnchor.LowerLeft,
            Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-24, 0));

        if (!unlocked)
        {
            var btn = go.gameObject.AddComponent<Button>();
            btn.targetGraphic = go;
            var id = t.traitId;
            btn.onClick.AddListener(() => TryUnlock(id));
            var colors = btn.colors; colors.highlightedColor = UITheme.CellHover; colors.pressedColor = UITheme.CellPressed; btn.colors = colors;
        }
    }

    void AddRowLabel(string msg)
    {
        var go = NewImage(listContent, "Msg", UITheme.PanelAlt);
        SetRowHeight(go.rectTransform, 30);
        MakeText(go.rectTransform, msg, 14, UITheme.TextMuted, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    void TryUnlock(string id)
    {
        var tm = TraitManager.Instance;
        if (tm == null) return;
        if (tm.IsUnlocked(id)) { ToastManager.Show("이미 보유한 특성", ToastManager.ToastType.Info); return; }
        if (tm.CanUnlock(id, out string reason))
        {
            tm.Unlock(id);
            Rebuild();
        }
        else ToastManager.Show(reason, ToastManager.ToastType.Warning);
    }

    static string CatName(TraitCategory c)
    {
        switch (c)
        {
            case TraitCategory.Combat: return "전투";
            case TraitCategory.Survival: return "생존·신체";
            case TraitCategory.Scavenging: return "회수·파밍";
            case TraitCategory.Stealth: return "잠행·기동";
            case TraitCategory.Social: return "사회";
            case TraitCategory.Anomaly: return "현상";
            default: return c.ToString();
        }
    }

    // ─────────────────────────────────────────────
    //  uGUI 헬퍼
    // ─────────────────────────────────────────────
    static void SetRowHeight(RectTransform rt, float h)
    {
        var le = rt.gameObject.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        le.minHeight = h; le.preferredHeight = h;
    }

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
        t.raycastTarget = false;   // 클릭은 행 버튼(Image)이 받게
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
        MakeText(rt, label, 13, UITheme.TextBright, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return btn;
    }
}
