using UnityEngine;

/// <summary>
/// 카툰 머티리얼을 만드는 **한 곳**. 플레이어·적·룩 체크 씬이 각자 `new Material(toon)`을
/// 하고 있었는데, 그러면 램프 텍스처처럼 나중에 붙는 것을 **한 곳만 빠뜨려도** 그 대상만
/// 다른 룩으로 나온다(실제로 추가 광원 키워드에서 같은 사고가 났다).
/// </summary>
public static class ToonMaterial
{
    static Shader _shader;
    static Texture2D _ramp;
    static bool _rampLooked;

    public static Shader Shader
    {
        get
        {
            if (_shader == null) _shader = UnityEngine.Shader.Find("BRB/Toon");
            return _shader;
        }
    }

    /// <summary>라이트 램프 — `Tools ▸ TopDown ▸ 개발 ▸ 카툰 램프 텍스처 생성`이 만든다.
    /// 없으면 null을 돌려주고, 셰이더는 밴딩으로 폴백한다(에러 아님).</summary>
    public static Texture2D Ramp
    {
        get
        {
            if (!_rampLooked)
            {
                _rampLooked = true;
                _ramp = Resources.Load<Texture2D>("Shaders/ToonRamp");
                if (_ramp == null)
                    Debug.LogWarning("[ToonMaterial] Resources/Shaders/ToonRamp 없음 — 램프 없이 밴딩으로 표시된다. "
                                   + "메뉴 `Tools ▸ TopDown ▸ 개발 ▸ 카툰 램프 텍스처 생성`으로 만들 것.");
            }
            return _ramp;
        }
    }

    /// <summary>원본 머티리얼의 **색만 물려받은** 카툰 머티리얼(인스턴스).
    /// 원본 에셋은 건드리지 않는다.</summary>
    public static Material From(Material src, string name)
    {
        Color c = Color.white;
        if (src != null)
            c = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src.color;
        return Create(c, name);
    }

    public static Material Create(Color baseColor, string name)
    {
        var sh = Shader;
        if (sh == null) return null;
        var m = new Material(sh) { name = name };
        m.SetColor("_BaseColor", baseColor);
        if (Ramp != null) m.SetTexture("_RampTex", Ramp);
        return m;
    }

    /// <summary>이 오브젝트(와 자식)의 머티리얼을 전부 카툰으로 갈아끼우고,
    /// 외곽선용 평균 법선까지 굽는다. 새로 만든 머티리얼은 owned에 쌓아 호출부가 정리한다.</summary>
    public static void ApplyTo(GameObject root, string namePrefix,
                               System.Collections.Generic.List<Material> owned = null)
    {
        if (root == null || Shader == null) return;
        OutlineNormals.Apply(root);   // 하드 엣지 모델이라 이게 없으면 테두리가 조각난다

        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var src = r.sharedMaterials;
            var dst = new Material[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                dst[i] = From(src[i], namePrefix + "_Toon_" + i);
                owned?.Add(dst[i]);
            }
            r.sharedMaterials = dst;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
        }
    }
}
