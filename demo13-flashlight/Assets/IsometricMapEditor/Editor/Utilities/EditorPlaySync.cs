using UnityEngine;
using UnityEditor;
using IsometricMapEditor;

namespace IsometricMapEditor.Editor
{
    /// <summary>
    /// 맵 에디터 전용 Play 동기화.
    /// 씬에 이미 bake된 맵이 있으면 아무것도 안 함.
    /// 맵 에디터 창에서 직접 "Play Test" 버튼으로만 부트스트랩 생성.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorPlaySync
    {
        static EditorPlaySync()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            // bake된 프리팹을 직접 사용 — Play 시 맵툴 간섭 없음
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                // Edit 모드 복귀 시 프리뷰 복원
                LivePreviewManager.InvalidateTracking();
            }
        }
    }
}
