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
    const string ScenePath = "Assets/Scenes/Safehouse.unity";   // 3D 마을이 곧 정식 Safehouse다(2026-09-09)

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

        _occ       = Shader.Find("Universal Render Pipeline/Lit");
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
        n += Building(map, "Repair",    12f, 44f, 14f, 10f, ShopH, "S", open: false, unlockFlag: "shop_repair_unlocked");
        n += Building(map, "Medical",   64f, 44f, 18f, 10f, ShopH, "S", open: false, unlockFlag: "shop_medical_unlocked");
        n += Building(map, "Furniture", 12f, 27f, 14f, 10f, ShopH, "E", open: false, unlockFlag: "shop_furniture_unlocked");
        n += Building(map, "BlackMarket",12f, 10f, 14f, 12f, ShopH, "E", open: false, unlockFlag: "shop_blackmarket_unlocked");

        // ── 집/은신처 = 솔리드 컨테이너. 발자국을 실내(14×9)와 일치시킨다 ──
        n += Slab(map, "Container_Home", 64f, 12f, 14f, 9f, ContH, _cont);
        n += Doorway(map, "Hideout_Entrance", 56.4f, 12f, "Hideout", "default");   // 컨테이너 서쪽 문 → 은신처
        n += Spawn(map, "default",      53.5f, 12f);
        n += Spawn(map, "from_hideout", 54.2f, 12f);
        n += Slab(map, "Container_D1", 52f, 20f, 6f, 2.4f, 2.6f, _cont);
        n += Slab(map, "Container_D2", 68f, 21f, 8f, 2.4f, 2.6f, _cont);

        // ── 광장 · 게시판 · NPC ──
        n += Spawn(map, "raid_return", 40f, 29f);
        n += Prop(map, "Board_Quest", 72f, 29f, new Vector3(3.0f, 2.4f, 0.35f), new Color(0.58f, 0.46f, 0.28f));
        // 출전 지도판 — 레이드로 나가는 유일한 입구. 동쪽 게이트로 가는 길목에 둔다.
        n += MapBoard(map, "Board_Dispatch", 72f, 24f, new Vector3(2.6f, 2.2f, 0.35f), new Color(0.42f, 0.50f, 0.58f));
        n += Npc (map, "회수꾼",       "veteran_scavenger",  67f, 29f, new Color(0.52f, 0.46f, 0.40f));
        n += Npc (map, "떠돌이 상인",  "wandering_merchant", 36f, 14f, new Color(0.55f, 0.40f, 0.45f));
        n += Npc (map, "구역 관리인", "district_warden",    50f, 16f, new Color(0.40f, 0.48f, 0.56f));

        // ── 마을 분위기 (골목·간판·가로등·잡동사니) ──
        n += Dressing(map);

        // ── 조명 ── (값은 Lighting3D 한 곳에 있다 — 레이드 맵과 같은 태양을 쓴다)
        Lighting3D.Apply(map, Lighting3D.Preset.Outdoor);
        n++;

        SceneHierarchyOrganizer.Organize(scene);   // 규약대로 기능별 폴더에 묶는다(docs/architecture.md §씬 하이어라키 규약)
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
                        float w, float d, float h, string doorSide, bool open,
                        string unlockFlag = null)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent.transform, false);
        root.transform.position = new Vector3(cx, 0f, cz);
        int n = 1;

        float hw = w * .5f, hd = d * .5f;

        // Visual floor overlay. Ground is the walking surface at Y=0; a raised collider
        // penetrates the Y-locked player capsule and turns friction into a movement lock.
        n += Box(root, "Floor_In", new Vector3(cx, -0.035f, cz), new Vector3(w, 0.08f, d), _shopFloor, false);

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

        // 문틀은 열림·잠금 상관없이 세운다. 해금은 **셔터를 걷는 것**이 전부여야
        // "닫혀 있던 가게가 열렸다"로 읽힌다 — 해금 때 건물을 새로 짓지 않는다.
        n += Box(root, "DoorFrame", door + Vector3.up * 1.2f,
                 doorFacesZ ? new Vector3(DoorW + 0.3f, 2.4f, 0.14f)
                            : new Vector3(0.14f, 2.4f, DoorW + 0.3f), _frame);
        var f = root.transform.Find("DoorFrame");
        if (f != null) Object.DestroyImmediate(f.GetComponent<Collider>());   // 장식 — 통과해야 한다

        if (!open)
        {
            n += Box(root, "Shutter", door + Vector3.up * 1.2f,
                     doorFacesZ ? new Vector3(DoorW, 2.4f, 0.2f)
                                : new Vector3(0.2f, 2.4f, DoorW), _shutter);

            var unlock = root.AddComponent<BuildingUnlock>();
            unlock.Configure(unlockFlag, root.transform.Find("Shutter")?.gameObject);
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
    /// <summary>컷어웨이가 필요 없는 것들(지면·실내 바닥·셔터·문틀).
    /// Stage 0에서 정한 스타일라이즈드 룩을 쓴다 — 레이드 맵(<c>Greybox3D</c>)과 같은 재질이어야
    /// 마을과 레이드가 한 게임으로 보인다.</summary>
    static Material Lit(Color c, float s)
    {
        var sty = Shader.Find("Universal Render Pipeline/Lit");
        if (sty != null)
        {
            var sm = new Material(sty);
            sm.SetColor("_BaseColor", c);
            return sm;
        }
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

    static int Box(GameObject parent, string name, Vector3 center, Vector3 size, Material mat, bool solid = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, true);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        go.GetComponent<Collider>().enabled = solid;
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

    /// <summary>출전 지도판 — 누르면 `MapSelectUI`가 열려 레이드 지역을 고른다.
    ///
    /// ⚠️ 이게 없으면 **마을에서 레이드로 나갈 방법이 아예 없다.** 3D 마을을 새로 지으면서
    ///    게시판을 장식용 `Prop`으로만 세워 두는 바람에 핵심 루프(나가서 → 돌아온다)의
    ///    "나가서"가 통째로 끊겨 있었다. 상호작용 타입 `MapBoard`가 그 UI를 연다.</summary>
    static int MapBoard(GameObject p, string name, float x, float z, Vector3 size, Color c)
    {
        int n = Box(p, name, new Vector3(x, size.y * .5f, z), size, Lit(c, 0.06f));

        var go = p.transform.Find(name)?.gameObject;
        if (go == null) return n;

        var io = go.AddComponent<InteractableObject>();
        var so = new SerializedObject(io);
        so.FindProperty("type").enumValueIndex = (int)InteractableObject.InteractType.MapBoard;
        so.FindProperty("promptText").stringValue = "출전 준비";
        // 판이 두껍고(0.35m) 플레이어가 정면에 서므로 기본 2m보다 조금 넉넉하게.
        so.FindProperty("interactRange").floatValue = 3.0f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return n;
    }

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
        var sp = go.AddComponent<SpawnPoint>();

        // ⚠️ **`pointId`를 반드시 채운다.** 오브젝트 이름만 바꾸면 필드는 기본값 "default"로 남아
        //    이 씬의 스폰이 **전부 "default"를 자칭**하게 된다. 그러면 어느 문으로 들어와도
        //    가장 먼저 만들어진 스폰으로 떨어진다 — 마을 첫 진입이 전당포 앞으로 가던 원인이 이것.
        var so = new SerializedObject(sp);
        so.FindProperty("pointId").stringValue = id;
        so.ApplyModifiedPropertiesWithoutUndo();
        return 1;
    }

    // ── 마을 분위기 ──────────────────────────────────────────────────
    /// <summary>마을을 "사람이 사는 곳"으로 보이게 하는 것들 — 골목·간판·가로등·잡동사니.
    ///
    /// 건물만 세워 두면 넓은 빈 판 위에 상자가 몇 개 놓인 것으로 보인다. 길이 보여야
    /// 어디로 가야 할지 알고, 간판이 있어야 어느 가게인지 알고, 불이 있어야 밤에 걸을 수 있다.
    ///
    /// ⚠️ 소품은 **문 앞을 막지 않는다.** 문은 남/동쪽에 있으므로 그 앞 2m는 비운다.</summary>
    static int Dressing(GameObject map)
    {
        var root = new GameObject("Dressing");
        root.transform.SetParent(map.transform, false);
        int n = 1;

        var road  = Lit(new Color(0.26f, 0.25f, 0.24f), 0.04f);   // 다져진 흙길
        var wood  = new Color(0.42f, 0.34f, 0.24f);
        var rust  = new Color(0.40f, 0.28f, 0.20f);
        var steel = new Color(0.34f, 0.36f, 0.37f);
        var cloth = new Color(0.46f, 0.42f, 0.34f);

        // ① 골목 — 상가 앞 동서 대로 + 광장에서 게이트로 나가는 동서 길 + 남북 연결로.
        //    지면과 살짝 다른 색이면 충분하다. 길이 보이면 마을이 격자로 읽힌다.
        n += Box(root, "Road_ShopFront", new Vector3(38f, 0.01f, 36.5f), new Vector3(66f, 0.02f, 5f), road, false);
        n += Box(root, "Road_Plaza",     new Vector3(44f, 0.01f, 29f),   new Vector3(58f, 0.02f, 6f), road, false);
        n += Box(root, "Road_Link",      new Vector3(40f, 0.01f, 32.5f), new Vector3(5f,  0.02f, 12f), road, false);
        n += Box(root, "Road_Home",      new Vector3(58f, 0.01f, 16f),   new Vector3(5f,  0.02f, 24f), road, false);

        // ② 간판 — 어느 가게인지 문 위에서 알려준다. 잠긴 가게도 이름은 보인다.
        n += Sign(root, "전당포",  34f, 37.6f);
        n += Sign(root, "수리점",  12f, 38.6f);
        n += Sign(root, "의료소",  64f, 38.6f);
        n += Sign(root, "가구점",  19.6f, 27f);
        n += Sign(root, "암시장",  19.6f, 10f);

        // ③ 가로등 — 밤에 걸을 수 있게. 광장과 상가 앞에.
        n += Lamp(root, "Lamp_Plaza_W", 30f, 31.5f);
        n += Lamp(root, "Lamp_Plaza_E", 58f, 31.5f);
        n += Lamp(root, "Lamp_Shop_W",  22f, 38.5f);
        n += Lamp(root, "Lamp_Shop_E",  52f, 38.5f);
        n += Lamp(root, "Lamp_Home",    58f, 16f);

        // ④ 광장 화톳불 — 사람이 모이는 자리. 마을에 중심이 생긴다.
        n += Prop(root, "Brazier", 44f, 29f, new Vector3(1.1f, 0.7f, 1.1f), new Color(0.28f, 0.26f, 0.25f));
        var fireGO = new GameObject("Brazier_Fire");
        fireGO.transform.SetParent(root.transform, false);
        fireGO.transform.position = new Vector3(44f, 1.1f, 29f);
        var fire = fireGO.AddComponent<Light>();
        fire.type = LightType.Point; fire.range = 12f; fire.intensity = 2.2f;
        fire.color = new Color(1.00f, 0.62f, 0.32f);
        n++;

        // ⑤ 잡동사니 — 벽에 붙여 쌓는다. 가운데 두면 동선만 막는다.
        (float x, float z, float w, float h, float d, Color c)[] junk =
        {
            (5.0f, 33.0f, 1.0f, 0.9f, 1.0f, wood),  (6.2f, 34.0f, 0.8f, 0.7f, 0.8f, wood),
            (5.4f, 20.0f, 0.9f, 1.1f, 0.9f, rust),  (6.4f, 19.0f, 0.9f, 0.6f, 0.9f, steel),
            (74.0f, 20.0f, 1.0f, 0.9f, 1.0f, rust), (75.2f, 21.0f, 0.8f, 1.2f, 0.8f, steel),
            (74.5f, 40.0f, 1.1f, 0.8f, 1.1f, wood), (73.4f, 41.2f, 0.7f, 0.6f, 0.7f, wood),
            (26.0f, 12.0f, 1.2f, 0.5f, 1.2f, cloth),(27.4f, 12.6f, 0.9f, 0.9f, 0.9f, rust),
            (46.0f, 47.0f, 1.0f, 1.0f, 1.0f, steel),(47.2f, 46.2f, 0.8f, 0.7f, 0.8f, wood),
        };
        foreach (var j in junk)
            n += Prop(root, "Junk", j.x, j.z, new Vector3(j.w, j.h, j.d), j.c);

        // ⑥ 게이트 앞 바리케이드 — 폐도시로 나가는 문턱임을 알린다(통과는 된다).
        n += Prop(root, "Barricade_A", 76.5f, 26.5f, new Vector3(2.2f, 1.0f, 0.4f), steel);
        n += Prop(root, "Barricade_B", 76.5f, 31.5f, new Vector3(2.2f, 1.0f, 0.4f), steel);

        return n;
    }

    /// <summary>가게 간판 — 문 위에 이름을 띄운다. 쿼터뷰라 카메라를 향해 세운다.</summary>
    static int Sign(GameObject p, string text, float x, float z)
    {
        var go = new GameObject("Sign_" + text);
        go.transform.SetParent(p.transform, false);
        go.transform.position = new Vector3(x, 3.1f, z);
        go.transform.rotation = Quaternion.Euler(55f, 0f, 0f);   // 카메라 피치와 맞춘다

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 64;
        tm.characterSize = 0.06f;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.93f, 0.88f, 0.72f);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) { tm.font = font; go.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
        var mr = go.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        return 1;
    }

    /// <summary>가로등 — 기둥 + 점광. 밤에 길이 보이게.</summary>
    static int Lamp(GameObject p, string name, float x, float z)
    {
        int n = Box(p, name + "_Pole", new Vector3(x, 1.6f, z), new Vector3(0.18f, 3.2f, 0.18f),
                    Lit(new Color(0.22f, 0.22f, 0.23f), 0.20f));
        var go = new GameObject(name + "_Light");
        go.transform.SetParent(p.transform, false);
        go.transform.position = new Vector3(x, 3.3f, z);
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.range = 13f; l.intensity = 1.8f;
        l.color = new Color(1.00f, 0.90f, 0.72f);
        return n + 1;
    }
}

#endif
