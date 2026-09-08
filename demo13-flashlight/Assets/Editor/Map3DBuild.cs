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
    static void In3D(System.Action build, Greybox3D.HeightSet heights)
    {
        var prevUse = GreyboxBuild.Use3D;
        var prevH   = Greybox3D.Heights;
        GreyboxBuild.Use3D = true;
        Greybox3D.EnsurePalette();
        Greybox3D.Heights = heights;
        try { build(); }
        finally { GreyboxBuild.Use3D = prevUse; Greybox3D.Heights = prevH; }
    }

    [MenuItem("Tools/TopDown/빌드3D/실내 전체(15씬)", priority = -80)]
    public static void BuildInteriorsAll() => In3D(Zone1Interiors.BuildAll, Greybox3D.Indoor);

    [MenuItem("Tools/TopDown/빌드3D/실내 · 약국", priority = -79)]
    public static void BuildPharmacy() => In3D(Zone1Interiors.BuildPharmacy, Greybox3D.Indoor);

    [MenuItem("Tools/TopDown/빌드3D/고철시장", priority = -70)]
    public static void BuildScrapMarket() => In3D(ScrapMarketGreyboxLayout.Build, Greybox3D.Default);

    [MenuItem("Tools/TopDown/빌드3D/지역1", priority = -69)]
    public static void BuildZone1() => In3D(Zone1GreyboxLayout.Build, Greybox3D.Default);
}
#endif
