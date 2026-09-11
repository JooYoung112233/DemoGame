using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// BRB/DecalPixel 머티리얼 인스펙터.
/// 상단에 "Blend Preset" 드롭다운을 두어 Alpha/Multiply/Additive를 원클릭으로 적용한다
/// (Src/Dst Blend + 멀티플라이 키워드 + 렌더큐를 한 번에 세팅). 그 아래는 기본 프로퍼티 전체.
/// 수동으로 Src/Dst를 바꾸면 Custom으로 인식하고 덮어쓰지 않는다.
/// </summary>
public class DecalPixelGUI : ShaderGUI
{
    enum Preset { Alpha, Multiply, Additive, Screen, Custom }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        var mat = materialEditor.target as Material;

        Preset current = Detect(mat);
        EditorGUI.BeginChangeCheck();
        var picked = (Preset)EditorGUILayout.EnumPopup(
            new GUIContent("Blend Preset",
                "Alpha = 핏자국·발자국·포스터(일반 스프라이트)\n" +
                "Multiply = 얼룩·그을음·녹(바닥을 어둡게 물들임)\n" +
                "Additive = 발광 데칼(이상현상·룬 등 밝게 더함)\n" +
                "Screen = 검정 배경 밝은 오염을 부드럽게 더함\n" +
                "Custom = Src/Dst를 직접 설정"),
            current);
        if (EditorGUI.EndChangeCheck() && picked != Preset.Custom)
        {
            Apply(mat, picked);
            EditorUtility.SetDirty(mat);
        }

        switch (current)
        {
            case Preset.Multiply:
                EditorGUILayout.HelpBox("Multiply: 텍스처를 흰 배경에 어둡게 그려야 자연스럽게 물듦. " +
                    "Light2D 반응은 무시(바닥의 빛을 그대로 곱함).", MessageType.None);
                break;
            case Preset.Additive:
                EditorGUILayout.HelpBox("Additive: 보통 Light2D 반응 OFF + 밝은 텍스처. 어두울수록 잘 보임.", MessageType.None);
                break;
            case Preset.Screen:
                EditorGUILayout.HelpBox("Screen: 검정 배경에 밝은 오염을 그려 부드럽게 더함(Additive보다 덜 과함).", MessageType.None);
                break;
        }

        if (mat != null && mat.IsKeywordEnabled("_MASK_MODE"))
            EditorGUILayout.HelpBox("마스크 모드 ON: 텍스처는 흑백(밝기=세기)으로만 쓰고 색은 Tint로 결정. " +
                "녹·이끼·그을음을 회색조 1장으로 만들고 Tint 색만 바꿔 재활용. (추천: Multiply 프리셋)", MessageType.Info);

        EditorGUILayout.Space();
        base.OnGUI(materialEditor, properties);
    }

    static Preset Detect(Material m)
    {
        if (!m.HasProperty("_SrcBlend") || !m.HasProperty("_DstBlend")) return Preset.Custom;
        bool mul = m.IsKeywordEnabled("_MULTIPLY_ON");
        int s = (int)m.GetFloat("_SrcBlend");
        int d = (int)m.GetFloat("_DstBlend");

        if (mul && s == (int)BlendMode.DstColor && d == (int)BlendMode.Zero) return Preset.Multiply;
        if (!mul && s == (int)BlendMode.SrcAlpha && d == (int)BlendMode.OneMinusSrcAlpha) return Preset.Alpha;
        if (!mul && s == (int)BlendMode.SrcAlpha && d == (int)BlendMode.One) return Preset.Additive;
        if (!mul && s == (int)BlendMode.OneMinusDstColor && d == (int)BlendMode.One) return Preset.Screen;
        return Preset.Custom;
    }

    static void Apply(Material m, Preset p)
    {
        switch (p)
        {
            case Preset.Alpha:    Set(m, BlendMode.SrcAlpha,        BlendMode.OneMinusSrcAlpha, false); break;
            case Preset.Multiply: Set(m, BlendMode.DstColor,        BlendMode.Zero,             true);  break;
            case Preset.Additive: Set(m, BlendMode.SrcAlpha,        BlendMode.One,              false); break;
            case Preset.Screen:   Set(m, BlendMode.OneMinusDstColor, BlendMode.One,             false); break;
        }
    }

    static void Set(Material m, BlendMode src, BlendMode dst, bool multiply)
    {
        m.SetFloat("_SrcBlend", (float)src);
        m.SetFloat("_DstBlend", (float)dst);
        if (m.HasProperty("_MultiplyToggle")) m.SetFloat("_MultiplyToggle", multiply ? 1f : 0f);
        if (multiply) m.EnableKeyword("_MULTIPLY_ON"); else m.DisableKeyword("_MULTIPLY_ON");
    }
}
