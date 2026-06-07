#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 에디터 씬 빌더 공용 유틸 — '현재 열린 씬을 닫지 않고' 새 씬을 폴더에만 생성.
///
/// 기존: NewScene(..., Single) → 작업 중이던 씬이 닫히고 새 씬이 열림(자동 로드).
/// 변경: NewScene(..., Additive)로 빈 씬을 추가한 뒤 잠깐 active로 전환(→ new GameObject가 새 씬에 생성됨),
///       저장 후 그 씬을 닫고(언로드) 원래 active 씬을 복구. 결과: 씬 파일만 생성, 현재 씬은 그대로.
///
/// 사용:
///   var scene = EditorSceneBuildUtil.NewDetachedScene(out var prevActive);
///   ... new GameObject(...) 등으로 콘텐츠 구성 ...
///   bool saved = EditorSceneBuildUtil.SaveAndClose(scene, path, prevActive);
/// </summary>
public static class EditorSceneBuildUtil
{
    /// <summary>현재 열린 씬을 유지한 채 새 빈 씬을 additive로 만들고 active로 전환. prevActive는 복구용.</summary>
    public static Scene NewDetachedScene(out Scene prevActive)
    {
        prevActive = EditorSceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);   // 이후 new GameObject / InstantiatePrefab(부모 없음)이 이 씬에 들어감
        return scene;
    }

    /// <summary>씬을 path에 저장한 뒤 닫고(언로드) 원래 active 씬을 복구. 저장 성공 여부 반환.</summary>
    public static bool SaveAndClose(Scene scene, string path, Scene prevActive)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, path);
        if (scene.IsValid() && scene.isLoaded)
            EditorSceneManager.CloseScene(scene, true);   // 하이어라키에서 제거(언로드) — 원래 씬은 유지
        if (prevActive.IsValid() && prevActive.isLoaded)
            EditorSceneManager.SetActiveScene(prevActive);
        return saved;
    }
}
#endif
