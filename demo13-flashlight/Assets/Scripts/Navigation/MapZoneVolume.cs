using UnityEngine;

/// <summary>
/// 레이드 맵의 한 구역을 정의·등록하는 씬 컴포넌트.
/// 플레이어가 영역에 들어오면 해당 존을 발견(fog 해제) 처리.
/// 영역: 본인 BoxCollider2D가 있으면 그 bounds, 없으면 size로 직접 지정.
/// 설계: docs/navigation.md §3.1
/// </summary>
public class MapZoneVolume : MonoBehaviour
{
    [SerializeField] string zoneId = "zone";
    [SerializeField] string displayName = "구역";
    [Tooltip("BoxCollider2D가 없을 때 사용할 영역 크기(월드 단위).")]
    [SerializeField] Vector2 size = new Vector2(10f, 10f);
    [Tooltip("발견 판정 폴링 간격(초).")]
    [SerializeField] float pollInterval = 0.25f;

    MapZone _zone;
    float _pollTimer;

    Rect ComputeBounds()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Vector2 c = Plan3D.ToPlan(transform.position) + box.offset;
            Vector2 s = Vector2.Scale(box.size, (Vector2)transform.lossyScale);
            return new Rect(c.x - s.x * 0.5f, c.y - s.y * 0.5f, s.x, s.y);
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
