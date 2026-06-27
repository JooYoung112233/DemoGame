#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 안전가옥 그레이박스 레이아웃을 gb_* 프리팹으로 Safehouse.unity에 배치(지역1=ScrapMarket과 동일 기법).
/// (docs/safehouse.md '# 격자 골목' + '실내 맵(별도 씬)' — 전당포(상중,내부 방+주인장) / 광장(중앙) / 컨테이너지역(우하,관리인+집 입구→Hideout 씬) / 떠돌이상인(중하) / 잠금건물=벽)
///
/// 생성물: Assets/Scenes/Safehouse.unity — '맵 콘텐츠'만(카메라/조명/매니저/플레이어는 Systems 부트 씬이 공급).
///   ※ 루프가 로드하는 씬에 바로 채워 "빈 안전가옥" 문제를 해소. 모든 그레이박스는 'Map' 루트 하위.
///
/// 배치(# 격자 골목 + 내부 보이는 건물):
///   • XY 평면, Z=0, 1u=1m. 맵 38(W)×27(H), 중심 (19,13.5). 골목이 #자(가로2+세로2)로 교차 → 3×3 = 9칸.
///     세로골목 X9~12·X22~25, 가로골목 Y9~12·Y17~20.
///   • 칸: 상단=수리점벽/전당포(건물 외관+입구 트리거)/의료소벽 · 중단=가구점벽/광장(허브,열림)/게시판+게이트 · 하단=블랙마켓벽/떠돌이상인/컨테이너지역(관리인+집).
///   • 전당포(상중) = 컨테이너 외관(솔리드) + 입구 BuildingEntrance 트리거(→Pawnshop.unity 실내 씬). 강무진/카운터는 Pawnshop.unity(PawnshopGreyboxLayout).
///   • 집/은신처(우하) = 컨테이너 외관(솔리드) + 입구 BuildingEntrance 트리거(→Hideout 씬). 들어가면 타르코프식 별도 실내 씬 — 침대·작업대 등은 Hideout.unity(HideoutGreyboxLayout). docs/safehouse.md '건물 전환 모델'.
///   • 동쪽 = 세로 방호벽 게이트(개구부 Y13~16 = 게시판 바로 우측) → 폐도시(출전은 게시판 의뢰로). 잠금건물(벽) 입구는 셔터.
///   • 벽/블록/방 = gb_wall, 셔터/방호벽 = gb_barricade. 스폰: default(집 앞,신규시작) / raid_return(중앙 광장) / from_pawnshop(전당포 복귀) / from_hideout(은신처 나가기 복귀,입구 문 앞).
///   • 멱등: 같은 경로로 저장하면 덮어씀. 프리팹은 InstantiatePrefab(링크 유지).
///
/// 메뉴: Tools ▸ TopDown ▸ Map ▸ Build Safehouse Greybox Layout
/// </summary>
public static class SafehouseGreyboxLayout
{
    const string ScenePath  = "Assets/Scenes/Safehouse.unity";
    const string PrefabRoot = "Props2D/Prefabs/";

    const float MapW = 38f, MapH = 27f;   // # 격자(내부 보임): 3×3칸 — 전당포(상중,내부+주인장)/광장(중앙)/컨테이너지역(우하,관리인+집내부)/떠돌이상인(중하)

    static readonly string[] RequiredPrefabIds =
    {
        "gb_floor", "gb_wall", "gb_barricade", "gb_spawn", "gb_npc", "gb_mapboard", "gb_exit",
    };

    [MenuItem("Tools/TopDown/개발/안전가옥 그레이박스(단독)")]
    public static void Build()
    {
        var scene = EditorSceneBuildUtil.NewDetachedScene(out var prevActive);  // 현재 씬 유지(폴더에만 생성)
        var map = new GameObject("Map");
        int n = 0;

        EnsureGreyboxPalette();

        // ── 바닥(38×27) + 외곽(남/북/서 벽). 동쪽 = 세로 방호벽, 게시판 우측에 게이트 개구부(Y13~16) → 폐도시. ──
        n += Floor(map, "Floor", 19f, 13.5f, MapW, MapH);
        n += Wall(map, "Wall_S", 19f,  0.5f, 38f, 1f);
        n += Wall(map, "Wall_N", 19f, 26.5f, 38f, 1f);
        n += Wall(map, "Wall_W",  0.5f,13.5f, 1f,27f);
        n += Barricade(map, "Gate_Wall_S", 37.5f,  7f, 1f, 12f, "폐도시 방면");   // 동벽 하 Y1~13
        n += Barricade(map, "Gate_Wall_N", 37.5f, 21f, 1f, 10f, "게이트→폐도시"); // 동벽 상 Y16~26
        //  개구부 = X37.5 Y13~16(중단 우측, 게시판 바로 우측) → 폐도시. 출격은 게시판 의뢰로.

        // ── 잠금 건물 = 솔리드 벽('나머진 다 벽'). 좌측 열 3 + 우상 의료소. 입구 셔터만 표시. ──
        n += Wall(map, "Block_Repair", 5f, 23f,   8f, 6f, "수리점 (잠금)");   // 좌상
        n += Wall(map, "Block_Med",   31f, 23f,  12f, 6f, "의료소 (잠금)");   // 우상
        n += Wall(map, "Block_Furn",   5f, 14.5f, 8f, 5f, "가구점 (잠금)");   // 좌중
        n += Wall(map, "Block_Black",  5f,  5f,   8f, 8f, "블랙마켓 (잠금)"); // 좌하
        n += Barricade(map, "Repair_Shutter", 5f, 20f, 3f, 1f, "셔터");  // 수리점 → 상단 가로골목
        n += Barricade(map, "Med_Shutter",   31f, 20f, 3f, 1f, "셔터");  // 의료소 → 상단 가로골목
        n += Barricade(map, "Furn_Shutter",   5f, 12f, 3f, 1f, "셔터");  // 가구점 → 하단 가로골목
        n += Barricade(map, "Black_Shutter",  5f,  9f, 3f, 1f, "셔터");  // 블랙마켓 → 하단 가로골목

        // ── 전당포(상단 중앙): 건물 외관(솔리드) + 입구 BuildingEntrance 트리거(→Pawnshop 실내 씬) ──
        //   강무진(주인장)은 전당포 실내 씬(Pawnshop.unity)으로 이동 — 여기선 외관+입구만.
        n += Wall(map, "Pawn_Building", 17f, 23.5f, 9f, 4.5f, "전당포");       // 외관(솔리드) X12.5~21.5 Y21.25~25.75
        n += Entrance(map, "Pawn_Entrance", 17f, 20.7f, 1.3f, 1f, "Pawnshop", "default", isExit: false); // 입구(남측 앞) — 문 한 칸
        n += Spawn(map, "from_pawnshop", 17f, 19.8f);                          // 전당포에서 복귀 → 건물 문 앞

        // ── 중앙 광장(허브, 열림) + 중앙 스폰 ──
        n += Spawn(map, "raid_return", 17f, 14.5f);   // ② 레이드 귀환(성공/실패/사망) — 중앙 광장

        // ── 게시판 + 회수꾼 + 게이트(중단 우측) ──
        n += Marker(map, "gb_mapboard","Board_Quest",       34f, 14.5f);// 게시판(의뢰) — 게이트 바로 좌측 ※코드는 MapBoard→MapSelectUI
        n += Npc (map, "NPC_Veteran",  "veteran_scavenger", 31f, 14.5f);// 베테랑 회수꾼 — 게시판 옆

        // ── 떠돌이 상인(중하, 옛 집자리) ──
        n += Npc (map, "NPC_Merchant", "wandering_merchant", 17f, 5f);  // 떠돌이 상인 — 옛 집 자리

        // ── 컨테이너 지역(우하): 집/은신처 = 컨테이너 외관 + 입구 BuildingEntrance 트리거(→Hideout 씬) + 관리인 + 집앞 스폰 ──
        //   은신처 퇴장은 HideoutController의 UI '나가기'/ESC가 담당(Safehouse/from_hideout 복귀, 캐릭터 없는 클릭화면이라 트리거 퇴장 불가).
        //   ★ 복귀 스폰(from_hideout)은 입구 트리거 밖에 둬야 함 — 안이면 복귀 즉시 재진입 루프. 입구(28,5,폭2.5→X26.75~29.25)보다 서쪽.
        n += Wall(map, "Container_Home", 32f, 5f, 8f, 5f, "집 (은신처)");      // 사는 컨테이너 외관(솔리드) — 내부는 Hideout 씬
        n += Entrance(map, "Hideout_Entrance", 28f, 5f, 1.3f, 1f, "Hideout", "default", isExit: false); // 입구 트리거 → Hideout.unity (문 한 칸)
        n += Spawn(map, "default",      25.5f, 5f);                          // ① 최초 시작 — 집 문 앞(입구 트리거 밖)
        // ③ 은신처(Hideout) UI '나가기'/ESC 복귀 → 입구 문 바로 앞.
        //   문은 서향(입구 트리거가 컨테이너 서벽 X=28 위에 걸침). 트리거(X26.75~29.25,Y4.5~5.5)·컨테이너벽(X28~36)
        //   둘 다 밖이어야 재진입 루프/벽 끼임 방지 → 문 서쪽 (26.2,5)(플레이어 반지름 0.3 고려해도 트리거 X서단 26.75에 0.25 여유).
        n += Spawn(map, "from_hideout", 26.2f, 5f);                          // 은신처 나가기 복귀 — 입구 문 바로 앞(서쪽, 트리거 밖)
        n += Npc (map, "NPC_Warden",  "district_warden",  26f, 6.8f);        // 구역 관리인 — 컨테이너 지역(집 배정, S-010)
        n += Wall(map, "Container_D1", 27f, 8.3f, 3f, 0.8f, "컨테이너");      // 지역 더미
        n += Wall(map, "Container_D2", 34f, 8.3f, 4f, 0.8f, "컨테이너");      // 지역 더미

        // ── 저장(덮어쓰기) ──
        Selection.activeObject = null;
        bool saved = EditorSceneBuildUtil.SaveAndClose(scene, ScenePath, prevActive);  // 저장 후 닫기(현재 씬 유지)
        if (!saved)
        {
            Debug.LogError("[SafehouseGB] 씬 저장 실패: " + ScenePath);
            return;
        }
        AssetDatabase.SaveAssets();

        // 전당포 입구가 가리키는 Pawnshop.unity를 함께 생성(빌드세팅 등록 포함) — 참조 일관성.
        PawnshopGreyboxLayout.Build();

        Debug.Log($"<color=cyan>[SafehouseGB]</color> 생성 완료: {ScenePath} — Map 하위 그레이박스 {n}개.\n" +
                  "  • # 격자(38×27): 골목 가로2(Y9~12·Y17~20)+세로2(X9~12·X22~25) → 3×3 9칸.\n" +
                  "  • 상단=수리점벽/전당포(건물 외관+입구 트리거)/의료소벽 · 중단=가구점벽/광장(허브+raid_return스폰)/게시판+회수꾼+게이트 · 하단=블랙마켓벽/떠돌이상인/컨테이너지역(관리인+집 입구).\n" +
                  "  • 건물 전환=BuildingEntrance 트리거(밟으면 페이드+additive, 캐릭터 유지). 전당포 입구→Pawnshop.unity(강무진은 그쪽으로 이동, 복귀 from_pawnshop). 집 입구→Hideout.unity(복귀 default, 은신처는 UI 나가기).\n" +
                  "  • 동쪽 방호벽 게이트(개구부 Y13~16 = 게시판 바로 우측) → 폐도시(출격은 게시판 의뢰로). 잠금건물=솔리드 벽+셔터.\n" +
                  "  • NPC는 storyNpcId로 NPCData 자동연결(SafehouseNpcBuilder 선행). 떠돌이상인(wandering_merchant)은 NPCData 없으면 빈 마커. (Pawnshop.unity 함께 생성·빌드세팅 등록.)");

        if (!Application.isBatchMode && !ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("Safehouse Greybox",
                $"{ScenePath} 생성 완료 — # 격자(38×27). 전당포·집=건물 트리거 진입.\n\nMap 루트 하위 그레이박스 {n}개:\n" +
                "  • 전당포(상중, 건물 외관+입구 트리거→Pawnshop.unity) · 광장(중앙, raid_return 스폰)\n" +
                "  • 집/은신처(우하 컨테이너) = 입구 트리거→Hideout 씬. 같은 지역에 관리인 + 집앞 default 스폰\n" +
                "  • 떠돌이상인(중하 옛 집자리) · 게시판+회수꾼+게이트(중우) · 잠금건물 4=벽\n\n" +
                "건물 진입/퇴장 = BuildingEntrance 트리거(페이드+캐릭터 유지). 은신처 퇴장만 UI/ESC.\n" +
                "Pawnshop.unity도 함께 생성됩니다. Systems 부트 씬이 카메라/조명/매니저/플레이어를 공급합니다.", "확인");
    }

    static void EnsureGreyboxPalette()
    {
        foreach (var id in RequiredPrefabIds)
            if (Resources.Load<GameObject>(PrefabRoot + id) == null)
            {
                Debug.Log($"<color=cyan>[SafehouseGB]</color> 프리팹 '{id}' 없음 → GreyboxPaletteBuilder.Generate.");
                GreyboxPaletteBuilder.Generate();
                return;
            }
    }

    // ── 헬퍼 (ScrapMarketGreyboxLayout과 동일 기법) ──
    // label != null 이면 자식 "Label" 텍스트를 교체(큰 블록/셔터를 씬 뷰에서 식별).
    static int Wall(GameObject parent, string name, float cx, float cy, float lenX, float thickY, string label = null)
        => Bar(parent, "gb_wall", name, cx, cy, lenX, thickY, label);
    static int Barricade(GameObject parent, string name, float cx, float cy, float lenX, float thickY, string label = null)
        => Bar(parent, "gb_barricade", name, cx, cy, lenX, thickY, label);

    static int Bar(GameObject parent, string prefabId, string name, float cx, float cy, float lenX, float thickY, string label = null)
    {
        var go = Inst(prefabId, name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.transform.localScale    = new Vector3(lenX, thickY, 1f);
        CounterScaleLabel(go);
        if (!string.IsNullOrEmpty(label)) SetLabel(go, label);
        return 1;
    }

    /// <summary>그레이박스 인스턴스의 자식 "Label" TextMesh 텍스트 교체(가독용).</summary>
    static void SetLabel(GameObject go, string text)
    {
        var label = go.transform.Find("Label");
        if (label == null) return;
        var tm = label.GetComponent<TextMesh>();
        if (tm != null) tm.text = text;
    }

    /// <summary>'내부 보이는 방' — 벽 4면 + 한 면 문틈(지붕 없음). 내부 가구/NPC는 호출 측에서 별도 배치.
    /// doorSide: "S"/"N"/"E"/"W". 벽 두께 0.5.</summary>
    static int Room(GameObject parent, string namePrefix, float cx, float cy, float w, float h, string doorSide, float doorW)
    {
        const float t = 0.5f;
        float left = cx - w / 2f, right = cx + w / 2f, bot = cy - h / 2f, top = cy + h / 2f;
        int n = 0;
        n += HWall(parent, namePrefix + "_S", left, right, bot, t, doorSide == "S" ? cx : float.NaN, doorW);
        n += HWall(parent, namePrefix + "_N", left, right, top, t, doorSide == "N" ? cx : float.NaN, doorW);
        n += VWall(parent, namePrefix + "_W", bot, top, left, t, doorSide == "W" ? cy : float.NaN, doorW);
        n += VWall(parent, namePrefix + "_E", bot, top, right, t, doorSide == "E" ? cy : float.NaN, doorW);
        return n;
    }

    /// <summary>가로 벽 한 줄(x0→x1, 높이 y). gapCenter가 NaN이 아니면 그 위치에 doorW 폭 문틈.</summary>
    static int HWall(GameObject parent, string name, float x0, float x1, float y, float t, float gapCenter, float gapW)
    {
        if (float.IsNaN(gapCenter))
            return Bar(parent, "gb_wall", name, (x0 + x1) / 2f, y, x1 - x0, t);
        int n = 0;
        float gl = gapCenter - gapW / 2f, gr = gapCenter + gapW / 2f;
        if (gl - x0 > 0.01f) n += Bar(parent, "gb_wall", name + "_a", (x0 + gl) / 2f, y, gl - x0, t);
        if (x1 - gr > 0.01f) n += Bar(parent, "gb_wall", name + "_b", (gr + x1) / 2f, y, x1 - gr, t);
        return n;
    }

    /// <summary>세로 벽 한 줄(y0→y1, 위치 x). gapCenter가 NaN이 아니면 그 위치에 doorW 폭 문틈.</summary>
    static int VWall(GameObject parent, string name, float y0, float y1, float x, float t, float gapCenter, float gapW)
    {
        if (float.IsNaN(gapCenter))
            return Bar(parent, "gb_wall", name, x, (y0 + y1) / 2f, t, y1 - y0);
        int n = 0;
        float gl = gapCenter - gapW / 2f, gr = gapCenter + gapW / 2f;
        if (gl - y0 > 0.01f) n += Bar(parent, "gb_wall", name + "_a", x, (y0 + gl) / 2f, t, gl - y0);
        if (y1 - gr > 0.01f) n += Bar(parent, "gb_wall", name + "_b", x, (gr + y1) / 2f, t, y1 - gr);
        return n;
    }

    static int Floor(GameObject parent, string name, float cx, float cy, float w, float h)
    {
        var go = Inst("gb_floor", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(cx, cy, 0f);
        go.transform.localScale    = new Vector3(w, h, 1f);
        CounterScaleLabel(go);
        return 1;
    }

    static int Marker(GameObject parent, string prefabId, string name, float x, float y)
    {
        var go = Inst(prefabId, name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);
        return 1;
    }

    /// <summary>스폰 배치 + pointId 직렬화 설정.</summary>
    static int Spawn(GameObject parent, string pointId, float x, float y)
    {
        var go = Inst("gb_spawn", "Spawn_" + pointId, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);
        var sp = go.GetComponentInChildren<SpawnPoint>();
        if (sp != null)
        {
            var so = new SerializedObject(sp);
            var p = so.FindProperty("pointId");
            if (p != null) { p.stringValue = pointId; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
        return 1;
    }

    /// <summary>BuildingEntrance(트리거 자동 전환) 마커 — gb_exit 시각 박스 재사용 + BoxCollider2D(trigger) + BuildingEntrance.
    /// 밟으면 페이드+additive로 targetScene/spawnId 전환(캐릭터/Systems 유지). 기존 gb_exit의 ExitPoint(E키)는 제거.</summary>
    static int Entrance(GameObject parent, string name, float x, float y, float w, float h,
        string targetScene, string spawnId, bool isExit)
    {
        var go = Inst("gb_exit", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);

        // 기존 gb_exit는 InteractableObject(ExitPoint, E키)를 갖는다 — 트리거 전환과 중복되니 제거.
        var oldIo = go.GetComponentInChildren<InteractableObject>();
        if (oldIo != null) Object.DestroyImmediate(oldIo);

        // 트리거 콜라이더 + BuildingEntrance(런타임 컴포넌트).
        var box = go.GetComponent<BoxCollider2D>();
        if (box == null) box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(w, h);

        var be = go.GetComponent<BuildingEntrance>();
        if (be == null) be = go.AddComponent<BuildingEntrance>();
        be.Configure(targetScene, spawnId, isExit);

        SetLabel(go, isExit ? "출구" : "입구");
        return 1;
    }

    /// <summary>NPC 마커 + storyNpcId 직렬화(대화/상점은 NPC Maker로 NPCData 별도 연결).</summary>
    static int Npc(GameObject parent, string name, string storyNpcId, float x, float y)
    {
        var go = Inst("gb_npc", name, parent);
        if (go == null) return 0;
        go.transform.localPosition = new Vector3(x, y, 0f);
        var npc = go.GetComponentInChildren<NPCController>();
        if (npc != null)
        {
            var so = new SerializedObject(npc);
            var sid = so.FindProperty("storyNpcId");
            if (sid != null) sid.stringValue = storyNpcId;
            // NPCData 자동 연결(SafehouseNpcBuilder로 만들어 둔 게 있으면)
            var data = AssetDatabase.LoadAssetAtPath<NPCData>($"Assets/Resources/Data/NPC/{storyNpcId}.asset");
            var dp = so.FindProperty("npcData");
            if (dp != null && data != null) dp.objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return 1;
    }

    static GameObject Inst(string prefabId, string name, GameObject parent)
    {
        var prefab = Resources.Load<GameObject>(PrefabRoot + prefabId);
        if (prefab == null)
        {
            Debug.LogError($"[SafehouseGB] 프리팹 로드 실패: Resources/{PrefabRoot}{prefabId}");
            return null;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        go.name = name;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;
        return go;
    }

    static void CounterScaleLabel(GameObject go)
    {
        var label = go.transform.Find("Label");
        if (label == null) return;
        var s = go.transform.localScale;
        float ix = Mathf.Approximately(s.x, 0f) ? 1f : 1f / s.x;
        float iy = Mathf.Approximately(s.y, 0f) ? 1f : 1f / s.y;
        label.localScale = new Vector3(ix, iy, 1f);
    }
}
#endif
