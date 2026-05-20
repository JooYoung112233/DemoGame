using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    public static class MapValidator
    {
        public struct ValidationResult
        {
            public List<string> errors;
            public List<string> warnings;
            public bool IsValid => errors.Count == 0;
        }

        public static ValidationResult Validate(MapData map)
        {
            var result = new ValidationResult
            {
                errors = new List<string>(),
                warnings = new List<string>()
            };

            if (map == null)
            {
                result.errors.Add("MapData is null.");
                return result;
            }

            if (string.IsNullOrEmpty(map.mapId))
                result.errors.Add("Map has no ID.");

            if (string.IsNullOrEmpty(map.mapName))
                result.warnings.Add("Map has no name.");

            if (map.gridSettings.mapWidth <= 0 || map.gridSettings.mapHeight <= 0)
                result.errors.Add("Map dimensions must be positive.");

            if (map.gridSettings.tileSize <= 0)
                result.errors.Add("Tile size must be positive.");

            ValidateBuildings(map, result);
            ValidateConnections(map, result);
            ValidateProps(map, result);
            ValidateHarvestables(map, result);

            return result;
        }

        static void ValidateBuildings(MapData map, ValidationResult result)
        {
            var occupiedCells = new HashSet<Vector2Int>();

            foreach (var building in map.buildings)
            {
                if (building.buildingDefinition == null)
                {
                    result.errors.Add($"Building '{building.instanceId}' has no definition.");
                    continue;
                }

                var cells = building.buildingDefinition.GetOccupiedCells(building.gridPosition);
                foreach (var cell in cells)
                {
                    if (!map.gridSettings.IsInBounds(cell))
                    {
                        result.errors.Add($"Building '{building.buildingDefinition.displayName}' at {building.gridPosition} extends out of bounds.");
                        break;
                    }
                    if (!occupiedCells.Add(cell))
                    {
                        result.warnings.Add($"Building overlap at cell {cell}.");
                    }
                }

                if (building.buildingDefinition.isEnterable &&
                    string.IsNullOrEmpty(building.buildingDefinition.interiorMapId))
                {
                    result.warnings.Add($"Enterable building '{building.buildingDefinition.displayName}' has no interior map linked.");
                }
            }
        }

        static void ValidateConnections(MapData map, ValidationResult result)
        {
            foreach (var conn in map.interiorConnections)
            {
                if (string.IsNullOrEmpty(conn.fromMapId) || string.IsNullOrEmpty(conn.toMapId))
                    result.errors.Add($"Connection '{conn.connectionId}' has missing map IDs.");
            }
        }

        static void ValidateProps(MapData map, ValidationResult result)
        {
            foreach (var prop in map.props)
            {
                if (prop.propDefinition == null)
                    result.warnings.Add($"Prop '{prop.instanceId}' has no definition.");
                else if (!map.gridSettings.IsInBounds(prop.gridPosition))
                    result.errors.Add($"Prop at {prop.gridPosition} is out of bounds.");
            }
        }

        static void ValidateHarvestables(MapData map, ValidationResult result)
        {
            foreach (var h in map.harvestables)
            {
                if (h.harvestableDefinition == null)
                    result.warnings.Add($"Harvestable '{h.instanceId}' has no definition.");
                else if (!map.gridSettings.IsInBounds(h.gridPosition))
                    result.errors.Add($"Harvestable at {h.gridPosition} is out of bounds.");
            }
        }

        public static void LogResults(ValidationResult result, string mapName)
        {
            if (result.IsValid && result.warnings.Count == 0)
            {
                Debug.Log($"[MapValidator] '{mapName}' passed validation.");
                return;
            }

            foreach (var error in result.errors)
                Debug.LogError($"[MapValidator] {mapName}: {error}");
            foreach (var warning in result.warnings)
                Debug.LogWarning($"[MapValidator] {mapName}: {warning}");
        }
    }
}
