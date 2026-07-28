#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 지역1 건물 '내부' 씬들 — 전당포식 씬 전환 모델(docs/level-scrapmarket.md 2026-07-11).
/// 외부(Zone1)의 건물 껍데기 문 → `BuildingEntrance` → 여기 내부 씬 → 출구 트리거 → Zone1/from_&lt;건물&gt;.
///
/// 1차 = 대표 2채(패턴 검증). 나머지 9채는 같은 헬퍼(`InteriorBuild`)로 이어서 추가.
///   • **약국**〔랜드마크A〕 — §1.4c #4: 단층(매장+약품실). 카운터 상자(key_pharmacy) → 잠긴 약품 캐비닛(의료 루트)
///     → 회수꾼 철제 상자(동료 유품 떡밥). 밴딧 1~2. **첫 열쇠 게이트 학습**.
///   • **폐상점** — §1.4c #1 / §1.4b: 첫 파밍 학습. 단층 잡화점, 선반×2·상자×1. 적 없음(학습용).
///
/// 메뉴: Tools ▸ TopDown ▸ 빌드 ▸ 내부 ▸ *
/// </summary>
public static class Zone1Interiors
{
    const string ProfilePath = "Assets/Resources/Data/MapSpawn/scrap_market.asset";
    const string RegionId    = "scrap_market";
    const string Outside     = "Zone1";

    // ── 약국(랜드마크A) ──────────────────────────────────────────────
    // 매장(남, 넓음) + 약품실(북, 잠긴 방). 문 갭은 남벽 중앙.
    // 동선: 진입 → 매장 파밍 → 카운터 상자에서 key_pharmacy → 약품실 문 → 캐비닛(의료) + 회수꾼 철제 상자.
    const string PharmacyPath = "Assets/Scenes/Int_Pharmacy.unity";

    [MenuItem("Tools/TopDown/빌드/내부/약국", priority = -90)]
    public static void BuildPharmacy()
    {
        var m = InteriorBuild.Begin(out var scene);
        int n = 0;
        const float W = 18f, H = 16f;

        n += InteriorBuild.Shell(m, W, H);                                                     // 사방 밀폐
        n += InteriorBuild.ExitDoorSouth(m, "Exit_ToZone1", 9f, Outside, BuildingReturn.BackSpawnId, 2.4f);  // 남쪽 정문(포탈)
        n += InteriorBuild.Spawn(m, "default", 9f, 2.9f);

        // 매장/약품실 칸막이(y=10) — 약품실 문 갭 x=13~15.5(잠금 게이트)
        n += GreyboxBuild.WallSeg(m, "P_Div_a", 1f, 10f, 13f, 11f);
        n += GreyboxBuild.WallSeg(m, "P_Div_b", 15.5f, 10f, W - 1f, 11f);
        n += GreyboxBuild.Marker(m, "gb_door", "Med_Gate(key_pharmacy)", 14.2f, 10.5f);

        // 매장 — 선반(벽) + 바닥 루트 + 카운터 상자(열쇠 앵커)
        n += GreyboxBuild.WallSeg(m, "P_Shelf1", 3f, 4f, 8f, 4.8f);
        n += GreyboxBuild.WallSeg(m, "P_Shelf2", 10f, 4f, 15f, 4.8f);
        n += GreyboxBuild.WallSeg(m, "P_Counter", 3f, 7.5f, 9f, 8.3f);
        n += InteriorBuild.GroundLoot(m, "P_G0", 5f, 6f);
        n += InteriorBuild.GroundLoot(m, "P_G1", 12f, 6f);
        n += InteriorBuild.GroundLoot(m, "P_G2", 14f, 3f);
        n += InteriorBuild.Crate(m, "P_Counter_Crate", 8f, 8.8f);   // key_pharmacy 자리(고정 열쇠는 후속)
        n += InteriorBuild.Crate(m, "P_SQ002_Box", 12f, 8.8f);      // 외부에서 이전(SQ002_Box)

        // 약품실(잠긴 방) — 의료 루트 집중 + 회수꾼 철제 상자
        n += InteriorBuild.Crate(m, "P_MedCab1", 4f, 13f);
        n += InteriorBuild.Crate(m, "P_MedCab2", 7f, 13f);
        n += InteriorBuild.Crate(m, "P_Salvager_Box", 14f, 13.5f);  // 동료 유품 떡밥(쪽지는 후속)
        n += InteriorBuild.GroundLoot(m, "P_G3", 11f, 12.5f);

        // 밴딧 1~2(매장)
        n += InteriorBuild.Enemy(m, "P_EZ", 9f, 6f, 10f, 6f, "bandit_melee_1", 2);

        n += InteriorBuild.Controller(m, ProfilePath, RegionId);
        InteriorBuild.End(scene, PharmacyPath, n, "지역1 내부 — 약국(랜드마크A: 매장+약품실 열쇠 게이트)");
    }

    // ── 폐상점(튜토 첫 파밍) ─────────────────────────────────────────
    // §1.4b: 단층 잡화점, 선반×2·상자×1. 학습용이라 적 없음.
    const string AbShopPath = "Assets/Scenes/Int_AbandonedShop.unity";

    [MenuItem("Tools/TopDown/빌드/내부/폐상점", priority = -89)]
    public static void BuildAbandonedShop()
    {
        var m = InteriorBuild.Begin(out var scene);
        int n = 0;
        const float W = 12f, H = 9f;

        n += InteriorBuild.Shell(m, W, H);
        n += InteriorBuild.ExitDoorSouth(m, "Exit_ToZone1", 6f, Outside, BuildingReturn.BackSpawnId, 2.2f);
        n += InteriorBuild.Spawn(m, "default", 6f, 2.9f);

        // 선반 2 + 상자 1 (문서 사양)
        n += GreyboxBuild.WallSeg(m, "S_Shelf1", 2f, 4f, 6f, 4.8f);
        n += GreyboxBuild.WallSeg(m, "S_Shelf2", 7f, 6f, 10f, 6.8f);
        n += InteriorBuild.Crate(m, "S_Crate", 3f, 7f);
        n += InteriorBuild.GroundLoot(m, "S_G0", 8f, 3.2f);
        n += InteriorBuild.GroundLoot(m, "S_G1", 5f, 5.6f);

        n += InteriorBuild.Controller(m, ProfilePath, RegionId);
        InteriorBuild.End(scene, AbShopPath, n, "지역1 내부 — 폐상점(첫 파밍 학습: 선반2·상자1, 적 없음)");
    }

    // ── 차고 (§1.4c #2: 둘째 파밍 / 단층 정비 차고 / 선반×1·상자×2) ──────
    const string GaragePath = "Assets/Scenes/Int_Garage.unity";

    [MenuItem("Tools/TopDown/빌드/내부/차고", priority = -88)]
    public static void BuildGarage()
    {
        var m = InteriorBuild.Begin(out var scene);
        int n = 0; const float W = 14f, H = 11f;

        n += InteriorBuild.Shell(m, W, H);
        n += InteriorBuild.ExitDoorSouth(m, "Exit_ToZone1", 7f, Outside, BuildingReturn.BackSpawnId, 2.4f);
        n += InteriorBuild.Spawn(m, "default", 7f, 2.9f);

        // 차량 잔해 2대(벽 블록) — 엄폐·동선 꺾기
        n += GreyboxBuild.WallSeg(m, "G_Car1", 2.5f, 4f, 6.5f, 6f);
        n += GreyboxBuild.WallSeg(m, "G_Car2", 8f, 7f, 12f, 9f);
        n += GreyboxBuild.WallSeg(m, "G_Shelf", 2f, 8.5f, 6f, 9.3f);   // 선반 ×1

        n += InteriorBuild.Crate(m, "G_Crate1", 11.5f, 4.5f);          // 상자 ×2
        n += InteriorBuild.Crate(m, "G_Crate2", 3f, 7f);
        n += InteriorBuild.GroundLoot(m, "G_G0", 9f, 5f);
        n += InteriorBuild.GroundLoot(m, "G_G1", 6f, 9.5f);

        n += InteriorBuild.Controller(m, ProfilePath, RegionId);
        InteriorBuild.End(scene, GaragePath, n, "지역1 내부 — 차고(정비, 선반1·상자2, 차량 잔해)");
    }

    // ── 창고 (§1.4c #3: 튜토 목표 + 지하창고 입구 / 셔터 / 민이 흔적 쪽지) ──
    const string WarehousePath = "Assets/Scenes/Int_Warehouse.unity";

    [MenuItem("Tools/TopDown/빌드/내부/창고", priority = -87)]
    public static void BuildWarehouse()
    {
        var m = InteriorBuild.Begin(out var scene);
        int n = 0; const float W = 17f, H = 14f;

        n += InteriorBuild.Shell(m, W, H);
        n += InteriorBuild.ExitDoorSouth(m, "Exit_ToZone1", 8.5f, Outside, BuildingReturn.BackSpawnId, 2.6f);   // 셔터
        n += InteriorBuild.Spawn(m, "default", 8.5f, 2.9f);

        // 적재 선반 열(창고다움) — 통로가 갈라지게
        n += GreyboxBuild.WallSeg(m, "W_Rack1", 2f, 5f, 7f, 5.9f);
        n += GreyboxBuild.WallSeg(m, "W_Rack2", 10f, 5f, 15f, 5.9f);
        n += GreyboxBuild.WallSeg(m, "W_Rack3", 2f, 9f, 7f, 9.9f);

        // MQ-001 민이 흔적 — 쪽지 2개(문서 §1.4b)
        n += GreyboxBuild.Note(m, "W_Note_Trace", 4.5f, 11.5f, "손자국 흔적",
            "먼지 쌓인 선반에 작은 손자국. 최근 것이다. 누군가 여기 숨어 있었다.");
        n += GreyboxBuild.Note(m, "W_Note_Knock", 12f, 11.5f, "노크 규칙",
            "벽에 긁어 쓴 글씨 — '두 번, 쉬고, 세 번.' 민이가 정한 신호다.");

        n += InteriorBuild.Crate(m, "W_Crate1", 8.5f, 7.5f);
        n += InteriorBuild.Crate(m, "W_Crate2", 14f, 11f);
        n += InteriorBuild.GroundLoot(m, "W_G0", 5f, 7.5f);
        n += InteriorBuild.GroundLoot(m, "W_G1", 11f, 10f);

        // 동측 지하창고 입구 — 짙은현상 phase 게이트는 후속(지금은 상시 진입)
        n += GreyboxBuild.Note(m, "W_BasementLabel", 15f, 7f, "지하창고 입구 ★★★★★",
            "짙은 현상 때만 열리는 구획(게이트 후속). 최고 위험·최고 보상.");
        n += InteriorBuild.Stairs(m, "Basement_Block", 15.3f, 7.8f, "Int_Basement", "default", "지하 계단");

        n += InteriorBuild.Enemy(m, "W_EZ", 8.5f, 8f, 10f, 6f, "bandit_melee_1", 1);
        n += InteriorBuild.Controller(m, ProfilePath, RegionId);
        InteriorBuild.End(scene, WarehousePath, n, "지역1 내부 — 창고(민이 흔적·노크 쪽지 + 지하창고 입구)");
    }

    // ── 짙은현상 지하창고 〔랜드마크C〕 (§1.4c #11: 최고 위험·보상, 캄캄) ──
    const string BasementPath = "Assets/Scenes/Int_Basement.unity";

    [MenuItem("Tools/TopDown/빌드/내부/지하창고", priority = -86)]
    public static void BuildBasement()
    {
        var m = InteriorBuild.Begin(out var scene);
        int n = 0; const float W = 19f, H = 15f;

        // 지하는 남쪽 '문'이 아니라 **올라가는 계단**으로 창고에 복귀한다(사방 밀폐 유지).
        n += InteriorBuild.Shell(m, W, H);
        n += InteriorBuild.Stairs(m, "Exit_ToWarehouse", 5f, 2.2f, "Int_Warehouse", BuildingReturn.BackSpawnId, "위층 계단");
        n += InteriorBuild.Spawn(m, "default", 9.5f, 2.9f);

        // 붕괴 기둥 미로 — 시야 차단(하강감은 조명·연출로, 문서 전제)
        n += GreyboxBuild.WallSeg(m, "B_P1", 4f, 5f, 6f, 10f);
        n += GreyboxBuild.WallSeg(m, "B_P2", 9f, 4f, 11f, 8f);
        n += GreyboxBuild.WallSeg(m, "B_P3", 13f, 6f, 15f, 12f);
        n += GreyboxBuild.WallSeg(m, "B_P4", 6f, 11.5f, 12f, 12.5f);

        n += GreyboxBuild.Note(m, "B_Label", 9.5f, 13.5f, "루디 결정 구역 ★★★★★",
            "MQ-002 · S-013/S-015. 시계 이상·사망 시간회수 튜토. 루디 회수 후 즉시 이탈 권장.");

        // 최고 보상 — 상자 밀집
        n += InteriorBuild.Crate(m, "B_Rudi1", 7.5f, 8f);
        n += InteriorBuild.Crate(m, "B_Rudi2", 12f, 9.5f);
        n += InteriorBuild.Crate(m, "B_Crate3", 16f, 4f);
        n += InteriorBuild.Crate(m, "B_Crate4", 3f, 12.5f);
        n += InteriorBuild.GroundLoot(m, "B_G0", 10f, 6f);
        n += InteriorBuild.GroundLoot(m, "B_G1", 15f, 13f);
        n += InteriorBuild.GroundLoot(m, "B_G2", 4f, 3.5f);

        // 최고 위험 — 중장 + 근접
        n += InteriorBuild.Enemy(m, "B_EZ_T", 10f, 8f, 8f, 6f, "bandit_tank", 1);
        n += InteriorBuild.Enemy(m, "B_EZ_M", 14f, 11f, 8f, 6f, "bandit_melee_1", 2);

        n += InteriorBuild.Controller(m, ProfilePath, RegionId);
        InteriorBuild.End(scene, BasementPath, n, "지역1 내부 — 짙은현상 지하창고(최고 위험·보상)");
    }

    // ── 무너진 상가 〔랜드마크B〕 (§1.4c #10: 잔해 미로 + 최심부 생존자) ──
    const string CollapsedPath = "Assets/Scenes/Int_CollapsedMall.unity";

    [MenuItem("Tools/TopDown/빌드/내부/무너진상가", priority = -85)]
    public static void BuildCollapsedMall()
    {
        var m = InteriorBuild.Begin(out var scene);
        int n = 0; const float W = 22f, H = 18f;

        n += InteriorBuild.Shell(m, W, H);
        n += InteriorBuild.ExitDoorSouth(m, "Exit_ToZone1", 11f, Outside, BuildingReturn.BackSpawnId, 2.4f);
        n += InteriorBuild.Spawn(m, "default", 11f, 2.9f);

        // 잔해 미로 — 안쪽으로 갈수록 통로가 좁아진다(문서: "깔린 틈을 기어간다")
        n += GreyboxBuild.WallSeg(m, "C_R1", 3f, 5f, 9f, 6f);
        n += GreyboxBuild.WallSeg(m, "C_R2", 12f, 4.5f, 19f, 5.5f);
        n += GreyboxBuild.WallSeg(m, "C_R3", 5f, 8.5f, 16f, 9.5f);
        n += GreyboxBuild.WallSeg(m, "C_R4", 3f, 12f, 10f, 13f);
        n += GreyboxBuild.WallSeg(m, "C_R5", 13f, 11.5f, 19f, 12.5f);
        n += GreyboxBuild.WallSeg(m, "C_R6", 8f, 15f, 15f, 15.8f);

        n += GreyboxBuild.Note(m, "C_Survivor", 11f, 16.6f, "갇힌 생존자 ★★★",
            "SQ-001. 잔해를 걷어내며 최심부까지 — 구조 시 파견지 해금. 라디오 RN-05 리드.");

        // 깔린 점포 틈 파밍
        n += InteriorBuild.Crate(m, "C_Crate1", 5f, 7f);
        n += InteriorBuild.Crate(m, "C_Crate2", 17f, 8f);
        n += InteriorBuild.Crate(m, "C_Crate3", 6f, 14f);
        n += InteriorBuild.GroundLoot(m, "C_G0", 10f, 6.8f);
        n += InteriorBuild.GroundLoot(m, "C_G1", 15f, 13.5f);
        n += InteriorBuild.GroundLoot(m, "C_G2", 4f, 10.5f);

        n += InteriorBuild.Enemy(m, "C_EZ", 11f, 10f, 12f, 8f, "bandit_melee_1", 2);
        n += InteriorBuild.Controller(m, ProfilePath, RegionId);
        InteriorBuild.End(scene, CollapsedPath, n, "지역1 내부 — 무너진 상가(잔해 미로 + 최심부 생존자)");
    }

    // ── 밀도 점포 4종 (§1.4c #6~9: 철물점·분식집·전파상·세탁소) ──────────
    //   각 점포는 '색'이 다르다(제작재료/식량/전자/천). 루트 카테고리 차등은
    //   region_loot 테이블 확장 후속 — 지금은 크기·구조·앵커 수로 성격을 낸다.

    //   ★ 2026-07-11: 이 5개 씬은 만들어만 두고 **Zone1에서 아무도 안 가리키는 고아**였다.
    //     유니크 건물로 전부 연결하면서, 예산 배율·루트 지역을 씬마다 달리해 '성격'을 데이터로 준다.
    //     (제작재료 / 식량 / 전자 / 천 — 위험도도 적 수로 차등)

    [MenuItem("Tools/TopDown/빌드/내부/철물점", priority = -84)]
    public static void BuildHardware() => BuildDensityShop(
        "Assets/Scenes/Int_Hardware.unity", "from_hardware", 13f, 10f, "HW",
        "철물점 — 공구·부품·못(제작·수리 재료)", crates: 3, ground: 2, enemy: 1, mult: 1.3f);

    [MenuItem("Tools/TopDown/빌드/내부/분식집", priority = -83)]
    public static void BuildDiner() => BuildDensityShop(
        "Assets/Scenes/Int_Diner.unity", "from_diner", 13f, 11f, "DN",
        "분식집 — 캔푸드·물(주방 뒤 창고)", crates: 2, ground: 3, enemy: 0, backRoom: true, mult: 1.0f);

    [MenuItem("Tools/TopDown/빌드/내부/컴퓨터가게", priority = -82)]
    public static void BuildElectronics() => BuildDensityShop(
        "Assets/Scenes/Int_Electronics.unity", "from_electronics", 12f, 10f, "EL",
        "컴퓨터가게 — 배터리·전선·전자부품(라디오/발전기 업글 재료)", crates: 3, ground: 2, enemy: 2,
        mult: 1.4f, lootRegion: "industrial");

    [MenuItem("Tools/TopDown/빌드/내부/세탁소", priority = -81)]
    public static void BuildLaundry() => BuildDensityShop(
        "Assets/Scenes/Int_Laundry.unity", "from_laundry", 11f, 9f, "LD",
        "세탁소 — 천·의류(방한·붕대 재료)", crates: 2, ground: 2, enemy: 0, mult: 0.9f);

    [MenuItem("Tools/TopDown/빌드/내부/골목점포", priority = -80)]
    public static void BuildAlleyShop() => BuildDensityShop(
        "Assets/Scenes/Int_AlleyShop.unity", "from_alleyshop", 9f, 7f, "AS",
        "골목 점포 — 잡템 1~2(약국 곁가지)", crates: 1, ground: 2, enemy: 0, mult: 0.9f);

    /// <summary>밀도 점포 공용 — 껍데기+진입/출구+선반+앵커. 성격은 크기·앵커 수·뒷방 + **예산 배율·루트 지역**으로.</summary>
    static void BuildDensityShop(string path, string returnSpawn, float W, float H, string pre,
                                 string label, int crates, int ground, int enemy, bool backRoom = false,
                                 float mult = 1f, string lootRegion = null)
    {
        var m = InteriorBuild.Begin(out var scene);
        int n = 0;

        n += InteriorBuild.Shell(m, W, H);
        n += InteriorBuild.ExitDoorSouth(m, "Exit_ToZone1", W * 0.5f, Outside, BuildingReturn.BackSpawnId, 2.2f);
        n += InteriorBuild.Spawn(m, "default", W * 0.5f, 2.9f);

        // 진열 선반 2열
        n += GreyboxBuild.WallSeg(m, $"{pre}_Shelf1", 2f, H * 0.42f, W * 0.45f, H * 0.42f + 0.8f);
        n += GreyboxBuild.WallSeg(m, $"{pre}_Shelf2", W * 0.55f, H * 0.62f, W - 2f, H * 0.62f + 0.8f);

        if (backRoom)   // 주방 뒤 창고(분식집) — 칸막이 + 문 갭
        {
            float dy = H - 3.5f;
            n += GreyboxBuild.WallSeg(m, $"{pre}_Div_a", 1f, dy, W * 0.4f, dy + 0.9f);
            n += GreyboxBuild.WallSeg(m, $"{pre}_Div_b", W * 0.62f, dy, W - 1f, dy + 0.9f);
        }

        for (int i = 0; i < crates; i++)
            n += InteriorBuild.Crate(m, $"{pre}_Crate{i}", 2.5f + i * (W - 5f) / Mathf.Max(1, crates), H - 2.2f);
        for (int i = 0; i < ground; i++)
            n += InteriorBuild.GroundLoot(m, $"{pre}_G{i}", 3f + i * (W - 6f) / Mathf.Max(1, ground), H * 0.5f);

        if (enemy > 0)
            n += InteriorBuild.Enemy(m, $"{pre}_EZ", W * 0.5f, H * 0.55f, W - 4f, H * 0.4f, "bandit_melee_1", enemy);

        n += InteriorBuild.Controller(m, ProfilePath, RegionId, mult, lootRegion);
        InteriorBuild.End(scene, path, n, "지역1 내부 — " + label);
    }

    // ── 보석상 〔유니크·코드 금고〕 ───────────────────────────────────────
    //   2026-07-11 사용자: "보석상은 비밀번호인데 비밀번호는 어디에 따로 있다던가 … 쪽지 파밍이지"
    //   매장(남) + **금고실(북, 코드 잠금)**. 번호는 경찰서 압수품에서 나온다.
    //   번호를 몰라도 강제 개방은 가능 — 대신 오래 걸리고 시끄럽다(알아낸 쪽이 항상 이득).
    public const string JewelryKnowledge = "code_jewelry_vault";
    const string JewelryPath = "Assets/Scenes/Int_Jewelry.unity";

    [MenuItem("Tools/TopDown/빌드/내부/보석상", priority = -77)]
    public static void BuildJewelry()
    {
        var m = InteriorBuild.Begin(out var scene);
        int n = 0; const float W = 20f, H = 15f;

        n += InteriorBuild.Shell(m, W, H);
        n += InteriorBuild.ExitDoorSouth(m, "Exit_ToZone1", 10f, Outside, BuildingReturn.BackSpawnId, 2.2f);
        n += InteriorBuild.Spawn(m, "default", 10f, 2.9f);

        // 매장 — 진열장(선반) + 깨진 케이스
        n += GreyboxBuild.WallSeg(m, "JW_Case1", 2f, 5f, 8f, 5.9f);
        n += GreyboxBuild.WallSeg(m, "JW_Case2", 12f, 5f, 18f, 5.9f);
        n += InteriorBuild.GroundLoot(m, "JW_G0", 5f, 7.5f);
        n += InteriorBuild.GroundLoot(m, "JW_G1", 15f, 7.5f);
        n += InteriorBuild.Crate(m, "JW_Counter", 10f, 7.5f);

        // 금고실 칸막이(y=9) — 문 갭 x9~11이 **금고문**
        n += GreyboxBuild.WallSeg(m, "JW_Div_a", 1f, 9f, 9f, 10f);
        n += GreyboxBuild.WallSeg(m, "JW_Div_b", 11f, 9f, W - 1f, 10f);
        n += InteriorBuild.Gate(m, "JW_Vault", 10f, 9.5f, 2f, 1f,
                                BlockedPassage.Mode.Code, "금고문", JewelryKnowledge,
                                "번호를 모른다. 경찰이 압수해 뒀다는 소문이 있었는데.");

        // 금고 안 — 최고 보상
        n += InteriorBuild.Crate(m, "JW_Vault1", 5f, 12.5f);
        n += InteriorBuild.Crate(m, "JW_Vault2", 10f, 12.5f);
        n += InteriorBuild.Crate(m, "JW_Vault3", 15f, 12.5f);
        n += InteriorBuild.GroundLoot(m, "JW_G2", 12.5f, 11f);

        // 위험도 — 매장에 근접 2, 금고 앞 견제 1
        n += InteriorBuild.Enemy(m, "JW_EZ", 10f, 6.5f, 12f, 5f, "bandit_melee_1", 2);
        n += InteriorBuild.Enemy(m, "JW_EZ_T", 16f, 12f, 5f, 4f, "bandit_tank", 1);

        n += InteriorBuild.Controller(m, ProfilePath, RegionId, 2.2f, "entertainment");
        InteriorBuild.End(scene, JewelryPath, n, "지역1 내부 — 보석상(코드 금고, 최고가 루트)");
    }

    // ── 경찰서 〔유니크·열쇠 무기고〕 ────────────────────────────────────
    //   사용자: "경찰서 같은 건 뒷문이 잠겨 있고 나중에 열쇠로 열고 하는 등의 인터랙티브도 좋고"
    //   로비(남, 자유 진입) + **무기고(북, 열쇠 잠금)**. 로비 압수품함에 보석상 금고 번호 쪽지.
    const string PolicePath = "Assets/Scenes/Int_Police.unity";
    public const string PoliceArmoryKey = "key_police_armory";

    [MenuItem("Tools/TopDown/빌드/내부/경찰서", priority = -76)]
    public static void BuildPolice()
    {
        var m = InteriorBuild.Begin(out var scene);
        int n = 0; const float W = 22f, H = 17f;

        n += InteriorBuild.Shell(m, W, H);
        n += InteriorBuild.ExitDoorSouth(m, "Exit_ToZone1", 11f, Outside, BuildingReturn.BackSpawnId, 2.4f);
        n += InteriorBuild.Spawn(m, "default", 11f, 2.9f);

        // 로비 — 민원 데스크 + 사물함
        n += GreyboxBuild.WallSeg(m, "PL_Desk", 3f, 6f, 12f, 7f);
        n += InteriorBuild.Crate(m, "PL_Locker1", 17f, 5f);
        n += InteriorBuild.Crate(m, "PL_Locker2", 19.5f, 8f);
        n += InteriorBuild.GroundLoot(m, "PL_G0", 6f, 4f);
        n += InteriorBuild.GroundLoot(m, "PL_G1", 14f, 9f);

        // ★ 압수품 보관함 옆 쪽지 = **보석상 금고 번호**(쪽지 파밍으로 코드 획득)
        n += InteriorBuild.Crate(m, "PL_Evidence", 4f, 9.5f);
        n += InteriorBuild.KnowledgeNote(m, "PL_Note_Code", 6.5f, 9.5f,
            "압수품 대장",
            "…압수: 보석상 금고 개방번호. 대장 여백에 급히 갈겨쓴 네 자리 숫자가 보인다.\n" +
            "외워 뒀다. 이제 그 금고는 열 수 있다.",
            JewelryKnowledge);

        // 무기고 칸막이(y=11) — 문 갭 x10~12가 **뒷문(열쇠)**
        n += GreyboxBuild.WallSeg(m, "PL_Div_a", 1f, 11f, 10f, 12f);
        n += GreyboxBuild.WallSeg(m, "PL_Div_b", 12f, 11f, W - 1f, 12f);
        n += InteriorBuild.Gate(m, "PL_Armory", 11f, 11.5f, 2f, 1f,
                                BlockedPassage.Mode.Locked, "무기고 뒷문", PoliceArmoryKey,
                                "잠겨 있다. 열쇠는 누가 가져갔을까.");

        // 무기고 — 무기·방어구 집중
        n += InteriorBuild.Crate(m, "PL_Arm1", 5f, 14.5f);
        n += InteriorBuild.Crate(m, "PL_Arm2", 11f, 14.5f);
        n += InteriorBuild.Crate(m, "PL_Arm3", 17f, 14.5f);
        n += InteriorBuild.GroundLoot(m, "PL_G2", 8f, 13f);

        // 위험도 최상급 — 근접 2 + 견제 1 + 중장 1
        n += InteriorBuild.Enemy(m, "PL_EZ", 11f, 7f, 14f, 6f, "bandit_melee_1", 2);
        n += InteriorBuild.Enemy(m, "PL_EZ_M2", 18f, 14f, 6f, 4f, "bandit_melee_1", 1);
        n += InteriorBuild.Enemy(m, "PL_EZ_T", 6f, 14f, 6f, 4f, "bandit_tank", 1);

        n += InteriorBuild.Controller(m, ProfilePath, RegionId, 1.8f, "industrial");
        InteriorBuild.End(scene, PolicePath, n, "지역1 내부 — 경찰서(열쇠 무기고 + 보석상 코드 쪽지)");
    }

    // ── 공용 내부 (그 외 모든 건물이 임시로 돌려 쓰는 1채) ────────────────
    //   2026-07-11 사용자 결정: "당장 없다면 임의로 1개 돌려 쓰고 나중에 확장".
    //   Zone1의 절차 생성 점포(Shops/Plaza 등) 전부가 이 씬을 가리킨다.
    //   복귀 위치는 고정 스폰이 아니라 **들어온 문 앞**으로 되돌린다(BuildingReturn).
    public const string GenericPath = "Assets/Scenes/Int_Generic.unity";

    // ── 식물원 돔 금고실 (외부에서 이전 — key_dome_code 최고 보상) ────────
    const string DomePath = "Assets/Scenes/Int_Dome.unity";

    [MenuItem("Tools/TopDown/빌드/내부/돔금고실", priority = -78)]
    public static void BuildDome()
    {
        var m = InteriorBuild.Begin(out var scene);
        int n = 0; const float W = 16f, H = 14f;

        n += InteriorBuild.Shell(m, W, H);
        n += InteriorBuild.ExitDoorSouth(m, "Exit_ToZone1", 8f, Outside, BuildingReturn.BackSpawnId, 2.2f);
        n += InteriorBuild.Spawn(m, "default", 8f, 2.9f);

        n += GreyboxBuild.Note(m, "D_Label", 8f, 12.5f, "돔 금고실 ★★★★",
            "key_dome_code로 여는 최고 보상 구역. 유리타워 상층 R&D에서 코드 획득.");

        // 외부에 있던 Dome_Reward / Dome_RareA / Dome_RareB를 이곳으로 이전
        n += InteriorBuild.Crate(m, "D_Reward", 8f, 8f);
        n += InteriorBuild.Crate(m, "D_RareA", 4f, 6f);
        n += InteriorBuild.Crate(m, "D_RareB", 12f, 6f);
        n += InteriorBuild.GroundLoot(m, "D_G0", 6f, 10f);
        n += InteriorBuild.GroundLoot(m, "D_G1", 11f, 10f);

        n += InteriorBuild.Enemy(m, "D_EZ", 8f, 8f, 10f, 8f, "bandit_tank", 1);
        n += InteriorBuild.Controller(m, ProfilePath, RegionId);
        InteriorBuild.End(scene, DomePath, n, "지역1 내부 — 돔 금고실(최고 보상, key_dome_code)");
    }

    [MenuItem("Tools/TopDown/빌드/내부/공용점포", priority = -79)]
    public static void BuildGeneric() => BuildDensityShop(
        GenericPath, BuildingReturn.BackSpawnId, 12f, 9f, "GN",
        "공용 점포(임시 — 모든 미제작 건물 공용, 후속 확장)", crates: 2, ground: 2, enemy: 1);

    [MenuItem("Tools/TopDown/빌드/내부/── 전부 ──", priority = -70)]
    public static void BuildAll()
    {
        BuildPharmacy();  BuildAbandonedShop(); BuildGarage();      BuildWarehouse();
        BuildBasement();  BuildCollapsedMall(); BuildHardware();    BuildDiner();
        BuildElectronics(); BuildLaundry();     BuildAlleyShop();   BuildDome();
        BuildJewelry();   BuildPolice();
        BuildGeneric();
        Debug.Log("[Zone1Interiors] 내부 씬 15개 생성 완료(건물 14채 + 공용 1).");
    }
}
#endif
