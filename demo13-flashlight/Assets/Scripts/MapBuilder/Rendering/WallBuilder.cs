using UnityEngine;

namespace TopDownMapEditor
{
    /// <summary>
    /// Builds wall cubes on the edges of a tile cell.
    /// rotation 0 = North (+Z), 1 = East (+X), 2 = South (-Z), 3 = West (-X)
    /// </summary>
    public static class WallBuilder
    {
        static Mesh _uprightBox;

        /// <summary>
        /// 모든 면의 UV가 일관되게 "위가 위"인 단위(1×1×1) 박스 메시.
        /// 유니티 기본 Cube는 마주보는 면의 UV 방향이 뒤집혀 한쪽 텍스처가 거꾸로 나오므로,
        /// 벽처럼 양면이 보이는 오브젝트는 이 메시를 써서 위아래 방향을 통일한다.
        /// (세로 면은 V = y, 가로 면은 임의 방향. 정점 위치+면 노멀로 UV를 직접 계산)
        /// </summary>
        public static Mesh GetUprightBoxMesh()
        {
            if (_uprightBox != null) return _uprightBox;

            Vector3 p0 = new(-0.5f, -0.5f, 0.5f), p1 = new(0.5f, -0.5f, 0.5f),
                    p2 = new(0.5f, -0.5f, -0.5f), p3 = new(-0.5f, -0.5f, -0.5f),
                    p4 = new(-0.5f, 0.5f, 0.5f), p5 = new(0.5f, 0.5f, 0.5f),
                    p6 = new(0.5f, 0.5f, -0.5f), p7 = new(-0.5f, 0.5f, -0.5f);

            // 면 순서: 0=Bottom(-Y) 1=Left(-X) 2=Front(+Z) 3=Back(-Z) 4=Right(+X) 5=Top(+Y)
            Vector3[] verts =
            {
                p0, p1, p2, p3,   // Bottom
                p7, p4, p0, p3,   // Left
                p4, p5, p1, p0,   // Front
                p6, p7, p3, p2,   // Back
                p5, p6, p2, p1,   // Right
                p7, p6, p5, p4,   // Top
            };

            var uvs = new Vector2[24];
            for (int f = 0; f < 6; f++)
            {
                for (int k = 0; k < 4; k++)
                {
                    Vector3 v = verts[f * 4 + k];
                    Vector2 uv;
                    if (f == 0 || f == 5)        // Bottom/Top
                        uv = new Vector2(v.x + 0.5f, v.z + 0.5f);
                    else if (f == 1 || f == 4)   // Left/Right
                        uv = new Vector2(v.z + 0.5f, v.y + 0.5f);
                    else                          // Front/Back — 세로 면, V는 항상 위가 위
                        uv = new Vector2(v.x + 0.5f, v.y + 0.5f);
                    uvs[f * 4 + k] = uv;
                }
            }

            var tris = new int[36];
            for (int f = 0; f < 6; f++)
            {
                int i = f * 4, t = f * 6;
                tris[t + 0] = i + 3; tris[t + 1] = i + 1; tris[t + 2] = i + 0;
                tris[t + 3] = i + 3; tris[t + 4] = i + 2; tris[t + 5] = i + 1;
            }

            _uprightBox = new Mesh { name = "UprightWallBox" };
            _uprightBox.vertices = verts;
            _uprightBox.uv = uvs;
            _uprightBox.triangles = tris;
            _uprightBox.RecalculateNormals();
            _uprightBox.RecalculateBounds();
            return _uprightBox;
        }

        /// <summary>머티리얼에 베이스 텍스처 주입. URP Lit(_BaseMap)·Standard(mainTexture) 모두 커버.</summary>
        static void ApplyTex(Material mat, Texture tex)
        {
            if (mat == null || tex == null) return;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            mat.mainTexture = tex;
        }

        /// <summary>
        /// 셀 중심(그리드 라인 사이 0.5칸)에 놓인 길이 length 벽의 양 끝을 그리드 라인에 맞추기 위한
        /// 길이축 보정량. 한 칸(length=tileSize)이면 0, 짝수 칸이면 ±반 칸을 돌려준다.
        /// </summary>
        public static float GridAlignShift(float length, float tileSize)
        {
            if (tileSize <= 0f) return 0f;
            // center - length/2 ≡ 0 (mod tileSize) 가 되도록. 셀 중심 = 0.5*tileSize 위치.
            float s = (length * 0.5f - tileSize * 0.5f) % tileSize;
            float half = tileSize * 0.5f;
            if (s > half) s -= tileSize;
            else if (s < -half) s += tileSize;
            return s;
        }

        public static GameObject CreateWallCube(
            PlacedTile tile, GridSettings settings,
            Transform parent = null, bool editorPreview = false)
        {
            var def = tile.tileDefinition;
            if (def == null || !def.IsWall) return null;

            // 벽 밑면 Y = 해당 층 바닥 높이. 0층=0, 2층=levelHeight ... 바닥 타일의 -0.05 깊이 오프셋은 깊이테스트용일 뿐 위치 기준 아님.
            float floorY = tile.level * settings.levelHeight;
            float height = def.wallHeight;
            float thickness = def.wallThickness;
            float tileSize = settings.tileSize;
            float halfTile = tileSize * 0.5f;
            // 벽 전용 비율 배율: 이미지 비율 유지하며 길이+높이를 함께 키움 (두께 제외)
            float wallScale = tile.wallScale <= 0f ? 1f : tile.wallScale;
            float scaledHeight = height * wallScale;
            // 벽 길이: 정의값(>0)이 있으면 그걸, 없으면 타일 한 칸. wallScale 배율 적용.
            float baseLength = def.wallLength > 0f ? def.wallLength : tileSize;
            float scaledLength = baseLength * wallScale;

            // 자유 배치 벽은 저장된 worldPosition을 그대로 쓰고, 스냅 벽은 셀 모서리로 보낸다.
            Vector3 worldPos;
            if (tile.freePlace)
            {
                worldPos = tile.worldPosition;
            }
            else
            {
                Vector3 cellCenter = TopDownGrid.GridToWorld(tile.gridPosition, settings);

                // Offset wall to the edge of the cell based on rotation
                // 0=North(+Z), 1=East(+X), 2=South(-Z), 3=West(-X)
                Vector3 edgeOffset = tile.rotation switch
                {
                    0 => new Vector3(0, 0, halfTile),    // North edge
                    1 => new Vector3(halfTile, 0, 0),    // East edge
                    2 => new Vector3(0, 0, -halfTile),   // South edge
                    3 => new Vector3(-halfTile, 0, 0),   // West edge
                    _ => Vector3.zero
                };

                worldPos = cellCenter + edgeOffset;

                // 긴 벽(길이≠한 칸)도 양 끝이 그리드 라인에 맞도록 길이축으로 보정.
                // 셀 중심은 그리드 라인 사이 0.5칸에 있어, 짝수 칸 길이면 반 칸 어긋난다.
                float lenShift = GridAlignShift(scaledLength, tileSize);
                if (tile.rotation == 0 || tile.rotation == 2)
                    worldPos.x += lenShift;   // North/South: 길이축 = X
                else
                    worldPos.z += lenShift;   // East/West: 길이축 = Z
            }

            var go = new GameObject($"Wall_{tile.gridPosition.x}_{tile.gridPosition.y}_E{tile.rotation}");
            if (editorPreview)
                go.hideFlags = HideFlags.DontSave;
            if (parent != null)
                go.transform.SetParent(parent);

            // 밑면이 바닥(floorY)에 닿도록: 중심 Y = floorY + 절반 높이
            go.transform.position = worldPos + new Vector3(0, floorY + scaledHeight * 0.5f, 0);

            // 자유 배치 벽은 루트를 yRotation으로 돌린다. (스냅 벽은 회전 0 = 축 정렬)
            if (tile.freePlace)
                go.transform.rotation = Quaternion.Euler(0, tile.yRotation, 0);

            // Main cube — UV가 일관된 커스텀 박스 메시 사용 (기본 Cube는 면별 UV가 뒤집힘)
            var cube = new GameObject("WallCube");
            if (editorPreview)
                cube.hideFlags = HideFlags.DontSave;
            cube.transform.SetParent(go.transform, false);
            cube.AddComponent<MeshFilter>().sharedMesh = GetUprightBoxMesh();
            cube.AddComponent<MeshRenderer>();
            if (!editorPreview)
                cube.AddComponent<BoxCollider>().size = Vector3.one;

            // 자유 배치 벽: 항상 로컬 X축으로 길이가 뻗음 (루트 yRotation이 방향을 정함).
            // 스냅 벽: North/South(0,2)는 X축, East/West(1,3)는 Z축으로 길이가 뻗음.
            Vector3 scale = tile.freePlace || tile.rotation == 0 || tile.rotation == 2
                ? new Vector3(scaledLength, scaledHeight, thickness)
                : new Vector3(thickness, scaledHeight, scaledLength);

            cube.transform.localScale = scale;

            // Apply material: per-instance override > definition material > sprite fallback.
            // 머티리얼 = 셰이더/룩. 스프라이트가 있으면 그 이미지를 베이스 텍스처로 주입한다.
            var renderer = cube.GetComponent<Renderer>();
            Material mat = tile.EffectiveMaterial;

            if (mat != null)
            {
                if (def.sprite != null)
                {
                    // 머티리얼을 복제해 벽 스프라이트 텍스처를 입힌다. (원본 머티리얼 오염 방지)
                    var inst = new Material(mat);
                    ApplyTex(inst, def.sprite.texture);
                    renderer.sharedMaterial = inst;
                    if (editorPreview) inst.hideFlags = HideFlags.DontSave;
                }
                else
                {
                    renderer.sharedMaterial = mat;
                }
            }
            else if (def.sprite != null)
            {
                var fallback = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (fallback.shader == null || fallback.shader.name == "Hidden/InternalErrorShader")
                    fallback = new Material(Shader.Find("Standard"));
                ApplyTex(fallback, def.sprite.texture);
                renderer.sharedMaterial = fallback;
                if (editorPreview)
                    fallback.hideFlags = HideFlags.DontSave;
            }

            // 벽이 바닥 타일 위에 렌더링되도록 sortingOrder 설정
            int wallSortOrder = TopDownGrid.GetSortingOrder(tile.gridPosition, TopDownGrid.OBJECT_SORT_BASE);
            renderer.sortingOrder = wallSortOrder;

            // 빛 차폐 그림자 프록시 (ShadowProxy)
            if (def.castsShadow && def.shadowBoxes != null && def.shadowBoxes.Length > 0)
                ShadowProxyBuilder.Build(def.shadowBoxes, go.transform, editorPreview);

            return go;
        }
    }
}
