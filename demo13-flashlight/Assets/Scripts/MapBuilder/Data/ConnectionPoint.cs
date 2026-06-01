using UnityEngine;

namespace TopDownMapEditor
{
    [System.Serializable]
    public class ConnectionPoint
    {
        public string connectionId;
        public string label;
        public Vector2Int gridPosition;
        public ConnectionDirection direction;
        public string targetMapId;
        public string targetConnectionId;
    }

    public enum ConnectionDirection
    {
        North,
        South,
        East,
        West
    }
}
