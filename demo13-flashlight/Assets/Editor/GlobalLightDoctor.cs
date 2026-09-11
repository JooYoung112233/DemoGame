using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// "More than one global light on layer ..." 경고 진단/정리.
/// URP 2D는 같은 sorting layer에 활성 Global Light2D가 2개 이상이면 경고를 뱉는다.
/// · 점검: 로드된 모든 씬의 Global Light2D를 로그로(이름/씬/활성).
/// · 1개만 남기기: Systems 글로벌 우선으로 1개만 켜고 나머지 끔.
/// </summary>
public static class GlobalLightDoctor
{
    [MenuItem("Tools/TopDown/초기설정/글로벌 라이트 점검(로그)")]
    static void Inspect()
    {
        var all = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var sb = new StringBuilder("[GlobalLightDoctor] Global Light2D 목록\n");
        int globals = 0, enabledCount = 0;
        foreach (var l in all)
        {
            if (!IsGlobal(l)) continue;
            globals++;
            if (l.enabled && l.gameObject.activeInHierarchy) enabledCount++;
            sb.AppendLine($"  · '{l.name}'  씬='{l.gameObject.scene.name}'  comp.enabled={l.enabled}  GO활성={l.gameObject.activeInHierarchy}  hideFlags={l.gameObject.hideFlags}");
        }
        sb.AppendLine($"→ 글로벌 총 {globals}개 / 실제 활성 {enabledCount}개.  (활성 2개 이상이면 경고)");
        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/TopDown/초기설정/글로벌 라이트 1개만 남기기")]
    static void KeepOne()
    {
        var all = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        Light2D owner = null;
        var sys = SceneManager.GetSceneByName("Systems");
        if (sys.IsValid() && sys.isLoaded)
            foreach (var l in all) if (IsGlobal(l) && l.gameObject.scene == sys) { owner = l; break; }
        if (owner == null) foreach (var l in all) if (IsGlobal(l) && l.enabled) { owner = l; break; }
        if (owner == null) foreach (var l in all) if (IsGlobal(l)) { owner = l; break; }
        if (owner == null) { Debug.Log("[GlobalLightDoctor] 글로벌 라이트가 없음."); return; }

        if (!owner.enabled) { Undo.RecordObject(owner, "enable global"); owner.enabled = true; EditorUtility.SetDirty(owner); }

        int off = 0;
        foreach (var l in all)
        {
            if (!IsGlobal(l) || l == owner || !l.enabled) continue;
            Undo.RecordObject(l, "disable extra global");
            l.enabled = false;
            EditorUtility.SetDirty(l);
            off++;
        }
        Debug.Log($"[GlobalLightDoctor] 유지='{owner.name}'(씬 '{owner.gameObject.scene.name}'), 나머지 {off}개 끔. " +
                  (off == 0 ? "(이미 1개뿐)" : "경고 사라질 거야. 씬 저장하면 영구 적용."));
    }

    static bool IsGlobal(Light2D l) => l != null && l.lightType == Light2D.LightType.Global;
}
