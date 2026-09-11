#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 건물 '내부' 씬 공용 빌더 — 전당포(`PawnshopGreyboxLayout`) 패턴의 재사용 헬퍼.
/// (docs/level-scrapmarket.md 2026-07-11 "건물 모델 = 전당포식 씬 전환")
///
/// 모델: 외부 맵(Zone1)의 건물은 **껍데기(외벽+문)** 뿐 → 문의 `BuildingEntrance` 트리거로 이 내부 씬 진입 →
///       내부 출구 트리거로 외부 복귀(`Zone1` / `from_&lt;건물&gt;` 스폰). 내부는 콘텐츠 전용
///       (Systems 씬이 카메라·조명·매니저·플레이어 공급).
///
/// 좌표 = 로컬 (0,0)~(w,h). 각 건물 빌더는 이 헬퍼로 바닥/벽/스폰/출구/루트/적만 얹으면 된다.
/// </summary>
public static class InteriorBuild
{
    /// <summary>내부 씬 시작 — 빈 씬 + 루트 오브젝트 반환.</summary>
    public static GameObject Begin(out UnityEngine.SceneManagement.Scene scene)
        => GreyboxBuild.BeginScene(out scene);

    /// <summary>바닥 + 외벽 4면(두께 1m) — <b>사방 완전 밀폐</b>.
    /// 2026-07-11 사용자 결정: 내부는 벽에 구멍을 뚫지 않는다. 밖으로 나가는 유일한 수단은
    /// 포탈(<see cref="ExitDoorSouth"/> / <see cref="Stairs"/>)뿐.</summary>
    public static int Shell(GameObject m, float w, float h)
    {
        int n = 0;
        n += GreyboxBuild.Floor(m, "Floor", w * 0.5f, h * 0.5f, w, h);
        n += GreyboxBuild.WallSeg(m, "W_S", 0f, 0f, w, 1f);
        n += GreyboxBuild.WallSeg(m, "W_N", 0f, h - 1f, w, h);
        n += GreyboxBuild.WallSeg(m, "W_W", 0f, 0f, 1f, h);
        n += GreyboxBuild.WallSeg(m, "W_E", w - 1f, 0f, w, h);
        return n;
    }

    /// <summary>남쪽 정문 = <b>벽은 그대로 두고</b> 그 앞 바닥에 복귀 포탈 발판을 깐다.
    ///
    /// 구(舊) `DoorGapSouth`는 남벽을 실제로 뚫었다 — 그래서 발판을 살짝 비껴 지나가면
    /// **구현 안 된 씬 바깥(캄캄한 공백)으로 걸어 나가졌다**(2026-07-11 사용자 보고).
    /// 지금은 벽에 문 '표시'(gb_door)만 붙이고, 통과는 오직 이 발판으로만 일어난다.
    ///
    /// 배치: 문 표시 y=0.5(벽 안), 발판 y=1.65(방 안쪽) → 진입 스폰은 y≈2.9면 안 겹친다.
    /// </summary>
    public static int ExitDoorSouth(GameObject m, string name, float doorX,
                                    string targetScene, string spawnId, float doorW = 2.2f)
    {
        int n = 0;
        n += GreyboxBuild.Marker(m, "gb_door", name + "_Door", doorX, 0.5f);
        n += Exit(m, name, doorX, 1.65f, targetScene, spawnId, doorW, 1.0f);
        // gb_exit 프리팹 라벨은 "탈출"(=맵 이탈)이라 건물 안에선 뜻이 어긋난다.
        GreyboxBuild.Relabel(Find(m.transform, name), "나가기");
        return n;
    }

    /// <summary>층간 이동 포탈(2층/지하 계단). 벽을 뚫지 않는 바닥 발판이라 밀폐가 유지된다.</summary>
    public static int Stairs(GameObject m, string name, float x, float y,
                             string targetScene, string spawnId, string label = "계단",
                             float w = 1.6f, float h = 1.6f)
    {
        int n = Exit(m, name, x, y, targetScene, spawnId, w, h);
        if (n > 0) GreyboxBuild.Relabel(Find(m.transform, name), label);
        return n;
    }

    /// <summary>진입 스폰(외부에서 들어왔을 때 서는 자리). pointId 주입.</summary>
    public static int Spawn(GameObject m, string pointId, float x, float y)
    {
        if (GreyboxBuild.Marker(m, "gb_spawn", $"Spawn_{pointId}", x, y) == 0) return 0;
        var t = Find(m.transform, $"Spawn_{pointId}");
        var sp = t != null ? t.GetComponentInChildren<SpawnPoint>() : null;
        if (sp != null)
        {
            var so = new SerializedObject(sp);
            var p = so.FindProperty("pointId");
            if (p != null) { p.stringValue = pointId; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
        return 1;
    }

    /// <summary>출구 트리거 — 밟으면 외부 맵으로 복귀(BuildingEntrance, isExit). 전당포 Exit 패턴.</summary>
    public static int Exit(GameObject m, string name, float x, float y, string targetScene, string spawnId,
                           float w = 2f, float h = 1f)
    {
        if (GreyboxBuild.Marker(m, "gb_exit", name, x, y) == 0) return 0;
        var t = Find(m.transform, name);
        if (t == null) return 0;
        var go = t.gameObject;

        // gb_exit의 InteractableObject(ExitPoint, E키)는 트리거 전환과 중복 → 제거.
        var io = go.GetComponentInChildren<InteractableObject>();
        if (io != null) Object.DestroyImmediate(io);

        // 출구 발판. 2D는 BoxCollider2D + BuildingEntrance, 3D는 BoxCollider + SceneDoor3D다.
        // (BuildingEntrance는 BoxCollider2D 전제라 3D 맵에서 동작하지 않는다 — SceneDoor3D 참조)
        if (GreyboxBuild.Use3D)
        {
            var box3 = go.GetComponent<BoxCollider>();
            if (box3 == null) box3 = go.AddComponent<BoxCollider>();
            box3.isTrigger = true;
            box3.size = new Vector3(w, 2.4f, h);
            box3.center = new Vector3(0f, 1.2f, 0f);   // 사람 키만큼 세워야 밟히는 게 아니라 통과로 잡힌다

            var d3 = go.GetComponent<SceneDoor3D>();
            if (d3 == null) d3 = go.AddComponent<SceneDoor3D>();
            d3.Configure(targetScene, spawnId);
        }
        else
        {
            var box = go.GetComponent<BoxCollider2D>();
            if (box == null) box = go.AddComponent<BoxCollider2D>();   // ??는 Unity 가짜 null을 통과시켜 못 씀
            box.isTrigger = true;
            box.size = new Vector2(w, h);

            var be = go.GetComponent<BuildingEntrance>();
            if (be == null) be = go.AddComponent<BuildingEntrance>();
            // 크기를 Configure로 함께 넘긴다 — 안 그러면 Awake가 triggerSize(1.3×1.0)로 덮어써서
            //   여기서 지정한 폭이 조용히 사라진다(발판 옆으로 빠져나가는 원인이었다).
            be.Configure(targetScene, spawnId, true, new Vector2(w, h));
        }
        return 1;
    }

    /// <summary>바닥 루트 앵커(ItemSpawnPoint.Ground) — 예산제가 분배.</summary>
    public static int GroundLoot(GameObject m, string name, float x, float y)
    {
        var go = GreyboxBuild.Point(m, name, x, y);
        SetType(go.AddComponent<ItemSpawnPoint>(), 0);
        return 1;
    }

    /// <summary>이 건물의 기본 상자 종류(루팅 표 int_*) — 각 Build*가 Begin 직후 정한다(2026-09-11).</summary>
    public static string CrateKind = "int_shop";

    /// <summary>상자 — **열 수 있는 진짜 상자**(LootContainer + 상호작용) + Container 앵커.
    /// 2026-09-11 전엔 모양만 있는 박스라 상자 예산이 전부 바닥 더미로 흘러나왔다(docs/region-loot.md §루팅 정리 결정).
    /// kind = 루팅 표 종류(비우면 CrateKind). register = 계산대, safe = 금고 — 고철은 여기서만(늘 채워진다).</summary>
    public static int Crate(GameObject m, string name, float x, float y, string kind = null)
    {
        if (GreyboxBuild.Marker(m, "gb_crate", name, x, y) == 0) return 0;
        var t = Find(m.transform, name);
        if (t == null) return 0;
        MakeSearchable(t.gameObject, kind ?? CrateKind);
        return 1;
    }

    /// <summary>오브젝트를 뒤질 수 있는 상자로 — LootContainer(종류·이름) + 상호작용 + Container 앵커.
    /// Zone1 빌더(나무상자·잡동사니·좌판·트렁크)도 이걸 쓴다.</summary>
    public static void MakeSearchable(GameObject go, string kind, string label = null, string prompt = null,
                                      int w = 3, int h = 2, float radius = 1.8f)
    {
        var lc = go.GetComponent<LootContainer>();
        if (lc == null) lc = go.AddComponent<LootContainer>();   // ??는 Unity 가짜 null을 통과시켜 못 씀
        lc.Setup(label ?? KindLabel(kind), w, h);
        lc.SetLootKind(kind);

        var io = go.GetComponent<InteractableObject>();
        if (io == null) io = go.AddComponent<InteractableObject>();
        io.Configure(InteractableObject.InteractType.Container, prompt ?? KindPrompt(kind), radius);

        var sp = go.GetComponent<ItemSpawnPoint>();
        if (sp == null) sp = go.AddComponent<ItemSpawnPoint>();
        SetType(sp, 1);   // Container
    }

    public static string KindLabel(string kind) => kind switch
    {
        "register" => "계산대", "safe" => "금고", "crate" => "나무상자", "junk" => "길가 잡동사니",
        "trunk" => "자동차 트렁크", "stall" => "좌판", "int_medical" => "약품장", "int_police" => "사물함",
        "int_jewelry" => "진열장", "int_tools" => "공구 상자", "int_food" => "식료품 상자",
        "int_electronics" => "부품 상자", "int_basement" => "낡은 궤짝", _ => "상자",
    };

    public static string KindPrompt(string kind) => kind switch
    {
        "register" => "계산대 뒤지기", "safe" => "금고 열기", "trunk" => "트렁크 뒤지기", "stall" => "좌판 뒤지기",
        _ => "뒤지기",
    };

    /// <summary>적 스폰 존.</summary>
    public static int Enemy(GameObject m, string name, float cx, float cy, float w, float h, string unitKey, int count)
    {
        var go = GreyboxBuild.Point(m, name, cx, cy);
        go.AddComponent<SpawnZone>().Setup(GreyboxBuild.PlanSize(w, h), count, unitKey);
        return 1;
    }

    /// <summary>내부 잠금 문(BlockedPassage) — 무기고·금고처럼 **내부 최고 보상을 가두는** 문.
    /// mode=Locked면 keyOrKnowledge = 아이템 id, Code면 지식 id.</summary>
    public static int Gate(GameObject m, string name, float cx, float cy, float w, float h,
                           BlockedPassage.Mode mode, string label, string keyOrKnowledge = null,
                           string hint = null)
    {
        if (GreyboxBuild.Barricade(m, name, cx, cy, w, h) == 0) return 0;
        var t = Find(m.transform, name);
        if (t == null) return 0;
        var go = t.gameObject;

        // 막힌 통로의 몸통. 2D는 BoxCollider2D, 3D는 BoxCollider다.
        // ⚠️ 3D 상자에 2D 콜라이더를 붙이면 AddComponent가 null을 돌려주고(기존 3D 콜라이더와 충돌)
        //    바로 다음 줄에서 NullReference가 난다 — 보석상·경찰서 빌드가 여기서 죽었다.
        if (GreyboxBuild.Use3D)
        {
            var box3 = go.GetComponent<BoxCollider>();
            if (box3 == null) box3 = go.AddComponent<BoxCollider>();
            box3.isTrigger = false;
            box3.size = Vector3.one;   // 부모 스케일(w, 높이, h)이 곱해진다
        }
        else
        {
            var box = go.GetComponent<BoxCollider2D>();
            if (box == null) box = go.AddComponent<BoxCollider2D>();   // ??는 Unity 가짜 null을 통과시켜 못 씀
            box.isTrigger = false;
            box.size = Vector2.one;   // 부모 스케일(w,h)이 곱해진다
        }

        var io = go.GetComponent<InteractableObject>();
        if (io == null) io = go.AddComponent<InteractableObject>();
        io.Configure(InteractableObject.InteractType.Passage, label, Mathf.Max(w, h) * 0.5f + 1.6f);

        var bp = go.GetComponent<BlockedPassage>();
        if (bp == null) bp = go.AddComponent<BlockedPassage>();
        bp.Configure(mode, label, keyOrKnowledge, hint);
        return 1;
    }

    /// <summary>지식을 주는 쪽지 — 읽으면 PlayerKnowledge에 남는다(아이템이 아니라 죽어도 안 잃음).</summary>
    public static int KnowledgeNote(GameObject m, string name, float x, float y,
                                    string title, string content, string knowledgeId)
    {
        if (GreyboxBuild.Marker(m, "gb_note", name, x, y) == 0) return 0;
        var t = Find(m.transform, name);
        var io = t != null ? t.GetComponentInChildren<InteractableObject>() : null;
        if (io != null) io.SetKnowledgeNote(content, title, knowledgeId);
        return 1;
    }

    /// <summary>루팅 예산 분배기 — 내부 씬도 제 예산을 갖는다(프로파일 경로 지정).
    /// budgetMult = 이 씬만의 후함(유니크 건물). lootRegion을 주면 **상위 지역 루트 테이블**을 쓴다
    /// (= "위험을 감수하면 더 좋은 물품"을 데이터로 표현).</summary>
    public static int Controller(GameObject m, string profilePath, string regionId,
                                 float budgetMult = 1f, string lootRegion = null)
    {
        if (!string.IsNullOrEmpty(lootRegion)) regionId = lootRegion;
        return ControllerInternal(m, profilePath, regionId, budgetMult);
    }

    static int ControllerInternal(GameObject m, string profilePath, string regionId, float budgetMult)
    {
        var go = new GameObject("MapSpawnController");
        go.transform.SetParent(m.transform, false);
        var c = go.AddComponent<MapSpawnController>();
        var profile = AssetDatabase.LoadAssetAtPath<MapSpawnProfile>(profilePath);
        if (profile == null) Debug.LogWarning($"[InteriorBuild] 프로파일 로드 실패: {profilePath}");
        var so = new SerializedObject(c);
        var p  = so.FindProperty("profile");              if (p  != null) p.objectReferenceValue = profile;
        var r  = so.FindProperty("regionIdOverride");     if (r  != null) r.stringValue = regionId;
        // 내부 씬 표시 — 런타임에 GameTuning.interiorLootBudgetMult가 곱해진다(맵 전체 예산 그대로 쓰면 과다).
        //   루트 **테이블**은 지역 확률(regionId) 그대로 → 내부 파밍도 지역 확률에 맞춰 나온다.
        var it = so.FindProperty("isInterior");           if (it != null) it.boolValue = true;
        var bm = so.FindProperty("budgetMult");           if (bm != null) bm.floatValue = budgetMult;
        so.ApplyModifiedPropertiesWithoutUndo();
        return 1;
    }

    /// <summary>씬 저장 + 빌드세팅 등록(안 하면 진입 시 LoadSceneAsync 실패).</summary>
    public static void End(UnityEngine.SceneManagement.Scene scene, string scenePath, int n, string label)
    {
        GreyboxBuild.EndScene(scene, scenePath, n, label);
        AddToBuildSettings(scenePath);
        AssetDatabase.SaveAssets();
    }

    public static void AddToBuildSettings(string scenePath)
    {
        var cur = EditorBuildSettings.scenes;
        foreach (var s in cur) if (s.path == scenePath) return;
        var arr = new EditorBuildSettingsScene[cur.Length + 1];
        System.Array.Copy(cur, arr, cur.Length);
        arr[cur.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = arr;
        Debug.Log($"[InteriorBuild] 빌드세팅 등록: {scenePath}");
    }

    static void SetType(ItemSpawnPoint sp, int type)
    {
        var so = new SerializedObject(sp);
        var t = so.FindProperty("spawnType");
        if (t != null) { t.enumValueIndex = type; so.ApplyModifiedPropertiesWithoutUndo(); }
    }

    public static Transform Find(Transform t, string n)
    {
        if (t.name == n) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var r = Find(t.GetChild(i), n);
            if (r != null) return r;
        }
        return null;
    }
}
#endif
