#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 안전구역(마을) — **3D 쿼터뷰 버전**. 2D `SafehouseGreyboxLayout`의 3D 대응.
///
/// 좌표 규약(docs/3d-migration.md): 월드 = XZ 평면, 위 = +Y, 1u = 1m.
/// 2D판 좌표 (x, y)를 (x, 0, y)로 그대로 옮겨 배치가 1:1로 대응한다.
///
/// 야외라 천장 문제는 없지만 **건물에 높이가 생기면 플레이어를 가린다** →
/// 건물 재질에 `Spike/OccluderFX`(컷어웨이)를 물리고 `CutawayDriver`가 전역 유니폼을 갱신한다.
///
/// 규격·소품 정의: docs/safehouse-3d.md
/// 메뉴: Tools ▸ TopDown ▸ 빌드 ▸ 안전구역 (3D)
/// </summary>
public static class Safehouse3DLayout
{
    const string ScenePath = "Assets/Scenes/Safehouse3D.unity";

    const float MapW = 38f, MapD = 27f;
    const float WallH   = 3.5f;   // 외곽 벽
    const float FenceH  = 4.0f;   // 동쪽 방호벽(게이트)
    const float ShopH   = 5.0f;   // 잠금 상가·전당포
    const float ContH   = 2.9f;   // 컨테이너 외관

    static Material _matGround, _matWall, _matShop, _matFence, _matCont, _matShutter;
    static Shader _occluder;

    [MenuItem("Tools/TopDown/빌드/안전구역 (3D)", priority = -97)]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var map = new GameObject("Map");
        int n = 0;

        _occluder   = Shader.Find("Spike/OccluderFX");
        _matGround  = Lit(new Color(0.32f, 0.31f, 0.30f), 0.03f);   // 흙·아스팔트
        _matWall    = Occ(new Color(0.42f, 0.41f, 0.39f));
        _matShop    = Occ(new Color(0.46f, 0.44f, 0.42f));
        _matFence   = Occ(new Color(0.38f, 0.36f, 0.33f));
        _matCont    = Occ(new Color(0.36f, 0.44f, 0.40f));          // 골판 컨테이너(녹청)
        _matShutter = Lit(new Color(0.55f, 0.48f, 0.32f), 0.20f);

        // ── 바닥 ──
        n += Box(map, "Ground", new Vector3(MapW * .5f, -0.05f, MapD * .5f),
                 new Vector3(MapW, 0.1f, MapD), _matGround);

        // ── 외곽 벽 (남/북/서). 동쪽은 방호벽 + 게이트 개구부 ──
        n += Solid(map, "Wall_S", 19f,  0.5f, 38f, 1f,  WallH, _matWall);
        n += Solid(map, "Wall_N", 19f, 26.5f, 38f, 1f,  WallH, _matWall);
        n += Solid(map, "Wall_W",  0.5f,13.5f, 1f, 27f, WallH, _matWall);
        // 동벽: Y1~13 / Y16~26 두 토막, 사이 Y13~16이 폐도시로 나가는 게이트
        n += Solid(map, "Gate_Wall_S", 37.5f,  7f, 1f, 12f, FenceH, _matFence);
        n += Solid(map, "Gate_Wall_N", 37.5f, 21f, 1f, 10f, FenceH, _matFence);

        // ── 잠금 건물(솔리드) + 셔터 ──
        n += Solid(map, "Block_Repair",  5f, 23f,    8f, 6f, ShopH, _matShop);
        n += Solid(map, "Block_Med",    31f, 23f,   12f, 6f, ShopH, _matShop);
        n += Solid(map, "Block_Furn",    5f, 14.5f,  8f, 5f, ShopH, _matShop);
        n += Solid(map, "Block_Black",   5f,  5f,    8f, 8f, ShopH, _matShop);
        n += Solid(map, "Repair_Shutter", 5f, 20f, 3f, 0.3f, 2.4f, _matShutter);
        n += Solid(map, "Med_Shutter",   31f, 20f, 3f, 0.3f, 2.4f, _matShutter);
        n += Solid(map, "Furn_Shutter",   5f, 12f, 3f, 0.3f, 2.4f, _matShutter);
        n += Solid(map, "Black_Shutter",  5f,  9f, 3f, 0.3f, 2.4f, _matShutter);

        // ── 전당포(외관 + 입구) ──
        n += Solid(map, "Pawn_Building", 17f, 23.5f, 9f, 4.5f, ShopH, _matShop);
        n += Doorway(map, "Pawn_Entrance", 17f, 20.7f);
        n += Spawn(map, "from_pawnshop", 17f, 19.8f);

        // ── 중앙 광장 ──
        n += Spawn(map, "raid_return", 17f, 14.5f);

        // ── 게시판 + 회수꾼 + 게이트 ──
        n += Prop(map, "Board_Quest", 34f, 14.5f, new Vector3(2.4f, 2.0f, 0.3f), new Color(0.58f, 0.46f, 0.28f));
        n += Npc (map, "NPC_Veteran", 31f, 14.5f, new Color(0.52f, 0.46f, 0.40f));

        // ── 떠돌이 상인 ──
        n += Npc (map, "NPC_Merchant", 17f, 5f, new Color(0.55f, 0.40f, 0.45f));

        // ── 컨테이너 지역(집/은신처) ──
        n += Solid(map, "Container_Home", 32f, 5f, 8f, 5f, ContH, _matCont);
        n += Doorway(map, "Hideout_Entrance", 28f, 5f);
        n += Spawn(map, "default",      25.5f, 5f);
        n += Spawn(map, "from_hideout", 26.2f, 5f);
        n += Npc (map, "NPC_Warden", 26f, 6.8f, new Color(0.40f, 0.48f, 0.56f));
        n += Solid(map, "Container_D1", 27f, 8.3f, 3f, 0.8f, 2.6f, _matCont);
        n += Solid(map, "Container_D2", 34f, 8.3f, 4f, 0.8f, 2.6f, _matCont);

        // ── 조명 (야외 — 은신처보다 훨씬 밝다) ──
        var sunGO = new GameObject("Sun");
        sunGO.transform.SetParent(map.transform, false);
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.05f;
        sun.color = new Color(1f, 0.95f, 0.86f);
        sun.shadows = LightShadows.Soft;
        sunGO.transform.rotation = Quaternion.Euler(50f, -40f, 0f);
        n++;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.40f, 0.44f, 0.52f);
        RenderSettings.ambientEquatorColor = new Color(0.30f, 0.30f, 0.32f);
        RenderSettings.ambientGroundColor  = new Color(0.16f, 0.15f, 0.14f);

        // ── 오클루전 구동기 — 건물이 플레이어를 가릴 때 구멍을 뚫는다 ──
        var drv = new GameObject("CutawayDriver");
        drv.transform.SetParent(map.transform, false);
        drv.AddComponent<CutawayDriver>();
        n++;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=cyan>[Safehouse3D]</color> 생성 완료: {ScenePath} — 오브젝트 {n}개. " +
                  "건물 재질 = Spike/OccluderFX(컷어웨이), 구동 = CutawayDriver.");
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────
    static Material Lit(Color c, float smooth)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        return m;
    }

    /// <summary>가릴 수 있는 것 = 컷어웨이 셰이더. 셰이더가 없으면 URP/Lit로 폴백.</summary>
    static Material Occ(Color c)
    {
        if (_occluder == null) return Lit(c, 0.06f);
        var m = new Material(_occluder);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Mode", 1f);   // cutaway
        return m;
    }

    static int Box(GameObject parent, string name, Vector3 center, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return 1;
    }

    /// <summary>2D판의 (cx, cy, lenX, thickY)를 그대로 받아 높이만 얹는다.</summary>
    static int Solid(GameObject p, string name, float cx, float cz, float lenX, float thickZ, float h, Material mat)
        => Box(p, name, new Vector3(cx, h * .5f, cz), new Vector3(lenX, h, thickZ), mat);

    /// <summary>입구 표시 — 문틀만. 실제 씬 전환 트리거 배선은 Stage 1(InteractableObject 3D화) 뒤에.</summary>
    static int Doorway(GameObject p, string name, float x, float z)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p.transform, false);
        go.transform.position = new Vector3(x, 0f, z);
        var trg = go.AddComponent<BoxCollider>();
        trg.isTrigger = true;
        trg.size = new Vector3(1.3f, 2.2f, 1.0f);
        trg.center = new Vector3(0f, 1.1f, 0f);
        // 눈에 보이는 문틀(장식)
        var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "Frame";
        frame.transform.SetParent(go.transform, false);
        frame.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        frame.transform.localScale = new Vector3(1.3f, 2.2f, 0.12f);
        frame.GetComponent<MeshRenderer>().sharedMaterial = Lit(new Color(0.20f, 0.18f, 0.16f), 0.1f);
        Object.DestroyImmediate(frame.GetComponent<Collider>());
        return 1;
    }

    static int Prop(GameObject p, string name, float x, float z, Vector3 size, Color c)
        => Box(p, name, new Vector3(x, size.y * .5f, z), size, Lit(c, 0.06f));

    /// <summary>NPC 자리표시 — 캡슐. 실제 3D NPC 모델은 후속(Stage 4).</summary>
    static int Npc(GameObject p, string name, float x, float z, Color c)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.SetParent(p.transform, false);
        go.transform.position = new Vector3(x, 0.9f, z);
        go.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
        go.GetComponent<MeshRenderer>().sharedMaterial = Lit(c, 0.08f);
        return 1;
    }

    static int Spawn(GameObject p, string pointId, float x, float z)
    {
        var go = new GameObject(pointId);
        go.transform.SetParent(p.transform, false);
        go.transform.position = new Vector3(x, 0f, z);
        go.AddComponent<SpawnPoint>();
        return 1;
    }
}
#endif
