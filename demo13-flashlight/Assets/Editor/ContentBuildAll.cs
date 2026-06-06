#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 안전가옥+레이드 그레이박스 콘텐츠를 한 번에 빌드. (개별 빌더 4개를 올바른 순서로 호출)
///   1) Build Safehouse NPCs   — NPCData/ShopData (먼저: 안전가옥이 storyNpcId로 자동연결)
///   2) Build Safehouse        — Safehouse.unity (NPC 연결 + 은신처 입구→Hideout)
///   3) Build Hideout          — Hideout.unity (+빌드세팅)
///   4) Build ScrapMarket      — ScrapMarket_GB.unity (RaidManager+탈출+빌드세팅)
/// 각 빌더의 완료 팝업은 Quiet로 끄고, 마지막에 요약 1개만 띄움.
///
/// 메뉴: Tools ▸ TopDown ▸ Build ▸ ▶ ALL Content (NPC+Safehouse+Hideout+Raid)
/// </summary>
public static class ContentBuildAll
{
    /// <summary>true면 개별 빌더가 완료 다이얼로그를 띄우지 않음(일괄 빌드 중).</summary>
    public static bool Quiet;

    [MenuItem("Tools/TopDown/Build/▶ ALL Content (NPC+Safehouse+Hideout+Raid)", priority = -100)]
    public static void BuildAll()
    {
        // 현재 열린 씬에 미저장 변경이 있으면 한 번만 물어봄(이후 NewScene들은 조용히 진행).
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Quiet = true;
        try
        {
            SafehouseNpcBuilder.Build();        // 1) NPC 데이터 먼저
            SafehouseGreyboxLayout.Build();     // 2) 안전가옥 (NPC 자동연결)
            HideoutGreyboxLayout.Build();       // 3) 컨테이너 실내
            ScrapMarketGreyboxLayout.Build();   // 4) 폐상가 레이드맵
        }
        catch (System.Exception e)
        {
            Quiet = false;
            Debug.LogError("[BuildAll] 일괄 빌드 중 오류: " + e);
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Build All Content", "오류로 중단:\n" + e.Message, "확인");
            return;
        }
        finally { Quiet = false; }

        AssetDatabase.SaveAssets();
        Debug.Log("<color=cyan>[BuildAll]</color> 일괄 생성 완료 — NPC + Safehouse + Hideout + ScrapMarket_GB.");

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Build All Content",
                "일괄 생성 완료:\n" +
                "  • NPCData 3 + ShopData(전당포)\n" +
                "  • Safehouse.unity (NPC 자동연결 + 은신처 입구)\n" +
                "  • Hideout.unity (컨테이너 실내)\n" +
                "  • ScrapMarket_GB.unity (RaidManager + 맨홀 탈출)\n\n" +
                "이제 Systems.unity 열고 Play → 한 바퀴 돌아갑니다.", "확인");
    }
}
#endif
