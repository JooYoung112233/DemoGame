#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 안전구역(마을) — **3D 쿼터뷰, 80 × 56 m**.
///
/// 2026-09-08 스케일 재조정: 구 38 × 27 m는 "실내를 안 보여준다"는 전제로 잡힌 크기였다.
/// 걸어 들어가는 실내(→ [building-interior.md]) 결정에 따라 건물이 사람을 수용해야 하므로
/// 마을을 **80 m 폭**으로 키우고 건물 발자국을 14~18 m 급으로 올렸다.
///
/// 좌표 규약: 월드 = XZ 평면, 위 = +Y, 1u = 1m.
///
/// 건물은 두 종류다:
///  • <b>걸어 들어가는 건물</b> — 벽·바닥·지붕·문틈. 지붕은 `Roof` 이름이라 `BuildingInterior`가
///    진입 시 끈다. 잠금 상가도 **껍데기는 실내로 지어두고** 셔터로 막는다(해금 시 그대로 열림).
///  • <b>솔리드</b> — 은신처 컨테이너 외관. 내부는 별도 씬(`Hideout3D`)의 클릭 화면이라
///    걸어 들어가지 않는다. 외관 발자국을 **내부와 같은 14 × 9 m**로 맞춰 모순을 없앴다.
///
/// 규격·소품: docs/safehouse-3d.md · 메뉴: Tools ▸ TopDown ▸ 빌드 ▸ 안전구역 (3D)
/// </summary>
public static class Safehouse3DLayout
{
    const string ScenePath = "Assets/Scenes/Safehouse3D.unity";

    const float MapW = 80f, MapD = 56f;
    const float PerimeterH = 4.0f;   // 외곽 방벽
    const float ShopH      = 4.5f;   // 상가 층고(외벽)
    const float ContH      = 2.9f;   // 컨테이너 외관
    const float WallT      = 0.3f;
    const float DoorW      = 1.6f;   // 문 폭 — 기준 치수(권장 1.2~1.4) 위쪽

    static Material _ground, _wallOut, _shopWall, _shopFloor, _roof, _cont, _shutter, _frame;
    static Shader _occ;

    [MenuItem("Tools/TopDown/빌드/안전구역 (3D)", priority = -97)]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var map = new GameObject("Map");
        int n = 0;

        _occ       = Shader.Find("Spike/OccluderFX");
        _ground    = Lit(new Color(0.32f, 0.31f, 0.30f), 0.03f);
        _wallOut   = Occ(new Color(0.40f, 0.39f, 0.36f));
        _shopWall  = Occ(new Color(0.47f, 0.45f, 0.42f));
        _shopFloor = Lit(new Color(0.36f, 0.34f, 0.32f), 0.05f);
        _roof      = Occ(new Color(0.30f, 0.29f, 0.28f));   // ★ 지붕도 컷어웨이 대상 — 건물 뒤로 가면 막는 건 대부분 지붕이다
        _cont      = Occ(new Color(0.36f, 0.44f, 0.40f));
        _shutter   = Lit(new Color(0.55f, 0.48f, 0.32f), 0.20f);
        _frame     = Lit(new Color(0.18f, 0.16f, 0.15f), 0.10f);

        // ── 지면 ──
        n += Box(map, "Ground", new Vector3(MapW * .5f, -0.05f, MapD * .5f),
                 new Vector3(MapW, 0.1f, MapD), _ground);

        // ── 외곽 방벽 (동쪽에 게이트 개구부) ──
        n += Slab(map, "Wall_S", MapW * .5f, 0.5f, MapW, 1f, PerimeterH, _wallOut);
        n += Slab(map, "Wall_N", MapW * .5f, MapD - 0.5f, MapW, 1f, PerimeterH, _wallOut);
        n += Slab(map, "Wall_W", 0.5f, MapD * .5f, 1f, MapD, PerimeterH, _wallOut);
        // 동벽 두 토막, 사이(Z 26~32)가 폐도시 게이트
        n += Slab(map, "Gate_Wall_S", MapW - 0.5f, 13f, 1f, 26f, PerimeterH, _wallOut);
        n += Slab(map, "Gate_Wall_N", MapW - 0.5f, 44f, 1f, 24f, PerimeterH, _wallOut);

        // ── 걸어 들어가는 건물들 ──
        // 전당포(북 중앙) — 유일하게 지금 열려 있는 실내
        n += Building(map, "Pawnshop", 34f, 44f, 16f, 12f, ShopH, "S", open: true);
        n += Spawn(map, "from_pawnshop", 34f, 36.5f);
        n += PawnshopInterior(map);

        // 잠금 상가 4채 — 실내까지 지어두고 셔터로 막는다(해금 시 그대로 열림)
        n += Building(map, "Repair",    12f, 44f, 14f, 10f, ShopH, "S", open: false);
        n += Building(map, "Medical",   64f, 44f, 18f, 10f, ShopH, "S", open: false);
        n += Building(map, "Furniture", 12f, 27f, 14f, 10f, ShopH, "E", open: false);
        n += Building(map, "BlackMarket",12f, 10f, 14f, 12f, ShopH, "E", open: false);

        // ── 집/은신처 = 솔리드 컨테이너. 발자국을 실내(14×9)와 일치시킨다 ──
        n += Slab(map, "Container_Home", 64f, 12f, 14f, 9f, ContH, _cont);
        n += Doorway(map, "Hideout_Entrance", 56.4f, 12f, "Hideout3D", "default");   // 컨테이너 서쪽 문 → 은신처
        n += Spawn(map, "default",      53.5f, 12f);
        n += Spawn(map, "from_hideout", 54.2f, 12f);
        n += Slab(map, "Container_D1", 52f, 20f, 6f, 2.4f, 2.6f, _cont);
        n += Slab(map, "Container_D2", 68f, 21f, 8f, 2.4f, 2.6f, _cont);

        // ── 광장 · 게시판 · NPC ──
        n += Spawn(map, "raid_return", 40f, 29f);
        n += Prop(map, "Board_Quest", 72f, 29f, new Vector3(3.0f, 2.4f, 0.35f), new Color(0.58f, 0.46f, 0.28f));
        n += Npc (map, "회수꾼",       "veteran_scavenger",  67f, 29f, new Color(0.52f, 0.46f, 0.40f));
        n += Npc (map, "떠돌이 상인",  "wandering_merchant", 36f, 14f, new Color(0.55f, 0.40f, 0.45f));
        n += Npc (map, "구역 관리인", "district_warden",    50f, 16f, new Color(0.40f, 0.48f, 0.56f));

        // ── 조명 ──
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

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=cyan>[Safehouse3D]</color> {ScenePath} — {MapW}×{MapD}m, 오브젝트 {n}개. " +
                  "걸어 들어가는 건물 5채(지붕=BuildingInterior가 끔) + 컨테이너 솔리드.");
    }

    /// <summary>빌드 세팅에 씬 등록 — 없으면 SceneManager.LoadScene("...")이 실패한다.
    /// (2D 빌더는 하는데 3D 빌더가 빠뜨려 마을↔은신처 전환이 통째로 안 됐다.)</summary>
    static void AddToBuildSettings(string scenePath)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == scenePath) { s.enabled = true; EditorBuildSettings.scenes = list.ToArray(); return; }
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ── 건물 ────────────────────────────────────────────────────────
    /// <summary>벽·바닥·지붕·문틈을 갖춘 걸어 들어가는 건물.
    /// <paramref name="doorSide"/> = 문이 뚫릴 면("N"/"S"/"E"/"W").
    /// <paramref name="open"/>=false면 문틈에 셔터를 세워 막는다(잠금 상가).</summary>
    static int Building(GameObject parent, string name, float cx, float cz,
                        float w, float d, float h, string doorSide, bool open)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent.transform, false);
        root.transform.position = new Vector3(cx, 0f, cz);
        int n = 1;

        float hw = w * .5f, hd = d * .5f;

        // 실내 바닥(외부 지면보다 살짝 위 — 문턱)
        n += Box(root, "Floor_In", new Vector3(cx, 0.02f, cz), new Vector3(w, 0.08f, d), _shopFloor);

        // 네 벽. 문이 있는 면은 두 토막으로 나눠 가운데를 비운다.
        n += WallRun(root, name + "_S", cx, cz - hd, w, h, true,  doorSide == "S");
        n += WallRun(root, name + "_N", cx, cz + hd, w, h, true,  doorSide == "N");
        n += WallRun(root, name + "_W", cx - hw, cz, d, h, false, doorSide == "W");
        n += WallRun(root, name + "_E", cx + hw, cz, d, h, false, doorSide == "E");

        // 지붕 — 이름이 Roof로 시작해야 BuildingInterior가 자동 수집한다.
        n += Box(root, "Roof", new Vector3(cx, h + 0.15f, cz),
                 new Vector3(w + WallT * 2f, 0.3f, d + WallT * 2f), _roof);

        // 진입 판정 볼륨 + 지붕 끄기
        var trg = root.AddComponent<BoxCollider>();
        trg.isTrigger = true;
        trg.center = new Vector3(0f, h * .5f, 0f);
        trg.size   = new Vector3(w - 0.2f, h, d - 0.2f);
        root.AddComponent<BuildingInterior>();

        // 문 위치 = 문이 있는 면의 중앙
        Vector3 door = doorSide switch
        {
            "S" => new Vector3(cx, 0f, cz - hd),
            "N" => new Vector3(cx, 0f, cz + hd),
            "W" => new Vector3(cx - hw, 0f, cz),
            _   => new Vector3(cx + hw, 0f, cz),
        };
        bool doorFacesZ = doorSide == "S" || doorSide == "N";

        if (open)
        {
            n += Box(root, "DoorFrame", door + Vector3.up * 1.2f,
                     doorFacesZ ? new Vector3(DoorW + 0.3f, 2.4f, 0.14f)
                                : new Vector3(0.14f, 2.4f, DoorW + 0.3f), _frame);
            // 문틀은 장식 — 통과해야 하므로 콜라이더 제거
            var f = root.transform.Find("DoorFrame");
            if (f != null) Object.DestroyImmediate(f.GetComponent<Collider>());
        }
        else
        {
            n += Box(root, "Shutter", door + Vector3.up * 1.2f,
                     doorFacesZ ? new Vector3(DoorW, 2.4f, 0.2f)
                                : new Vector3(0.2f, 2.4f, DoorW), _shutter);
        }
        return n;
    }

    /// <summary>벽 한 면. <paramref name="gap"/>이면 가운데 문 폭만큼 비운다.</summary>
    static int WallRun(GameObject root, string name, float cx, float cz,
                       float len, float h, bool alongX, bool gap)
    {
        if (!gap)
        {
            var size = alongX ? new Vector3(len + WallT, h, WallT) : new Vector3(WallT, h, len + WallT);
            return Box(root, name, new Vector3(cx, h * .5f, cz), size, _shopWall);
        }
        float seg = (len - DoorW) * .5f;
        float off = (DoorW + seg) * .5f;
        int n = 0;
        if (alongX)
        {
            n += Box(root, name + "_a", new Vector3(cx - off, h * .5f, cz), new Vector3(seg, h, WallT), _shopWall);
            n += Box(root, name + "_b", new Vector3(cx + off, h * .5f, cz), new Vector3(seg, h, WallT), _shopWall);
        }
        else
        {
            n += Box(root, name + "_a", new Vector3(cx, h * .5f, cz - off), new Vector3(WallT, h, seg), _shopWall);
            n += Box(root, name + "_b", new Vector3(cx, h * .5f, cz + off), new Vector3(WallT, h, seg), _shopWall);
        }
        return n;
    }

    // ── 기본 헬퍼 ───────────────────────────────────────────────────
    static Material Lit(Color c, float s)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", s);
        return m;
    }

    static Material Occ(Color c)
    {
        if (_occ == null) return Lit(c, 0.06f);
        var m = new Material(_occ);
        m.SetColor("_BaseColor", c); m.SetFloat("_Mode", 1f);
        return m;
    }

    static int Box(GameObject parent, string name, Vector3 center, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, true);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return 1;
    }

    static int Slab(GameObject p, string name, float cx, float cz, float lenX, float lenZ, float h, Material m)
        => Box(p, name, new Vector3(cx, h * .5f, cz), new Vector3(lenX, h, lenZ), m);

    /// <summary>씬 전환 문. 2D BuildingEntrance는 BoxCollider2D 전제라 3D에선 SceneDoor3D를 쓴다.</summary>
    static int Doorway(GameObject p, string name, float x, float z, string scene = null, string spawn = "default")
    {
        var go = new GameObject(name);
        go.transform.SetParent(p.transform, false);
        go.transform.position = new Vector3(x, 0f, z);
        var t = go.AddComponent<BoxCollider>();
        t.isTrigger = true;
        t.size = new Vector3(1.6f, 2.2f, 1.2f);
        t.center = new Vector3(0f, 1.1f, 0f);
        if (!string.IsNullOrEmpty(scene)) go.AddComponent<SceneDoor3D>().Configure(scene, spawn);
        return 1;
    }

    static int Prop(GameObject p, string name, float x, float z, Vector3 size, Color c)
        => Box(p, name, new Vector3(x, size.y * .5f, z), size, Lit(c, 0.06f));

    /// <summary>말을 걸 수 있는 NPC. 캡슐 몸 + 상호작용 + 대화 배선.
    ///
    /// 2D판은 `gb_npc` 스프라이트 프리팹을 썼다. 3D에선 아직 NPC 모델이 없으므로(Stage 4)
    /// 캡슐 그레이박스로 세우되 **배선은 진짜로** 한다 — NPCData를 물려 대화가 실제로 열린다.
    /// 모델만 나중에 갈아끼우면 된다.</summary>
    static int Npc(GameObject p, string name, string storyNpcId, float x, float z, Color c)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.SetParent(p.transform, true);
        go.transform.position = new Vector3(x, 0.9f, z);
        go.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
        go.GetComponent<MeshRenderer>().sharedMaterial = Lit(c, 0.08f);

        // 몸은 밀리지 않게 — 대화 상대가 플레이어에 밀려 가게 두면 안 된다.
        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        var io = go.AddComponent<InteractableObject>();
        var so = new SerializedObject(io);
        var tp = so.FindProperty("type");
        if (tp != null) tp.enumValueIndex = (int)InteractableObject.InteractType.NPC;
        var pt = so.FindProperty("promptText");
        if (pt != null) pt.stringValue = "대화하기";
        // ⚠️ 상점 주인은 **카운터 너머**에 선다. 기본 사거리(2m)면 카운터 두께 + 서로의 몸
        //    때문에 손님 자리에서 말이 안 걸린다. NPC만 넉넉히 준다(전역으로 늘리면
        //    상자·문까지 멀리서 집히게 된다).
        var rp = so.FindProperty("interactRange");
        if (rp != null) rp.floatValue = 3.2f;
        so.ApplyModifiedPropertiesWithoutUndo();

        var npc = go.AddComponent<NPCController>();
        var nso = new SerializedObject(npc);
        var sid = nso.FindProperty("storyNpcId");
        if (sid != null) sid.stringValue = storyNpcId;
        var data = AssetDatabase.LoadAssetAtPath<NPCData>($"Assets/Resources/Data/NPC/{storyNpcId}.asset");
        var dp = nso.FindProperty("npcData");
        if (dp != null && data != null) dp.objectReferenceValue = data;
        nso.ApplyModifiedPropertiesWithoutUndo();
        if (data == null)
            Debug.LogWarning($"[Safehouse3D] NPCData를 못 찾았다: Data/NPC/{storyNpcId} — 대화가 안 열린다.");

        return 1;
    }

    // ── 전당포 실내 ──────────────────────────────────────────────────
    /// <summary>전당포 안을 채운다. 건물(34,44 / 16×12)은 Building이 이미 세웠고,
    /// 여기서는 **그 안의 내용**만 넣는다 — 카운터·주인장·선반.
    ///
    /// 2D판은 별도 씬(`Pawnshop.unity`)이었지만 3D는 걸어 들어가므로 같은 맵 안에 둔다
    /// (로딩이 끊기지 않는 것이 3D 실내의 요점 — building-interior.md).</summary>
    static int PawnshopInterior(GameObject map)
    {
        var root = new GameObject("Pawnshop_Interior");
        root.transform.SetParent(map.transform, false);
        int n = 1;

        const float CX = 34f, CZ = 44f;   // 건물 중심

        // 카운터 — 손님(남쪽)과 주인(북쪽)을 가르는 선. 이게 있어야 점포로 읽힌다.
        n += Slab(root, "Pawn_Counter", CX, CZ + 1.5f, 8f, 0.9f, 1.05f, Lit(new Color(0.42f, 0.33f, 0.24f), 0.06f));

        // 주인장 강무진 — 카운터 **뒤**에 선다.
        n += Npc(root, "전당포 주인", "pawnshop", CX, CZ + 2.4f, new Color(0.62f, 0.55f, 0.42f));   // 카운터에 붙어 선다

        // 북벽 선반 — 저당 잡힌 물건들이 놓이는 자리(모델은 후속).
        n += Prop(root, "Shelf_N1", CX - 5.5f, CZ + 4.6f, new Vector3(3.4f, 1.9f, 0.5f), new Color(0.34f, 0.31f, 0.28f));
        n += Prop(root, "Shelf_N2", CX + 5.5f, CZ + 4.6f, new Vector3(3.4f, 1.9f, 0.5f), new Color(0.34f, 0.31f, 0.28f));

        // 손님 쪽 — 기다리는 자리. 비워 두면 방이 넓기만 하고 쓸모가 없어 보인다.
        n += Prop(root, "Crate_A", CX - 6.2f, CZ - 3.4f, new Vector3(1.0f, 0.8f, 1.0f), new Color(0.45f, 0.38f, 0.28f));
        n += Prop(root, "Crate_B", CX - 5.2f, CZ - 4.2f, new Vector3(0.8f, 0.6f, 0.8f), new Color(0.42f, 0.35f, 0.26f));
        n += Prop(root, "Barrel",  CX + 6.2f, CZ - 3.6f, new Vector3(0.9f, 1.1f, 0.9f), new Color(0.33f, 0.36f, 0.34f));

        // 실내 전구 — 지붕이 꺼지면 BuildingInterior가 켠다(lightsFollowRoof).
        var lightGO = new GameObject("Bulb");
        lightGO.transform.SetParent(root.transform, false);
        lightGO.transform.position = new Vector3(CX, 3.2f, CZ);
        var lt = lightGO.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.range = 14f; lt.intensity = 2.6f;
        lt.color = new Color(1.00f, 0.86f, 0.66f);
        n++;

        return n;
    }

    static int Spawn(GameObject p, string id, float x, float z)
    {
        var go = new GameObject(id);
        go.transform.SetParent(p.transform, false);
        go.transform.position = new Vector3(x, 0f, z);
        go.AddComponent<SpawnPoint>();
        return 1;
    }
}
#endif
