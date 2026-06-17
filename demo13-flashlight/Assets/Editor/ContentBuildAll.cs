#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 안전구역(홈 베이스) 그레이박스를 한 번에 빌드 — NPC + Safehouse + Hideout 만.
///   1) Build Safehouse NPCs   — NPCData/ShopData (먼저: 안전가옥이 storyNpcId로 자동연결)
///   2) Build Safehouse        — Safehouse.unity (NPC 연결 + 은신처 입구→Hideout)
///   3) Build Hideout          — Hideout.unity (+빌드세팅)
/// ※ 지역(레이드맵)은 안전구역과 분리 — 각 지역 빌더로(예: Build ScrapMarket Greybox Layout). 여기 안 섞음.
/// 각 빌더의 완료 팝업은 Quiet로 끄고, 마지막에 요약 1개만 띄움.
///
/// 메뉴: Tools ▸ TopDown ▸ Build ▸ ▶ ALL Content (NPC+Safehouse+Hideout+Raid)
/// </summary>
public static class ContentBuildAll
{
    /// <summary>true면 개별 빌더가 완료 다이얼로그를 띄우지 않음(일괄 빌드 중).</summary>
    public static bool Quiet;

    [MenuItem("Tools/TopDown/빌드/안전구역", priority = -100)]
    public static void BuildAll()
    {
        // 빌더들이 additive로 씬을 만들어 현재 열린 씬을 닫지 않으므로(폴더에만 생성) 저장 프롬프트 불필요.
        Quiet = true;
        try
        {
            SafehouseNpcBuilder.Build();        // 1) NPC 데이터 먼저
            SafehouseGreyboxLayout.Build();     // 2) 안전가옥 (NPC 자동연결)
            HideoutGreyboxLayout.Build();       // 3) 컨테이너 실내
            // ※ 지역(레이드맵)은 분리 — 'Build ScrapMarket Greybox Layout' 등 각 지역 빌더로 따로 빌드.
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
        Debug.Log("<color=cyan>[SafeZone]</color> 안전구역 일괄 생성 완료 — NPC + Safehouse + Hideout. (지역은 각 지역 빌더로 따로)");

        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Build Safe Zone",
                "안전구역 일괄 생성 완료:\n" +
                "  • NPCData 3 + ShopData(전당포)\n" +
                "  • Safehouse.unity (NPC 자동연결 + 은신처 입구)\n" +
                "  • Hideout.unity (컨테이너 실내)\n\n" +
                "지역(레이드맵)은 분리 — 'Build ScrapMarket Greybox Layout' 등 각 지역 빌더로.\n" +
                "Systems.unity 열고 Play.", "확인");
    }
}
#endif
