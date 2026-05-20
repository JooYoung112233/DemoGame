using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public class FlashlightSceneSetup : EditorWindow
{
    const float TILE_W = 1.0f;
    const float TILE_H = 0.5f;

    [MenuItem("Tools/Setup Flashlight Prototype Scene")]
    static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog("Setup Scene",
            "쿼터뷰 손전등 프로토타입 씬을 생성합니다. 계속?",
            "Yes", "Cancel"))
            return;

        // --- Global Light ---
        var globalLightGO = new GameObject("Global Light 2D");
        var globalLight = globalLightGO.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.intensity = 0.08f;
        globalLight.color = new Color(0.08f, 0.08f, 0.18f);
        Undo.RegisterCreatedObjectUndo(globalLightGO, "Create Global Light");

        // --- Player ---
        var playerGO = new GameObject("Player");
        playerGO.transform.position = GridToWorld(6, 4);
        var playerSR = playerGO.AddComponent<SpriteRenderer>();
        playerSR.sprite = CreateCharacterSprite();
        playerSR.sortingOrder = 50;
        var rb = playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = playerGO.AddComponent<CircleCollider2D>();
        col.radius = 0.15f;
        var playerCtrl = playerGO.AddComponent<PlayerController>();
        Undo.RegisterCreatedObjectUndo(playerGO, "Create Player");

        // --- Flashlight ---
        var flashPivot = new GameObject("FlashlightPivot");
        flashPivot.transform.SetParent(playerGO.transform);
        flashPivot.transform.localPosition = Vector3.zero;

        var coneLightGO = new GameObject("ConeLight");
        coneLightGO.transform.SetParent(flashPivot.transform);
        coneLightGO.transform.localPosition = Vector3.zero;
        var coneLight = coneLightGO.AddComponent<Light2D>();
        coneLight.lightType = Light2D.LightType.Point;
        coneLight.pointLightOuterAngle = 90f;
        coneLight.pointLightInnerAngle = 55f;
        coneLight.pointLightOuterRadius = 8f;
        coneLight.pointLightInnerRadius = 0.5f;
        coneLight.intensity = 2f;
        coneLight.color = new Color(1f, 0.93f, 0.75f);
        coneLight.falloffIntensity = 0.6f;

        var glowGO = new GameObject("AmbientGlow");
        glowGO.transform.SetParent(playerGO.transform);
        glowGO.transform.localPosition = Vector3.zero;
        var glow = glowGO.AddComponent<Light2D>();
        glow.lightType = Light2D.LightType.Point;
        glow.pointLightOuterAngle = 360f;
        glow.pointLightInnerAngle = 360f;
        glow.pointLightOuterRadius = 2.5f;
        glow.pointLightInnerRadius = 0.8f;
        glow.intensity = 0.5f;
        glow.color = new Color(0.7f, 0.75f, 0.9f);

        var flashCtrl = playerGO.AddComponent<FlashlightController>();
        var so = new SerializedObject(flashCtrl);
        so.FindProperty("coneLight").objectReferenceValue = coneLight;
        so.FindProperty("ambientGlow").objectReferenceValue = glow;
        so.ApplyModifiedProperties();

        var playerSO = new SerializedObject(playerCtrl);
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
        cam.orthographicSize = 7;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
        cam.transform.position = new Vector3(0, 0, -10);
        var camFollow = cam.gameObject.AddComponent<CameraFollow>();
        var camSO = new SerializedObject(camFollow);
        camSO.FindProperty("target").objectReferenceValue = playerGO.transform;
        camSO.ApplyModifiedProperties();

        // --- DayNightCycle ---
        var dnGO = new GameObject("DayNightCycle");
        var dnCycle = dnGO.AddComponent<DayNightCycle>();
        var dnSO = new SerializedObject(dnCycle);
        dnSO.FindProperty("globalLight").objectReferenceValue = globalLight;
        dnSO.FindProperty("flashlight").objectReferenceValue = flashCtrl;
        dnSO.ApplyModifiedProperties();

        // --- FogOfWar ---
        var fogGO = new GameObject("FogOfWar");
        var fogSystem = fogGO.AddComponent<FogOfWarSystem>();
        var fogSO = new SerializedObject(fogSystem);
        fogSO.FindProperty("player").objectReferenceValue = playerGO.transform;
        fogSO.FindProperty("flashlight").objectReferenceValue = flashCtrl;
        fogSO.FindProperty("dayNight").objectReferenceValue = dnCycle;
        fogSO.ApplyModifiedProperties();

        // --- HUD ---
        var hudGO = new GameObject("PrototypeHUD");
        var hud = hudGO.AddComponent<PrototypeHUD>();
        var hudSO = new SerializedObject(hud);
        hudSO.FindProperty("dayNight").objectReferenceValue = dnCycle;
        hudSO.FindProperty("flashlight").objectReferenceValue = flashCtrl;
        hudSO.ApplyModifiedProperties();

        // --- 건물 (바닥 + 벽) ---
        CreateBuilding();

        Debug.Log("[Flashlight Prototype] 씬 생성 완료!");
    }

    static void CreateBuilding()
    {
        var building = new GameObject("Building");
        Undo.RegisterCreatedObjectUndo(building, "Create Building");

        int w = 12, h = 10;
        Sprite floorTile = CreateDiamondSprite(64, new Color(0.28f, 0.26f, 0.22f));

        // ---- 바닥 ----
        var floorParent = new GameObject("Floor");
        floorParent.transform.SetParent(building.transform);
        for (int gx = 1; gx < w; gx++)
        {
            for (int gy = 1; gy < h; gy++)
            {
                var go = new GameObject($"F_{gx}_{gy}");
                go.transform.SetParent(floorParent.transform);
                go.transform.position = GridToWorld(gx, gy);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = floorTile;
                sr.sortingOrder = gx + gy - 10;
                sr.color = (gx + gy) % 2 == 0
                    ? new Color(0.30f, 0.28f, 0.24f)
                    : new Color(0.25f, 0.23f, 0.19f);
            }
        }

        // ---- 벽 ----
        var wallParent = new GameObject("Walls");
        wallParent.transform.SetParent(building.transform);
        Color wallColor = new Color(0.35f, 0.30f, 0.25f);

        // 뒷벽 (위)
        for (int gx = 0; gx <= w; gx++)
            CreateWall(wallParent, gx, h, wallColor);

        // 좌벽
        for (int gy = 0; gy <= h; gy++)
            CreateWall(wallParent, 0, gy, wallColor);

        // 우벽
        for (int gy = 0; gy <= h; gy++)
            CreateWall(wallParent, w, gy, wallColor);

        // 앞벽 (출입구 gx 5~7 비움)
        for (int gx = 0; gx <= w; gx++)
        {
            if (gx >= 5 && gx <= 7) continue;
            CreateWall(wallParent, gx, 0, wallColor);
        }
    }

    // ====== 벽 1칸 ======
    static void CreateWall(GameObject parent, int gx, int gy, Color color)
    {
        Vector3 pos = GridToWorld(gx, gy);
        float wallH = 0.5f;

        var go = new GameObject($"W_{gx}_{gy}");
        go.transform.SetParent(parent.transform);
        go.transform.position = pos + new Vector3(0, wallH * 0.5f, 0);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateWallSprite(32, 24, color);
        sr.sortingOrder = (gx + gy) + 30;

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(TILE_W * 0.7f, TILE_H + wallH);
        box.offset = new Vector2(0, -wallH * 0.2f);
    }

    // ====== 좌표 변환 ======
    static Vector3 GridToWorld(int gx, int gy)
    {
        float wx = (gx - gy) * TILE_W * 0.5f;
        float wy = (gx + gy) * TILE_H * 0.5f;
        return new Vector3(wx, wy, 0);
    }

    // ====== 스프라이트 생성 ======

    static Sprite CreateDiamondSprite(int size, Color color)
    {
        var tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - half + 0.5f) / half;
                float dy = Mathf.Abs(y - half + 0.5f) / half;
                float d = dx + dy;
                if (d <= 1.0f)
                    tex.SetPixel(x, y, d > 0.92f ? color * 0.6f : color);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size / TILE_W);
    }

    static Sprite CreateWallSprite(int w, int h, Color color)
    {
        int texW = w;
        int texH = h + w / 2;
        var tex = new Texture2D(texW, texH);
        tex.filterMode = FilterMode.Point;

        Color top = color * 1.1f; top.a = 1;
        Color left = color * 0.7f; left.a = 1;
        Color right = color * 0.5f; right.a = 1;

        for (int y = 0; y < texH; y++)
            for (int x = 0; x < texW; x++)
                tex.SetPixel(x, y, Color.clear);

        float halfW = texW * 0.5f;

        // 상단 다이아몬드면
        int diaH = texW / 2;
        int diaStart = h;
        for (int dy = 0; dy < diaH; dy++)
        {
            int py = diaStart + dy;
            if (py >= texH) break;
            float ratio = (dy < diaH / 2)
                ? (float)(dy + 1) / (diaH / 2)
                : (float)(diaH - dy) / (diaH / 2);
            int span = Mathf.Max(1, (int)(halfW * ratio));
            int cx = texW / 2;
            for (int x = cx - span; x <= cx + span; x++)
                if (x >= 0 && x < texW) tex.SetPixel(x, py, top);
        }

        // 좌측면
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / h;
            int edgeX = (int)(halfW * (1f - t));
            for (int x = edgeX; x < (int)halfW; x++)
                if (x >= 0) tex.SetPixel(x, y, left);
        }

        // 우측면
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / h;
            int edgeX = (int)(halfW + halfW * t);
            for (int x = (int)halfW; x <= edgeX; x++)
                if (x < texW) tex.SetPixel(x, y, right);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.3f), texW / TILE_W);
    }

    static Sprite CreateCharacterSprite()
    {
        int w = 16, h = 24;
        var tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, Color.clear);

        Color body = new Color(0.2f, 0.35f, 0.2f);
        Color head = new Color(0.7f, 0.6f, 0.5f);
        Color helmet = new Color(0.25f, 0.3f, 0.2f);

        for (int y = 0; y < 14; y++)
            for (int x = 3; x < 13; x++)
                tex.SetPixel(x, y, body);
        for (int y = 14; y < 22; y++)
            for (int x = 4; x < 12; x++)
                tex.SetPixel(x, y, head);
        for (int y = 20; y < 24; y++)
            for (int x = 3; x < 13; x++)
                tex.SetPixel(x, y, helmet);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.2f), 24);
    }
}
