using UnityEngine;

namespace IsometricMapEditor
{
    public class MapBuilderGridOverlay : MonoBehaviour
    {
        public GridSettings gridSettings;
        public Color gridColor = new(1f, 1f, 1f, 0.15f);
        public Color borderColor = new(1f, 0.8f, 0f, 0.5f);
        public Color highlightColor = new(0f, 1f, 0f, 0.3f);

        Material _lineMaterial;
        Vector2Int _highlightCell = new(-1, -1);
        bool _showHighlight;

        // Free mode cursor
        bool _freeMode;
        Vector3 _freeWorldPos;

        public void SetHighlightCell(Vector2Int cell, bool show)
        {
            _highlightCell = cell;
            _showHighlight = show;
            _freeMode = false;
        }

        public void SetFreeHighlight(Vector3 worldPos, Vector2Int cell, bool inBounds)
        {
            _freeWorldPos = worldPos;
            _highlightCell = cell;
            _showHighlight = inBounds;
            _freeMode = true;
        }

        void CreateLineMaterial()
        {
            if (_lineMaterial != null) return;
            var shader = Shader.Find("Hidden/Internal-Colored");
            _lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _lineMaterial.SetInt("_ZWrite", 0);
        }

        void OnRenderObject()
        {
            if (gridSettings == null) return;
            CreateLineMaterial();

            _lineMaterial.SetPass(0);

            int w = gridSettings.mapWidth;
            int h = gridSettings.mapHeight;
            float ts = gridSettings.tileSize;
            Vector3 origin = gridSettings.originOffset;

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            // Grid lines
            GL.Begin(GL.LINES);
            GL.Color(gridColor);

            for (int x = 0; x <= w; x++)
            {
                Vector3 start = origin + new Vector3(x * ts, 0.01f, 0);
                Vector3 end = origin + new Vector3(x * ts, 0.01f, h * ts);
                GL.Vertex(start);
                GL.Vertex(end);
            }
            for (int y = 0; y <= h; y++)
            {
                Vector3 start = origin + new Vector3(0, 0.01f, y * ts);
                Vector3 end = origin + new Vector3(w * ts, 0.01f, y * ts);
                GL.Vertex(start);
                GL.Vertex(end);
            }

            GL.End();

            // Border
            GL.Begin(GL.LINES);
            GL.Color(borderColor);
            Vector3 bl = origin + new Vector3(0, 0.02f, 0);
            Vector3 br = origin + new Vector3(w * ts, 0.02f, 0);
            Vector3 tl = origin + new Vector3(0, 0.02f, h * ts);
            Vector3 tr = origin + new Vector3(w * ts, 0.02f, h * ts);
            GL.Vertex(bl); GL.Vertex(br);
            GL.Vertex(br); GL.Vertex(tr);
            GL.Vertex(tr); GL.Vertex(tl);
            GL.Vertex(tl); GL.Vertex(bl);
            GL.End();

            // Highlight
            if (_showHighlight && gridSettings.IsInBounds(_highlightCell))
            {
                if (_freeMode)
                {
                    // Free mode: crosshair + small diamond at exact world position
                    float size = ts * 0.3f;
                    float y = 0.03f;
                    float px = _freeWorldPos.x;
                    float pz = _freeWorldPos.z;

                    // Diamond
                    GL.Begin(GL.QUADS);
                    GL.Color(new Color(1f, 0.6f, 0.2f, 0.5f));
                    GL.Vertex3(px, y, pz - size);
                    GL.Vertex3(px + size, y, pz);
                    GL.Vertex3(px, y, pz + size);
                    GL.Vertex3(px - size, y, pz);
                    GL.End();

                    // Crosshair lines
                    GL.Begin(GL.LINES);
                    GL.Color(new Color(1f, 0.6f, 0.2f, 0.8f));
                    GL.Vertex3(px - size * 1.5f, y, pz);
                    GL.Vertex3(px + size * 1.5f, y, pz);
                    GL.Vertex3(px, y, pz - size * 1.5f);
                    GL.Vertex3(px, y, pz + size * 1.5f);
                    GL.End();
                }
                else
                {
                    // Snap mode: full cell highlight
                    GL.Begin(GL.QUADS);
                    GL.Color(highlightColor);
                    float cx = _highlightCell.x * ts + origin.x;
                    float cz = _highlightCell.y * ts + origin.z;
                    float y = 0.02f;
                    GL.Vertex3(cx, y, cz);
                    GL.Vertex3(cx + ts, y, cz);
                    GL.Vertex3(cx + ts, y, cz + ts);
                    GL.Vertex3(cx, y, cz + ts);
                    GL.End();
                }
            }

            GL.PopMatrix();
        }

        void OnDestroy()
        {
            if (_lineMaterial != null)
                DestroyImmediate(_lineMaterial);
        }
    }
}
