using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 공용 토스트 메시지 UI (Canvas/uGUI, 코드 생성).
/// 화면 하단 중앙에 짧은 알림을 띄운다. 문/상호작용 피드백, 잔액 부족 등 범용.
/// GameBootstrap에서 자동 생성되는 싱글톤.
/// 사용: ToastManager.Show("메시지"); / ToastManager.Show("실패", ToastType.Warning);
/// </summary>
public class ToastManager : MonoBehaviour
{
    public static ToastManager Instance { get; private set; }

    public enum ToastType { Info, Warning, Success }

    const int MaxVisible = 4;
    const float DefaultDuration = 2.2f;

    // uGUI 영속 스켈레톤 (프리팹 베이크 시 직렬화 보존)
    [SerializeField] Canvas canvas;
    [SerializeField] RectTransform stack;          // 토스트가 쌓이는 세로 컨테이너
    readonly List<ToastItem> active = new List<ToastItem>();   // 동적 토스트 — 직렬화 안 함

    public bool IsGenerated => canvas != null;

    class ToastItem
    {
        public RectTransform rt;
        public CanvasGroup group;
        public Text text;
        public float life;        // 남은 표시 시간
        public float total;       // 전체 수명
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        HierarchyFolder.Persist(gameObject);
        if (!IsGenerated) BuildCanvas();   // 폴백: 프리팹 없이 코드로 생성
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ═══════════════════════════
    //  공개 API
    // ═══════════════════════════

    /// <summary>토스트 메시지 표시 (정적 헬퍼).</summary>
    public static void Show(string message, ToastType type = ToastType.Info, float duration = DefaultDuration)
    {
        if (Instance == null) return;
        Instance.ShowInternal(message, type, duration);
    }

    void ShowInternal(string message, ToastType type, float duration)
    {
        if (string.IsNullOrEmpty(message)) return;

        // 최대 개수 초과 시 가장 오래된 것 제거
        while (active.Count >= MaxVisible)
        {
            var oldest = active[0];
            active.RemoveAt(0);
            if (oldest.rt != null) Destroy(oldest.rt.gameObject);
        }

        var item = CreateToast(message, type, duration);
        active.Add(item);
    }

    // ═══════════════════════════
    //  빌드
    // ═══════════════════════════

    void BuildCanvas()
    {
        var canvasGO = new GameObject("Toast_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // HUD/대부분 UI 위

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 토스트는 클릭을 막지 않음 → GraphicRaycaster 생략

        var stackGO = new GameObject("Toast_Stack");
        stackGO.transform.SetParent(canvasGO.transform, false);
        stack = stackGO.AddComponent<RectTransform>();
        stack.anchorMin = new Vector2(0.5f, 0f);
        stack.anchorMax = new Vector2(0.5f, 0f);
        stack.pivot = new Vector2(0.5f, 0f);
        stack.anchoredPosition = new Vector2(0, 140); // 하단에서 위로 띄움

        var layout = stackGO.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — BuildCanvas를 1회 실행해 프리팹화할 영속 계층을 만든다.</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        BuildCanvas();
    }
#endif

    ToastItem CreateToast(string message, ToastType type, float duration)
    {
        var go = new GameObject("Toast");
        go.transform.SetParent(stack, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(560, 50);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(UITheme.Panel.r, UITheme.Panel.g, UITheme.Panel.b, 0.92f);

        var group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        // 좌측 색상 띠
        var barGO = new GameObject("Accent");
        barGO.transform.SetParent(go.transform, false);
        var barRT = barGO.AddComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0, 0);
        barRT.anchorMax = new Vector2(0, 1);
        barRT.pivot = new Vector2(0, 0.5f);
        barRT.sizeDelta = new Vector2(5, 0);
        barRT.anchoredPosition = Vector2.zero;
        var barImg = barGO.AddComponent<Image>();
        barImg.color = AccentColor(type);

        // 텍스트
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(16, 0);
        txtRT.offsetMax = new Vector2(-12, 0);

        var txt = txtGO.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 20;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Truncate;
        txt.text = message;

        var shadow = txtGO.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(1, -1);

        return new ToastItem { rt = rt, group = group, text = txt, life = duration, total = duration };
    }

    Color AccentColor(ToastType type)
    {
        switch (type)
        {
            case ToastType.Warning: return new Color(1f, 0.45f, 0.15f);   // 의미색(주의) 유지
            case ToastType.Success: return new Color(0.3f, 0.9f, 0.4f);   // 의미색(성공) 유지
            default: return UITheme.AccentBright;                          // 일반(Info) — 웜 탄 강조
        }
    }

    // ═══════════════════════════
    //  애니메이션
    // ═══════════════════════════

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var t = active[i];
            if (t.rt == null) { active.RemoveAt(i); continue; }

            t.life -= dt;

            // 페이드 인 (앞 0.2s) / 페이드 아웃 (뒤 0.4s)
            float fadeIn = Mathf.Clamp01((t.total - t.life) / 0.2f);
            float fadeOut = Mathf.Clamp01(t.life / 0.4f);
            t.group.alpha = Mathf.Min(fadeIn, fadeOut);

            if (t.life <= 0f)
            {
                Destroy(t.rt.gameObject);
                active.RemoveAt(i);
            }
        }
    }
}
