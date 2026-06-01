using UnityEngine;
using System.Collections.Generic;

namespace TopDownMapEditor
{
    [System.Serializable]
    public class EscapePoint
    {
        public string escapeId;
        public string label;
        public Vector2Int gridPosition;
        public EscapeType type;
        public string targetMapId;
        public string targetConnectionId;
        public bool requiresKey;
        public string requiredKeyItemId;
        public bool isHidden;
        public List<SpawnCondition> availableConditions = new();
    }

    public enum EscapeType
    {
        Door,
        Window,
        SecretPassage,
        Emergency
    }
}
