using UnityEngine;

namespace IsometricMapEditor
{
    public enum MapObjectType
    {
        SpawnPoint,
        EscapePoint,
        LootContainer,
        EnemySpawn,
        ItemDrop,
        Trigger,
        Custom
    }

    [System.Serializable]
    public class PlacedMapObject
    {
        public string instanceId;
        public MapObjectType objectType;
        public Vector2Int gridPosition;
        public bool freePlace;
        public Vector3 worldPosition;
        public float yRotation;
        public string label;
        public string customData;

        public Vector3 GetWorldPosition(GridSettings settings)
        {
            return freePlace ? worldPosition : IsometricGrid.GridToWorld(gridPosition, settings);
        }
    }
}
