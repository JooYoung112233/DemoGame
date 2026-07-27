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
    /// <summary>해당 경로의 씬이 이미 열려 있으면 닫는다(언로드). 재빌드 전 호출 — 저장 충돌·중복 라이트 경고 방지.</summary>
    public static void CloseSceneIfOpen(string path)
    {
        var s = EditorSceneManager.GetSceneByPath(path);
        if (s.IsValid() && s.isLoaded)
            EditorSceneManager.CloseScene(s, true);
    }

    /// <summary>현재 열린 씬을 유지한 채 새 빈 씬을 additive로 만들고 active로 전환. prevActive는 복구용.</summary>
    /// <remarks>2026-07-11: **무제(untitled) 미저장 씬**이 열려 있으면 Unity가 additive 생성을 거부한다
    /// (`InvalidOperationException: Cannot create a new scene additively with an untitled scene unsaved`).
    /// 빌더를 돌릴 때 흔히 밟는 상황이라, 먼저 그 상태를 감지해 정리하고 진행한다.</remarks>
    public static Scene NewDetachedScene(out Scene prevActive)
    {
        EnsureNoUntitledUnsavedScene();
        prevActive = EditorSceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);   // 이후 new GameObject / InstantiatePrefab(부모 없음)이 이 씬에 들어감
        return scene;
    }

    /// <summary>열린 씬 중 '경로 없는(무제) 미저장' 씬이 있으면 정리한다. 없으면 아무것도 안 함.
    /// 내용이 있으면 사용자에게 저장/버림을 묻고, 빈 무제 씬이면 조용히 새 씬으로 대체한다.</summary>
    static void EnsureNoUntitledUnsavedScene()
    {
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var s = EditorSceneManager.GetSceneAt(i);
            if (!s.IsValid() || !string.IsNullOrEmpty(s.path)) continue;   // 저장된 씬은 대상 아님

            bool hasContent = s.rootCount > 0;
            if (!hasContent && !s.isDirty)
            {
                // 완전히 빈 무제 씬 — Single로 새 씬을 열어 조용히 치운다(작업 손실 없음).
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                return;
            }

            // 내용이 있는 무제 씬 = 사용자가 뭔가 만들던 중일 수 있음 → 임의로 버리지 않는다.
            int choice = EditorUtility.DisplayDialogComplex(
                "저장되지 않은 '무제' 씬",
                "빌더는 새 씬을 추가로 열어야 하는데, 저장되지 않은 무제 씬이 있으면 Unity가 이를 거부합니다.\n\n" +
                "무제 씬을 어떻게 할까요?",
                "저장하고 계속", "버리고 계속", "취소");

            if (choice == 0)        // 저장하고 계속
            {
                if (!EditorSceneManager.SaveScene(s))
                    throw new System.OperationCanceledException("[Builder] 무제 씬 저장이 취소되어 빌드를 중단합니다.");
            }
            else if (choice == 1)   // 버리고 계속
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            else                    // 취소
            {
                throw new System.OperationCanceledException("[Builder] 사용자가 취소했습니다.");
            }
            return;
        }
    }

    /// <summary>씬을 path에 저장한 뒤 닫고(언로드) 원래 active 씬을 복구. 저장 성공 여부 반환.</summary>
    public static bool SaveAndClose(Scene scene, string path, Scene prevActive)
    {
        // 같은 경로의 씬이 이미 열려 있으면 "Overwriting the same path as another open scene" 에러 →
        // 먼저 닫는다(재빌드라 기존 내용은 어차피 덮어씀). 닫을 씬이 복구 대상이면 복구에서 제외.
        var existing = EditorSceneManager.GetSceneByPath(path);
        if (existing.IsValid() && existing.isLoaded && existing != scene)
        {
            if (prevActive == existing) prevActive = default;
            EditorSceneManager.CloseScene(existing, true);
        }

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
