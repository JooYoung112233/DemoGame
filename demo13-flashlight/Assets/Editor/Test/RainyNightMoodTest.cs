using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 비오는 밤거리 무드 테스트 — 어두운 포스트프로세싱 + 가로등 1 + 광택(젖은) 바닥 + 비.
/// 현 씬에 오브젝트를 추가한다(빈 씬에서 실행 권장). Tools ▸ TopDown ▸ 맵 ▸ 비오는 밤 무드 테스트.
/// ⚠️ 2D라 진짜 물 반사는 없음 — 어두운 광택 바닥 + 네온 Bloom 번짐으로 흉내. 진짜 반사는 반사형 바닥 스프라이트(아트) 필요.
/// ⚠️ 포스트프로세싱이 보이려면 카메라 'Post Processing' 체크 + URP 렌더러 Post-processing on (Systems 씬 카메라는 이미 켜짐).
/// </summary>
public static class RainyNightMoodTest
{
    [MenuItem("Tools/TopDown/테스트/비오는 밤 무드")]
    static void Build()
    {
        var root = new GameObject("RainyNightMood_Test");
        Undo.RegisterCreatedObjectUndo(root, "Rainy Night Mood");

        // 1) 어두운 글로벌 앰비언트(밤·푸른 기운)
        var gl = NewChild(root, "GlobalLight_NightAmbient").AddComponent<Light2D>();
        gl.lightType = Light2D.LightType.Global;
        gl.intensity = 0.12f;
        gl.color = new Color(0.14f, 0.17f, 0.30f);

        // 2) 젖은 바닥 (어두운 광택 — 반사는 흉내)
        var ground = NewChild(root, "WetGround");
        var sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = SolidSprite();
        sr.color = new Color(0.05f, 0.06f, 0.09f);
        sr.sortingOrder = -100;
        ground.transform.localScale = new Vector3(40, 26, 1);

        // 3) 가로등 1개 (따뜻한 주황, 좁은 폴오프 → 바닥에 길게 번짐)
        var lampPole = NewChild(root, "StreetLamp");
        lampPole.transform.position = new Vector3(-3, 2.5f, 0);
        var lampSr = lampPole.AddComponent<SpriteRenderer>();
        lampSr.sprite = SolidSprite();
        lampSr.color = new Color(0.9f, 0.7f, 0.45f);
        lampPole.transform.localScale = new Vector3(0.25f, 0.25f, 1); // 전구 점
        lampSr.sortingOrder = 10;

        var lamp = NewChild(lampPole, "Light").AddComponent<Light2D>();
        lamp.lightType = Light2D.LightType.Point;
        lamp.color = new Color(1f, 0.72f, 0.42f);
        lamp.intensity = 2.4f;
        lamp.pointLightInnerRadius = 0.4f;
        lamp.pointLightOuterRadius = 8f;
        lamp.falloffIntensity = 0.75f;
        lamp.shadowIntensity = 0f;

        // 4) 네온 점 1~2개 (반사 강조용 강한 색광)
        AddNeon(root, new Vector3(4.5f, 1.2f, 0), new Color(0.2f, 0.6f, 1f), 1.6f);   // 청록 네온
        AddNeon(root, new Vector3(2.0f, -2.0f, 0), new Color(1f, 0.25f, 0.5f), 1.2f); // 핑크 네온

        // 5) 포스트프로세싱 (어두운 네온 — Bloom/Vignette/ColorGrade)
        var vol = NewChild(root, "MoodVolume").AddComponent<Volume>();
        vol.isGlobal = true;
        var prof = ScriptableObject.CreateInstance<VolumeProfile>();
        prof.name = "RainyNightProfile";
        vol.sharedProfile = prof;

        var bloom = prof.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(1.6f);
        bloom.threshold.Override(0.7f);
        bloom.scatter.Override(0.75f);

        var vig = prof.Add<Vignette>(true);
        vig.active = true;
        vig.intensity.Override(0.46f);
        vig.smoothness.Override(0.5f);
        vig.color.Override(new Color(0f, 0f, 0.02f));

        var ca = prof.Add<ColorAdjustments>(true);
        ca.active = true;
        ca.postExposure.Override(-0.35f);
        ca.contrast.Override(14f);
        ca.saturation.Override(16f);

        // 6) 비
        var rain = RainController.CreateRainParticle(root.transform);
        if (rain != null)
        {
            rain.transform.position = new Vector3(0, 9, 0);
            rain.Play();
        }

        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("비오는 밤 무드 테스트",
            "씬에 추가됨. Play로 확인.\n\n" +
            "• 포스트프로세싱이 안 보이면: 카메라 'Post Processing' 체크 + URP 렌더러 Post-processing on\n" +
            "• 물 반사는 2D라 흉내(어두운 광택 바닥 + Bloom). 진짜 반사는 반사형 바닥 스프라이트(아트) 필요\n" +
            "• 가로등/네온 색·반경, Volume 값으로 튜닝", "확인");
    }

    static void AddNeon(GameObject root, Vector3 pos, Color col, float intensity)
    {
        var go = NewChild(root, "Neon");
        go.transform.position = pos;
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = col;
        l.intensity = intensity;
        l.pointLightInnerRadius = 0.2f;
        l.pointLightOuterRadius = 5f;
        l.falloffIntensity = 0.8f;
        l.shadowIntensity = 0f;
    }

    static GameObject NewChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static Sprite cached;
    static Sprite SolidSprite()
    {
        if (cached != null) return cached;
        var tex = new Texture2D(4, 4);
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        cached = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return cached;
    }
}
