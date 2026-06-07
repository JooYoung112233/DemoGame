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
public class HideoutController : MonoBehaviour
{
    [Header("고정 카메라(은신처 프레이밍)")]
    [Tooltip("카메라가 바라볼 방 중심(월드 XY)")]
    [SerializeField] Vector2 cameraCenter = new Vector2(7f, 4.5f);
    [Tooltip("정사영 카메라 크기(화면 절반 높이). 방 전체가 들어오도록")]
    [SerializeField] float cameraSize = 5f;

    [Header("복귀")]
    [SerializeField] string safehouseScene = "Safehouse";
    [SerializeField] string safehouseSpawn = "default";   // 집 문 앞

    // ── 복원용 상태 ──
    TopDownPlayer _player;
    Camera _cam;            // 클릭 레이캐스트에 쓸 현재 카메라(=하이드아웃 전용)
    Camera _rigCam;         // PlayerRig 카메라(은신처 동안 렌더 끔)
    Camera _hideoutCam;     // 하이드아웃 전용 카메라(런타임 생성)
    Light2D _hideoutLight;  // 은신처 전용 글로벌 라이트(밤/낮 무관 풀 조명)
    bool _hadPlayer, _prevCanMove;
    bool _hadFollow, _prevFollowEnabled;
    bool _prevRigCamEnabled;
    InteractionSystem _interaction;
    bool _prevInteractionEnabled;
    readonly List<SpriteRenderer> _hiddenSprites = new List<SpriteRenderer>();
    readonly List<Light2D> _hiddenLights = new List<Light2D>();

    GameObject _uiRoot;
    static Font _krFont;

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
            foreach (var sr in _player.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.enabled) { sr.enabled = false; _hiddenSprites.Add(sr); }
            foreach (var lt in _player.GetComponentsInChildren<Light2D>(true))
                if (lt.enabled) { lt.enabled = false; _hiddenLights.Add(lt); }
        }

        // 걸어다니며 E 줍는 기존 상호작용(범위/프롬프트) 정지 — 여기선 클릭만.
        _interaction = FindFirstObjectByType<InteractionSystem>();
        if (_interaction != null) { _prevInteractionEnabled = _interaction.enabled; _interaction.enabled = false; }

        // 기존(PlayerRig) 카메라: 추적 끄고 렌더만 끔(같은 GO의 AudioListener는 유지 → 경고 없음).
        if (CameraFollow.Instance != null)
        {
            _hadFollow = true;
            _prevFollowEnabled = CameraFollow.Instance.enabled;
            CameraFollow.Instance.enabled = false;          // 추적 정지
            _rigCam = CameraFollow.Instance.GetComponent<Camera>();
        }
        if (_rigCam == null) _rigCam = Camera.main;
        if (_rigCam != null) { _prevRigCamEnabled = _rigCam.enabled; _rigCam.enabled = false; }

        // 하이드아웃 전용 카메라(런타임 생성). CameraFollow.DisableOtherCameras는 sceneLoaded 때 이미 실행됐으므로
        // Start 시점에 만든 이 카메라는 살아남는다. PlayerRig 카메라가 꺼져도 방이 보임.
        _hideoutCam = CreateHideoutCamera(_rigCam);
        _cam = _hideoutCam != null ? _hideoutCam : _rigCam;

        // 은신처는 밤/낮 영향 없이 항상 환하게 — 전용 글로벌 라이트(나갈 때 제거).
        var lightGo = new GameObject("HideoutLight");
        _hideoutLight = lightGo.AddComponent<Light2D>();
        _hideoutLight.lightType = Light2D.LightType.Global;
        _hideoutLight.intensity = 1.1f;
        _hideoutLight.color = Color.white;
    }

    Camera CreateHideoutCamera(Camera src)
    {
        var go = new GameObject("HideoutCamera");
        var c = go.AddComponent<Camera>();
        c.orthographic       = true;
        c.orthographicSize   = cameraSize;
        c.clearFlags         = src != null ? src.clearFlags : CameraClearFlags.SolidColor;
        c.backgroundColor    = src != null ? src.backgroundColor : new Color(0.05f, 0.05f, 0.06f, 1f);
        c.cullingMask        = src != null ? src.cullingMask : ~0;
        c.nearClipPlane      = src != null ? src.nearClipPlane : 0.3f;
        c.farClipPlane       = src != null ? src.farClipPlane : 1000f;
        c.depth              = src != null ? src.depth : 0f;
        c.transparencySortMode = TransparencySortMode.CustomAxis;   // 탑다운 2D Y정렬(아래가 앞)
        c.transparencySortAxis = new Vector3(0f, 1f, 0f);
        float z = src != null ? src.transform.position.z : -10f;
        go.transform.position = new Vector3(cameraCenter.x, cameraCenter.y, z);
        return c;
    }

    void OnDestroy()
    {
        if (_hadPlayer && _player != null) _player.SetCanMove(_prevCanMove);
        foreach (var sr in _hiddenSprites) if (sr != null) sr.enabled = true;
        foreach (var lt in _hiddenLights) if (lt != null) lt.enabled = true;
        if (_interaction != null) _interaction.enabled = _prevInteractionEnabled;
        if (_hideoutCam != null) Destroy(_hideoutCam.gameObject);
        if (_hideoutLight != null) Destroy(_hideoutLight.gameObject);
        if (_rigCam != null) _rigCam.enabled = _prevRigCamEnabled;      // PlayerRig 카메라 렌더 복구
        if (_hadFollow && CameraFollow.Instance != null)
            CameraFollow.Instance.enabled = _prevFollowEnabled;        // 추적 재개 → 안전구역에서 플레이어로 스냅
        if (_uiRoot != null) Destroy(_uiRoot);
    }

    // ─────────────────────────────────────────────
    //  클릭 → 시설 상호작용 / ESC → 나가기
    // ─────────────────────────────────────────────
    void Update()
    {
        // CameraFollow.DisableOtherCameras가 씬 로드 타이밍에 전용 카메라를 꺼도 매 프레임 되살림.
        if (_hideoutCam != null)
        {
            if (!_hideoutCam.enabled) _hideoutCam.enabled = true;
            if (_rigCam != null && _rigCam.enabled) _rigCam.enabled = false;
        }

        bool uiOpen = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();

        if (Input.GetKeyDown(KeyCode.Escape) && !uiOpen) { ExitHideout(); return; }

        if (!Input.GetMouseButtonDown(0)) return;
        if (uiOpen) return;                                                  // 시설 UI가 열려 있으면 무시
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return; // 화면 버튼 클릭
        if (_cam == null) return;

        Vector3 mw = _cam.ScreenToWorldPoint(Input.mousePosition);
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
        MakeText("Hint", "시설을 클릭해 사용  ·  인벤토리/창고 = 아래 버튼(또는 Tab)  ·  ESC로 나가기", 22, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(1100f, 36f),
            new Color(0.8f, 0.82f, 0.85f, 0.9f));

        // 좌하단 버튼: 나가기 + 인벤토리/창고
        MakeButton("ExitButton",      "← 안전구역으로 나가기", new Vector2(40f, 40f),  new Vector2(320f, 64f), ExitHideout);
        MakeButton("InventoryButton", "인벤토리 / 창고",        new Vector2(376f, 40f), new Vector2(260f, 64f), OpenInventory);
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

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    static Font LoadKoreanFont()
    {
        var f = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Dotum", "Batang", "Arial" }, 24);
        return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
