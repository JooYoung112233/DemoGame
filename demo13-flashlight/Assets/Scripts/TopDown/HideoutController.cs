using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 은신처(Hideout) 실내 = 타르코프식 '화면'. (docs/safehouse.md '실내 맵(별도 씬)')
/// 캐릭터로 걸어다니며 E 누르는 대신:
///   • 플레이어 캐릭터를 숨기고(이동 잠금 + 스프라이트/라이트 끔) 카메라를 방에 고정.
///   • 마우스로 시설(침대/작업대/의료대/조리대 등 InteractableObject)을 클릭 → 그 시설의 기존 UI가 열림.
///   • 화면의 '나가기' 버튼(또는 ESC) → 안전구역으로 복귀(SceneTransition).
/// Hideout.unity에 1개 배치(HideoutGreyboxLayout이 생성). 씬 언로드 시 OnDestroy에서 원상복구.
/// </summary>
[DefaultExecutionOrder(-60)]   // ESC를 UIManager(-50)보다 먼저 잡아 'UI 닫기 → 다음 ESC에 나가기 확인' 순서 보장
public class HideoutController : MonoBehaviour
{
    /// <summary>하이드아웃 실내 화면이 떠 있는 동안 true. UIManager가 ESC=PauseMenu를 양보(여기서 처리).</summary>
    public static bool IsActive { get; private set; }

    [Header("고정 카메라(은신처 프레이밍)")]
    [Tooltip("카메라가 바라볼 방 중심(월드 XY)")]
    [SerializeField] Vector2 cameraCenter = new Vector2(7f, 4.5f);
    [Tooltip("정사영 카메라 크기(화면 절반 높이). 방 전체가 들어오도록")]
    [SerializeField] float cameraSize = 5f;

    [Header("복귀")]
    [SerializeField] string safehouseScene = "Safehouse";
    [SerializeField] string safehouseSpawn = "from_hideout";   // 은신처 입구 문 바로 앞(트리거 밖). SafehouseGreyboxLayout이 (26.2,5)에 구움

    // ── 복원용 상태 ──
    TopDownPlayer _player;
    Camera _cam;            // 클릭 레이캐스트용(= PlayerRig 카메라 그대로 사용)
    Camera _rigCam;         // PlayerRig 카메라 — 은신처 동안 방 중심으로 이동·리사이즈해 그대로 렌더
    Light2D _systemsGlobal; // 하이드아웃 동안 밝게 덮어쓸 기존 Systems 글로벌 라이트
    float _prevGlobalIntensity;
    Color _prevGlobalColor;
    // 카메라 원복용
    bool _hadCam;
    Vector3 _prevCamPos;
    bool _prevCamOrtho;
    float _prevCamSize;
    bool _hadPlayer, _prevCanMove;
    bool _hadFollow, _prevFollowEnabled;
    InteractionSystem _interaction;
    bool _prevInteractionEnabled;
    // SpriteRenderer + Spine MeshRenderer 등 모든 렌더러를 함께 숨긴다 (Spine 캐릭터는 MeshRenderer)
    readonly List<Renderer> _hiddenRenderers = new List<Renderer>();
    readonly List<Light2D> _hiddenLights = new List<Light2D>();

    GameObject _uiRoot;
    GameObject _confirmRoot;   // 나가기 확인창 패널
    bool _confirmShowing;
    static Font _krFont;

    void Awake() => IsActive = true;

    void Start()
    {
        HidePlayerAndFixCamera();
        BuildScreenUI();
    }

    // ─────────────────────────────────────────────
    //  캐릭터 숨김 + 카메라 고정 (+ 복원값 저장)
    // ─────────────────────────────────────────────
    void HidePlayerAndFixCamera()
    {
        _player = TopDownPlayer.Instance;
        if (_player != null)
        {
            _hadPlayer = true;
            _prevCanMove = _player.CanMove;
            _player.SetCanMove(false);
            foreach (var r in _player.GetComponentsInChildren<Renderer>(true))
                if (r.enabled) { r.enabled = false; _hiddenRenderers.Add(r); }
            foreach (var lt in _player.GetComponentsInChildren<Light2D>(true))
                if (lt.enabled) { lt.enabled = false; _hiddenLights.Add(lt); }
        }

        // 걸어다니며 E 줍는 기존 상호작용(범위/프롬프트) 정지 — 여기선 클릭만.
        _interaction = FindFirstObjectByType<InteractionSystem>();
        if (_interaction != null) { _prevInteractionEnabled = _interaction.enabled; _interaction.enabled = false; }

        // 카메라: 별도 카메라를 만들지 않고 검증된 PlayerRig 카메라를 그대로 쓴다.
        //   추적(CameraFollow)만 끄고, 카메라를 방 중심으로 이동 + 정사영 크기만 맞춘다.
        //   (런타임 생성 카메라는 'No cameras rendering'으로 무력화되는 문제가 있어 폐기.)
        if (CameraFollow.Instance != null)
        {
            _hadFollow = true;
            _prevFollowEnabled = CameraFollow.Instance.enabled;
            CameraFollow.Instance.enabled = false;          // 추적 정지(플레이어 따라가지 않음)
            _rigCam = CameraFollow.Instance.GetComponent<Camera>();
        }
        if (_rigCam == null) _rigCam = Camera.main;
        _cam = _rigCam;

        if (_rigCam != null)
        {
            _hadCam = true;
            _prevCamPos   = _rigCam.transform.position;
            _prevCamOrtho = _rigCam.orthographic;
            _prevCamSize  = _rigCam.orthographicSize;

            _rigCam.enabled = true;                          // 확실히 켜둠(렌더 유지)
            _rigCam.orthographic = true;
            _rigCam.orthographicSize = cameraSize;
            _rigCam.transform.position = new Vector3(cameraCenter.x, cameraCenter.y, _prevCamPos.z);
        }

        // 은신처는 밤/낮 영향 없이 항상 환하게.
        // ★ 새 글로벌 라이트를 만들면 기존 Systems 글로벌과 겹쳐 "More than one global light" 에러가 난다.
        //   → 새로 만들지 말고, 이미 모든 레이어를 비추는 Systems 글로벌을 잠시 밝게 덮어쓰고 OnDestroy에서 복원.
        _systemsGlobal = FindActiveGlobalLight();
        if (_systemsGlobal != null)
        {
            _prevGlobalIntensity = _systemsGlobal.intensity;
            _prevGlobalColor     = _systemsGlobal.color;
            _systemsGlobal.intensity = 1.1f;
            _systemsGlobal.color     = Color.white;
        }
        else
        {
            Debug.LogWarning("[Hideout] 활성 글로벌 라이트를 못 찾음 — 방이 어두울 수 있음.");
        }

        Debug.Log($"<color=lime>[Hideout]</color> 카메라 준비 완료 — rigCam={(_rigCam != null)} " +
                  $"위치={(_rigCam != null ? _rigCam.transform.position.ToString() : "없음")}, orthoSize={cameraSize}, " +
                  $"player={(_player != null)}, 활성카메라수={Camera.allCamerasCount}, 글로벌라이트={(_systemsGlobal != null)}");
    }

    /// <summary>현재 활성(enabled) 글로벌 Light2D 1개를 찾는다 — Systems 씬이 소유한 글로벌(SystemsSceneEnforcer가 유일하게 켜둠).</summary>
    static Light2D FindActiveGlobalLight()
    {
        foreach (var l in FindObjectsByType<Light2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (l != null && l.enabled && l.lightType == Light2D.LightType.Global)
                return l;
        return null;
    }

    /// <summary>은신처 카메라 상태 유지 — 추적이 되살아나거나 카메라가 꺼지면 매 프레임 되돌린다.</summary>
    void KeepHideoutCamera()
    {
        if (_hadFollow && CameraFollow.Instance != null && CameraFollow.Instance.enabled)
            CameraFollow.Instance.enabled = false;          // 추적 재활성 방지
        if (_rigCam != null)
        {
            if (!_rigCam.enabled) _rigCam.enabled = true;   // 누가 꺼도 다시 켬
            // 위치가 흐트러지면 방 중심으로 재고정
            var p = _rigCam.transform.position;
            if (Mathf.Abs(p.x - cameraCenter.x) > 0.01f || Mathf.Abs(p.y - cameraCenter.y) > 0.01f)
                _rigCam.transform.position = new Vector3(cameraCenter.x, cameraCenter.y, p.z);
        }
    }

    void OnDestroy()
    {
        IsActive = false;
        if (_hadPlayer && _player != null) _player.SetCanMove(_prevCanMove);
        foreach (var r in _hiddenRenderers) if (r != null) r.enabled = true;
        foreach (var lt in _hiddenLights) if (lt != null) lt.enabled = true;
        if (_interaction != null) _interaction.enabled = _prevInteractionEnabled;
        if (_systemsGlobal != null)                                    // 글로벌 라이트 원복(밝기/색)
        {
            _systemsGlobal.intensity = _prevGlobalIntensity;
            _systemsGlobal.color     = _prevGlobalColor;
        }
        if (_hadCam && _rigCam != null)                                // 카메라 정사영/크기 원복(위치는 CameraFollow가 스냅)
        {
            _rigCam.orthographic     = _prevCamOrtho;
            _rigCam.orthographicSize = _prevCamSize;
        }
        if (_hadFollow && CameraFollow.Instance != null)
            CameraFollow.Instance.enabled = _prevFollowEnabled;        // 추적 재개 → 안전구역에서 플레이어로 스냅
        if (_uiRoot != null) Destroy(_uiRoot);
    }

    // ─────────────────────────────────────────────
    //  클릭 → 시설 상호작용 / ESC → 나가기
    // ─────────────────────────────────────────────
    void Update()
    {
        // 은신처 카메라(=rig 카메라) 상태 유지(추적 재활성/꺼짐/이동 방지).
        KeepHideoutCamera();

        // 이 컴포넌트는 UIManager(-50)보다 먼저 실행(-60)되므로, 여기서 본 uiOpen은 'UIManager가 닫기 전' 상태다.
        bool uiOpen = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();

        if (GameInput.GetKeyDown(KeyCode.Escape))
        {
            if (uiOpen) return;                                 // 열린 UI는 UIManager가 닫음 — 여기선 나가지 않음
            if (_confirmShowing) { HideExitConfirm(); return; } // 확인창 떠 있으면 ESC=취소
            ShowExitConfirm();                                  // UI 없을 때만 '나가기 확인창'
            return;
        }

        if (_confirmShowing) return;                            // 확인창 떠 있으면 시설 클릭 차단

        if (!GameInput.GetMouseButtonDown(0)) return;
        if (uiOpen) return;                                                  // 시설 UI가 열려 있으면 무시
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return; // 화면 버튼 클릭
        if (_cam == null) return;

        Vector3 mw = _cam.ScreenToWorldPoint(GameInput.mousePosition);
        var hits = Physics2D.OverlapPointAll(new Vector2(mw.x, mw.y));
        if (hits == null || hits.Length == 0) return;

        var pgo = _player != null ? _player.gameObject : GameObject.FindGameObjectWithTag("Player");
        foreach (var h in hits)
        {
            var io = h.GetComponentInParent<InteractableObject>();
            if (io != null) { io.Interact(pgo); break; }                     // 침대/작업대/의료대/조리대 → 기존 UI
        }
    }

    void ExitHideout()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(safehouseScene, safehouseSpawn);
    }

    // ─────────────────────────────────────────────
    //  화면 UI (제목 + 안내 + 나가기 버튼) — 코드 생성(uGUI)
    // ─────────────────────────────────────────────
    void BuildScreenUI()
    {
        EnsureEventSystem();
        if (_krFont == null) _krFont = LoadKoreanFont();

        _uiRoot = new GameObject("HideoutScreenUI");
        var canvas = _uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = _uiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        _uiRoot.AddComponent<GraphicRaycaster>();

        // 제목
        MakeText("Title", "은신처", 40, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(600f, 60f),
            new Color(0.95f, 0.95f, 0.9f, 1f));
        // 안내
        MakeText("Hint", "시설 클릭 = 건설 · 업그레이드 · 사용  ·  인벤토리 = Tab/버튼  ·  ESC로 나가기", 22, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(1100f, 36f),
            new Color(0.8f, 0.82f, 0.85f, 0.9f));

        // 좌하단 버튼: 나가기 + 인벤토리만(시설은 방 안 타일을 클릭).
        MakeButton("ExitButton",      "← 나가기",  new Vector2(40f, 40f),  new Vector2(200f, 64f), ShowExitConfirm);
        MakeButton("InventoryButton", "인벤토리",  new Vector2(250f, 40f), new Vector2(200f, 64f), OpenInventory);

        BuildConfirmDialog();
    }

    // ── 나가기 확인창 ──
    void BuildConfirmDialog()
    {
        _confirmRoot = new GameObject("ExitConfirm");
        _confirmRoot.transform.SetParent(_uiRoot.transform, false);
        var rt = _confirmRoot.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        _confirmRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);  // 화면 딤(클릭 차단)

        // 중앙 박스
        var box = new GameObject("Box");
        box.transform.SetParent(_confirmRoot.transform, false);
        var boxRT = box.AddComponent<RectTransform>();
        boxRT.anchorMin = boxRT.anchorMax = boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(560f, 240f);
        box.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.98f);

        MakeTextIn(box.transform, "Msg", "안전구역으로 나가시겠습니까?", 26, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(520f, 50f),
            new Color(0.95f, 0.95f, 0.92f, 1f));

        MakeButtonIn(box.transform, "Yes", "나가기", new Vector2(0.5f, 0f), new Vector2(-150f, 40f),
            new Vector2(220f, 64f), new Color(0.5f, 0.22f, 0.22f, 0.95f), ExitHideout);
        MakeButtonIn(box.transform, "No",  "취소",   new Vector2(0.5f, 0f), new Vector2(150f, 40f),
            new Vector2(220f, 64f), new Color(0.18f, 0.2f, 0.26f, 0.95f), HideExitConfirm);

        _confirmRoot.SetActive(false);
    }

    void ShowExitConfirm()
    {
        if (_confirmRoot == null) return;
        _confirmShowing = true;
        _confirmRoot.SetActive(true);
    }

    void HideExitConfirm()
    {
        _confirmShowing = false;
        if (_confirmRoot != null) _confirmRoot.SetActive(false);
    }

    void OpenInventory()
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowCharacterPanel();   // Tab 인벤토리(창고 = 인벤토리 버튼)
    }


    void MakeButton(string name, string label, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(_uiRoot.transform, false);
        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.15f, 0.16f, 0.2f, 0.92f);
        var brt = btnGO.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0f, 0f);
        brt.pivot = new Vector2(0f, 0f);
        brt.anchoredPosition = anchoredPos;
        brt.sizeDelta = size;
        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(btnGO.transform, false);
        var lbl = lblGO.AddComponent<Text>();
        lbl.text = label;
        lbl.font = _krFont;
        lbl.fontSize = 22;
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        var lrt = lbl.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
    }

    void MakeText(string name, string text, int size, TextAnchor anchor,
        Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_uiRoot.transform, false);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = _krFont;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = anchor;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos; rt.sizeDelta = sizeDelta;
    }

    void MakeButtonIn(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPos,
        Vector2 size, Color bg, UnityEngine.Events.UnityAction onClick)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        var img = btnGO.AddComponent<Image>();
        img.color = bg;
        var brt = btnGO.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = brt.pivot = anchor;
        brt.anchoredPosition = anchoredPos;
        brt.sizeDelta = size;
        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(btnGO.transform, false);
        var lbl = lblGO.AddComponent<Text>();
        lbl.text = label; lbl.font = _krFont; lbl.fontSize = 22;
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        var lrt = lbl.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
    }

    void MakeTextIn(Transform parent, string name, string text, int size, TextAnchor anchor,
        Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = text; t.font = _krFont; t.fontSize = size; t.fontStyle = FontStyle.Bold;
        t.alignment = anchor; t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos; rt.sizeDelta = sizeDelta;
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
    }

    static Font LoadKoreanFont()
    {
        var f = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Dotum", "Batang", "Arial" }, 24);
        return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
