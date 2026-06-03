using UnityEditor;
using UnityEngine;

/// <summary>
/// 선택한 프롭(SpriteRenderer)에 "씬 편집 자동연동" 컴포넌트를 붙여주는 메뉴.
/// - ColliderAutoFit: BoxCollider2D가 스프라이트/Tiled 크기를 따라가게.
/// - GroundShadow2D: 발밑 그림자(이미 자체 라이브 동기화 — 없는 손제작 벽에 부착용).
/// 맵툴로 박은 게 아니라 씬에서 직접 만든/수정한 벽에 일괄로 붙일 때 편함.
/// </summary>
public static class PropSyncMenu
{
    const string Root = "Tools/TopDown/Map/";

    [MenuItem(Root + "선택에 Collider Auto-Fit 부착", false, 200)]
    static void AddColliderAutoFit()
    {
        int n = 0;
        foreach (var go in Selection.gameObjects)
        {
            if (go == null) continue;
            if (go.GetComponent<SpriteRenderer>() == null) continue;
            if (go.GetComponent<BoxCollider2D>() == null)
            {
                Debug.LogWarning($"[PropSync] '{go.name}'에 BoxCollider2D가 없어 건너뜀(Box 전용).");
                continue;
            }
            if (go.GetComponent<ColliderAutoFit>() != null) continue;
            Undo.AddComponent<ColliderAutoFit>(go);
            n++;
        }
        Debug.Log($"[PropSync] ColliderAutoFit {n}개 부착.");
    }

    [MenuItem(Root + "선택에 GroundShadow 부착", false, 201)]
    static void AddGroundShadow()
    {
        int n = 0;
        foreach (var go in Selection.gameObjects)
        {
            if (go == null || go.GetComponent<SpriteRenderer>() == null) continue;
            if (go.GetComponent<GroundShadow2D>() != null) continue;
            Undo.AddComponent<GroundShadow2D>(go);
            n++;
        }
        Debug.Log($"[PropSync] GroundShadow2D {n}개 부착(자체 라이브 동기화).");
    }

    [MenuItem(Root + "선택에 Collider Auto-Fit 부착", true)]
    [MenuItem(Root + "선택에 GroundShadow 부착", true)]
    static bool ValidateHasSelection() => Selection.gameObjects.Length > 0;
}
