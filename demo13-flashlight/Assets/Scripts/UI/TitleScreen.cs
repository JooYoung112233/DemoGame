using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 시작 화면(타이틀/메인 메뉴). 부팅 시 GameBoot가 호출(showTitleOnBoot).
/// 버튼: 새 게임 / 이어하기(세이브 있을 때만) / 종료.
/// 타이틀은 **세이브 상태 + 씬 전환**만 제어 — 실제 새게임/로드 분기는 Safehouse 씬의 GameStartHandler가 처리
/// (세이브 있으면 로드, 없으면 프롤로그 S-000). 절차적 uGUI(legacy Text), ToastManager 캔버스 관례.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    public static TitleScreen Instance { get; private set; }

    bool IsGenerated => canvas != null;

    [SerializeField] Canvas canvas;
    [SerializeField] Button newGameBtn;
    [SerializeField] Button continueBtn;
    [SerializeField] Button quitBtn;
    Font font;

    /// <summary>타이틀을 띄운다(없으면 생성). 부팅/타이틀복귀에서 호출.</summary>
    public static TitleScreen Show()
    {
        if (Instance == null)
        {
            // 프리팹 우선(Instantiate가 Awake로 Instance 세팅), 없으면 코드 생성 폴백.
            var prefab = Resources.Load<GameObject>("UI/TitleScreen");
            GameObject go = prefab != null ? Instantiate(prefab) : new GameObject("TitleScreen");
            go.name = "TitleScreen";
            DontDestroyOnLoad(go);
            if (prefab == null) Instance = go.AddComponent<TitleScreen>();   // 폴백: Awake가 BuildUI
            EnsureEventSystem();
        }
        Instance.SetVisible(true);
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (!IsGenerated) BuildUI();   // 폴백: 프리팹 없이 코드로 생성
        WireEvents();                  // onClick은 프리팹에 직렬화 안 됨 → 양쪽 경로에서 항상 재부착
    }

    /// <summary>버튼 onClick 재부착. 프리팹 인스턴스는 BuildUI를 스킵하므로 직렬화된 버튼 ref에 리스너를 다시 건다.
    /// 스테일/부분 베이크(직렬화 ref 누락) 대비 — null이면 자식 이름으로 재해결.</summary>
    void WireEvents()
    {
        if (newGameBtn  == null) newGameBtn  = FindButton("Btn_새 게임");
        if (continueBtn == null) continueBtn = FindButton("Btn_이어하기");
        if (quitBtn     == null) quitBtn     = FindButton("Btn_종료");
        if (newGameBtn  != null) { newGameBtn.onClick.RemoveAllListeners();  newGameBtn.onClick.AddListener(OnNewGame); }
        if (continueBtn != null) { continueBtn.onClick.RemoveAllListeners(); continueBtn.onClick.AddListener(OnContinue); }
        if (quitBtn     != null) { quitBtn.onClick.RemoveAllListeners();     quitBtn.onClick.AddListener(OnQuit); }
    }

    /// <summary>자식 계층에서 이름으로 Button 찾기(직렬화 ref 누락 폴백).</summary>
    Button FindButton(string childName)
    {
        foreach (var b in GetComponentsInChildren<Button>(true))
            if (b.gameObject.name == childName) return b;
        return null;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void BuildUI()
    {
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGO = new GameObject("Title_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // 모든 UI 위
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 배경(어두운 풀스크린)
        var bg = MakeRect("BG", canvasGO.transform);
        Stretch(bg);
        bg.gameObject.AddComponent<Image>().color = UITheme.Backdrop;

        // 타이틀
        var title = MakeText("Title", canvasGO.transform, "다녀올게", 96, FontStyle.Bold,
            UITheme.TextBright);
        Anchor(title, new Vector2(0.5f, 0.5f), new Vector2(0, 240), new Vector2(900, 140));

        // 부제
        var sub = MakeText("Subtitle", canvasGO.transform, "— 안전가옥에서 다시 돌아오기까지 —", 28,
            FontStyle.Italic, UITheme.TextMuted);
        Anchor(sub, new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(900, 50));

        // 버튼들
        MakeButton("새 게임", new Vector2(0, 0), OnNewGame, out newGameBtn);
        MakeButton("이어하기", new Vector2(0, -80), OnContinue, out continueBtn);
        MakeButton("종료", new Vector2(0, -160), OnQuit, out quitBtn);

        // 버전 표기
        var ver = MakeText("Version", canvasGO.transform, "프로토타입 v0.1", 20, FontStyle.Normal,
            UITheme.TextDim);
        Anchor(ver, new Vector2(1f, 0f), new Vector2(-110, 30), new Vector2(200, 30));
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — BuildUI를 1회 실행해 프리팹화할 계층을 만든다(EventSystem 등 런타임 셋업 제외).</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
    }
#endif

    void SetVisible(bool v)
    {
        // 캔버스가 꺼진 채 베이크/편집돼도 안전하게 보이도록 강제 활성.
        if (v && canvas != null && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        if (canvas != null) canvas.gameObject.SetActive(v);
        if (v && continueBtn != null)   // 슬롯 아무 데나 세이브 있으면 활성(currentSlot 기준 HasSave는 슬롯0만 봄)
            continueBtn.interactable = SaveManager.Instance != null && SaveManager.Instance.HasAnySave();
    }

    void Hide() => SetVisible(false);

    // ── 버튼 동작 ───────────────────────────────────────────────

    void OnNewGame()  => OpenSlotPicker(newMode: true);
    void OnContinue() => OpenSlotPicker(newMode: false);

    // ── 저장 슬롯 선택 (런타임 오버레이 — 매번 생성·닫으면 파괴, 재베이크 의존 없음) ──
    GameObject slotOverlay;

    void OpenSlotPicker(bool newMode)
    {
        if (SaveManager.Instance == null)   // 세이브 매니저 없으면(맵툴 등) 구 동작 폴백
        {
            if (newMode) StartNewInSlot(0); else ContinueInSlot(0);
            return;
        }
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CloseSlotPicker();

        slotOverlay = new GameObject("SlotOverlay", typeof(RectTransform));
        slotOverlay.transform.SetParent(canvas.transform, false);
        Stretch(slotOverlay.GetComponent<RectTransform>());
        slotOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        var title = MakeText("SlotTitle", slotOverlay.transform,
            newMode ? "새 게임 — 슬롯 선택" : "이어하기 — 슬롯 선택", 44, FontStyle.Bold, UITheme.Gold);
        Anchor(title, new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(1000, 60));

        const float cardW = 300f, cardH = 380f, gap = 44f;
        float totalW = cardW * SaveManager.SlotCount + gap * (SaveManager.SlotCount - 1);
        float startX = -totalW / 2f + cardW / 2f;
        for (int i = 0; i < SaveManager.SlotCount; i++)
            BuildSlotCard(slotOverlay.transform, i, newMode,
                new Vector2(startX + i * (cardW + gap), 20f), new Vector2(cardW, cardH));

        MakeOverlayButton(slotOverlay.transform, "← 뒤로", new Vector2(0, -cardH / 2f - 70f),
            new Vector2(220, 56), CloseSlotPicker, true, UITheme.Cell);
    }

    void CloseSlotPicker()
    {
        if (slotOverlay != null) { Destroy(slotOverlay); slotOverlay = null; }
    }

    void BuildSlotCard(Transform parent, int slot, bool newMode, Vector2 pos, Vector2 size)
    {
        var sum = SaveManager.Instance.PeekSlot(slot);

        var cardGO = new GameObject($"Slot_{slot}", typeof(RectTransform));
        cardGO.transform.SetParent(parent, false);
        var rt = cardGO.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        cardGO.AddComponent<Image>().color = UITheme.Panel;

        MakeText($"Hdr", cardGO.transform, $"슬롯 {slot + 1}", 26, FontStyle.Bold, UITheme.TextBright)
            .rectTransform.anchoredPosition = new Vector2(0, size.y / 2f - 34f);

        // 요약 본문
        string body = sum.exists
            ? $"Lv {sum.level}\n특성 {sum.traitCount}개\n<color=#E8C86A>◈ {sum.currency:N0}</color>\n\n<size=16><color=#8A8170>{sum.saveTime}</color></size>"
            : "<color=#8A8170>— 비어 있음 —</color>";
        var bodyT = MakeText("Body", cardGO.transform, body, 22, FontStyle.Normal, UITheme.TextBright);
        bodyT.supportRichText = true;
        bodyT.rectTransform.anchoredPosition = new Vector2(0, 24f);
        bodyT.rectTransform.sizeDelta = new Vector2(size.x - 32f, size.y - 150f);

        // 액션 버튼
        int captured = slot;
        if (newMode)
        {
            string label = sum.exists ? "덮어쓰기" : "여기서 시작";
            var col = sum.exists ? UITheme.Negative : UITheme.Positive;
            MakeCardButton(cardGO.transform, label, size, col, true, () =>
            {
                if (sum.exists) ConfirmOverwrite(captured);
                else StartNewInSlot(captured);
            });
        }
        else
        {
            MakeCardButton(cardGO.transform, sum.exists ? "이어하기" : "비어 있음", size,
                sum.exists ? UITheme.Positive : UITheme.Cell, sum.exists,
                () => ContinueInSlot(captured));
        }
    }

    void MakeCardButton(Transform card, string label, Vector2 cardSize, Color col, bool enabled, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject($"Act_{label}", typeof(RectTransform));
        go.transform.SetParent(card, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, 20f);
        rt.sizeDelta = new Vector2(cardSize.x - 40f, 52f);
        var img = go.AddComponent<Image>(); img.color = col;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img; btn.interactable = enabled;
        var c = btn.colors; c.highlightedColor = UITheme.CellHover; c.pressedColor = UITheme.CellPressed;
        c.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); btn.colors = c;
        btn.onClick.AddListener(action);
        Stretch(MakeText("L", go.transform, label, 22, FontStyle.Bold, UITheme.TextBright).rectTransform);
    }

    void MakeOverlayButton(Transform parent, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action, bool enabled, Color col)
    {
        var go = new GameObject($"Ov_{label}", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>(); img.color = col;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img; btn.interactable = enabled;
        var c = btn.colors; c.highlightedColor = UITheme.CellHover; c.pressedColor = UITheme.CellPressed; btn.colors = c;
        btn.onClick.AddListener(action);
        Stretch(MakeText("L", go.transform, label, 22, FontStyle.Bold, UITheme.TextBright).rectTransform);
    }

    /// <summary>찬 슬롯에 새 게임 덮어쓰기 확인 모달.</summary>
    void ConfirmOverwrite(int slot)
    {
        var box = new GameObject("Confirm", typeof(RectTransform));
        box.transform.SetParent(slotOverlay.transform, false);
        var rt = box.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(560, 240);
        box.AddComponent<Image>().color = UITheme.PanelAlt;

        var msg = MakeText("Msg", box.transform, $"슬롯 {slot + 1}의 저장을 지우고\n새로 시작할까요?", 26, FontStyle.Bold, UITheme.TextBright);
        msg.rectTransform.anchoredPosition = new Vector2(0, 50f);
        msg.rectTransform.sizeDelta = new Vector2(520, 100);

        MakeOverlayButton(box.transform, "덮어쓰기", new Vector2(-130, -60), new Vector2(220, 56),
            () => StartNewInSlot(slot), true, UITheme.Negative);
        MakeOverlayButton(box.transform, "취소", new Vector2(130, -60), new Vector2(220, 56),
            () => Destroy(box), true, UITheme.Cell);
    }

    // ── 슬롯 확정 후 실제 진입 ──
    /// <summary>지정 슬롯으로 새 게임 시작(버튼 클릭과 동일 경로). 자동화·디버그용 공개 진입점.
    /// 가상 입력은 uGUI EventSystem을 구동하지 못하므로(폴링 소비자만 구동) 버튼은 이렇게 호출한다.</summary>
    public static bool StartNewGame(int slot = 0)
    {
        if (Instance == null) { Debug.LogWarning("[Title] TitleScreen 인스턴스 없음 — 새 게임 불가"); return false; }
        Instance.StartNewInSlot(Mathf.Clamp(slot, 0, 2));
        return true;
    }

    /// <summary>지정 슬롯 이어하기(세이브 없으면 false). 자동화·디버그용.</summary>
    public static bool ContinueGame(int slot = 0)
    {
        if (Instance == null) return false;
        if (SaveManager.Instance == null || !SaveManager.Instance.HasSave(slot)) return false;
        Instance.ContinueInSlot(slot);
        return true;
    }

    void StartNewInSlot(int slot)
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetSlot(slot);
            SaveManager.Instance.DeleteSave(slot);       // 슬롯 파일 제거 → GameStartHandler가 프롤로그 분기
            SaveManager.Instance.ResetToNewGame();       // 인메모리 이월 차단(같은 세션 재시작 대비)
        }
        GameStartHandler.ResetSession();
        Debug.Log($"[Title] 새 게임(슬롯{slot}) → Safehouse");
        CloseSlotPicker();
        Hide();
        // 로드 시작 전부터 스토리 페이드 오버레이(#2, sortingOrder 999)로 즉시 덮는다.
        // → 전환 커버(#1=OnGUI)와 레이어가 엇갈려 생기던 첫 깜박임 제거. 프롤로그가 같은 #2로 이어받음.
        ScreenEffectManager.Instance?.CoverInstant();
        // 새 게임은 커버를 유지한 채 진입 — reveal 없이 곧바로 프롤로그 암전 페이드로 넘긴다.
        GoToSafehouse(keepCovered: true);
    }

    void ContinueInSlot(int slot)
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HasSave(slot)) return;   // 빈 슬롯 방어
        SaveManager.Instance.SetSlot(slot);
        GameStartHandler.ResetSession();               // 안전가옥 진입 시 Load() 1회 재실행(현재 슬롯)
        Debug.Log($"[Title] 이어하기(슬롯{slot}) → Safehouse");
        CloseSlotPicker();
        Hide();
        GoToSafehouse();
    }

    void OnQuit()
    {
        Debug.Log("[Title] 종료");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void GoToSafehouse(bool keepCovered = false)
    {
        if (SceneTransitionManager.Instance != null)
            // 즉시 검게 덮은 채 로드 → 셋업 → reveal. 클릭 순간 화면이 검어져 HUD/타이틀 깜빡임 없음.
            // keepCovered면 reveal 생략(새 게임: 프롤로그가 화면을 이어받음).
            SceneTransitionManager.Instance.TransitionTo("Safehouse", "default", instantCover: true, keepCovered: keepCovered);
        else
            Debug.LogWarning("[Title] SceneTransitionManager 없음 — Safehouse 전환 불가(씬 미빌드?)");
    }

    // ── uGUI 헬퍼 ───────────────────────────────────────────────

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
        DontDestroyOnLoad(es);
    }

    static RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void Anchor(Text t, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    Text MakeText(string name, Transform parent, string content, int size, FontStyle style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void MakeButton(string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick, out Button button)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(360, 64);

        var img = go.AddComponent<Image>();
        img.color = UITheme.Cell;

        button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = UITheme.CellHover;
        colors.pressedColor = UITheme.CellPressed;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        button.colors = colors;
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        var label_t = MakeText("Label", go.transform, label, 30, FontStyle.Bold,
            UITheme.TextBright);
        Stretch(label_t.rectTransform);
    }
}
