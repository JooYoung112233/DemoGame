using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NavGrid 막힘 격자 위 8방향 A*. 코너 끼임 방지(대각 이동 시 양옆 둘 다 walkable이어야).
/// 셀 경로를 월드 웨이포인트 리스트로 반환. 이진 최소힙 오픈셋.
/// </summary>
public static class AStarPathfinder
{
    static readonly Vector2Int[] Dirs8 =
    {
        new Vector2Int(1, 0),  new Vector2Int(-1, 0), new Vector2Int(0, 1),  new Vector2Int(0, -1),
        new Vector2Int(1, 1),  new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    const float SQRT2 = 1.41421356f;

    /// <summary>경로 탐색. 성공 시 outPath에 월드 웨이포인트 채움.</summary>
    public static bool FindPath(NavGrid grid, Vector2 startW, Vector2 goalW, List<Vector2> outPath)
    {
        outPath.Clear();
        if (grid == null || !grid.Ready) return false;

        int W = grid.Width, H = grid.Height;
        Vector2Int start = grid.NearestWalkable(grid.WorldToCell(startW));
        Vector2Int goal  = grid.NearestWalkable(grid.WorldToCell(goalW));

        int startId = start.y * W + start.x;
        int goalId  = goal.y * W + goal.x;
        if (startId == goalId) { outPath.Add(grid.CellToWorld(goal.x, goal.y)); return true; }

        var open   = new MinHeap(256);
        var came   = new Dictionary<int, int>();
        var gScore = new Dictionary<int, float>();

        gScore[startId] = 0f;
        open.Push(startId, Heuristic(start, goal));

        bool found = false;
        int guard = 0, maxGuard = W * H * 4;
        while (open.Count > 0 && guard++ < maxGuard)
        {
            int curId = open.Pop();
            if (curId == goalId) { found = true; break; }

            int cx = curId % W, cy = curId / W;
            float curG = gScore[curId];

            for (int k = 0; k < 8; k++)
            {
                var d = Dirs8[k];
                int nx = cx + d.x, ny = cy + d.y;
                if (!grid.IsWalkable(nx, ny)) continue;

                // 코너 끼임 방지
                if (d.x != 0 && d.y != 0)
                    if (!grid.IsWalkable(cx + d.x, cy) || !grid.IsWalkable(cx, cy + d.y)) continue;

                int nId = ny * W + nx;
                float step = (d.x != 0 && d.y != 0) ? SQRT2 : 1f;
                float tentative = curG + step;
                if (!gScore.TryGetValue(nId, out float known) || tentative < known)
                {
                    gScore[nId] = tentative;
                    came[nId] = curId;
                    open.Push(nId, tentative + Heuristic(new Vector2Int(nx, ny), goal));
                }
            }
        }

        if (!found) return false;

        // 역추적
        var rev = new List<Vector2>();
        int node = goalId;
        while (node != startId)
        {
            rev.Add(grid.CellToWorld(node % W, node / W));
            if (!came.TryGetValue(node, out node)) break;
        }
        rev.Add(grid.CellToWorld(start.x, start.y));
        for (int i = rev.Count - 1; i >= 0; i--) outPath.Add(rev[i]);
        return true;
    }

    static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) + (SQRT2 - 2f) * Mathf.Min(dx, dy); // octile
    }

    // ── 이진 최소힙 (우선순위 = float) ───────────────────────────────
    class MinHeap
    {
        struct Node { public int id; public float pri; }
        Node[] _a;
        int _n;

        public MinHeap(int cap) { _a = new Node[Mathf.Max(16, cap)]; }
        public int Count => _n;

        public void Push(int id, float pri)
        {
            if (_n == _a.Length) System.Array.Resize(ref _a, _a.Length * 2);
            _a[_n] = new Node { id = id, pri = pri };
            int i = _n++;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (_a[p].pri <= _a[i].pri) break;
                (_a[p], _a[i]) = (_a[i], _a[p]);
                i = p;
            }
        }

        public int Pop()
        {
            int top = _a[0].id;
            _a[0] = _a[--_n];
            int i = 0;
            while (true)
            {
                int l = 2 * i + 1, r = 2 * i + 2, s = i;
                if (l < _n && _a[l].pri < _a[s].pri) s = l;
                if (r < _n && _a[r].pri < _a[s].pri) s = r;
                if (s == i) break;
                (_a[s], _a[i]) = (_a[i], _a[s]);
                i = s;
            }
            return top;
        }
    }
}
