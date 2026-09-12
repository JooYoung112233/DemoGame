#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 기존 맵 빌더를 **3D로 돌리는 진입점**.
///
/// 지역1 빌더(<see cref="Zone1GreyboxLayout"/>, 튜토 구역은 <see cref="ScrapMarketGreyboxLayout.Place"/>)는
/// <see cref="GreyboxBuild"/> 위에 서 있고, 프리미티브는 <see cref="Greybox3D"/>가 3D로 짓는다.
/// 여기서 높이·평면 배율·조명 프리셋을 정한 뒤 빌더를 부른다.
/// ⚠️ 뽑으면 **같은 씬 파일을 덮어쓴다.**
///
/// 2026-09-12 시스템 정리 5단계: 2D 경로·2D 메뉴·고철시장 단독 씬 삭제 — 이제 3D가 유일한 빌드다.
/// 설계: docs/3d-migration.md Stage 3
/// </summary>
public static class Map3DBuild
{
    /// <summary><paramref name="build"/>를 실행한다. 예외가 나도 높이·배율·프리셋을 반드시 되돌린다.</summary>
    static void In3D(System.Action build, Greybox3D.HeightSet heights, float planScale = 1f,
                     Lighting3D.Preset preset = Lighting3D.Preset.Outdoor)
    {
        var prevH   = Greybox3D.Heights;
        var prevS   = Greybox3D.PlanScale;
        var prevP   = Greybox3D.ScenePreset;
        var prevQ   = ContentBuildAll.Quiet;
        // ⚠️ 조용히 굽는다. 빌더는 씬마다 완료 대화상자를 띄우는데, 자동화에는 누를 사람이
        //    없어 메인 스레드가 그대로 멈춘다(실제로 여러 번 밟았다).
        ContentBuildAll.Quiet = true;
        Greybox3D.EnsurePalette();
        Greybox3D.Heights = heights;
        Greybox3D.PlanScale = planScale;
        Greybox3D.ScenePreset = preset;
        try { build(); }
        finally
        {
            Greybox3D.Heights = prevH;
            Greybox3D.PlanScale = prevS; Greybox3D.ScenePreset = prevP;
            ContentBuildAll.Quiet = prevQ;
        }
    }

    [MenuItem("Tools/TopDown/빌드3D/지역1", priority = -69)]
    public static void BuildZone1() => In3D(Zone1GreyboxLayout.Build, Greybox3D.Default, OutdoorScale);

    /// <summary>지역1·고철시장에 쓰는 평면 배율.
    /// 2D 시절 건물 상한(MaxSpan 13m)은 스프라이트 아트 제약이라 실내를 별도 씬(최대 22×18m)으로
    /// 뺐다. 3D엔 그 제약이 없으므로 맵째로 키워 실내를 건물 안에 담는다 —
    /// 건물 13→22m, 맵 160×168→272×286m.</summary>
    public const float OutdoorScale = 1.7f;
}
#endif
