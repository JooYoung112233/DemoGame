using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 격자 A* 경로 추종 에이전트 (적 AI용). 타겟까지 경로를 받아 웨이포인트를 따라가는
/// 방향(DesiredDirection)을 매 프레임 제공. 실제 이동은 EnemyController가 Rigidbody로 적용.
/// 경로·방향은 전부 **평면(XZ) Vector2**다 — 물리 검사에서만 월드로 올린다.
/// NavGrid가 없거나 경로 실패면 타겟 직진으로 폴백. 주기적 리패스(스태거).
/// </summary>
public class NavAgent : MonoBehaviour
{
    [SerializeField] float repathInterval = 0.4f;
    [SerializeField] float arriveRadius   = 0.25f;
    [Tooltip("이 거리 안에서는 길찾기를 끄고 직진한다. 상호작용 대상·문·플레이어는 벽에 붙어 있어 "
             + "팽창된 격자에선 그 셀이 '막힘'이라, A*가 옆 칸만 오가며 제자리 왕복(와리가리)한다.")]
    [SerializeField] float nearDirectDist = 1.8f;
    [Tooltip("LOS 스킵(부드러운 경로)용 캐스트 반경")]
    [SerializeField] float agentRadius = 0.3f;
    [SerializeField] LayerMask obstacleMask = ~0;

    readonly List<Vector2> _path = new List<Vector2>();
    int     _wp;
    Vector2 _dest;
    bool    _hasDest;
    float   _repathTimer;
    int     _losMask;

    /// <summary>추종 목표 방향(정규화). 길찾기 결과 또는 직진 폴백.</summary>
    public Vector2 DesiredDirection { get; private set; }
    public bool HasPath => _wp < _path.Count;

    void Awake()
    {
        int ignore = 0;
        int p = LayerMask.NameToLayer("Player"); if (p >= 0) ignore |= 1 << p;
        int e = LayerMask.NameToLayer("Enemy");  if (e >= 0) ignore |= 1 << e;
        _losMask = obstacleMask & ~ignore;       // 벽/프롭만 LOS 차단 (플레이어·다른 적 무시)
    }

    /// <summary>추격 목표 갱신 (매 프레임 호출 OK).</summary>
    public void SetDestination(Vector2 worldDest)
    {
        _dest = worldDest;
        _hasDest = true;
    }

    public void Stop()
    {
        _hasDest = false;
        _path.Clear();
        _wp = 0;
        DesiredDirection = Vector2.zero;
    }

    void Update()
    {
        if (!_hasDest) { DesiredDirection = Vector2.zero; return; }

        Vector2 pos = Plan3D.ToPlan(transform.position);

        // 코앞이면 길찾기를 쓰지 않는다 — 2026-07-28 QA에서 목표 1.9m 앞인데
        // 6초간 12m를 왕복(OSCILLATION)했다. 목표 셀이 팽창으로 막혀 A*가 옆 칸만 오간 것.
        Vector2 toDest = _dest - pos;
        if (toDest.sqrMagnitude <= nearDirectDist * nearDirectDist)
        {
            DesiredDirection = toDest.sqrMagnitude > 0.0001f ? toDest.normalized : Vector2.zero;
            return;
        }

        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
        {
            _repathTimer = repathInterval + Random.value * 0.1f; // 동시 리패스 분산
            Repath(pos);
        }

        if (!HasPath)
        {
            Vector2 to = _dest - pos;
            DesiredDirection = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.zero;
            return;
        }

        AdvanceWaypoint(pos);

        Vector2 target = _path[Mathf.Min(_wp, _path.Count - 1)];
        Vector2 dir = target - pos;
        DesiredDirection = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero;
    }

    void Repath(Vector2 pos)
    {
        var grid = NavGrid.Instance;
        if (grid == null || !grid.Ready) { _path.Clear(); _wp = 0; return; }
        if (AStarPathfinder.FindPath(grid, pos, _dest, _path)) _wp = 0;
        else { _path.Clear(); _wp = 0; }
    }

    void AdvanceWaypoint(Vector2 pos)
    {
        // 가까운 웨이포인트 통과
        while (_wp < _path.Count - 1 && Vector2.Distance(pos, _path[_wp]) <= arriveRadius)
            _wp++;

        // LOS skip-ahead: 더 먼 웨이포인트가 직선으로 막힘 없이 보이면 당겨서 부드럽게
        // ⚠️ 검사는 **몸 높이**에서 한다. 지면에서 쏘면 바닥·연석에 걸려 항상 막힌 것으로 나온다.
        float y = transform.position.y + BodyProbeY;
        for (int i = _path.Count - 1; i > _wp; i--)
        {
            Vector2 to = _path[i] - pos;
            float dist = to.magnitude;
            if (dist < 0.001f) { _wp = i; break; }
            bool blocked = Physics.SphereCast(Plan3D.ToWorld(pos, y), agentRadius * 0.8f,
                                              Plan3D.ToWorld(to / dist), out _, dist,
                                              _losMask, QueryTriggerInteraction.Ignore);
            if (!blocked) { _wp = i; break; }
        }
    }

    /// <summary>LOS 검사를 쏘는 높이(발밑이 아니라 몸통).</summary>
    const float BodyProbeY = 0.9f;
}
