using UnityEngine;

/// <summary>
/// 2D 길찾기 격자 (탑다운). 맵 영역을 cellSize 격자로 나눠 셀별 막힘을 베이크.
/// 막힘 = 비-트리거 Collider2D가 셀에 겹침 (Player/Enemy 레이어는 제외 — 장애물 아님).
/// 에이전트 바디 반경만큼 막힘을 dilate → "전신"이 벽에 안 끼는 경로만 통과.
/// 씬에 1개 배치(빌더가 생성). NavAgent가 Instance로 조회. 정적 맵 가정(필요 시 Rebuild).
/// </summary>
public class NavGrid : MonoBehaviour
{
    public static NavGrid Instance { get; private set; }

    [Header("영역 (이 오브젝트 위치 중심)")]
    [SerializeField] Vector2 areaSize = new Vector2(60f, 60f);
    [SerializeField] float cellSize = 0.5f;

    [Header("장애물")]
    [Tooltip("막힘으로 검사할 레이어 (기본 전부 — 코드에서 트리거/Player/Enemy 제외)")]
    [SerializeField] LayerMask obstacleMask = ~0;
    [Tooltip("에이전트 바디 반경 — 이만큼 막힘을 부풀려 전신 통과 보장")]
    [SerializeField] float agentRadius = 0.35f;

    [Header("베이크")]
    [SerializeField] bool bakeOnStart = true;
    [SerializeField] bool drawGizmos = false;

    int _w, _h;
    Vector2 _origin;            // 셀(0,0) 중심의 월드 좌표
    bool[,] _blocked;           // dilate 반영된 최종 막힘
    int _ignoreMask;            // Player|Enemy
    readonly Collider2D[] _buf = new Collider2D[16];

    public bool Ready => _blocked != null;
    public int  Width  => _w;
    public int  Height => _h;
    public float CellSize => cellSize;

    void Awake()
    {
        Instance = this;
        int p = LayerMask.NameToLayer("Player");
        int e = LayerMask.NameToLayer("Enemy");
        if (p >= 0) _ignoreMask |= 1 << p;
        if (e >= 0) _ignoreMask |= 1 << e;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start() { if (bakeOnStart) Rebuild(); }

    /// <summary>격자를 다시 굽는다. 맵이 바뀌면 호출.</summary>
    public void Rebuild()
    {
        _w = Mathf.Max(1, Mathf.RoundToInt(areaSize.x / cellSize));
        _h = Mathf.Max(1, Mathf.RoundToInt(areaSize.y / cellSize));
        Vector2 c = transform.position;
        _origin = c - areaSize * 0.5f + Vector2.one * (cellSize * 0.5f);

        var raw = new bool[_w, _h];
        Vector2 box = Vector2.one * (cellSize * 0.95f);
        for (int x = 0; x < _w; x++)
            for (int y = 0; y < _h; y++)
                raw[x, y] = CellHasObstacle(CellToWorld(x, y), box);

        int r = Mathf.Max(0, Mathf.CeilToInt(agentRadius / cellSize - 0.001f));
        if (r == 0) { _blocked = raw; return; }

        var dil = new bool[_w, _h];
        for (int x = 0; x < _w; x++)
            for (int y = 0; y < _h; y++)
                if (raw[x, y]) MarkDisk(dil, x, y, r);
        _blocked = dil;
    }

    bool CellHasObstacle(Vector2 center, Vector2 box)
    {
        int n = Physics2D.OverlapBoxNonAlloc(center, box, 0f, _buf, obstacleMask);
        for (int i = 0; i < n; i++)
        {
            var col = _buf[i];
            if (col == null || col.isTrigger) continue;
            if (((1 << col.gameObject.layer) & _ignoreMask) != 0) continue;
            return true;
        }
        return false;
    }

    static void MarkDisk(bool[,] g, int cx, int cy, int r)
    {
        int w = g.GetLength(0), h = g.GetLength(1);
        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                if (dx * dx + dy * dy > r * r) continue;
                int x = cx + dx, y = cy + dy;
                if (x >= 0 && x < w && y >= 0 && y < h) g[x, y] = true;
            }
    }

    // ── 좌표 변환 / 쿼리 ─────────────────────────────────────────────
    public Vector2 CellToWorld(int x, int y) => _origin + new Vector2(x * cellSize, y * cellSize);

    public Vector2Int WorldToCell(Vector2 p) => new Vector2Int(
        Mathf.RoundToInt((p.x - _origin.x) / cellSize),
        Mathf.RoundToInt((p.y - _origin.y) / cellSize));

    public bool InBounds(int x, int y) => x >= 0 && x < _w && y >= 0 && y < _h;
    public bool IsWalkable(int x, int y) => InBounds(x, y) && _blocked != null && !_blocked[x, y];

    /// <summary>막힘 안/밖이면 가장 가까운 walkable 셀로 보정.</summary>
    public Vector2Int NearestWalkable(Vector2Int c, int maxR = 8)
    {
        if (IsWalkable(c.x, c.y)) return c;
        for (int r = 1; r <= maxR; r++)
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue; // 링만
                    int x = c.x + dx, y = c.y + dy;
                    if (IsWalkable(x, y)) return new Vector2Int(x, y);
                }
        return c;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0f));
        if (!drawGizmos || _blocked == null) return;
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        for (int x = 0; x < _w; x++)
            for (int y = 0; y < _h; y++)
                if (_blocked[x, y]) Gizmos.DrawCube(CellToWorld(x, y), Vector3.one * cellSize * 0.9f);
    }
#endif
}
