using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public class FlashlightSceneSetup : EditorWindow
{
    // 아이소메트릭 타일 크기 (2:1 비율)
    const float TILE_W = 1.0f;
    const float TILE_H = 0.5f;

    [MenuItem("Tools/Setup Flashlight Prototype Scene")]
    static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog("Setup Scene",
            "쿼터뷰 손전등 프로토타입 씬을 생성합니다. 계속?",
            "Yes", "Cancel"))
            return;

        // --- Global Light (ambient) ---
        var globalLightGO = new GameObject("Global Light 2D");
        var globalLight = globalLightGO.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.intensity = 0.08f; // 밤 상태로 시작
        globalLight.color = new Color(0.08f, 0.08f, 0.18f);
        Undo.RegisterCreatedObjectUndo(globalLightGO, "Create Global Light");

        // --- Player ---
        var playerGO = new GameObject("Player");
        playerGO.transform.position = GridToWorld(5, 2);
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

        // --- Flashlight Pivot ---
        var flashPivot = new GameObject("FlashlightPivot");
        flashPivot.transform.SetParent(playerGO.transform);
        flashPivot.transform.localPosition = Vector3.zero;

        // Cone Light (손전등)
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

        // Ambient glow (기본 시야)
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

        // Wire components
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
        Undo.RegisterCreatedObjectUndo(dnGO, "Create DayNightCycle");

        // --- Fog of War ---
        var fogGO = new GameObject("FogOfWar");
        var fogSystem = fogGO.AddComponent<FogOfWarSystem>();
        var fogSO = new SerializedObject(fogSystem);
        fogSO.FindProperty("player").objectReferenceValue = playerGO.transform;
        fogSO.FindProperty("flashlight").objectReferenceValue = flashCtrl;
        fogSO.FindProperty("dayNight").objectReferenceValue = dnCycle;
        fogSO.ApplyModifiedProperties();
        Undo.RegisterCreatedObjectUndo(fogGO, "Create FogOfWar");

        // --- HUD ---
        var hudGO = new GameObject("PrototypeHUD");
        var hud = hudGO.AddComponent<PrototypeHUD>();
        var hudSO = new SerializedObject(hud);
        hudSO.FindProperty("dayNight").objectReferenceValue = dnCycle;
        hudSO.FindProperty("flashlight").objectReferenceValue = flashCtrl;
        hudSO.ApplyModifiedProperties();

        // --- 쿼터뷰 맵 생성 ---
        CreateIsometricMap();

        Debug.Log("[Flashlight Prototype] 쿼터뷰 씬 생성 완료! Play 누르면 테스트.");
        EditorUtility.DisplayDialog("Done",
            "쿼터뷰 씬 생성 완료! (밤 상태로 시작)\n\n" +
            "Controls:\n" +
            "- WASD: 이동\n" +
            "- Mouse: 손전등 방향\n" +
            "- F: 손전등 ON/OFF\n" +
            "- T: 낮/밤 전환\n" +
            "- Ctrl: 웅크리기", "OK");
    }

    static void CreateIsometricMap()
    {
        var envParent = new GameObject("Environment");
        Undo.RegisterCreatedObjectUndo(envParent, "Create Environment");

        // ============ 길거리 (도로) ============
        var streetParent = new GameObject("Street");
        streetParent.transform.SetParent(envParent.transform);
        CreateStreet(streetParent);

        // ============ 편의점 건물 ============
        var storeParent = new GameObject("ConvenienceStore");
        storeParent.transform.SetParent(envParent.transform);
        CreateConvenienceStore(storeParent);

        // ============ 주변 건물들 (어둠 속 실루엣) ============
        var buildingsParent = new GameObject("Buildings");
        buildingsParent.transform.SetParent(envParent.transform);
        CreateSurroundingBuildings(buildingsParent);

        // ============ 가로등 (포인트 라이트) ============
        var lightsParent = new GameObject("StreetLights");
        lightsParent.transform.SetParent(envParent.transform);
        CreateStreetLights(lightsParent);
    }

    // ====== 길거리 (아스팔트 + 인도) ======
    static void CreateStreet(GameObject parent)
    {
        Sprite roadTile = CreateDiamondSprite(64, new Color(0.15f, 0.15f, 0.17f), true);
        Sprite sidewalkTile = CreateDiamondSprite(64, new Color(0.25f, 0.24f, 0.22f), true);

        // 도로 (가로 방향, 맵 하단)
        for (int gx = -8; gx <= 20; gx++)
        {
            for (int gy = -8; gy <= -4; gy++)
            {
                bool isSidewalk = (gy == -4 || gy == -8);
                var tile = CreateTileObject(
                    parent, $"Road_{gx}_{gy}", gx, gy,
                    isSidewalk ? sidewalkTile : roadTile,
                    -5
                );
                if (!isSidewalk)
                {
                    var sr = tile.GetComponent<SpriteRenderer>();
                    sr.color = new Color(
                        0.13f + Random.value * 0.04f,
                        0.13f + Random.value * 0.04f,
                        0.15f + Random.value * 0.04f
                    );
                }
            }
        }

        // 도로 위 오브젝트: 차량, 쓰레기통, 우편함
        CreateIsoCube(parent, "Car1", 3, -6, 2.0f, 1.0f, new Color(0.25f, 0.12f, 0.1f), 20);
        CreateIsoCube(parent, "Car2", 12, -7, 2.0f, 1.0f, new Color(0.1f, 0.15f, 0.25f), 20);
        CreateIsoCube(parent, "Trashcan1", 0, -4, 0.4f, 0.5f, new Color(0.2f, 0.22f, 0.2f), 25, false);
        CreateIsoCube(parent, "Trashcan2", 8, -4, 0.4f, 0.5f, new Color(0.2f, 0.22f, 0.2f), 25, false);
        CreateIsoCube(parent, "Mailbox", 5, -4, 0.35f, 0.6f, new Color(0.15f, 0.15f, 0.4f), 25, false);
    }

    // ====== 편의점 내부 ======
    static void CreateConvenienceStore(GameObject parent)
    {
        Sprite floorTile = CreateDiamondSprite(64, new Color(0.28f, 0.26f, 0.22f), true);
        Color wallColor = new Color(0.35f, 0.3f, 0.25f);

        // 편의점 바닥 (10x8 그리드)
        var floorParent = new GameObject("Floor");
        floorParent.transform.SetParent(parent.transform);
        for (int gx = 0; gx <= 10; gx++)
        {
            for (int gy = 0; gy <= 8; gy++)
            {
                bool isCheckered = (gx + gy) % 2 == 0;
                var tile = CreateTileObject(
                    floorParent, $"StoreFloor_{gx}_{gy}", gx, gy,
                    floorTile, -3
                );
                var sr = tile.GetComponent<SpriteRenderer>();
                sr.color = isCheckered
                    ? new Color(0.30f, 0.28f, 0.24f)
                    : new Color(0.26f, 0.24f, 0.20f);
            }
        }

        // 벽 (아이소 큐브)
        var wallParent = new GameObject("Walls");
        wallParent.transform.SetParent(parent.transform);

        // 뒷벽 (위쪽)
        for (int gx = 0; gx <= 10; gx++)
            CreateIsoWall(wallParent, $"WallBack_{gx}", gx, 8, wallColor, 30);

        // 좌벽
        for (int gy = 0; gy <= 8; gy++)
            CreateIsoWall(wallParent, $"WallLeft_{gy}", 0, gy, wallColor, 30);

        // 우벽
        for (int gy = 0; gy <= 8; gy++)
            CreateIsoWall(wallParent, $"WallRight_{gy}", 10, gy, wallColor, 30);

        // 앞벽 (출입구 빈칸 포함)
        for (int gx = 0; gx <= 10; gx++)
        {
            if (gx >= 4 && gx <= 6) continue; // 출입구
            CreateIsoWall(wallParent, $"WallFront_{gx}", gx, 0, wallColor, 30);
        }

        // ---- 내부 가구 ----
        var furnitureParent = new GameObject("Furniture");
        furnitureParent.transform.SetParent(parent.transform);

        // ① 선반 (상품 진열) - 세로 2줄 (통로 넓게)
        for (int gy = 3; gy <= 6; gy++)
        {
            CreateIsoCube(furnitureParent, $"Shelf1_{gy}", 3, gy, 0.7f, 0.7f,
                new Color(0.4f, 0.32f, 0.2f), 35);
            CreateIsoCube(furnitureParent, $"Shelf2_{gy}", 7, gy, 0.7f, 0.7f,
                new Color(0.38f, 0.30f, 0.2f), 35);
        }

        // ② 계산대
        CreateIsoCube(furnitureParent, "Counter", 3, 1, 1.5f, 0.6f,
            new Color(0.35f, 0.28f, 0.18f), 40);

        // ③ 냉장고 (뒷벽)
        CreateIsoCube(furnitureParent, "Fridge1", 1, 7, 0.7f, 0.9f,
            new Color(0.5f, 0.55f, 0.6f), 35);
        CreateIsoCube(furnitureParent, "Fridge2", 3, 7, 0.7f, 0.9f,
            new Color(0.5f, 0.55f, 0.6f), 35);

        // 냉장고 안 푸른 빛
        CreatePointLight(furnitureParent, "FridgeLight", 2, 7,
            new Color(0.4f, 0.6f, 1f), 0.8f, 2f);

        // ④ 진열대 (라면/통조림)
        CreateIsoCube(furnitureParent, "DisplayRack", 7, 7, 1.2f, 0.7f,
            new Color(0.3f, 0.25f, 0.2f), 35);

        // ⑤ 뒷문 (잠김) 표시
        CreateIsoCube(furnitureParent, "BackDoor", 9, 8, 0.6f, 0.9f,
            new Color(0.28f, 0.22f, 0.15f), 32);

        // ⑥ 창고 문
        CreateIsoCube(furnitureParent, "StorageGate", 10, 5, 0.5f, 0.8f,
            new Color(0.3f, 0.28f, 0.22f), 32);

        // 편의점 간판 빛 (입구 위)
        CreatePointLight(parent, "SignLight", 5, 0,
            new Color(1f, 0.3f, 0.2f), 0.6f, 3f);
    }

    // ====== 주변 건물 실루엣 ======
    static void CreateSurroundingBuildings(GameObject parent)
    {
        Color darkBuilding = new Color(0.1f, 0.1f, 0.12f);

        // 좌측 건물 (시각용, 콜라이더 없음)
        for (int gy = 0; gy <= 10; gy++)
            for (int gx = -4; gx <= -1; gx++)
                CreateIsoWall(parent, $"BldgLeft_{gx}_{gy}", gx, gy, darkBuilding, 15, false);

        // 우측 건물 (시각용, 콜라이더 없음)
        for (int gy = 0; gy <= 10; gy++)
            for (int gx = 12; gx <= 15; gx++)
                CreateIsoWall(parent, $"BldgRight_{gx}_{gy}", gx, gy, darkBuilding, 15, false);

        // 맞은편 건물 (시각용, 콜라이더 없음)
        for (int gx = -4; gx <= 15; gx++)
            for (int gy = -12; gy <= -9; gy++)
                CreateIsoWall(parent, $"BldgFar_{gx}_{gy}", gx, gy,
                    darkBuilding * 0.8f, 10, false);
    }

    // ====== 가로등 ======
    static void CreateStreetLights(GameObject parent)
    {
        // 가로등 기둥 + 포인트 라이트
        int[] lampX = { -1, 6, 14 };
        foreach (int gx in lampX)
        {
            CreateIsoCube(parent, $"LampPost_{gx}", gx, -4, 0.15f, 0.8f,
                new Color(0.3f, 0.3f, 0.3f), 26);
            CreatePointLight(parent, $"LampLight_{gx}", gx, -4,
                new Color(1f, 0.85f, 0.5f), 0.7f, 4f);
        }
    }

    // ====== 유틸리티 ======

    static Vector3 GridToWorld(int gx, int gy)
    {
        float wx = (gx - gy) * TILE_W * 0.5f;
        float wy = (gx + gy) * TILE_H * 0.5f;
        return new Vector3(wx, wy, 0);
    }

    static int IsoSortOrder(int gx, int gy, int layerOffset)
    {
        return (gx + gy) + layerOffset;
    }

    static GameObject CreateTileObject(GameObject parent, string name, int gx, int gy,
        Sprite sprite, int sortBase)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = GridToWorld(gx, gy);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = IsoSortOrder(gx, gy, sortBase);
        return go;
    }

    static void CreateIsoWall(GameObject parent, string name, int gx, int gy,
        Color color, int sortBase, bool addCollider = true)
    {
        Vector3 pos = GridToWorld(gx, gy);
        float wallH = 0.4f;

        var wall = new GameObject(name);
        wall.transform.SetParent(parent.transform);
        wall.transform.position = pos + new Vector3(0, wallH * 0.5f, 0);
        var sr = wall.AddComponent<SpriteRenderer>();
        sr.sprite = CreateIsoCubeSprite(32, 20, color);
        sr.sortingOrder = IsoSortOrder(gx, gy, sortBase);

        if (addCollider)
        {
            var boxCol = wall.AddComponent<BoxCollider2D>();
            boxCol.size = new Vector2(TILE_W * 0.8f, TILE_H + wallH);
            boxCol.offset = new Vector2(0, -wallH * 0.25f);
        }
    }

    static void CreateIsoCube(GameObject parent, string name, int gx, int gy,
        float scaleW, float scaleH, Color color, int sortBase, bool addCollider = true)
    {
        Vector3 pos = GridToWorld(gx, gy);

        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = pos + new Vector3(0, scaleH * 0.2f, 0);
        var sr = go.AddComponent<SpriteRenderer>();

        int sprW = Mathf.Max(16, (int)(32 * scaleW));
        int sprH = Mathf.Max(16, (int)(32 * scaleH));
        sr.sprite = CreateIsoCubeSprite(sprW, sprH, color);
        sr.sortingOrder = IsoSortOrder(gx, gy, sortBase);

        if (addCollider)
        {
            var boxCol = go.AddComponent<BoxCollider2D>();
            boxCol.size = new Vector2(TILE_W * scaleW * 0.6f, TILE_H * scaleH + 0.2f);
        }
    }

    static void CreatePointLight(GameObject parent, string name, int gx, int gy,
        Color color, float intensity, float radius)
    {
        Vector3 pos = GridToWorld(gx, gy);
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = pos + new Vector3(0, 0.3f, 0);
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.pointLightOuterAngle = 360f;
        light.pointLightInnerAngle = 360f;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.3f;
        light.intensity = intensity;
        light.color = color;
        light.falloffIntensity = 0.5f;
    }

    // 다이아몬드 타일 스프라이트
    static Sprite CreateDiamondSprite(int size, Color color, bool withBorder)
    {
        var tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - half + 0.5f) / half;
                float dy = Mathf.Abs(y - half + 0.5f) / half;
                float d = dx + dy;

                if (d <= 1.0f)
                {
                    if (withBorder && d > 0.9f)
                        tex.SetPixel(x, y, color * 0.5f);
                    else
                        tex.SetPixel(x, y, color);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size / TILE_W);
    }

    // 아이소 큐브 스프라이트 (상단면 + 좌/우 측면)
    static Sprite CreateIsoCubeSprite(int w, int h, Color color)
    {
        int texW = w;
        int texH = h + w / 2;
        var tex = new Texture2D(texW, texH);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < texH; y++)
            for (int x = 0; x < texW; x++)
                tex.SetPixel(x, y, Color.clear);

        float halfW = texW * 0.5f;
        float topY = h;

        // 상단면 (다이아몬드)
        for (int y = topY > 0 ? (int)topY : 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                float dy = y - topY;
                float halfDiaH = (texH - topY) * 0.5f;
                float dx = Mathf.Abs(x - halfW + 0.5f);
                float ratio = dy < halfDiaH
                    ? dx / (halfW * (dy / halfDiaH + 0.01f))
                    : dx / (halfW * ((texH - 1 - y) / halfDiaH + 0.01f));

                if (ratio <= 1.0f)
                    tex.SetPixel(x, y, color * 1.2f);
            }
        }

        // 좌측면
        Color leftColor = color * 0.7f;
        leftColor.a = 1f;
        for (int y = 0; y < (int)topY && y < texH; y++)
        {
            int startX = 0;
            int endX = (int)halfW;
            float t = (float)y / topY;
            for (int x = startX; x < endX; x++)
            {
                float edgeX = halfW * (1f - t);
                if (x >= edgeX)
                    tex.SetPixel(x, y, leftColor);
            }
        }

        // 우측면
        Color rightColor = color * 0.5f;
        rightColor.a = 1f;
        for (int y = 0; y < (int)topY && y < texH; y++)
        {
            int startX = (int)halfW;
            int endX = texW;
            float t = (float)y / topY;
            for (int x = startX; x < endX; x++)
            {
                float edgeX = halfW + halfW * t;
                if (x <= edgeX)
                    tex.SetPixel(x, y, rightColor);
            }
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

        // 몸통 (어두운 녹색)
        Color body = new Color(0.2f, 0.35f, 0.2f);
        Color head = new Color(0.7f, 0.6f, 0.5f);
        for (int y = 0; y < 14; y++)
            for (int x = 3; x < 13; x++)
                tex.SetPixel(x, y, body);

        // 머리
        for (int y = 14; y < 22; y++)
            for (int x = 4; x < 12; x++)
                tex.SetPixel(x, y, head);

        // 헬멧
        Color helmet = new Color(0.25f, 0.3f, 0.2f);
        for (int y = 20; y < 24; y++)
            for (int x = 3; x < 13; x++)
                tex.SetPixel(x, y, helmet);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.2f), 24);
    }
}
