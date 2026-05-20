using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public class FlashlightSceneSetup : EditorWindow
{
    [MenuItem("Tools/Setup Flashlight Prototype Scene")]
    static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog("Setup Scene",
            "This will create the flashlight prototype scene with all necessary objects. Continue?",
            "Yes", "Cancel"))
            return;

        // --- Global Light (ambient) ---
        var globalLightGO = new GameObject("Global Light 2D");
        var globalLight = globalLightGO.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.intensity = 1f;
        globalLight.color = Color.white;
        Undo.RegisterCreatedObjectUndo(globalLightGO, "Create Global Light");

        // --- Player ---
        var playerGO = new GameObject("Player");
        playerGO.transform.position = Vector3.zero;
        var playerSR = playerGO.AddComponent<SpriteRenderer>();
        playerSR.sprite = CreateCircleSprite(16, new Color(0.3f, 0.8f, 0.4f));
        playerSR.sortingOrder = 10;
        var rb = playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        playerGO.AddComponent<CircleCollider2D>();
        var playerCtrl = playerGO.AddComponent<PlayerController>();
        Undo.RegisterCreatedObjectUndo(playerGO, "Create Player");

        // --- Flashlight Pivot (child of Player) ---
        var flashPivot = new GameObject("FlashlightPivot");
        flashPivot.transform.SetParent(playerGO.transform);
        flashPivot.transform.localPosition = Vector3.zero;

        // Cone Light
        var coneLightGO = new GameObject("ConeLight");
        coneLightGO.transform.SetParent(flashPivot.transform);
        coneLightGO.transform.localPosition = Vector3.zero;
        var coneLight = coneLightGO.AddComponent<Light2D>();
        coneLight.lightType = Light2D.LightType.Point;
        coneLight.pointLightOuterAngle = 90f;
        coneLight.pointLightInnerAngle = 60f;
        coneLight.pointLightOuterRadius = 7f;
        coneLight.pointLightInnerRadius = 0f;
        coneLight.intensity = 1.5f;
        coneLight.color = new Color(1f, 0.95f, 0.8f);
        coneLight.enabled = true;

        // Ambient glow (always-on small circle around player)
        var glowGO = new GameObject("AmbientGlow");
        glowGO.transform.SetParent(playerGO.transform);
        glowGO.transform.localPosition = Vector3.zero;
        var glow = glowGO.AddComponent<Light2D>();
        glow.lightType = Light2D.LightType.Point;
        glow.pointLightOuterAngle = 360f;
        glow.pointLightInnerAngle = 360f;
        glow.pointLightOuterRadius = 2f;
        glow.pointLightInnerRadius = 0.5f;
        glow.intensity = 0.6f;
        glow.color = new Color(0.8f, 0.85f, 1f);

        // Wire FlashlightController
        var flashCtrl = playerGO.AddComponent<FlashlightController>();
        SerializedObject so = new SerializedObject(flashCtrl);
        so.FindProperty("coneLight").objectReferenceValue = coneLight;
        so.FindProperty("ambientGlow").objectReferenceValue = glow;
        so.ApplyModifiedProperties();

        // Wire PlayerController.flashlightPivot
        SerializedObject playerSO = new SerializedObject(playerCtrl);
        playerSO.FindProperty("flashlightPivot").objectReferenceValue = flashPivot.transform;
        playerSO.ApplyModifiedProperties();

        // --- Camera ---
        var cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 10;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        cam.transform.position = new Vector3(0, 0, -10);
        var camFollow = cam.gameObject.AddComponent<CameraFollow>();
        SerializedObject camSO = new SerializedObject(camFollow);
        camSO.FindProperty("target").objectReferenceValue = playerGO.transform;
        camSO.ApplyModifiedProperties();

        // --- DayNightCycle ---
        var dnGO = new GameObject("DayNightCycle");
        var dnCycle = dnGO.AddComponent<DayNightCycle>();
        SerializedObject dnSO = new SerializedObject(dnCycle);
        dnSO.FindProperty("globalLight").objectReferenceValue = globalLight;
        dnSO.FindProperty("flashlight").objectReferenceValue = flashCtrl;
        dnSO.ApplyModifiedProperties();
        Undo.RegisterCreatedObjectUndo(dnGO, "Create DayNightCycle");

        // --- Fog of War ---
        var fogGO = new GameObject("FogOfWar");
        var fogSystem = fogGO.AddComponent<FogOfWarSystem>();
        SerializedObject fogSO = new SerializedObject(fogSystem);
        fogSO.FindProperty("player").objectReferenceValue = playerGO.transform;
        fogSO.FindProperty("flashlight").objectReferenceValue = flashCtrl;
        fogSO.FindProperty("dayNight").objectReferenceValue = dnCycle;
        fogSO.ApplyModifiedProperties();
        Undo.RegisterCreatedObjectUndo(fogGO, "Create FogOfWar");

        // --- HUD ---
        var hudGO = new GameObject("PrototypeHUD");
        var hud = hudGO.AddComponent<PrototypeHUD>();
        SerializedObject hudSO = new SerializedObject(hud);
        hudSO.FindProperty("dayNight").objectReferenceValue = dnCycle;
        hudSO.FindProperty("flashlight").objectReferenceValue = flashCtrl;
        hudSO.ApplyModifiedProperties();
        Undo.RegisterCreatedObjectUndo(hudGO, "Create HUD");

        // --- Sample Environment (walls and floor) ---
        CreateSampleEnvironment();

        Debug.Log("[Flashlight Prototype] Scene setup complete! Press Play to test.");
        EditorUtility.DisplayDialog("Done", "Scene setup complete!\n\nPress Play to test.\n\nControls:\n- WASD: Move\n- Mouse: Aim\n- F: Toggle flashlight\n- T: Toggle day/night\n- Ctrl: Crouch", "OK");
    }

    static void CreateSampleEnvironment()
    {
        var envParent = new GameObject("Environment");
        Undo.RegisterCreatedObjectUndo(envParent, "Create Environment");

        // Floor tiles (scattered)
        var floorParent = new GameObject("Floor");
        floorParent.transform.SetParent(envParent.transform);
        Sprite floorTile = CreateSquareSprite(32, new Color(0.25f, 0.25f, 0.3f));
        for (int x = -15; x <= 15; x++)
        {
            for (int y = -15; y <= 15; y++)
            {
                var tile = new GameObject($"Floor_{x}_{y}");
                tile.transform.SetParent(floorParent.transform);
                tile.transform.position = new Vector3(x, y, 0);
                var sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = floorTile;
                sr.sortingOrder = -10;
                sr.color = new Color(
                    0.2f + Random.value * 0.1f,
                    0.2f + Random.value * 0.1f,
                    0.25f + Random.value * 0.1f
                );
            }
        }

        // Walls (room outline)
        Sprite wallSprite = CreateSquareSprite(32, new Color(0.4f, 0.35f, 0.3f));
        var wallParent = new GameObject("Walls");
        wallParent.transform.SetParent(envParent.transform);

        // Outer walls
        int[][] wallPositions = new int[][]
        {
            // Outer boundary
            new[] { -10, -10, 20, 1 }, // bottom
            new[] { -10, 10, 20, 1 },  // top
            new[] { -10, -10, 1, 20 }, // left
            new[] { 10, -10, 1, 20 },  // right
            // Inner room dividers
            new[] { -5, -5, 1, 8 },
            new[] { 3, -2, 8, 1 },
            new[] { -2, 4, 6, 1 },
        };

        foreach (var wp in wallPositions)
        {
            for (int dx = 0; dx < wp[2]; dx++)
            {
                for (int dy = 0; dy < wp[3]; dy++)
                {
                    int wx = wp[0] + dx;
                    int wy = wp[1] + dy;

                    var wall = new GameObject($"Wall_{wx}_{wy}");
                    wall.transform.SetParent(wallParent.transform);
                    wall.transform.position = new Vector3(wx, wy, 0);
                    var sr = wall.AddComponent<SpriteRenderer>();
                    sr.sprite = wallSprite;
                    sr.sortingOrder = 5;
                    sr.color = new Color(
                        0.35f + Random.value * 0.1f,
                        0.3f + Random.value * 0.1f,
                        0.25f + Random.value * 0.1f
                    );
                    wall.AddComponent<BoxCollider2D>();
                }
            }
        }

        // Some "furniture" objects
        Sprite furnitureSprite = CreateSquareSprite(24, new Color(0.5f, 0.35f, 0.2f));
        Vector2[] furniturePositions = new Vector2[]
        {
            new(-3, -3), new(-3, -1), new(5, 5), new(7, -5),
            new(-7, 7), new(2, 2), new(-8, -4), new(6, 8),
        };
        var furnitureParent = new GameObject("Furniture");
        furnitureParent.transform.SetParent(envParent.transform);

        foreach (var fp in furniturePositions)
        {
            var furn = new GameObject($"Furniture_{fp.x}_{fp.y}");
            furn.transform.SetParent(furnitureParent.transform);
            furn.transform.position = new Vector3(fp.x, fp.y, 0);
            var sr = furn.AddComponent<SpriteRenderer>();
            sr.sprite = furnitureSprite;
            sr.sortingOrder = 5;
            furn.AddComponent<BoxCollider2D>();
        }
    }

    static Sprite CreateCircleSprite(int size, Color color)
    {
        var tex = new Texture2D(size, size);
        float center = size * 0.5f;
        float r = center - 1;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, dist <= r ? color : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }

    static Sprite CreateSquareSprite(int size, Color color)
    {
        var tex = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool border = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                tex.SetPixel(x, y, border ? color * 0.7f : color);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }
}
