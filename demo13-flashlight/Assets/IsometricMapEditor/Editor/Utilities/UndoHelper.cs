using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public static class UndoHelper
    {
        public static void RecordMapChange(MapData map, string description)
        {
            Undo.RecordObject(map, description);
        }

        public static void MarkDirty(MapData map)
        {
            EditorUtility.SetDirty(map);
            map.MarkDirty();
        }
    }
}
