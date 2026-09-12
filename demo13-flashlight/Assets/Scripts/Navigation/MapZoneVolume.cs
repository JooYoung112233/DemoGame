using UnityEngine;

/// <summary>
/// 레이드 맵의 한 구역을 정의·등록하는 씬 컴포넌트.
/// 플레이어가 영역에 들어오면 해당 존을 발견(fog 해제) 처리.
/// 영역: 본인 BoxCollider(3D)가 있으면 그 bounds, 없으면 size로 직접 지정.
/// 설계: docs/navigation.md §3.1
/// </summary>
public class MapZoneVolume : MonoBehaviour
{
    [SerializeField] string zoneId = "zone";
    [SerializeField] string displayName = "구역";
    [Tooltip("BoxCollider가 없을 때 사용할 영역 크기(월드 단위, 평면 x·z).")]
    [SerializeField] Vector2 size = new Vector2(10f, 10f);
    [Tooltip("발견 판정 폴링 간격(초).")]
    [SerializeField] float pollInterval = 0.25f;

    MapZone _zone;
    float _pollTimer;

    /// <summary>런타임 생성용(기본 구역) — 비활성 상태에서 설정한 뒤 켜야 OnEnable 등록에 반영된다.</summary>
    public void Configure(string id, string name, Vector2 areaSize)
    {
        zoneId = id;
        displayName = name;
        size = areaSize;
    }

    Rect ComputeBounds()
    {
        // 3D 박스 콜라이더가 있으면 그 월드 영역(평면 x=월드X, y=월드Z). 2D 시절 BoxCollider2D 경로는 2026-09-12 제거.
        var box = GetComponent<BoxCollider>();
        if (box != null)
        {
            var b = box.bounds;
            return new Rect(b.min.x, b.min.z, b.size.x, b.size.z);
        }
        Vector2 cc = Plan3D.ToPlan(transform.position);
        return new Rect(cc.x - size.x * 0.5f, cc.y - size.y * 0.5f, size.x, size.y);
    }

    void OnEnable()
    {
        _zone = new MapZone(zoneId, displayName, ComputeBounds());
        var m = RaidMapManager.Instance;
        if (m != null) m.RegisterZone(_zone);
    }

    void OnDisable()
    {
        var m = RaidMapManager.InstanceIfExists;
        if (m != null && _zone != null) m.UnregisterZone(_zone);
        _zone = null;
    }

    void Update()
    {
        if (_zone == null || _zone.discovered) return;
        _pollTimer -= Time.deltaTime;
        if (_pollTimer > 0f) return;
        _pollTimer = pollInterval;

        var p = TopDownPlayer.Instance;
        if (p == null) return;
        if (_zone.Contains(Plan3D.ToPlan(p.transform.position)))
        {
            var m = RaidMapManager.InstanceIfExists;
            if (m != null) m.DiscoverAt(Plan3D.ToPlan(p.transform.position));
        }
    }

    void OnDrawGizmosSelected()
    {
        var r = ComputeBounds();
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireCube(new Vector3(r.center.x, r.center.y, 0f), new Vector3(r.width, r.height, 0.1f));
    }
}
