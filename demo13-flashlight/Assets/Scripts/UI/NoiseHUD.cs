using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 소음 HUD 미터 — "내가 지금 얼마나 시끄러운지"를 화면 좌하단 막대로 표시.
/// (docs/combat.md 소음 시스템 2026-07-10) PlayerNoise.Level01(0~1)을 읽어 채움 + 색(조용=초록 → 시끄러움=빨강).
/// 조용(레벨≈0)하거나 안전가옥이면 흐려짐. 자가 부트스트랩(QuickSlotBar 패턴).
/// </summary>
public class NoiseHUD : MonoBehaviour
{
    public static NoiseHUD Instance { get; private set; }

    Canvas canvas;
    RectTransform fill;
    Image fillImg;
    CanvasGroup group;
    Font font;

    const int SortingOrder = 29;   // 퀵슬롯(30) 바로 아래

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (MapToolScene.IsActive) return;
        if (Instance != null) return;
        var go = new GameObject("[NoiseHUD]");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<NoiseHUD>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 14)
               ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Build();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        var pn = PlayerNoise.Instance;
        bool inSafehouse = UIManager.Instance != null && UIManager.Instance.IsSafehouse;
        float level = (pn != null && !inSafehouse) ? pn.Level01 : 0f;

        if (fill != null) fill.anchorMax = new Vector2(Mathf.Clamp01(level), 1f);
        if (fillImg != null)
            fillImg.color = level < 0.4f ? new Color(0.4f, 0.8f, 0.4f)
                          : level < 0.75f ? new Color(0.9f, 0.8f, 0.3f)
                          : new Color(0.9f, 0.35f, 0.3f);
        // 조용하면 흐리게(존재는 유지 — "지금 조용함"도 정보).
        if (group != null) group.alpha = Mathf.Lerp(group.alpha, level > 0.02f ? 1f : 0.35f, Time.deltaTime * 8f);
    }

    void Build()
    {
        var canvasGO = new GameObject("Noise_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var rootGO = new GameObject("Root", typeof(RectTransform));
        rootGO.transform.SetParent(canvas.transform, false);
        group = rootGO.AddComponent<CanvasGroup>();
        var rt = rootGO.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(40, 40);
        rt.sizeDelta = new Vector2(220, 40);

        // 라벨
        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(rt, false);
        var lbl = lblGO.AddComponent<Text>();
        lbl.font = font; lbl.text = "소음"; lbl.fontSize = 16; lbl.color = new Color(0.85f, 0.85f, 0.85f);
        lbl.alignment = TextAnchor.MiddleLeft;
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(0, 1);
        lrt.pivot = new Vector2(0, 0.5f); lrt.anchoredPosition = new Vector2(0, 0); lrt.sizeDelta = new Vector2(44, 0);

        // 바 배경
        var barGO = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
        barGO.transform.SetParent(rt, false);
        var brt = barGO.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0, 0.5f); brt.anchorMax = new Vector2(1, 0.5f);
        brt.pivot = new Vector2(0, 0.5f); brt.anchoredPosition = new Vector2(50, 0);
        brt.sizeDelta = new Vector2(-50, 16);
        barGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        // 채움
        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(barGO.transform, false);
        fill = fillGO.GetComponent<RectTransform>();
        fill.anchorMin = new Vector2(0, 0); fill.anchorMax = new Vector2(0f, 1f);
        fill.pivot = new Vector2(0, 0.5f);
        fill.offsetMin = new Vector2(1, 1); fill.offsetMax = new Vector2(-1, -1);
        fillImg = fillGO.GetComponent<Image>();
        fillImg.color = new Color(0.4f, 0.8f, 0.4f);
    }
}
