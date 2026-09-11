#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// **URP/Lit 머티리얼을 게임 전용 셰이더 `BRB/GameLit`으로 바꾼다** (docs/rendering.md §게임 전용 셰이더, 2026-09-11).
///
/// 속성 이름이 URP/Lit과 같아서 셰이더만 바꿔도 색·텍스처·수치가 그대로 넘어온다. 여기서 하는 일은
/// ① 키워드를 맞추고(노멀·AO·발광·알파 자르기 — 셰이더 GUI가 없어 자동으로 안 켜진다)
/// ② 캐릭터/환경에 따라 어두운 사실풍 값을 넣는 것(캐릭터 = 몸에 붙는 때 + 실루엣 림, 환경 = 월드 때).
/// 투명 머티리얼은 URP/Lit으로 둔다(GameLit은 불투명 전용).
///
/// ⚠️ 다른 세션이 작업 중인 곳은 건드리지 않는다 — `Hideout02` 폴더 · `Hideout` 씬.
/// 씬은 **추가로 열었다 닫는다** — 지금 열려 있는 씬(다른 작업)을 닫지 않게.
/// 몇 번 돌려도 같은 결과. 되돌리기 메뉴도 있다(GameLit → URP/Lit, 값은 그대로).
/// </summary>
public static class GameLitConverter
{
    const string GameLitName = "BRB/GameLit";
    const string UrpLitName = "Universal Render Pipeline/Lit";
    static readonly string[] SkipPaths = { "/Hideout02/" };
    // Safehouse = 마을 — 다른 세션이 Town02로 짓는 중(2026-09-11). 1차 전환 때 한 번 저장했으니 더는 건드리지 않는다.
    static readonly string[] SkipScenes = { "Assets/Scenes/Hideout.unity", "Assets/Scenes/Safehouse.unity" };

    static bool Skipped(string path)
    {
        foreach (var s in SkipPaths) if (path.Contains(s)) return true;
        return false;
    }

    static bool IsCharacterPath(string path)
        => path.Contains("/ChibiSurvivor/") || path.Contains("/Characters/") || path.Contains("Bandit");

    static void Kw(Material m, string k, bool on) { if (on) m.EnableKeyword(k); else m.DisableKeyword(k); }

    /// <summary>한 머티리얼 전환. 이미 검수한 GameLit 재질의 개별 룩 값은 보존한다.</summary>
    public static bool Convert(Material m, Shader gameLit, bool character)
    {
        if (m == null || m.shader == null) return false;
        if (m.shader.name == GameLitName) return false;
        if (m.shader.name != UrpLitName) return false;
        if (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f) return false;   // 투명은 그대로

        // ⚠️ 셰이더를 바꾸기 **전에** 읽는다 — 키워드는 셰이더에 묶여 있어 바꾸면 사라질 수 있다.
        bool alphaClip = m.HasProperty("_AlphaClip") && m.GetFloat("_AlphaClip") > 0.5f;
        bool normal = m.HasProperty("_BumpMap") && m.GetTexture("_BumpMap") != null;
        bool occlusion = m.HasProperty("_OcclusionMap") && m.GetTexture("_OcclusionMap") != null;
        bool emission = m.IsKeywordEnabled("_EMISSION");
        bool packedMask = m.IsKeywordEnabled("_METALLICSPECGLOSSMAP") &&
                          m.HasProperty("_MetallicGlossMap") && m.GetTexture("_MetallicGlossMap") != null;

        m.shader = gameLit;
        Kw(m, "_NORMALMAP", normal);
        Kw(m, "_OCCLUSIONMAP", occlusion);
        Kw(m, "_EMISSION", emission);
        Kw(m, "_ALPHATEST_ON", alphaClip);
        if (packedMask && m.HasProperty("_UsePackedMask"))
        {
            // URP's mapped metallic workflow reads R directly, ignoring the scalar.
            // GameLit multiplies it; preserve the source mask by using a unit multiplier.
            m.SetFloat("_UsePackedMask", 1f);
            m.SetFloat("_Metallic", 1f);
            Kw(m, "_GAMELIT_PACKED_MASK", true);
        }
        m.renderQueue = alphaClip ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;
        ApplyLook(m, character);
        return true;
    }

    /// <summary>어두운 사실풍 값 — 캐릭터/환경.</summary>
    static void ApplyLook(Material m, bool character)
    {
        // 반짝임 줄이기(2026-09-11 사용자 "대리석 같다") — 하늘 반사는 전부 끄고, 스펙 하이라이트는 금속이 아닌 환경만 끈다.
        //   캐릭터·금속(총·철물)은 하이라이트를 남겨 재질이 읽히게 한다.
        bool metal = m.HasProperty("_Metallic") && m.GetFloat("_Metallic") > 0.01f;
        Kw(m, "_ENVIRONMENTREFLECTIONS_OFF", true);
        Kw(m, "_SPECULARHIGHLIGHTS_OFF", !character && !metal);

        if (character)
        {
            // 캐릭터 — 때는 몸에 붙어 따라다니고(오브젝트 공간) 옅게, 어둠 속 실루엣은 림으로 살린다.
            m.SetFloat("_GrimeObjectSpace", 1f);
            m.SetFloat("_GrimeStrength", 0.30f);
            m.SetFloat("_GrimeScale", 3f);
            m.SetFloat("_ShadowDesaturation", 0.45f);
            m.SetFloat("_RimStrength", 0.12f);
        }
        else
        {
            // 환경 — 월드 공간 때(잘게 · 벽 밑동에 모인다), 림 없음.
            // 2026-09-11: 0.6/0.5는 바닥에 1.6m짜리 얼룩이 깔려 구름처럼 보였다 → 2.5/0.35.
            m.SetFloat("_GrimeObjectSpace", 0f);
            m.SetFloat("_GrimeStrength", 0.35f);
            m.SetFloat("_GrimeScale", 2.5f);
            m.SetFloat("_ShadowDesaturation", 0.55f);
            m.SetFloat("_RimStrength", 0f);
        }
    }

    static bool Revert(Material m, Shader urpLit)
    {
        if (m == null || m.shader == null || m.shader.name != GameLitName) return false;
        bool normal = m.IsKeywordEnabled("_NORMALMAP"), emission = m.IsKeywordEnabled("_EMISSION"),
             occlusion = m.IsKeywordEnabled("_OCCLUSIONMAP"), clip = m.IsKeywordEnabled("_ALPHATEST_ON");
        m.shader = urpLit;
        Kw(m, "_NORMALMAP", normal); Kw(m, "_EMISSION", emission); Kw(m, "_OCCLUSIONMAP", occlusion); Kw(m, "_ALPHATEST_ON", clip);
        if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", clip ? 1f : 0f);
        return true;
    }

    [MenuItem("Tools/TopDown/렌더/게임 셰이더로 전환 (머티리얼 에셋)")]
    public static string ConvertAssets()
    {
        var gameLit = Shader.Find(GameLitName);
        if (gameLit == null) { Debug.LogError("[게임 셰이더] " + GameLitName + " 없음."); return "셰이더 없음"; }
        int chars = 0, env = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Skipped(path)) continue;
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool character = IsCharacterPath(path);
            if (!Convert(m, gameLit, character)) continue;
            EditorUtility.SetDirty(m);
            if (character) chars++; else env++;
        }
        AssetDatabase.SaveAssets();
        string msg = $"머티리얼 에셋 전환 — 캐릭터 {chars}개 · 환경 {env}개";
        Debug.Log("[게임 셰이더] " + msg);
        return msg;
    }

    [MenuItem("Tools/TopDown/렌더/게임 셰이더로 전환 (씬에 박힌 것까지)")]
    public static string ConvertScenes()
    {
        var gameLit = Shader.Find(GameLitName);
        if (gameLit == null) return "셰이더 없음";
        var sb = new StringBuilder(ConvertAssets()).AppendLine();
        ForEachBuildScene((scene, isOpen) =>
        {
            int here = 0;
            var seen = new HashSet<Material>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null || !seen.Add(m) || AssetDatabase.Contains(m)) continue;   // 에셋은 위에서 처리
                        if (Convert(m, gameLit, r is SkinnedMeshRenderer)) here++;
                    }
            if (here > 0) sb.AppendLine($"  {scene.path} — 박힌 머티리얼 {here}개");
            return here > 0;
        });
        Debug.Log("[게임 셰이더] " + sb);
        return sb.ToString();
    }

    [MenuItem("Tools/TopDown/렌더/게임 셰이더 되돌리기 (URP/Lit)")]
    public static string RevertAll()
    {
        var urpLit = Shader.Find(UrpLitName);
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (Revert(m, urpLit)) { EditorUtility.SetDirty(m); n++; }
        }
        AssetDatabase.SaveAssets();
        int s = 0;
        ForEachBuildScene((scene, isOpen) =>
        {
            int here = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials)
                        if (m != null && !AssetDatabase.Contains(m) && Revert(m, urpLit)) here++;
            s += here;
            return here > 0;
        });
        string msg = $"되돌림 — 에셋 {n}개 · 씬에 박힌 것 {s}개";
        Debug.Log("[게임 셰이더] " + msg);
        return msg;
    }

    /// <summary>빌드세팅의 씬마다 — 이미 열려 있으면 그대로, 아니면 추가로 열었다가 닫는다. 바뀌면 저장.</summary>
    static void ForEachBuildScene(System.Func<Scene, bool, bool> work)
    {
        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (string.IsNullOrEmpty(entry.path) || !System.IO.File.Exists(entry.path)) continue;
            if (System.Array.IndexOf(SkipScenes, entry.path) >= 0) continue;
            var scene = SceneManager.GetSceneByPath(entry.path);
            bool wasOpen = scene.IsValid() && scene.isLoaded;
            if (!wasOpen) scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Additive);
            if (work(scene, wasOpen))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
#endif
