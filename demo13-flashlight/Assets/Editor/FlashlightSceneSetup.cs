using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using System.Reflection;

public class FlashlightSceneSetup : EditorWindow
{
    const float TILE_W = 1.0f;
    const float TILE_H = 0.5f;
    const int PPU = 64;

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
        globalLight.intensity = 0.06f;
        globalLight.color = new Color(0.06f, 0.06f, 0.15f);
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
        coneLight.shadowsEnabled = true;
        coneLight.shadowIntensity = 0.9f;

        var glowGO = new GameObject("AmbientGlow");
        glowGO.transform.SetParent(playerGO.transform);
        glowGO.transform.localPosition = Vector3.zero;
        var glow = glowGO.AddComponent<Light2D>();
        glow.lightType = Light2D.LightType.Point;
        glow.pointLightOuterAngle = 360f;
        glow.pointLightInnerAngle = 360f;
        glow.pointLightOuterRadius = 2.5f;
        glow.pointLightInnerRadius = 0.8f;
        glow.intensity = 0.4f;
        glow.color = new Color(0.7f, 0.75f, 0.9f);
        glow.shadowsEnabled = true;
        glow.shadowIntensity = 0.7f;

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
        cam.orthographicSize = 6;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f);
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

        // --- 건물 ---
        CreateBuilding();

        Debug.Log("[Flashlight Prototype] 씬 생성 완료!");
    }

    static void CreateBuilding()
    {
        var root = new GameObject("Building");
        Undo.RegisterCreatedObjectUndo(root, "Create Building");

        int bw = 12, bh = 10;

        // ===== 바깥 바닥 (아스팔트) =====
        var outsideParent = new GameObject("OutsideGround");
        outsideParent.transform.SetParent(root.transform);
        Sprite asphaltTile = CreateAsphaltTile();

        for (int gx = -6; gx <= bw + 6; gx++)
        {
            for (int gy = -6; gy <= bh + 6; gy++)
            {
                if (gx >= 1 && gx < bw && gy >= 1 && gy < bh) continue;
                var go = new GameObject($"R_{gx}_{gy}");
                go.transform.SetParent(outsideParent.transform);
                go.transform.position = GridToWorld(gx, gy);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = asphaltTile;
                sr.sortingOrder = gx + gy - 15;
            }
        }

        // ===== 건물 내부 바닥 (콘크리트) =====
        var floorParent = new GameObject("Floor");
        floorParent.transform.SetParent(root.transform);
        Sprite floorTile = CreateConcreteFloorTile();

        for (int gx = 1; gx < bw; gx++)
        {
            for (int gy = 1; gy < bh; gy++)
            {
                var go = new GameObject($"F_{gx}_{gy}");
                go.transform.SetParent(floorParent.transform);
                go.transform.position = GridToWorld(gx, gy);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = floorTile;
                sr.sortingOrder = gx + gy - 10;
            }
        }

        // ===== 벽 (그림자 캐스터 포함) =====
        var wallParent = new GameObject("Walls");
        wallParent.transform.SetParent(root.transform);
        Color wallBase = new Color(0.30f, 0.27f, 0.23f);

        // 뒷벽
        for (int gx = 0; gx <= bw; gx++)
            CreateWall(wallParent, gx, bh, wallBase);
        // 좌벽
        for (int gy = 0; gy <= bh; gy++)
            CreateWall(wallParent, 0, gy, wallBase);
        // 우벽
        for (int gy = 0; gy <= bh; gy++)
            CreateWall(wallParent, bw, gy, wallBase);
        // 앞벽 (출입구 5~7 비움)
        for (int gx = 0; gx <= bw; gx++)
        {
            if (gx >= 5 && gx <= 7) continue;
            CreateWall(wallParent, gx, 0, wallBase);
        }
    }

    static void CreateWall(GameObject parent, int gx, int gy, Color baseColor)
    {
        Vector3 pos = GridToWorld(gx, gy);

        var go = new GameObject($"W_{gx}_{gy}");
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;

        // 벽 스프라이트 (아이소 벽돌)
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateBrickWallSprite(baseColor);
        sr.sortingOrder = (gx + gy) + 30;

        // 콜라이더
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(TILE_W * 0.7f, TILE_H * 1.5f);
        box.offset = new Vector2(0, 0.1f);

        // ShadowCaster2D (빛 차단)
        var shadow = go.AddComponent<ShadowCaster2D>();
        shadow.selfShadows = false;

        // ShadowCaster2D 경로를 사각형으로 설정 (리플렉션 필요)
        SetShadowCasterShape(shadow, new Vector3[]
        {
            new(-0.35f, -0.15f, 0),
            new( 0.35f, -0.15f, 0),
            new( 0.35f,  0.45f, 0),
            new(-0.35f,  0.45f, 0),
        });
    }

    static void SetShadowCasterShape(ShadowCaster2D caster, Vector3[] path)
    {
        // ShadowCaster2D의 내부 필드에 접근 (리플렉션)
        var shapeField = typeof(ShadowCaster2D).GetField("m_ShapePath",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (shapeField != null)
            shapeField.SetValue(caster, path);

        var hashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (hashField != null)
            hashField.SetValue(caster, Random.Range(int.MinValue, int.MaxValue));
    }

    // ====== 좌표 변환 ======
    static Vector3 GridToWorld(int gx, int gy)
    {
        float wx = (gx - gy) * TILE_W * 0.5f;
        float wy = (gx + gy) * TILE_H * 0.5f;
        return new Vector3(wx, wy, 0);
    }

    // ================================================================
    //  스프라이트 생성: 아이소 타일 + 벽돌 벽
    // ================================================================

    // 아스팔트 바닥 타일 (어두운 도로)
    static Sprite CreateAsphaltTile()
    {
        int s = PPU;
        var tex = new Texture2D(s, s);
        tex.filterMode = FilterMode.Point;
        float half = s * 0.5f;

        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Abs(x - half + 0.5f) / half;
                float dy = Mathf.Abs(y - half + 0.5f) / half;
                if (dx + dy <= 1.0f)
                {
                    float noise = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.04f;
                    float v = 0.14f + noise;
                    bool line = (dx + dy > 0.94f);
                    tex.SetPixel(x, y, line
                        ? new Color(0.18f, 0.17f, 0.16f)
                        : new Color(v, v, v + 0.01f));
                }
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), Vector2.one * 0.5f, s / TILE_W);
    }

    // 콘크리트 실내 바닥 타일
    static Sprite CreateConcreteFloorTile()
    {
        int s = PPU;
        var tex = new Texture2D(s, s);
        tex.filterMode = FilterMode.Point;
        float half = s * 0.5f;

        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Abs(x - half + 0.5f) / half;
                float dy = Mathf.Abs(y - half + 0.5f) / half;
                if (dx + dy <= 1.0f)
                {
                    float noise = Mathf.PerlinNoise(x * 0.2f + 50, y * 0.2f + 50) * 0.06f;
                    float v = 0.24f + noise;
                    bool border = (dx + dy > 0.92f);
                    bool gridLine = (x % 16 == 0 || y % 16 == 0) && (dx + dy < 0.9f);
                    if (border)
                        tex.SetPixel(x, y, new Color(0.18f, 0.17f, 0.15f));
                    else if (gridLine)
                        tex.SetPixel(x, y, new Color(v - 0.03f, v - 0.03f, v - 0.02f));
                    else
                        tex.SetPixel(x, y, new Color(v, v - 0.01f, v - 0.02f));
                }
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), Vector2.one * 0.5f, s / TILE_W);
    }

    // 벽돌 벽 스프라이트 (아이소 큐브: 상단면 + 좌측면 + 우측면)
    static Sprite CreateBrickWallSprite(Color baseColor)
    {
        int w = PPU;
        int wallH = 40;
        int topH = w / 2;
        int texH = wallH + topH;
        var tex = new Texture2D(w, texH);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < texH; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, Color.clear);

        float halfW = w * 0.5f;

        // --- 좌측면 (벽돌 패턴) ---
        Color leftBase = baseColor * 0.6f; leftBase.a = 1;
        for (int y = 0; y < wallH; y++)
        {
            float t = (float)y / wallH;
            int edgeX = (int)(halfW * (1f - t));
            for (int x = edgeX; x < (int)halfW; x++)
            {
                // 벽돌 패턴
                int brickRow = y / 5;
                int offset = (brickRow % 2 == 0) ? 0 : 4;
                int brickCol = ((x - edgeX) + offset) % 8;
                bool mortar = (y % 5 == 0) || (brickCol == 0);

                float noise = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.05f;
                Color c = mortar
                    ? leftBase * 0.7f
                    : new Color(leftBase.r + noise, leftBase.g + noise * 0.8f, leftBase.b + noise * 0.5f);
                c.a = 1;
                tex.SetPixel(x, y, c);
            }
        }

        // --- 우측면 (벽돌 패턴, 더 어둡게) ---
        Color rightBase = baseColor * 0.4f; rightBase.a = 1;
        for (int y = 0; y < wallH; y++)
        {
            float t = (float)y / wallH;
            int edgeX = (int)(halfW + halfW * t);
            for (int x = (int)halfW; x <= edgeX; x++)
            {
                int brickRow = y / 5;
                int offset = (brickRow % 2 == 0) ? 0 : 4;
                int brickCol = ((x - (int)halfW) + offset) % 8;
                bool mortar = (y % 5 == 0) || (brickCol == 0);

                float noise = Mathf.PerlinNoise(x * 0.3f + 100, y * 0.3f) * 0.04f;
                Color c = mortar
                    ? rightBase * 0.7f
                    : new Color(rightBase.r + noise, rightBase.g + noise * 0.8f, rightBase.b + noise * 0.5f);
                c.a = 1;
                tex.SetPixel(x, y, c);
            }
        }

        // --- 상단면 (다이아몬드, 밝음) ---
        Color topColor = baseColor * 0.85f; topColor.a = 1;
        for (int dy = 0; dy < topH; dy++)
        {
            int py = wallH + dy;
            float halfDia = topH * 0.5f;
            float ratio = (dy < halfDia)
                ? (dy + 0.5f) / halfDia
                : (topH - dy - 0.5f) / halfDia;
            int span = Mathf.Max(0, (int)(halfW * ratio));
            int cx = w / 2;
            for (int x = cx - span; x <= cx + span; x++)
            {
                if (x < 0 || x >= w) continue;
                float noise = Mathf.PerlinNoise(x * 0.2f + 200, dy * 0.2f) * 0.04f;
                Color c = new Color(topColor.r + noise, topColor.g + noise, topColor.b + noise * 0.5f);
                c.a = 1;
                tex.SetPixel(x, py, c);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, texH), new Vector2(0.5f, 0.38f), PPU / TILE_W);
    }

    // 캐릭터 스프라이트
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
