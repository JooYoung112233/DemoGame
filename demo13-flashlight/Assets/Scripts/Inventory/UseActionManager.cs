using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아이템 사용 채널(시전 시간). useTimeSeconds>0인 아이템은 진행바를 띄우고
/// 시간이 차면 onComplete(효과 적용)를 호출한다. 0이면 호출부에서 즉시 적용.
/// timeScale=0(안전가옥)에서도 동작하도록 unscaled 시간 사용. ESC/이동 등으로 Cancel 가능.
/// 한 번에 하나의 채널만 진행(IsBusy).
/// </summary>
public class UseActionManager : MonoBehaviour
{
    public static UseActionManager Instance { get; private set; }

    bool active;
    float duration, elapsed;
    System.Action onComplete;

    GameObject root;
    RectTransform barFill;
    Text labelText;

    public bool IsBusy => active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        if (FindFirstObjectByType<UseActionManager>(FindObjectsInactive.Include) != null) return;
        var go = new GameObject("[UseActionManager]");
        DontDestroyOnLoad(go);
        go.AddComponent<UseActionManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    /// <summary>채널 시작. 이미 진행 중이면 무시(호출부에서 IsBusy 체크 권장).</summary>
    public void Begin(string text, float seconds, System.Action complete)
    {
        if (active) return;
        active = true;
        duration = Mathf.Max(0.01f, seconds);
        elapsed = 0f;
        onComplete = complete;
        if (labelText != null) labelText.text = text;
        if (root != null) root.SetActive(true);
        Apply(0f);
    }

    /// <summary>진행 취소(효과 미적용).</summary>
    public void Cancel()
    {
        if (!active) return;
        active = false;
        onComplete = null;
        if (root != null) root.SetActive(false);
    }

    void Update()
    {
        if (!active) return;
        elapsed += Time.unscaledDeltaTime;
        float p = Mathf.Clamp01(elapsed / duration);
        Apply(p);
        if (p >= 1f)
        {
            var cb = onComplete;
            active = false;
            onComplete = null;
            if (root != null) root.SetActive(false);
            cb?.Invoke();
        }
    }

    void Apply(float p)
    {
        if (barFill != null) barFill.anchorMax = new Vector2(p, 1f);
        if (labelText != null && active)
        {
            float remain = Mathf.Max(0f, duration - elapsed);
            int idx = labelText.text.IndexOf("  (");
            string baseTxt = idx >= 0 ? labelText.text.Substring(0, idx) : labelText.text;
            labelText.text = $"{baseTxt}  ({remain:0.0}s)";
        }
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("UseChannelCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;   // HUD 위, 인벤 패널 비슷
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 컨테이너(하단 중앙)
        root = new GameObject("UseChannelRoot", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvasGO.transform, false);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, 180);
        rt.sizeDelta = new Vector2(360, 52);
        root.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.9f);

        // 라벨
        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(rt, false);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 1); lblRT.anchorMax = new Vector2(1, 1);
        lblRT.pivot = new Vector2(0.5f, 1);
        lblRT.anchoredPosition = new Vector2(0, -4);
        lblRT.sizeDelta = new Vector2(-16, 24);
        labelText = lblGO.AddComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 15;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = new Color(0.9f, 0.95f, 1f);

        // 진행바 트랙
        var trackGO = new GameObject("Track", typeof(RectTransform), typeof(Image));
        trackGO.transform.SetParent(rt, false);
        var trackRT = trackGO.GetComponent<RectTransform>();
        trackRT.anchorMin = new Vector2(0, 0); trackRT.anchorMax = new Vector2(1, 0);
        trackRT.pivot = new Vector2(0.5f, 0);
        trackRT.anchoredPosition = new Vector2(0, 8);
        trackRT.sizeDelta = new Vector2(-16, 14);
        trackGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

        // 진행바 채움
        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(trackRT, false);
        barFill = fillGO.GetComponent<RectTransform>();
        barFill.anchorMin = new Vector2(0, 0); barFill.anchorMax = new Vector2(0, 1);
        barFill.pivot = new Vector2(0, 0.5f);
        barFill.offsetMin = Vector2.zero; barFill.offsetMax = Vector2.zero;
        fillGO.GetComponent<Image>().color = new Color(0.3f, 0.8f, 0.4f, 0.95f);

        root.SetActive(false);
    }
}
