#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 기존 맵 빌더를 **3D로 돌리는 진입점**.
///
/// 맵 빌더(지역1 2223줄, 고철시장 500줄, 실내 15씬)는 전부 <see cref="GreyboxBuild"/> 위에
/// 서 있다. 그래서 빌더를 고치는 대신 프리미티브 계층만 3D로 바꾸고(<see cref="Greybox3D"/>),
/// 여기서 <see cref="GreyboxBuild.Use3D"/>를 켠 채 같은 빌더를 부른다.
///
/// 이렇게 하면 2D 빌더 코드가 한 줄도 바뀌지 않는다 — 아직 3D로 안 옮긴 맵은 예전 메뉴로
/// 그대로 2D를 뽑을 수 있고, 두 결과를 나란히 비교할 수 있다.
/// ⚠️ 3D로 뽑으면 **같은 씬 파일을 덮어쓴다.** 되돌리려면 2D 메뉴로 다시 뽑으면 된다.
///
/// 설계: docs/3d-migration.md Stage 3
/// </summary>
public static class Map3DBuild
{
    /// <summary>3D 모드로 <paramref name="build"/>를 실행한다. 예외가 나도 모드를 반드시 되돌린다 —
    /// 켜진 채로 남으면 그다음 2D 빌드까지 3D로 나온다.</summary>
    static void In3D(System.Action build, Greybox3D.HeightSet heights, float planScale = 1f)
    {
        var prevUse = GreyboxBuild.Use3D;
        var prevH   = Greybox3D.Heights;
        var prevS   = Greybox3D.PlanScale;
        var prevQ   = ContentBuildAll.Quiet;
        // ⚠️ 조용히 굽는다. 빌더는 씬마다 완료 대화상자를 띄우는데, 자동화에는 누를 사람이
        //    없어 메인 스레드가 그대로 멈춘다(실제로 여러 번 밟았다).
        ContentBuildAll.Quiet = true;
        GreyboxBuild.Use3D = true;
        Greybox3D.EnsurePalette();
        Greybox3D.Heights = heights;
        Greybox3D.PlanScale = planScale;
        try { build(); }
        finally
        {
            GreyboxBuild.Use3D = prevUse; Greybox3D.Heights = prevH;
            Greybox3D.PlanScale = prevS; ContentBuildAll.Quiet = prevQ;
        }
    }

    [MenuItem("Tools/TopDown/빌드3D/실내 전체(15씬)", priority = -80)]
    public static void BuildInteriorsAll() => In3D(Zone1Interiors.BuildAll, Greybox3D.Indoor);

    [MenuItem("Tools/TopDown/빌드3D/실내 · 약국", priority = -79)]
    public static void BuildPharmacy() => In3D(Zone1Interiors.BuildPharmacy, Greybox3D.Indoor);

    [MenuItem("Tools/TopDown/빌드3D/고철시장", priority = -70)]
    public static void BuildScrapMarket() => In3D(ScrapMarketGreyboxLayout.Build, Greybox3D.Default, OutdoorScale);

    [MenuItem("Tools/TopDown/빌드3D/지역1", priority = -69)]
    public static void BuildZone1() => In3D(Zone1GreyboxLayout.Build, Greybox3D.Default, OutdoorScale);

    /// <summary>지역1·고철시장에 쓰는 평면 배율.
    /// 2D 시절 건물 상한(MaxSpan 13m)은 스프라이트 아트 제약이라 실내를 별도 씬(최대 22×18m)으로
    /// 뺐다. 3D엔 그 제약이 없으므로 맵째로 키워 실내를 건물 안에 담는다 —
    /// 건물 13→22m, 맵 160×168→272×286m.</summary>
    public const float OutdoorScale = 1.7f;
}
#endif
