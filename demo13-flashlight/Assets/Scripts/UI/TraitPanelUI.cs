using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 특성(퍽) 패널 — 카테고리별 특성 목록 + PP 잔량 + 클릭 해금(선행·비용·부정상한 준수).
/// 'K' 토글, ESC 닫기. 데이터: TraitManager 코어. self-spawn DontDestroyOnLoad 싱글톤(QuestLogUI 패턴). 그레이박스.
/// </summary>
public class TraitPanelUI : MonoBehaviour
{
    static TraitPanelUI instance;
    static GameObject uiRoot;
    static bool isShowing;
    public static bool IsShowing => isShowing;

    RectTransform listContent;
    Text ppText;

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
        var go = new GameObject("TraitPanelUI");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<TraitPanelUI>();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    public static void Show()
    {
        if (isShowing) { Hide(); return; }
        if (instance == null)
        {
            var go = new GameObject("TraitPanelUI");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<TraitPanelUI>();
        }
        instance.BuildUI();
        isShowing = true;
    }

    public static void Hide()
    {
        isShowing = false;
        if (uiRoot != null) { Destroy(uiRoot); uiRoot = null; }
    }

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
    //  UI 빌드
    // ─────────────────────────────────────────────
    void BuildUI()
    {
        uiRoot = new GameObject("TraitPanelCanvas");
        var canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 920;
        var scaler = uiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
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

        // 디버그 +10 PP
        MakeButton(pRT, "+10 PP (디버그)", UITheme.Cell, new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-120, -16), new Vector2(130, 30), () =>
            {
                if (TraitManager.Instance != null) { TraitManager.Instance.GrantPP(10); Rebuild(); }
            });

        // 닫기 X
        MakeButton(pRT, "✕", UITheme.Negative, new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-44, -16), new Vector2(30, 30), Hide);

        // 안내
        MakeText(pRT, "행을 클릭해 PP로 해금 · 부정 특성(낙인)은 PP 환급 · 부정 최대 3개 · K/ESC 닫기",
            13, UITheme.TextMuted, TextAnchor.MiddleLeft,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -56), new Vector2(-48, 22));

        // 스크롤 리스트
        BuildScroll(pRT);

        Rebuild();
    }

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
            ppText.text = "PP -";
            AddRowLabel("TraitManager 없음 (런타임 미부팅)");
            return;
        }
        if (tm.AllTraits.Count == 0) tm.EnsureLoaded();

        ppText.text = $"PP {tm.AvailablePP}   ·   부정 {tm.NegativeTraitCount}/3";

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

    void MakeButton(RectTransform parent, string label, Color col, Vector2 aMin, Vector2 aMax,
                    Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var img = NewImage(parent, $"Btn_{label}", col);
        var rt = img.rectTransform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = aMin;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors; colors.highlightedColor = UITheme.CellHover; colors.pressedColor = UITheme.CellPressed; btn.colors = colors;
        btn.onClick.AddListener(onClick);
        MakeText(rt, label, 13, UITheme.TextBright, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }
}
