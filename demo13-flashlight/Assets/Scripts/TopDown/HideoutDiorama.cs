using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 은신처 3D 디오라마 — **걸어다니지 않는 클릭 화면이되, 캐릭터를 숨기지 않는다.**
///
/// 2D판(<see cref="HideoutController"/>)은 캐릭터를 통째로 숨기고 카메라를 방에 고정했다.
/// 3D에서는 그러면 만든 캐릭터가 보이지 않아 의미가 없다. 그래서:
///   • 아무것도 안 눌렀으면 캐릭터는 **의자에 앉아** 있다.
///   • 시설을 클릭하면 그 앞으로 가서 시설을 바라보고 자세를 잡는다(침대=눕기 등).
///   • 카메라가 **부드럽게** 이동해 캐릭터를 한쪽으로 밀고 UI 자리를 비운다.
///     UI가 캐릭터를 덮으면 이 화면의 의미가 사라지므로.
///
/// 이 컴포넌트가 씬에 있으면 <see cref="HideoutController"/>는 캐릭터 숨김·카메라 고정·
/// 2D 클릭 판정을 **하지 않는다**(<see cref="Active"/>로 판단). 나가기 UI·ESC는 그대로 둔다.
///
/// 설계: docs/hideout-3d.md
/// </summary>
[DefaultExecutionOrder(-55)]   // HideoutController(-60) 뒤, UIManager(-50) 앞
public partial class HideoutDiorama : MonoBehaviour
{
    /// <summary>씬에 디오라마가 있는가 — HideoutController가 2D 연출을 건너뛸지 판단한다.</summary>
    public static bool Active { get; private set; }

    [Tooltip("시설 클릭을 받을 레이어. 기본 전체.")]
    [SerializeField] LayerMask clickMask = ~0;

    [Tooltip("방 중심 — 카메라가 여기에 고정된다. 비우면 시설 앵커들의 평균 위치.")]
    [SerializeField] Transform roomCenter;

    [Tooltip("방을 담는 카메라 오소 크기.")]
    [SerializeField] float roomOrthoSize = 7.0f;

    /// <summary>기본 시야에서 방이 차지하는 반지름 비율. 시야 보정 계산의 기준.</summary>
    /// <summary>기본 시야(오소 7·16:9)에서 방이 차지하는 반지름 비율. 실측값 — 프레이밍 계산의 기준.</summary>
    const float RoomHalfFracX = 0.31f;   // 실측 0.29 + 여유 — 딱 맞추면 벽이 화면 끝에 붙어 잘린다
    const float RoomHalfFracY = 0.35f;

    readonly List<HideoutFacilityAnchor> _anchors = new List<HideoutFacilityAnchor>();
    HideoutFacilityAnchor _idle;
    HideoutFacilityAnchor _current;
    bool _uiWasOpen;   // UI가 실제로 열린 적이 있어야 '닫힘'을 판단할 수 있다

    Transform _roomFocus;
    TopDownPlayer _player;
    Transform _view;              // Character3D
    Camera _cam;

    void Awake() => Active = true;
    void OnDestroy()
    {
        RestorePresentation();
        Active = false;
        if (CameraFollow.Instance != null) CameraFollow.Instance.ClearFocus();   // 나갈 때만 캐릭터 추적 복구
    }

    void Start()
    {
        _anchors.AddRange(FindObjectsByType<HideoutFacilityAnchor>(FindObjectsInactive.Include));
        foreach (var a in _anchors)
            if (a.moduleKey == "idle") _idle = a;

        _player = TopDownPlayer.Instance;
        if (_player == null) _player = FindFirstObjectByType<TopDownPlayer>();
        _cam = Camera.main;

        if (_player == null)
        {
            Debug.LogWarning("[HideoutDiorama] 플레이어가 없다 — 디오라마를 세울 수 없다.", this);
            enabled = false;
            return;
        }

        // 걸어다니지 않는 화면 — 이동·마우스 페이싱을 멈춘다.
        // (컴포넌트를 끄면 ChibiPlayerVisual.UpdateMotion도 안 불리므로 회전은 여기서 준다.)
        _player.enabled = false;
        var rb = _player.GetComponent<Rigidbody>();
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }

        // ⚠️ 근접 E 프롬프트를 끈다. 걸어다니지 않는 화면이라 "다가가서 E"가 성립하지 않고,
        //    무엇을 누를 수 있는지는 소품 머리 위 FacilityLabel이 알려준다.
        //    (HideoutController가 하던 일인데 디오라마가 그 경로를 양보받으면서 같이 빠졌었다.)
        var interaction = _player.GetComponent<InteractionSystem>();
        if (interaction != null) interaction.enabled = false;

        _view = _player.transform.Find("Character3D");
        ApplyPresentation();

        // ① 카메라는 **캐릭터가 아니라 방**에 고정한다.
        //    디오라마는 무대다 — 무대는 고정이고 배우가 움직인다.
        //    캐릭터를 쫓으면 의자↔시설을 오갈 때마다 화면 전체가 흔들려 정신이 없다.
        SetupRoomFocus();
        FrameRoom();

        BuildFacilityBar();

        if (_idle != null) { PlaceAt(_idle, instant: true); _current = _idle; RefreshFacilityBar(); }
        else Debug.LogWarning("[HideoutDiorama] moduleKey=\"idle\" 앵커(대기 의자)가 없다.", this);
    }


    void Update()
    {
        if (_player == null) return;

        TryApplyFocus();   // 카메라가 늦게 생겨도 방에 붙인다

        bool uiOpen = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();

        // 열린 시설 UI가 **닫혔을 때만** 대기 자리로 돌아간다.
        // ⚠️ uiOpen만 보면 UI가 열리기도 전에(같은 프레임) 복귀가 발동해
        //    시설 선택이 즉시 취소된다.
        bool dockOpen = HideoutDockPanel.Instance != null && HideoutDockPanel.Instance.IsOpen;
        if (_frameWidth != Screen.width || _frameHeight != Screen.height || (_cam != null && Quaternion.Angle(_frameRotation, _cam.transform.rotation) > .1f))
        {
            if (dockOpen && _current != null)
                FrameInto(HideoutDockPanel.FreeScreenRect(_current.moduleKey, _current.dock), _current.cameraBias);
            else FrameRoom();
        }
        // ② 닫아도 **대기 자리로 돌아가지 않는다.** 방금 쓰던 시설 앞에 그대로 서 있는 편이
        //    자연스럽고, 의자 왕복이 사라져 카메라 이동이 절반으로 준다.
        //    의자는 '처음 들어왔을 때의 자세'로만 쓴다.
        if (uiOpen) _uiWasOpen = true;
        else if (_uiWasOpen) { _uiWasOpen = false; FrameRoom(); }
        else if (!dockOpen && _framed) FrameRoom();   // 패널 닫힘 = 프레임만 복귀

        // ⚠️ 도킹 패널이 열려 있어도 **다른 시설을 바로 고를 수 있게** 한다.
        //    매번 '닫기'를 눌러야 다음 시설이 눌리면 번거롭다.
        //    다만 전체화면 UI(uiOpen)가 떠 있으면 그쪽이 입력을 가지므로 막는다.
        if (uiOpen) return;
        if (!GameInput.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }

        // 3D 클릭 판정 — 2D판의 ScreenToWorldPoint/OverlapPoint는 쿼터뷰에서 의미가 없다.
        var ray = _cam.ScreenPointToRay(GameInput.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, clickMask, QueryTriggerInteraction.Ignore)) return;

        var anchor = hit.collider.GetComponentInParent<HideoutFacilityAnchor>();
        if (anchor == null || anchor == _idle) return;

        Select(anchor);
    }

    /// <summary>시설 선택 — 캐릭터를 그 앞에 세우고, 카메라로 UI 자리를 비우고, 기존 UI를 연다.</summary>
    void Select(HideoutFacilityAnchor a)
    {
        _current = a;
        RefreshFacilityBar();
        _uiWasOpen = false;
        PlaceAt(a, instant: false);

        // 카메라는 방에 고정된 채 **UI 자리만큼만** 한 번 밀린다(캐릭터를 따라가지 않는다).
        // 앵커에 cameraBias를 직접 넣어 뒀으면 그것이 우선한다(수동 연출용).
        FrameInto(HideoutDockPanel.FreeScreenRect(a.moduleKey, a.dock), a.cameraBias);

        // 도킹 패널 — 캐릭터를 가리지 않는 자리(우측/하단)에 붙는다.
        // 기존 시설 UI는 전체화면 전제라, 그 내용을 옮기기 전까지는 패널의 '열기'가 띄운다.
        var io = a.GetComponentInParent<InteractableObject>();
        var dock = HideoutDockPanel.Instance;
        if (dock == null) { if (io != null) io.Interact(_player.gameObject); return; }

        dock.Show(FacilityLabel.KoreanFor(a.moduleKey), DescriptionFor(a.moduleKey), a.dock, null, a.moduleKey);

        // 기존 시설 UI를 **도킹 안으로 끌어들인다.** 전체화면으로 따로 뜨지 않게.
        // 각 UI가 구조가 달라 개별 접근자가 없으므로, Interact 전후로 새로 켜진
        // Canvas를 찾아 그 내용을 통째로 받는다.
        if (io != null)
        {
            var before = ActivePanels();
            io.Interact(_player.gameObject);
            var opened = FindNewPanel(before);
            if (opened != null) dock.Adopt(opened);
        }
    }

}
