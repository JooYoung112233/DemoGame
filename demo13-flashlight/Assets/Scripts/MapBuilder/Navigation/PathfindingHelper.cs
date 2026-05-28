using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
    public static class PathfindingHelper
    {
        static readonly Vector2Int[] Directions = {
            new(0, 1), new(0, -1), new(1, 0), new(-1, 0),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
        };

        public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, WalkabilityData walkability, GridSettings settings)
        {
            if (!settings.IsInBounds(start) || !settings.IsInBounds(end))
                return null;

            if (walkability.GetCell(end) == WalkableType.Blocked)
                return null;

            var openSet = new SortedSet<(float f, int order, Vector2Int pos)>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float> { [start] = 0 };
            int insertOrder = 0;

            float h = Heuristic(start, end);
            openSet.Add((h, insertOrder++, start));

            while (openSet.Count > 0)
            {
                var current = openSet.Min;
                openSet.Remove(current);
                Vector2Int currentPos = current.pos;

                if (currentPos == end)
                    return ReconstructPath(cameFrom, currentPos);

                foreach (var dir in Directions)
                {
                    Vector2Int neighbor = currentPos + dir;
                    if (!settings.IsInBounds(neighbor)) continue;

                    var cellType = walkability.GetCell(neighbor);
                    if (cellType == WalkableType.Blocked) continue;

                    float moveCost = dir.x != 0 && dir.y != 0 ? 1.414f : 1f;
                    if (cellType == WalkableType.SlowZone) moveCost *= 2f;

                    float tentativeG = gScore[currentPos] + moveCost;
                    if (tentativeG < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                    {
                        cameFrom[neighbor] = currentPos;
                        gScore[neighbor] = tentativeG;
                        float f = tentativeG + Heuristic(neighbor, end);
                        openSet.Add((f, insertOrder++, neighbor));
                    }
                }
            }
            return null;
        }

        static float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }
    }
}
