using UnityEngine;

namespace IsometricMapEditor
{
    public class IsometricSortingManager : MonoBehaviour
    {
        public void AssignSortingOrders(MapData mapData)
        {
            foreach (var layer in mapData.layers)
            {
                foreach (var tile in layer.tiles)
                {
                    if (tile.tileDefinition == null) continue;

                    string goName = $"Tile_{tile.gridPosition.x}_{tile.gridPosition.y}";
                    var tileGO = transform.Find(goName);
                    if (tileGO == null) continue;

                    var sr = tileGO.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = IsometricGrid.GetSortingOrder(
                            tile.gridPosition,
                            layer.sortingLayerOffset
                        ) + tile.tileDefinition.sortingOffset;
                    }
                }
            }
        }
    }
}
