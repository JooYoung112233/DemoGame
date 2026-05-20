using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    [Overlay(typeof(SceneView), "Isometric Map Tools", true)]
    public class MapSceneOverlay : ToolbarOverlay
    {
        MapSceneOverlay() : base(
            WalkabilityToggle.Id,
            RoadToggle.Id,
            PropToggle.Id,
            ConnectionToggle.Id,
            ValidateButton.Id
        ) { }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    class WalkabilityToggle : EditorToolbarToggle
    {
        public const string Id = "IsometricMap/Walkability";

        WalkabilityToggle()
        {
            text = "Walk";
            tooltip = "Walkability Paint Tool";
            this.RegisterValueChangedCallback(evt =>
            {
                WalkabilityPaintTool.SetActive(evt.newValue);
                if (evt.newValue)
                {
                    RoadTool.SetActive(false);
                    PropPlaceTool.SetActive(false);
                    ConnectionTool.SetActive(false);
                }
            });
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    class RoadToggle : EditorToolbarToggle
    {
        public const string Id = "IsometricMap/Road";

        RoadToggle()
        {
            text = "Road";
            tooltip = "Road Paint Tool";
            this.RegisterValueChangedCallback(evt =>
            {
                RoadTool.SetActive(evt.newValue);
                if (evt.newValue)
                {
                    WalkabilityPaintTool.SetActive(false);
                    PropPlaceTool.SetActive(false);
                    ConnectionTool.SetActive(false);
                }
            });
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    class PropToggle : EditorToolbarToggle
    {
        public const string Id = "IsometricMap/Prop";

        PropToggle()
        {
            text = "Prop";
            tooltip = "Prop Place Tool";
            this.RegisterValueChangedCallback(evt =>
            {
                PropPlaceTool.SetActive(evt.newValue);
                if (evt.newValue)
                {
                    WalkabilityPaintTool.SetActive(false);
                    RoadTool.SetActive(false);
                    ConnectionTool.SetActive(false);
                }
            });
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    class ConnectionToggle : EditorToolbarToggle
    {
        public const string Id = "IsometricMap/Connection";

        ConnectionToggle()
        {
            text = "Connect";
            tooltip = "Connection Tool";
            this.RegisterValueChangedCallback(evt =>
            {
                ConnectionTool.SetActive(evt.newValue);
                ConnectionGizmoDrawer.SetVisible(evt.newValue);
                if (evt.newValue)
                {
                    WalkabilityPaintTool.SetActive(false);
                    RoadTool.SetActive(false);
                    PropPlaceTool.SetActive(false);
                }
            });
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    class ValidateButton : EditorToolbarButton
    {
        public const string Id = "IsometricMap/Validate";

        ValidateButton()
        {
            text = "Validate";
            tooltip = "Validate current map";
            clicked += OnClick;
        }

        void OnClick()
        {
            var map = MapEditorWindow.ActiveMap;
            if (map == null)
            {
                Debug.LogWarning("No active map to validate.");
                return;
            }

            var result = MapValidator.Validate(map);
            MapValidator.LogResults(result, map.mapName);

            if (result.IsValid)
                EditorUtility.DisplayDialog("Validation", "Map is valid!", "OK");
            else
                EditorUtility.DisplayDialog("Validation",
                    $"Found {result.errors.Count} errors and {result.warnings.Count} warnings. Check console.",
                    "OK");
        }
    }
}
