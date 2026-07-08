using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 바닥 중첩 아이템 줍기 UI — 한 자리에 여러 WorldItem이 겹쳐 있을 때 목록으로 띄워
/// **휠/방향키로 선택 → E·클릭으로 선택적으로 줍기**.
///
/// 흐름: InteractableObject.HandlePickup이 플레이어 주변 WorldItem 클러스터(≥2)를 감지하면
///       GroundPickupUI.Show(목록, 플레이어) 호출 → 목록 표시 →
///       휠/↑↓ 선택, [E]/[Enter]/클릭 = 선택 줍기(성공 시 목록에서 제거, 비면 닫힘),
///       [F] 전부 줍기, [Esc] 닫기.
///
/// 싱글톤(자가 부트스트랩, NoteUI와 동일 방식). UIManager.IsAnyUIOpen()에 포함되어
/// 열려 있는 동안 플레이어 이동/상호작용이 차단된다. 프로시저럴 uGUI(씬 배치 불필요).
/// </summary>
[DefaultExecutionOrder(60)]   // InteractionSystem(0)보다 늦게 — 같은 프레임 E 재처리(자동 닫힘 직후) 방지
public class GroundPickupUI : MonoBehaviour
{
    public static GroundPickupUI Instance { get; private set; }

    /// <summary>플레이어로부터 이 반경 안의 WorldItem들을 한 목록으로 모은다(클러스터 판정).</summary>
    public const float ClusterRadius = 1.6f;

    public static bool IsShowing => Instance != null && Instance.isShowing;
    public static void Hide() { if (Instance != null) Instance.Close(); }

    bool isShowing;
    int openFrame = -1;
    int selected;
    GameObject playerGO;
    readonly List<WorldItem> items = new List<WorldItem>();

    // uGUI (프리팹 베이크 시 직렬화 보존 — 영속 스켈레톤만. 동적 행은 직렬화 안 함)
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject panelRoot;
    [SerializeField] RectTransform listRoot;
    [SerializeField] Text headerText;
    Font koreanFont;   // 런타임 동적 OS 폰트 — 직렬화 안 함(Instantiate 후 재바인딩)

    const int SortingOrder = 108;      // NoteUI(110) 아래, DialogueUI(100) 위
    const float PanelWidth = 360f;
    const float RowHeight = 34f;
    const float RowGap = 3f;

    bool IsGenerated => canvas != null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        koreanFont = LoadKoreanFont();
        if (!IsGenerated) GenerateUI();   // 폴백: 프리팹 없이 코드로 생성
        else ApplyFonts();                // 프리팹 인스턴스: 동적 폰트 재바인딩
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>인스턴스 보장 — Systems 씬에 없으면 런타임 자동 생성. 첫 호출 시.</summary>
    public static GroundPickupUI Ensure()
    {
        if (Instance == null)
        {
            // 프리팹 우선(Instantiate가 Awake로 Instance 세팅), 없으면 코드 생성 폴백.
            var prefab = Resources.Load<GameObject>("UI/GroundPickupUI");
            GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("[GroundPickupUI]");
            go.name = "[GroundPickupUI]";
            if (prefab == null) go.AddComponent<GroundPickupUI>();
            DontDestroyOnLoad(go);
        }
        return Instance;
    }

    // ── 공개 API ──────────────────────────────────────────────────────

    public static void Show(List<WorldItem> cluster, GameObject player)
    {
        if (cluster == null || cluster.Count == 0) return;
        Ensure().Open(cluster, player);
    }

    void Open(List<WorldItem> cluster, GameObject player)
    {
        if (!IsGenerated) GenerateUI();

        items.Clear();
        for (int i = 0; i < cluster.Count; i++)
            if (cluster[i] != null) items.Add(cluster[i]);
        if (items.Count == 0) return;

        playerGO = player;
        selected = 0;
        isShowing = true;
        openFrame = Time.frameCount;
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        panelRoot.SetActive(true);
        RebuildRows();
    }

    public void Close()
    {
        if (!isShowing) return;
        isShowing = false;
        items.Clear();
        playerGO = null;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Update()
    {
        if (!isShowing) return;

        // 외부 파괴/소진된 항목 정리 — 비면 닫힘
        PruneDead();
        if (items.Count == 0) { Close(); return; }

        if (Time.frameCount == openFrame) return;   // 연 프레임의 입력 무시

        if (GameInput.GetKeyDown(KeyCode.Escape)) { Close(); return; }

        // 휠/방향키 선택
        float wheel = GameInput.mouseScrollDelta.y;
        if (wheel > 0.01f || GameInput.GetKeyDown(KeyCode.UpArrow)) MoveSelection(-1);
        else if (wheel < -0.01f || GameInput.GetKeyDown(KeyCode.DownArrow)) MoveSelection(1);

        // 전부 줍기
        if (GameInput.GetKeyDown(KeyCode.F)) { PickAll(); return; }

        // 선택 줍기
        if (GameInput.GetKeyDown(KeyCode.E) || GameInput.GetKeyDown(KeyCode.Return) || GameInput.GetKeyDown(KeyCode.KeypadEnter))
            PickAt(selected);
    }

    void MoveSelection(int dir)
    {
        if (items.Count == 0) return;
        selected = (selected + dir + items.Count) % items.Count;
        RefreshRowHighlight();
    }

    /// <summary>선택 인덱스 항목을 줍는다. 성공 시 목록에서 제거 후 재구성, 비면 닫힘.</summary>
    void PickAt(int index)
    {
        if (index < 0 || index >= items.Count) return;
        var wi = items[index];
        if (wi == null) { items.RemoveAt(index); RebuildRows(); return; }
        if (playerGO == null) return;

        if (wi.TryPickup(playerGO))   // 성공 시 WorldItem GO Destroy
        {
            items.RemoveAt(index);
            if (selected >= items.Count) selected = Mathf.Max(0, items.Count - 1);
            if (items.Count == 0) { Close(); return; }
            RebuildRows();
        }
        // 실패(공간/가방) → TryPickup이 토스트 표시. 목록 유지.
    }

    void PickAll()
    {
        // 앞에서부터 줍되, 공간 부족으로 실패하면 멈춤(나머지는 남김).
        for (int i = 0; i < items.Count; )
        {
            var wi = items[i];
            if (wi == null) { items.RemoveAt(i); continue; }
            if (playerGO != null && wi.TryPickup(playerGO))
                items.RemoveAt(i);
            else
                break;
        }
        selected = 0;
        if (items.Count == 0) Close();
        else RebuildRows();
    }

    void PruneDead()
    {
        for (int i = items.Count - 1; i >= 0; i--)
            if (items[i] == null) items.RemoveAt(i);
        if (selected >= items.Count) selected = Mathf.Max(0, items.Count - 1);
    }

    // ── UI 생성/갱신 ──────────────────────────────────────────────────

    void RebuildRows()
    {
        if (listRoot == null) return;
        for (int i = listRoot.childCount - 1; i >= 0; i--)
            Destroy(listRoot.GetChild(i).gameObject);

        if (headerText != null)
            headerText.text = $"바닥 아이템 ({items.Count})";

        float panelH = 44f + items.Count * (RowHeight + RowGap) + 30f;  // 헤더 + 행들 + 힌트
        var pr = (RectTransform)panelRoot.transform.GetChild(1); // Panel
        pr.sizeDelta = new Vector2(PanelWidth, panelH);

        listRoot.sizeDelta = new Vector2(PanelWidth - 16, items.Count * (RowHeight + RowGap));

        for (int i = 0; i < items.Count; i++)
        {
            var wi = items[i];
            var inst = wi != null ? wi.Item : null;
            string label = inst != null ? inst.DisplayName : "(사라짐)";
            Color rarity = inst != null && inst.data != null ? inst.data.RarityColor : Color.gray;

            int captured = i;
            var rowGO = new GameObject($"Row_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
            rowGO.transform.SetParent(listRoot, false);
            var rt = rowGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(0, RowHeight);
            rt.anchoredPosition = new Vector2(0, -i * (RowHeight + RowGap));

            rowGO.GetComponent<Image>().color = RowColor(i == selected);
            var btn = rowGO.GetComponent<Button>();
            btn.onClick.AddListener(() => { selected = captured; RefreshRowHighlight(); PickAt(captured); });

            // 희귀도 색 스와치
            var sw = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
            sw.transform.SetParent(rowGO.transform, false);
            var swRT = sw.GetComponent<RectTransform>();
            swRT.anchorMin = new Vector2(0, 0.5f); swRT.anchorMax = new Vector2(0, 0.5f);
            swRT.pivot = new Vector2(0, 0.5f);
            swRT.anchoredPosition = new Vector2(8, 0);
            swRT.sizeDelta = new Vector2(14, 14);
            sw.GetComponent<Image>().color = rarity;

            // 이름
            var nameTxt = MakeText(rowGO.transform, "Name", 16, FontStyle.Bold, rarity, TextAnchor.MiddleLeft);
            nameTxt.text = label;
            var nRT = (RectTransform)nameTxt.transform;
            nRT.anchorMin = new Vector2(0, 0); nRT.anchorMax = new Vector2(1, 1);
            nRT.offsetMin = new Vector2(30, 0); nRT.offsetMax = new Vector2(-8, 0);
        }
    }

    void RefreshRowHighlight()
    {
        if (listRoot == null) return;
        for (int i = 0; i < listRoot.childCount; i++)
        {
            var img = listRoot.GetChild(i).GetComponent<Image>();
            if (img != null) img.color = RowColor(i == selected);
        }
    }

    static Color RowColor(bool sel) => sel
        ? new Color(UITheme.Accent.r, UITheme.Accent.g, UITheme.Accent.b, 0.95f)
        : new Color(UITheme.Cell.r, UITheme.Cell.g, UITheme.Cell.b, 0.9f);

    void GenerateUI()
    {
        var canvasGO = new GameObject("GroundPickupUI_Canvas");
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

        // child 0 = dim(클릭 차단), child 1 = panel (RebuildRows가 GetChild(1) 사용)
        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(panelRoot.transform, false);
        var dimRT = dim.GetComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = Vector2.zero; dimRT.offsetMax = Vector2.zero;
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(panelRoot.transform, false);
        var pRT = panel.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.5f, 0.5f);
        pRT.anchorMax = new Vector2(0.5f, 0.5f);
        pRT.pivot = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(PanelWidth, 200);
        pRT.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(UITheme.Panel.r, UITheme.Panel.g, UITheme.Panel.b, 0.97f);

        // 헤더
        headerText = MakeText(panel.transform, "Header", 17, FontStyle.Bold,
            UITheme.Gold, TextAnchor.MiddleCenter);
        headerText.text = "바닥 아이템";
        var hRT = (RectTransform)headerText.transform;
        hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
        hRT.pivot = new Vector2(0.5f, 1f);
        hRT.offsetMin = new Vector2(8, -34); hRT.offsetMax = new Vector2(-8, -6);

        // 목록 루트 (헤더 아래, 힌트 위)
        var listGO = new GameObject("List", typeof(RectTransform));
        listGO.transform.SetParent(panel.transform, false);
        listRoot = listGO.GetComponent<RectTransform>();
        listRoot.anchorMin = new Vector2(0, 1); listRoot.anchorMax = new Vector2(1, 1);
        listRoot.pivot = new Vector2(0.5f, 1f);
        listRoot.anchoredPosition = new Vector2(0, -40);
        listRoot.offsetMin = new Vector2(8, 0); listRoot.offsetMax = new Vector2(-8, 0);
        listRoot.sizeDelta = new Vector2(PanelWidth - 16, 0);

        // 힌트 (패널 하단)
        var hint = MakeText(panel.transform, "Hint", 12, FontStyle.Italic,
            UITheme.TextMuted, TextAnchor.MiddleCenter);
        hint.text = "[휠/↑↓] 선택   [E] 줍기   [F] 전부   [Esc] 닫기";
        var hintRT = (RectTransform)hint.transform;
        hintRT.anchorMin = new Vector2(0, 0); hintRT.anchorMax = new Vector2(1, 0);
        hintRT.pivot = new Vector2(0.5f, 0f);
        hintRT.offsetMin = new Vector2(6, 6); hintRT.offsetMax = new Vector2(-6, 26);

        panelRoot.SetActive(false);
    }

    /// <summary>프리팹 인스턴스화 시 동적 OS 폰트 재바인딩 — 전체 자식 Text 일괄.
    /// headerText만 재바인딩하던 방식은 미직렬화 하단 힌트 줄('[휠/↑↓] 선택…')을 놓쳐
    /// 프리팹 경로에서 안 보였다(전체 검수 2026-07-07). 이 패널의 모든 텍스트 = koreanFont.</summary>
    void ApplyFonts()
    {
        var f = koreanFont != null ? koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (var t in GetComponentsInChildren<Text>(true))
            if (t != null) t.font = f;
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — GenerateUI를 1회 실행해 프리팹화할 계층을 만든다.</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        koreanFont = LoadKoreanFont();
        GenerateUI();
    }
#endif

    Text MakeText(Transform parent, string name, int size, FontStyle style, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = koreanFont != null ? koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = anchor;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Font LoadKoreanFont()
    {
        var f = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Dotum", "Batang", "Arial" }, 18);
        return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
