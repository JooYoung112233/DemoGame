#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 3D 레이드용 포스트프로세싱 프로파일을 만든다 — `Resources/PlayerRigVolume3D.asset`.
///
/// 왜 필요한가 — 이 프로파일은 **컴포넌트가 하나도 없는 빈 에셋**이었다. 톤매핑이 없으면
/// URP는 선형 값을 그대로 잘라 출력해서, 밝은 쪽이 뭉치고 전체가 물빠진 것처럼 보인다.
/// 2026-09-10 실측 비교에서 "셰이더로 만들려던 룩"의 상당 부분이 사실 이 자리였다.
///
/// 톤 방향 — **색은 조명에서 만든다.** 그림자에 파랑을, 하이라이트에 노랑을 미는 그레이딩은
/// 일부러 넣지 않는다(레퍼런스 PikPok 아트디렉터: *"색이 옛날 영화처럼 카메라를 통해 자연히
/// 나오게 했다. 그림자에 파랑을, 하이라이트에 노랑을 밀지 않았다"*). 여기서는 톤매핑·대비·
/// 비네트·그레인까지만 담당하고, 색조는 WeatherData의 앰비언트·광원 색이 만든다.
///
/// ⚠️ `profile.Add&lt;T&gt;()`는 메모리에만 만든다. `AddObjectToAsset`으로 서브에셋으로 붙이지
///    않으면 **저장되지 않는다** — 실행 직후엔 보이지만 도메인 리로드 한 번에 전부 사라진다.
///    (실제로 그렇게 만들었다가 프로파일이 22줄·컴포넌트 0개로 남은 적이 있다.)
/// </summary>
public static class PostProfileBuilder
{
    const string Path = "Assets/Resources/PlayerRigVolume3D.asset";

    static void Keep(VolumeProfile profile, VolumeComponent c)
    {
        c.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(c, profile);
    }

    [MenuItem("Tools/TopDown/개발/포스트프로세싱 프로파일 생성 (3D)")]
    public static void Build()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, Path);
        }
        else
        {
            // 다시 실행하면 덮어쓴다. 서브에셋이므로 떼어낸 뒤 파괴해야 유령이 안 남는다.
            for (int i = profile.components.Count - 1; i >= 0; i--)
            {
                var c = profile.components[i];
                if (c == null) continue;
                AssetDatabase.RemoveObjectFromAsset(c);
                Object.DestroyImmediate(c, true);
            }
            profile.components.Clear();
        }

        // ── 톤매핑 ── 이 프로파일에서 가장 큰 한 줄.
        //    Neutral을 쓴다. ACES는 대비를 세게 밀어 이미 어두운 팔레트에서 그림자가 통째로
        //    뭉치고 색조도 붉게 돈다.
        var tone = profile.Add<Tonemapping>(true);
        Keep(profile, tone);
        tone.mode.overrideState = true;
        tone.mode.value = TonemappingMode.Neutral;

        // ── 색 보정 ── 대비만 살짝. 채도는 거의 건드리지 않는다(세계가 이미 바랜 색이다).
        var color = profile.Add<ColorAdjustments>(true);
        Keep(profile, color);
        color.postExposure.overrideState = true; color.postExposure.value = 0.20f;
        color.contrast.overrideState     = true; color.contrast.value     = 10f;
        color.saturation.overrideState   = true; color.saturation.value   = -6f;

        // ── 블룸 ── 문턱을 높게. 랜턴·창문 같은 실제 광원에만 걸린다.
        //    문턱이 낮으면 밝은 옷·벽까지 번져 안개 낀 것처럼 된다.
        var bloom = profile.Add<Bloom>(true);
        Keep(profile, bloom);
        bloom.threshold.overrideState = true; bloom.threshold.value = 1.10f;
        bloom.intensity.overrideState = true; bloom.intensity.value = 0.45f;
        bloom.scatter.overrideState   = true; bloom.scatter.value   = 0.60f;

        // ── 비네트 ── 쿼터뷰라 화면 가장자리에 정보가 적다. 살짝 눌러 시선을 가운데로.
        var vig = profile.Add<Vignette>(true);
        Keep(profile, vig);
        vig.intensity.overrideState  = true; vig.intensity.value  = 0.24f;
        vig.smoothness.overrideState = true; vig.smoothness.value = 0.45f;

        // ── 필름 그레인 ── 아주 약하게. 로우폴리 면이 매끈하게 뭉치는 걸 깨 준다.
        var grain = profile.Add<FilmGrain>(true);
        Keep(profile, grain);
        grain.type.overrideState      = true; grain.type.value      = FilmGrainLookup.Medium1;
        grain.intensity.overrideState = true; grain.intensity.value = 0.14f;
        grain.response.overrideState  = true; grain.response.value  = 0.8f;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[포스트프로세싱] 생성 — {Path} (컴포넌트 {profile.components.Count}개)\n"
                + "  톤매핑 Neutral · 노출 +0.20 · 대비 +10 · 채도 −6 · 블룸 문턱 1.10 · 비네트 0.24 · 그레인 0.14\n"
                + "  색조는 여기서 안 만든다 — WeatherData의 앰비언트·광원 색이 만든다.");
        if (!ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("포스트프로세싱", $"컴포넌트 {profile.components.Count}개 생성.", "확인");
    }

    /// <summary>이 프로파일을 쓰는 전역 Volume을 씬에 만든다(부트스트랩이 없는 룩 체크 씬용).</summary>
    public static Volume CreateGlobalVolume(string name = "PostProcess")
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Path);
        if (profile == null) { Debug.LogWarning("[포스트프로세싱] 프로파일 없음 — 메뉴로 먼저 생성할 것."); return null; }
        var go = new GameObject(name);
        var v = go.AddComponent<Volume>();
        v.isGlobal = true; v.priority = 0f; v.sharedProfile = profile;
        return v;
    }
}
#endif
