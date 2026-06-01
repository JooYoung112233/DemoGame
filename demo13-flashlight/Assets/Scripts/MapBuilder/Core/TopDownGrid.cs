using UnityEngine;

namespace TopDownMapEditor
{
    public static class TopDownGrid
    {
        // ─────────────────────────────────────────────────────────────
        //  뷰(시점) 단일 설정 — 탑다운의 단일 출처.
        //  핵심(2026-06-02 결정, safehouse.md "구현 방식 확정"):
        //   · 카메라 = 2D 직교(orthographic) **평면 뷰** — 3D 틸트 카메라가 아님. 바닥(XZ)을
        //     수직으로 내려다본다(정직하). 그래서 바닥에 깔린 스프라이트를 정면으로 본다.
        //   · 시점의 틸트(≈80°)는 **아트 자체에 미리 그려넣는다**(baked-in perspective).
        //     모든 타일/프랍/펜스/캐릭터를 같은 80° 시점으로 그려 자연스럽게 합친다.
        //   · 맵 = 타일 조립식. 바닥 타일을 격자로 깔고 그 위에 스프라이트 배치.
        //  → 카메라 각도/스프라이트 회전은 "바닥 정면"으로 고정하고, 입체감은 텍스처가 담당.
        // ─────────────────────────────────────────────────────────────

        /// <summary>카메라가 내려다보는 각(도). 90=정직하(바닥을 수직으로 봄). 2D 평면 뷰라 90 고정.
        /// (입체 틸트는 카메라가 아니라 아트(≈80°)에 그려넣음 — 카메라를 기울이면 아트와 이중 틸트가 됨.)</summary>
        public const float CameraPitch = 90f;

        /// <summary>카메라 Y회전(도). 0=정사각 그리드(탑다운).</summary>
        public const float CameraYaw = 0f;

        /// <summary>카메라 회전(피치×요). 모든 카메라 코드가 이 값을 쓴다.</summary>
        public static Quaternion CameraRotation => Quaternion.Euler(CameraPitch, CameraYaw, 0f);

        /// <summary>
        /// 스프라이트 프롭/캐릭터를 바닥(XZ 평면)에 평평하게 눕히는 회전. 카메라가 바닥을 정면으로
        /// 내려다보므로 텍스처(80° 시점으로 그려진)가 정면으로 보인다(타일과 동일 규약). yaw는 호출측에서 곱한다.
        /// </summary>
        public static Quaternion SpriteFlatRotation => Quaternion.Euler(90f, 0f, 0f);

        /// <summary>
        /// Grid cell → World position on XZ plane (Y=0).
        /// Tiles laid out on a flat XZ grid, camera provides the view angle.
        /// </summary>
        public static Vector3 GridToWorld(Vector2Int cell, GridSettings settings)
        {
            // Cell center at (x+0.5, 0, y+0.5) * tileSize so cell edges align with Unity grid lines
            float x = (cell.x + 0.5f) * settings.tileSize;
            float z = (cell.y + 0.5f) * settings.tileSize;
            return new Vector3(x, 0f, z) + settings.originOffset;
        }

        /// <summary>
        /// World position → nearest grid cell (ignores Y height).
        /// </summary>
        public static Vector2Int WorldToGrid(Vector3 worldPos, GridSettings settings)
        {
            Vector3 local = worldPos - settings.originOffset;
            int col = Mathf.FloorToInt(local.x / settings.tileSize);
            int row = Mathf.FloorToInt(local.z / settings.tileSize);
            return new Vector2Int(col, row);
        }

        /// <summary>
        /// Sorting order for sprite rendering. Higher row+col = rendered later (in front).
        /// Floor parameter reserved for future multi-floor support.
        /// </summary>
        /// <summary>
        /// Base sorting offset for objects (buildings, props) that sit ON TOP of floor tiles.
        /// Added to their sorting order so they always render above ground tiles at the same cell.
        /// </summary>
        public const int OBJECT_SORT_BASE = 5;

        public static int GetSortingOrder(Vector2Int cell, int layerOffset = 0, int floor = 0)
        {
            // 탑다운(yaw=0): 화면 깊이축 = 월드 +Z(=cell.y 행). 카메라는 -Z 뒤·+Y 위에서 내려다보므로
            // z가 작은(카메라에 가까운, 화면 아래) 셀이 '앞'. → 행이 클수록 뒤로 가도록 -cell.y.
            // x는 좌우(깊이 무관)라 정렬에 안 씀.
            // ※ Unity에서 앞뒤가 뒤집혀 보이면 이 부호만 +로 바꾸면 됨.
            return floor * 1000 - cell.y * 10 + layerOffset;
        }

        /// <summary>
        /// 카메라(CameraRotation)의 정규화된 시선 방향.
        /// 직교 카메라에서는 이 축을 따라 이동해도 화면 위치는 변하지 않고 '깊이'만 바뀐다.
        /// </summary>
        public static Vector3 ViewDir => (CameraRotation * Vector3.forward).normalized;

        /// <summary>sortingOrder 1단위당 변환되는 깊이 오프셋(월드 단위).</summary>
        public const float SORT_DEPTH_EPS = 0.003f;

        /// <summary>
        /// sortingOrder를 시선축 깊이 오프셋으로 변환한다. 불투명 메시(건물/벽 큐브 등)는
        /// sortingOrder를 무시하고 깊이 버퍼로 정렬되므로, 같은 셀에 겹친 코플래너 조각
        /// (바닥/벽/천장)의 앞뒤를 이 미세 깊이 차이로 확정한다. 값이 클수록 카메라에 가깝게(앞) 그려진다.
        /// </summary>
        public static Vector3 SortDepthOffset(int sortingOrder)
        {
            return -ViewDir * (sortingOrder * SORT_DEPTH_EPS);
        }

        /// <summary>
        /// Returns 4 corners of a cell on the XZ plane (Y=0).
        /// </summary>
        public static Vector3[] GetCellWorldCorners(Vector2Int cell, GridSettings settings)
        {
            Vector3 center = GridToWorld(cell, settings);
            float half = settings.tileSize * 0.5f;
            return new Vector3[]
            {
                new(center.x - half, 0, center.z + half), // top-left
                new(center.x + half, 0, center.z + half), // top-right
                new(center.x + half, 0, center.z - half), // bottom-right
                new(center.x - half, 0, center.z - half), // bottom-left
            };
        }

        public static Vector3 GetTileScale(Sprite sprite, GridSettings settings)
        {
            if (sprite == null) return Vector3.one;
            var bounds = sprite.bounds.size;
            float scaleX = bounds.x > 0 ? settings.tileSize / bounds.x : 1f;
            float scaleY = bounds.y > 0 ? settings.tileSize / bounds.y : 1f;
            return new Vector3(scaleX, scaleY, 1f);
        }

        /// <summary>
        /// Axis-aligned bounding box of the entire map on XZ plane.
        /// </summary>
        public static Bounds GetMapWorldBounds(GridSettings settings)
        {
            Vector3 min = GridToWorld(Vector2Int.zero, settings);
            Vector3 max = GridToWorld(new Vector2Int(settings.mapWidth - 1, settings.mapHeight - 1), settings);
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = new(
                Mathf.Abs(max.x - min.x) + settings.tileSize,
                0.1f,
                Mathf.Abs(max.z - min.z) + settings.tileSize
            );
            return new Bounds(center, size);
        }
    }
}
