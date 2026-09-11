using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 상단 **시설 바로가기 바**. 소품을 직접 누르는 것과 완전히 같은 동작이다.
///
/// 소품만으로는 불편하다 — 소품이 작고, 어디에 뭐가 있는지 외워야 한다.
/// 버튼 폭은 도크를 뺀 남는 자리에 맞춰 나눈다(시설이 늘어도 도크 밑으로 안 들어가게).
/// </summary>
public partial class HideoutDiorama
{
    // ── 상단 시설 바로가기 ───────────────────────────────────────────
    // 소품을 직접 누르는 것만으로는 불편하다 — 소품이 작고, 어디에 뭐가 있는지 외워야 한다.
    // 화면 상단에 시설 목록을 한 줄로 두고, 눌렀을 때 소품을 누른 것과 **똑같이** 동작시킨다.

    const float BarBtnW = 132f, BarBtnH = 50f, BarGap = 6f, BarLeft = 40f, BarTop = -120f;

    GameObject _barRoot;
    readonly List<UnityEngine.UI.Image> _barBtns = new List<UnityEngine.UI.Image>();
    readonly List<HideoutFacilityAnchor> _barKeys = new List<HideoutFacilityAnchor>();

    static readonly Color BarIdle = UITheme.Cell;
    static readonly Color BarOn   = UITheme.Accent;

    void BuildFacilityBar()
    {
        _barRoot = new GameObject("HideoutFacilityBar");
        _barRoot.transform.SetParent(transform, false);

        var canvas = _barRoot.AddComponent<Canvas>();
        canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 35;                       // 인벤토리(40)·도크(60)보다 아래
        var scaler = _barRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _barRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        // 버튼 폭은 **남는 자리에 맞춰** 나눈다. 고정 폭으로 두면 시설이 하나 늘어난 순간
        // 마지막 버튼이 도크 밑으로 들어가 안 눌린다.
        var listed = new List<HideoutFacilityAnchor>();
        foreach (var a in _anchors)
        {
            if (a == null || a.moduleKey == "idle" || a.moduleKey == "dispatch") continue;
            if (string.IsNullOrEmpty(FacilityLabel.KoreanFor(a.moduleKey))) continue;
            listed.Add(a);
        }
        string[] order = { "stash", "workbench", "bed", "cooking", "medical", "radio", "generator" };
        listed.Sort((a,b)=>System.Array.IndexOf(order,a.moduleKey).CompareTo(System.Array.IndexOf(order,b.moduleKey)));
        if (listed.Count == 0) return;

        float avail = 1920f - HideoutDockPanel.RightMargin
                    - HideoutDockPanel.MaxRightDockWidth - BarLeft - 16f;
        float btnW = Mathf.Min(BarBtnW, (avail - BarGap * (listed.Count - 1)) / listed.Count);

        float x = BarLeft;

        foreach (var a in listed)
        {

            var go = new GameObject("Btn_" + a.moduleKey);
            go.transform.SetParent(_barRoot.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, BarTop);
            rt.sizeDelta = new Vector2(btnW, BarBtnH);

            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = BarIdle;

            var btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            var target = a;                                        // ⚠️ 클로저 캡처 — 반복 변수 그대로 쓰면 전부 마지막 것이 된다
            btn.onClick.AddListener(() => Select(target));

            var tGO = new GameObject("Label");
            tGO.transform.SetParent(go.transform, false);
            var trt = tGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var txt = tGO.AddComponent<UnityEngine.UI.Text>();
            txt.text = FacilityLabel.KoreanFor(a.moduleKey);
            txt.raycastTarget = false;
            txt.font = font;
            txt.fontSize = 20;
            // 도크가 넓어지면 버튼이 좁아진다 — 줄바꿈으로 잘리느니 살짝 넘치게 둔다.
            txt.horizontalOverflow = UnityEngine.HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = UnityEngine.VerticalWrapMode.Overflow;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.92f, 0.90f, 0.85f);

            _barBtns.Add(img);
            _barKeys.Add(a);
            x += btnW + BarGap;
        }
    }

    /// <summary>지금 선택된 시설을 바에서 강조한다. 어디를 보고 있는지 한눈에 알려야 한다.</summary>
    void RefreshFacilityBar()
    {
        for (int i = 0; i < _barBtns.Count; i++)
            if (_barBtns[i] != null)
                _barBtns[i].color = (_barKeys[i] == _current) ? BarOn : BarIdle;
    }
}
