using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 소음 HUD — "내가 지금 얼마나 시끄러운지"를 좌하단 **귀 아이콘**으로 표시.
/// (docs/combat.md 소음 시스템, 2026-07-11 변경: 월드 원형 VFX 제거 → 귀 아이콘만)
/// `PlayerNoise.Level01`(0~1)에 따라 귀 색(조용=초록 → 시끄러움=빨강)·밝기·세기 점(3개)이 반응.
/// 조용/안전가옥이면 흐려짐. 자가 부트스트랩(QuickSlotBar 패턴).
/// </summary>
public class NoiseHUD : MonoBehaviour
{
    public static NoiseHUD Instance { get; private set; }

    CanvasGroup group;
    RectTransform earRT;
    Image earImg;
    Image[] dots = new Image[3];

    static Sprite _earSprite, _dotSprite;

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
        Build();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        var pn = PlayerNoise.Instance;
        bool inSafehouse = UIManager.Instance != null && UIManager.Instance.IsSafehouse;
        float level = (pn != null && !inSafehouse) ? pn.Level01 : 0f;

        Color c = level < 0.4f ? new Color(0.4f, 0.8f, 0.4f)
                : level < 0.75f ? new Color(0.9f, 0.8f, 0.3f)
                : new Color(0.9f, 0.35f, 0.3f);

        if (earImg != null) earImg.color = c;
        // 시끄러울수록 귀가 살짝 커짐(주목).
        if (earRT != null) earRT.localScale = Vector3.one * (1f + 0.18f * level);

        // 세기 점 3개 — 레벨 임계 넘을 때마다 켜짐.
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null) continue;
            bool on = level >= (i + 1) / 4f;   // 0.25 / 0.5 / 0.75
            var dc = on ? c : new Color(c.r, c.g, c.b, 0.15f);
            dots[i].color = dc;
        }

        // 조용하면 흐리게(존재는 유지 — "지금 조용함"도 정보).
        if (group != null) group.alpha = Mathf.Lerp(group.alpha, level > 0.02f ? 1f : 0.4f, Time.deltaTime * 8f);
    }

    void Build()
    {
        var canvasGO = new GameObject("Noise_Canvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
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
        rt.anchoredPosition = new Vector2(46, 46);
        rt.sizeDelta = new Vector2(140, 56);

        // 귀 아이콘
        var earGO = new GameObject("Ear", typeof(RectTransform), typeof(Image));
        earGO.transform.SetParent(rt, false);
        earRT = earGO.GetComponent<RectTransform>();
        earRT.anchorMin = earRT.anchorMax = earRT.pivot = new Vector2(0, 0.5f);
        earRT.anchoredPosition = new Vector2(0, 0);
        earRT.sizeDelta = new Vector2(48, 48);
        earImg = earGO.GetComponent<Image>();
        earImg.sprite = EarSprite();
        earImg.color = new Color(0.4f, 0.8f, 0.4f);

        // 세기 점 3개(귀 오른쪽, 위로 갈수록 큼)
        for (int i = 0; i < dots.Length; i++)
        {
            var dGO = new GameObject("Dot" + i, typeof(RectTransform), typeof(Image));
            dGO.transform.SetParent(rt, false);
            var drt = dGO.GetComponent<RectTransform>();
            drt.anchorMin = drt.anchorMax = drt.pivot = new Vector2(0, 0.5f);
            float sz = 9f + i * 4f;
            drt.sizeDelta = new Vector2(sz, sz);
            drt.anchoredPosition = new Vector2(56 + i * 22f, 0);
            var img = dGO.GetComponent<Image>();
            img.sprite = DotSprite();
            img.color = new Color(0.4f, 0.8f, 0.4f, 0.15f);
            dots[i] = img;
        }
    }

    // ── 절차적 스프라이트 ─────────────────────────────────────────────
    /// <summary>귀 실루엣(크레센트 귓바퀴 + 귓불).</summary>
    static Sprite EarSprite()
    {
        if (_earSprite != null) return _earSprite;
        const int S = 64;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var px = new Color[S * S];
        Vector2 outerC = new Vector2(S * 0.46f, S * 0.54f); float outerR = S * 0.40f;
        Vector2 innerC = new Vector2(S * 0.57f, S * 0.52f); float innerR = S * 0.23f;
        Vector2 lobeC  = new Vector2(S * 0.40f, S * 0.18f); float lobeR  = S * 0.15f;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                Vector2 p = new Vector2(x, y);
                bool outer = (p - outerC).sqrMagnitude <= outerR * outerR;
                bool inner = (p - innerC).sqrMagnitude <  innerR * innerR;
                bool lobe  = (p - lobeC).sqrMagnitude  <= lobeR * lobeR;
                bool ear = (outer && !inner) || lobe;   // 귓바퀴(크레센트) + 귓불
                px[y * S + x] = new Color(1f, 1f, 1f, ear ? 1f : 0f);
            }
        tex.SetPixels(px); tex.Apply();
        _earSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        return _earSprite;
    }

    /// <summary>세기 점(작은 원).</summary>
    static Sprite DotSprite()
    {
        if (_dotSprite != null) return _dotSprite;
        const int R = 16;
        var tex = new Texture2D(R * 2, R * 2, TextureFormat.RGBA32, false);
        var px = new Color[R * 2 * R * 2];
        for (int y = 0; y < R * 2; y++)
            for (int x = 0; x < R * 2; x++)
            {
                float d = Mathf.Sqrt((x - R) * (x - R) + (y - R) * (y - R)) / R;
                px[y * R * 2 + x] = new Color(1f, 1f, 1f, d <= 1f ? 1f : 0f);
            }
        tex.SetPixels(px); tex.Apply();
        _dotSprite = Sprite.Create(tex, new Rect(0, 0, R * 2, R * 2), new Vector2(0.5f, 0.5f), R * 2);
        return _dotSprite;
    }
}
