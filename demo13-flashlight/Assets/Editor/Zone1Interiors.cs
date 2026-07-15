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

        n += InteriorBuild.Shell(m, W, H);
        n += InteriorBuild.DoorGapSouth(m, W, 9f, 2.4f);          // 남쪽 정문
        n += InteriorBuild.Spawn(m, "default", 9f, 2.2f);
        n += InteriorBuild.Exit(m, "Exit_ToZone1", 9f, 1.4f, Outside, "from_pharmacy", 2.4f, 1.2f);

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
        n += InteriorBuild.DoorGapSouth(m, W, 6f, 2.2f);
        n += InteriorBuild.Spawn(m, "default", 6f, 2.2f);
        n += InteriorBuild.Exit(m, "Exit_ToZone1", 6f, 1.4f, Outside, "from_abshop", 2.2f, 1.2f);

        // 선반 2 + 상자 1 (문서 사양)
        n += GreyboxBuild.WallSeg(m, "S_Shelf1", 2f, 4f, 6f, 4.8f);
        n += GreyboxBuild.WallSeg(m, "S_Shelf2", 7f, 6f, 10f, 6.8f);
        n += InteriorBuild.Crate(m, "S_Crate", 3f, 7f);
        n += InteriorBuild.GroundLoot(m, "S_G0", 8f, 3.2f);
        n += InteriorBuild.GroundLoot(m, "S_G1", 5f, 5.6f);

        n += InteriorBuild.Controller(m, ProfilePath, RegionId);
        InteriorBuild.End(scene, AbShopPath, n, "지역1 내부 — 폐상점(첫 파밍 학습: 선반2·상자1, 적 없음)");
    }

    [MenuItem("Tools/TopDown/빌드/내부/── 전부 ──", priority = -80)]
    public static void BuildAll()
    {
        BuildPharmacy();
        BuildAbandonedShop();
        Debug.Log("[Zone1Interiors] 내부 씬 전부 생성 완료. (나머지 9채는 같은 헬퍼로 이어서 추가)");
    }
}
#endif
