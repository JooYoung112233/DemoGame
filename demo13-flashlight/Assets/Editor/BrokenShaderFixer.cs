#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// **셰이더가 사라진 머티리얼을 URP/Lit으로 되살린다.**
///
/// 왜 필요한가 — 2026-09-10 커스텀 셰이더 13종을 전부 지웠다(`7ae3e5ca`). 코드의
/// `Shader.Find`는 같이 고쳤지만, **머티리얼은 셰이더를 guid로 물고 있어서** 코드를 고쳐도
/// 그대로 깨진다. 깨진 머티리얼은 `Hidden/InternalErrorShader`가 되어 화면에 **마젠타**로 뜬다
/// (실측: 안전가옥 씬에서 머티리얼 슬롯 2126개 중 338개가 마젠타였다).
///
/// 셰이더를 다시 만들 때마다 이 상황이 반복되므로, 일회성 스크립트 대신 도구로 남긴다.
/// 몇 번 돌려도 같은 결과다(이미 정상인 머티리얼은 건드리지 않는다).
///
/// 색·텍스처는 최대한 옮긴다 — 옛 셰이더가 `_BaseColor`/`_Color`, `_BaseMap`/`_MainTex` 중
/// 무엇을 썼는지 모르므로 양쪽을 다 읽어 양쪽에 다 쓴다.
/// </summary>
public static class BrokenShaderFixer
{
    const string Fallback = "Universal Render Pipeline/Lit";

    static bool IsBroken(Material m)
    {
        if (m == null) return false;
        var sh = m.shader;
        return sh == null || !sh.isSupported || sh.name.StartsWith("Hidden/InternalError");
    }

    /// <summary>깨진 머티리얼을 폴백 셰이더로 바꾸고 색·텍스처를 옮긴다.</summary>
    static bool Repair(Material m, Shader fallback)
    {
        if (!IsBroken(m)) return false;

        // ⚠️ 셰이더를 바꾸기 **전에** 읽어야 한다. 바꾸고 나면 옛 프로퍼티는 사라진다.
        Color color = Color.white;
        if (m.HasProperty("_BaseColor")) color = m.GetColor("_BaseColor");
        else if (m.HasProperty("_Color")) color = m.GetColor("_Color");

        Texture tex = null;
        if (m.HasProperty("_BaseMap")) tex = m.GetTexture("_BaseMap");
        if (tex == null && m.HasProperty("_MainTex")) tex = m.GetTexture("_MainTex");

        m.shader = fallback;

        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color"))     m.SetColor("_Color", color);
        if (tex != null)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        }
        // 그레이박스 톤에 맞춰 무광 기본값 — 깨진 머티리얼이 번들거리며 되살아나면 더 눈에 띈다.
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.08f);
        if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic", 0f);
        return true;
    }

    [MenuItem("Tools/TopDown/개발/깨진 셰이더 복구 (머티리얼 에셋)")]
    public static void FixAssets()
    {
        var fallback = Shader.Find(Fallback);
        if (fallback == null) { Debug.LogError("[셰이더 복구] " + Fallback + " 없음."); return; }

        var sb = new StringBuilder();
        int fixedCount = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!Repair(m, fallback)) continue;
            EditorUtility.SetDirty(m);
            sb.AppendLine("  " + path);
            fixedCount++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[셰이더 복구] 머티리얼 에셋 {fixedCount}개 복구 → {Fallback}\n" + sb);
        if (!ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("깨진 셰이더 복구", $"머티리얼 에셋 {fixedCount}개 복구.", "확인");
    }

    /// <summary>씬에 **박힌**(에셋이 아닌) 머티리얼까지 고친다. 빌드세팅의 모든 씬을 열었다 저장한다.
    ///
    /// 에셋 복구로는 안 잡히는 것들이 있다 — 빌더가 런타임/에디터에서 `new Material(...)`로 만들어
    /// 씬에 직접 저장한 머티리얼. 이건 씬을 열어야 보인다.</summary>
    [MenuItem("Tools/TopDown/개발/깨진 셰이더 복구 (씬에 박힌 것까지)")]
    public static void FixScenes()
    {
        var fallback = Shader.Find(Fallback);
        if (fallback == null) { Debug.LogError("[셰이더 복구] " + Fallback + " 없음."); return; }

        // 먼저 에셋을 고쳐 둔다 — 씬이 참조하는 대부분이 여기서 해결된다.
        FixAssets();

        string current = EditorSceneManager.GetActiveScene().path;
        if (!string.IsNullOrEmpty(current) && EditorSceneManager.GetActiveScene().isDirty)
            EditorSceneManager.SaveOpenScenes();

        var sb = new StringBuilder();
        int sceneCount = 0, matCount = 0;
        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (string.IsNullOrEmpty(entry.path) || !System.IO.File.Exists(entry.path)) continue;
            var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
            var seen = new HashSet<Material>();
            int here = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null || !seen.Add(m)) continue;
                        if (Repair(m, fallback)) here++;
                    }
            if (here > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                sb.AppendLine($"  {entry.path} — {here}개");
                sceneCount++; matCount += here;
            }
        }

        if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
        Debug.Log($"[셰이더 복구] 씬 {sceneCount}개에서 박힌 머티리얼 {matCount}개 복구\n" + sb);
        if (!ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("깨진 셰이더 복구", $"씬 {sceneCount}개 / 머티리얼 {matCount}개 복구.", "확인");
    }
}
#endif
