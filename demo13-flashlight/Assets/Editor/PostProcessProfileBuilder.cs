#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// **3D 레이드용 포스트프로세싱 프로파일**을 만든다 — `Resources/PlayerRigVolume3D.asset`.
///
/// 2026-09-10 카툰을 접고 리얼리티 쪽으로 방향을 바꾸면서 필요해졌다(사용자: "우리 세계관에
/// 툰은 별로다 … 라이트도 그에 맞춰 포스트프로세싱도 만져보자").
///
/// 왜 필요한가 — 이 프로파일은 **컴포넌트가 하나도 없는 빈 에셋**이었다. 톤매핑이 없으면
/// URP는 선형 값을 그대로 잘라 출력해서, 밝은 쪽이 뭉치고 전체가 물빠진 것처럼 보인다.
/// 카툰일 땐 램프가 톤을 직접 지정해서 티가 안 났지만, PBR로 가면 이게 룩의 절반이다.
///
/// 톤 방향 — "붕괴된 사회". 채도를 빼되 죽이지는 않고, 그림자는 차갑게 하이라이트는
/// 탁한 온색으로. 대비는 살짝 올려 실루엣이 서게. 블룸은 **문턱을 높여** 광원에만 걸리게 한다
/// (문턱이 낮으면 밝은 옷까지 번져 안개 낀 것처럼 된다).
///
/// 값을 바꾸려면 아래 상수를 고치고 메뉴를 다시 실행한다. 인스펙터에서 직접 만져도 되지만,
/// 그러면 이 파일과 실제 값이 갈리니 되도록 여기서 고칠 것.
/// </summary>
public static class PostProcessProfileBuilder
{
    const string Path = "Assets/Resources/PlayerRigVolume3D.asset";

    /// <summary>볼륨 컴포넌트를 프로파일의 **서브에셋으로** 붙인다. 이게 없으면 저장이 안 된다.</summary>
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
            // 다시 실행하면 **덮어쓴다** — 손으로 만진 값이 남아 있으면 여기 값과 갈린다.
            // 서브에셋이므로 에셋에서 떼어낸 뒤 파괴해야 파일에 유령 오브젝트가 안 남는다.
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
        //    Neutral을 쓴다. ACES는 대비를 세게 밀어 어두운 게임에서 그림자가 통째로 뭉치고
        //    색조도 붉게 돌아, 이미 어두운 팔레트와 겹친다.
        // ⚠️ `profile.Add<T>()`는 메모리에만 만든다. **`AddObjectToAsset`으로 서브에셋으로 붙이지
        //    않으면 저장되지 않는다** — 실행 직후엔 화면에 보이지만 도메인 리로드 한 번에 전부
        //    사라진다(실제로 그렇게 만들었다가 프로파일이 22줄·컴포넌트 0개로 남았다).
        //    아래 Keep()이 그 한 줄을 대신 챙긴다.
        var tone = profile.Add<Tonemapping>(true);
        Keep(profile, tone);
        tone.mode.overrideState = true;
        tone.mode.value = TonemappingMode.Neutral;

        // ── 색 보정 ── 채도를 빼되 죽이지 않는다. 노출을 살짝 올려 PBR이 어두워진 걸 되돌린다.
        var color = profile.Add<ColorAdjustments>(true);
        Keep(profile, color);
        color.postExposure.overrideState = true; color.postExposure.value = 0.35f;
        color.contrast.overrideState    = true; color.contrast.value    = 12f;
        color.saturation.overrideState  = true; color.saturation.value  = -18f;

        // ── 화이트 밸런스 ── 전체를 살짝 차갑게. 폐허의 습한 공기.
        var wb = profile.Add<WhiteBalance>(true);
        Keep(profile, wb);
        wb.temperature.overrideState = true; wb.temperature.value = -8f;
        wb.tint.overrideState        = true; wb.tint.value        = 3f;

        // ── 그림자/중간/하이라이트 ── 카툰 램프가 하던 "차가운 그늘 · 탁한 온색 빛"을
        //    이제 여기서 만든다. 램프와 달리 화면 전체에 일관되게 걸린다.
        var smh = profile.Add<ShadowsMidtonesHighlights>(true);
        Keep(profile, smh);
        smh.shadows.overrideState    = true; smh.shadows.value    = new Vector4(0.88f, 0.94f, 1.12f, 0f);
        smh.midtones.overrideState   = true; smh.midtones.value   = new Vector4(1.00f, 1.00f, 0.98f, 0f);
        smh.highlights.overrideState = true; smh.highlights.value = new Vector4(1.06f, 1.02f, 0.92f, 0f);

        // ── 블룸 ── 문턱을 높게. 랜턴·창문 같은 실제 광원에만 걸린다.
        var bloom = profile.Add<Bloom>(true);
        Keep(profile, bloom);
        bloom.threshold.overrideState = true; bloom.threshold.value = 1.15f;
        bloom.intensity.overrideState = true; bloom.intensity.value = 0.55f;
        bloom.scatter.overrideState   = true; bloom.scatter.value   = 0.62f;
        bloom.tint.overrideState      = true; bloom.tint.value      = new Color(1f, 0.95f, 0.86f);

        // ── 비네트 ── 쿼터뷰라 화면 가장자리에 정보가 적다. 살짝 눌러 시선을 가운데로.
        var vig = profile.Add<Vignette>(true);
        Keep(profile, vig);
        vig.intensity.overrideState = true; vig.intensity.value = 0.26f;
        vig.smoothness.overrideState = true; vig.smoothness.value = 0.45f;
        vig.color.overrideState = true; vig.color.value = new Color(0.03f, 0.035f, 0.05f);

        // ── 필름 그레인 ── 아주 약하게. 저폴리 면이 매끈하게 뭉치는 걸 깨 준다.
        var grain = profile.Add<FilmGrain>(true);
        Keep(profile, grain);
        grain.type.overrideState = true; grain.type.value = FilmGrainLookup.Medium1;
        grain.intensity.overrideState = true; grain.intensity.value = 0.16f;
        grain.response.overrideState = true; grain.response.value = 0.8f;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[포스트프로세싱] 생성 — {Path} (컴포넌트 {profile.components.Count}개)\n"
                + "  톤매핑 Neutral · 노출 +0.35 · 대비 +12 · 채도 −18 · 차가운 그늘 · 블룸 문턱 1.15");
        if (!ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("포스트프로세싱", $"{Path}\n컴포넌트 {profile.components.Count}개 생성 완료.", "확인");
    }

    /// <summary>이 프로파일을 쓰는 전역 Volume을 씬에 만든다(룩 체크 씬처럼 부트스트랩이 없는 씬용).</summary>
    public static Volume CreateGlobalVolume(string name = "PostProcess")
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Path);
        if (profile == null) { Debug.LogWarning("[포스트프로세싱] 프로파일 없음 — 메뉴로 먼저 생성할 것."); return null; }
        var go = new GameObject(name);
        var v = go.AddComponent<Volume>();
        v.isGlobal = true;
        v.priority = 0f;
        v.sharedProfile = profile;
        return v;
    }
}
#endif
