using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도크 프레임의 **uGUI 생성**. 배경 · 제목(좌상단) · 닫기 X(우상단) · 버튼 · 입양 자리.
///
/// 모양만 다루는 부분이다 — 무엇을 띄울지는 Facility, 어디에 앉힐지는 Layout이 정한다.
/// </summary>
public partial class HideoutDockPanel
{

    // ── uGUI 코드 생성 ──────────────────────────────────────────────
    void Build()
    {
        _root = new GameObject("HideoutDockPanel");
        _root.transform.SetParent(transform, false);

        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefW, RefH);
        scaler.matchWidthOrHeight = 0.5f;
        _root.AddComponent<GraphicRaycaster>();

        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(_root.transform, false);
        _panel = panelGO.AddComponent<RectTransform>();
        var bg = panelGO.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.07f, 0.085f, 0.93f);

        _title   = MakeText(panelGO.transform, "Title", 40, TextAnchor.UpperLeft,
                            // ⚠️ 상단 stretch에서 offMax.y는 위쪽 바깥으로 나가는 값이다.
                            // +56이면 제목이 패널 밖으로 떠서 씬 헤더와 겹친다. 안쪽(-)으로 잡는다.
                            // 우상단 X 자리(-90)를 비워 둔다.
                            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -84f), new Vector2(-90f, -24f));
        _body    = MakeText(panelGO.transform, "Body", 26, TextAnchor.UpperLeft,
                            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28f, 96f), new Vector2(-56f, -190f));
        _body.color = new Color(0.78f, 0.78f, 0.74f);

        _openBtn  = MakeButton(panelGO.transform, "Open",  "열기",  new Vector2(0f, 0f), new Vector2(28f, 24f),  new Vector2(200f, 56f));
        // 닫기 = 우측 상단 X. 아래에 두면 내용이 쓸 세로 자리를 100px 가까이 잡아먹는다.
        _closeBtn = MakeButton(panelGO.transform, "Close", "✕", new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(52f, 52f));

        _statusText = MakeText(panelGO.transform, "Status", 24, TextAnchor.LowerLeft,
                               new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(28f, 170f), new Vector2(-56f, 340f));
        _statusText.color = new Color(0.86f, 0.82f, 0.60f);

        _upgradeBtn = MakeButton(panelGO.transform, "Upgrade", "업그레이드",
                                 new Vector2(0f, 0f), new Vector2(28f, 92f), new Vector2(260f, 56f));

        // 기능 버튼 - 건설 버튼 오른쪽. 시설을 실제로 쓰는 입구(제작/요리/창고/휴식).
        _useBtn = MakeButton(panelGO.transform, "Use", "사용",
                             new Vector2(0f, 0f), new Vector2(300f, 92f), new Vector2(232f, 56f));
        _useBtn.gameObject.SetActive(false);

        // 입양 자리 — 제목 아래, 버튼 위
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(panelGO.transform, false);
        _content = contentGO.AddComponent<RectTransform>();
        _content.anchorMin = new Vector2(0f, 0f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.offsetMin = new Vector2(16f, 92f);
        _content.offsetMax = new Vector2(-16f, -80f);
        // 입양하는 UI들은 1920 폭 전체화면 전제로 만들어져 있어, 도크에 넣으면
        // 자식들이 밖으로 삐져나온다. 도크 경계에서 잘라 화면이 지저분해지지 않게.
        contentGO.AddComponent<RectMask2D>();

        _openBtn.onClick.AddListener(() => { _onOpen?.Invoke(); });
        _closeBtn.onClick.AddListener(Hide);
        _useBtn.onClick.AddListener(UseFacility);
        _upgradeBtn.onClick.AddListener(() =>
        {
            var mm = HideoutModuleManager.Instance;
            if (mm == null || string.IsNullOrEmpty(_module)) return;
            if (mm.Upgrade(_module)) RefreshModule();
            else { mm.CanUpgrade(_module, out string why); ToastManager.Show(why, ToastManager.ToastType.Warning); }
        });
    }

    static Text MakeText(Transform parent, string name, int size, TextAnchor anchor,
                         Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = new Vector2(offMin.x, offMin.y);
        rt.offsetMax = new Vector2(offMax.x, offMax.y);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    static Button MakeButton(Transform parent, string name, string label,
                             Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.25f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var t = MakeText(go.transform, "Label", 28, TextAnchor.MiddleCenter,
                         Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        t.text = label;
        return btn;
    }
}
