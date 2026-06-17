using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>짙은현상 구간 배치 도구. (Tools ▸ TopDown ▸ Build ▸ Anomaly Zone)</summary>
public static class AnomalyTools
{
    [MenuItem("Tools/TopDown/개발/이상현상 구역 배치")]
    static void PlaceZone()
    {
        var go = new GameObject("AnomalyZone");
        go.AddComponent<AnomalyZone>();

        var sv = SceneView.lastActiveSceneView;
        Vector3 p = sv != null ? sv.pivot : Vector3.zero;
        go.transform.position = new Vector3(p.x, p.y, 0f);

        // 씬에 매니저 하나 보장(런타임엔 AnomalyZone.OnEnable이 자동 생성하지만, 씬 배치본은 인스펙터 조절 가능)
        if (Object.FindFirstObjectByType<AnomalyManager>() == null)
            new GameObject("AnomalyManager").AddComponent<AnomalyManager>();

        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Place AnomalyZone");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("[Anomaly] AnomalyZone 배치. 반경/쿨다운=인스펙터, 타이밍=GameTuning(Control Panel). 플레이 중 K키=즉시 발생.");
    }
}
