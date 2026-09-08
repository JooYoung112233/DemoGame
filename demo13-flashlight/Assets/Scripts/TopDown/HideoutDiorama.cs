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
public class HideoutDiorama : MonoBehaviour
{
    /// <summary>씬에 디오라마가 있는가 — HideoutController가 2D 연출을 건너뛸지 판단한다.</summary>
    public static bool Active { get; private set; }

    [Tooltip("시설 클릭을 받을 레이어. 기본 전체.")]
    [SerializeField] LayerMask clickMask = ~0;

    [Tooltip("시설을 볼 때 카메라 오소 크기. 0이면 유지.")]
    [SerializeField] float focusOrthoSize = 6.0f;

    readonly List<HideoutFacilityAnchor> _anchors = new List<HideoutFacilityAnchor>();
    HideoutFacilityAnchor _idle;
    HideoutFacilityAnchor _current;
    bool _uiWasOpen;   // UI가 실제로 열린 적이 있어야 '닫힘'을 판단할 수 있다

    TopDownPlayer _player;
    Transform _view;              // Character3D
    Camera _cam;

    void Awake() => Active = true;
    void OnDestroy() { Active = false; ClearFocus(); }

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

        if (_idle != null) PlaceAt(_idle, instant: true);
        else Debug.LogWarning("[HideoutDiorama] moduleKey=\"idle\" 앵커(대기 의자)가 없다.", this);
    }

    void Update()
    {
        if (_player == null) return;

        bool uiOpen = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();

        // 열린 시설 UI가 **닫혔을 때만** 대기 자리로 돌아간다.
        // ⚠️ uiOpen만 보면 UI가 열리기도 전에(같은 프레임) 복귀가 발동해
        //    시설 선택이 즉시 취소된다.
        bool dockOpen = HideoutDockPanel.Instance != null && HideoutDockPanel.Instance.IsOpen;
        if (uiOpen) _uiWasOpen = true;
        else if (_uiWasOpen && _current != null && _current != _idle) { _uiWasOpen = false; ReturnToIdle(); }
        else if (!dockOpen && _current != null && _current != _idle) ReturnToIdle();   // 패널 닫힘 = 대기 복귀

        if (uiOpen || dockOpen) return;
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
        _uiWasOpen = false;
        PlaceAt(a, instant: false);

        if (CameraFollow.Instance != null)
            CameraFollow.Instance.SetFocus(_player.transform, a.ResolvedBias, focusOrthoSize);

        // 도킹 패널 — 캐릭터를 가리지 않는 자리(우측/하단)에 붙는다.
        // 기존 시설 UI는 전체화면 전제라, 그 내용을 옮기기 전까지는 패널의 '열기'가 띄운다.
        var io = a.GetComponentInParent<InteractableObject>();
        if (HideoutDockPanel.Instance != null)
        {
            System.Action open = io != null ? () => io.Interact(_player.gameObject) : (System.Action)null;
            HideoutDockPanel.Instance.Show(FacilityLabel.KoreanFor(a.moduleKey), DescriptionFor(a.moduleKey), a.dock, open);
        }
        else if (io != null) io.Interact(_player.gameObject);
    }

    /// <summary>시설 한 줄 설명 — 도킹 패널 본문.</summary>
    static string DescriptionFor(string key) => key switch
    {
        "bed"       => "잠을 자 체력을 회복하고 시간을 넘긴다.",
        "workbench" => "무기·장비를 만들고 수리한다.",
        "stash"     => "가져온 것을 보관한다. 레이드에 들고 나가지 않는 짐.",
        "radio"     => "바깥 소식을 듣는다. 전력이 필요하다.",
        "cooking"   => "재료로 음식을 만든다. 버프가 붙는다.",
        "medical"   => "붕대·진통제 같은 일회용 치료품을 만든다.",
        "dispatch"  => "사람을 내보낸다. 돌아올 때까지 시간이 걸린다.",
        "generator" => "전력을 켜고 끈다. 라디오·파견의 전제.",
        _           => "",
    };

    void ReturnToIdle()
    {
        _current = _idle;
        if (_idle != null) PlaceAt(_idle, instant: false);
        if (HideoutDockPanel.Instance != null) HideoutDockPanel.Instance.Hide();
        ClearFocus();
    }

    static void ClearFocus()
    {
        if (CameraFollow.Instance != null) CameraFollow.Instance.ClearFocus();
    }

    /// <summary>캐릭터를 앵커 자리에 놓고 시설을 바라보게 한다.</summary>
    void PlaceAt(HideoutFacilityAnchor a, bool instant)
    {
        if (_player == null || a == null) return;

        Vector3 pos = a.StandPosition;
        _player.transform.position = pos;
        var rb = _player.GetComponent<Rigidbody>();
        if (rb != null) rb.position = pos;
        Physics.SyncTransforms();

        // 시설을 바라본다. 기울기는 카메라가 주므로 yaw만.
        if (_view != null)
        {
            Vector2 plan = Plan3D.ToPlan(a.FaceDirection);
            float yaw = Mathf.Atan2(plan.x, plan.y) * Mathf.Rad2Deg;
            _view.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        ApplyPose(a.pose);

        if (instant && CameraFollow.Instance != null) CameraFollow.Instance.SnapToTarget();
    }

    /// <summary>자세 적용. ⚠️ 앉기·눕기 애니메이션은 아직 없다(치비 = idle/walk/run 3종).
    /// 애니메이터에 같은 이름의 파라미터가 있을 때만 걸고, 없으면 조용히 넘어간다
    /// — 없는 파라미터를 건드리면 매 클릭마다 경고가 쌓인다. 제작되면 자동으로 살아난다.</summary>
    void ApplyPose(HideoutFacilityAnchor.Pose pose)
    {
        if (_view == null) return;
        var an = _view.GetComponentInChildren<Animator>();
        if (an == null || an.runtimeAnimatorController == null) return;

        string wanted = pose.ToString();   // Stand / Sit / Lie
        foreach (var p in an.parameters)
        {
            if (p.type != AnimatorControllerParameterType.Trigger || p.name != wanted) continue;
            an.SetTrigger(wanted);
            return;
        }
    }
}
