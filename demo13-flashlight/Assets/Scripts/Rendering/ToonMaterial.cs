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

    /// <summary>원본 머티리얼의 **색과 알베도 맵**을 물려받은 카툰 머티리얼(인스턴스).
    /// 원본 에셋은 건드리지 않는다.
    ///
    /// ⚠️ 맵을 같이 넘기는 게 핵심이다. 예전엔 색만 복사했는데, 그러면 모델에 UV·텍스처가
    /// 있어도 카툰 셰이더를 씌우는 순간 파트마다 단색으로 뭉개졌다 — 팔뚝·소매·방망이가
    /// 통째로 검게 보이던 원인이다(2026-09-10).</summary>
    public static Material From(Material src, string name)
    {
        Color c = Color.white;
        Color emis = Color.black;
        Texture map = null;
        Vector4 st = new Vector4(1f, 1f, 0f, 0f);
        if (src != null)
        {
            c = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src.color;
            if (src.HasProperty("_BaseMap"))      { map = src.GetTexture("_BaseMap");   st = src.GetVector("_BaseMap_ST"); }
            else if (src.HasProperty("_MainTex")) { map = src.GetTexture("_MainTex");   st = src.GetVector("_MainTex_ST"); }
            // 발광은 키워드가 꺼져 있으면 색이 남아 있어도 안 쓴 것이다 — 그대로 옮기면
            // 안 빛나야 할 파트가 통째로 빛난다.
            if (src.IsKeywordEnabled("_EMISSION") && src.HasProperty("_EmissionColor"))
                emis = src.GetColor("_EmissionColor");
        }
        return Create(c, name, map, st, emis);
    }

    public static Material Create(Color baseColor, string name,
                                  Texture baseMap = null, Vector4? baseMapST = null,
                                  Color? emission = null)
    {
        var sh = Shader;
        if (sh == null) return null;
        var m = new Material(sh) { name = name };
        m.SetColor("_BaseColor", baseColor);
        if (Ramp != null) m.SetTexture("_RampTex", Ramp);
        if (baseMap != null)
        {
            m.SetTexture("_BaseMap", baseMap);
            m.SetVector("_BaseMap_ST", baseMapST ?? new Vector4(1f, 1f, 0f, 0f));
        }
        if (emission.HasValue) m.SetColor("_EmissionColor", emission.Value);
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
