using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    [InitializeOnLoad]
    public static class LivePreviewManager
    {
        static MapData lastTrackedMap;
        static int lastTileCount;

        static LivePreviewManager()
        {
            EditorApplication.update += OnEditorUpdate;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        static void OnEditorUpdate()
        {
            var map = MapEditorWindow.ActiveMap;
            if (map == null) return;

            int currentCount = 0;
            foreach (var layer in map.layers)
                currentCount += layer.tiles.Count;

            if (map != lastTrackedMap || currentCount != lastTileCount)
            {
                lastTrackedMap = map;
                lastTileCount = currentCount;
                RefreshScenePreview(map);
            }
        }

        static void OnUndoRedo()
        {
            var map = MapEditorWindow.ActiveMap;
            if (map != null)
                RefreshScenePreview(map);
        }

        static void RefreshScenePreview(MapData map)
        {
            var previewRoot = GameObject.Find("__MapEditorPreview__");
            if (previewRoot == null)
            {
                previewRoot = new GameObject("__MapEditorPreview__");
                previewRoot.hideFlags = HideFlags.DontSave;
            }

            foreach (Transform child in previewRoot.transform)
                Object.DestroyImmediate(child.gameObject);

            foreach (var layer in map.layers)
            {
                if (!layer.isVisible) continue;

                foreach (var tile in layer.tiles)
                {
                    if (tile.tileDefinition == null || tile.tileDefinition.sprite == null)
                        continue;

                    Vector3 worldPos = IsometricGrid.GridToWorld(tile.gridPosition, map.gridSettings);
                    var go = new GameObject($"Preview_{tile.gridPosition.x}_{tile.gridPosition.y}");
                    go.hideFlags = HideFlags.DontSave;
                    go.transform.SetParent(previewRoot.transform);
                    go.transform.position = worldPos;

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = tile.tileDefinition.sprite;
                    sr.flipX = tile.flipX;
                    sr.sortingOrder = IsometricGrid.GetSortingOrder(tile.gridPosition, layer.sortingLayerOffset)
                                      + tile.tileDefinition.sortingOffset;
                }
            }

            SceneView.RepaintAll();
        }

        public static void ClearPreview()
        {
            var previewRoot = GameObject.Find("__MapEditorPreview__");
            if (previewRoot != null)
                Object.DestroyImmediate(previewRoot);
        }
    }
}
