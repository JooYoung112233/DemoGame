using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 문/통로 상태를 미니맵에 사일런트 힐식으로 자동 주석.
/// - DoorController가 같이 붙어 있으면 잠금(Key/Quest) → Locked로 자동 해석(수동 배치, 영속).
/// - randomBlock이면 매 레이드 확률로 Blocked(절차적, 이번 판 한정 — 영속 저장 X).
/// - 플레이어가 근처에 닿으면 '알게 됨'으로 표기(구역 전체 해금과 독립).
/// 설계: docs/navigation.md §3.4
/// </summary>
public class PassageMarker : MonoBehaviour
{
    public static readonly List<PassageMarker> All = new List<PassageMarker>();

    [Header("상태")]
    [Tooltip("DoorController도 randomBlock도 없을 때 쓸 고정 상태(잠김/일방통행 수동 배치).")]
    [SerializeField] PassageState authoredState = PassageState.Open;

    [Header("막힘 = 확률 배치 (절차적)")]
    [Tooltip("켜면 매 레이드 확률로 막힘. 잠긴 문(DoorController)과 함께 쓰지 말 것.")]
    [SerializeField] bool randomBlock = false;
    [Range(0f, 1f)][SerializeField] float blockChance = 0.18f;

    [Header("영속(잠김 주석용 안정 ID)")]
    [Tooltip("지역 내 고정된 잠긴 통로의 고유 id. 비우면 영속 저장 안 함(막힘은 어차피 저장 X).")]
    [SerializeField] string passageId = "";

    [Tooltip("이 거리 안에 들어오면 통로를 '알게 됨'으로 표기.")]
    [SerializeField] float knownRadius = 2.5f;

    PassageState _state;
    DoorController _door;
    bool _known;

    public PassageState State => _state;
    public Vector2 Position => transform.position;

    /// <summary>플레이어가 직접 부딪혔거나(이번 판), 과거에 알게 된(영속) 통로인가.</summary>
    public bool Known
    {
        get
        {
            if (_known) return true;
            var m = RaidMapManager.InstanceIfExists;
            return m != null && m.IsPassageKnown(passageId);
        }
    }

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        _door = GetComponent<DoorController>();
        ResolveState();
        if (_door != null) _door.OnDoorOpened += OnDoorOpened;
    }

    void OnDestroy()
    {
        if (_door != null) _door.OnDoorOpened -= OnDoorOpened;
    }

    void ResolveState()
    {
        if (_door != null)
        {
            if (_door.IsOpen) _state = PassageState.Open;
            else if (_door.Lock == DoorController.LockType.Key || _door.Lock == DoorController.LockType.Quest)
                _state = PassageState.Locked;
            else
                _state = PassageState.Open;
            return;
        }
        if (randomBlock)
        {
            _state = (Random.value < blockChance) ? PassageState.Blocked : PassageState.Open;
            return;
        }
        _state = authoredState;
    }

    void OnDoorOpened(DoorController _) => _state = PassageState.Open;

    void Update()
    {
        if (_known) return;
        var p = TopDownPlayer.Instance;
        if (p == null) return;
        if (((Vector2)p.transform.position - (Vector2)transform.position).sqrMagnitude <= knownRadius * knownRadius)
        {
            _known = true;
            // 잠김 주석만 영속(막힘은 이번 판 한정)
            if (_state == PassageState.Locked)
            {
                var m = RaidMapManager.InstanceIfExists;
                if (m != null && !string.IsNullOrEmpty(passageId)) m.MarkPassageKnown(passageId);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, knownRadius);
    }
}
