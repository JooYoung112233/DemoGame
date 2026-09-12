Shader "BRB/GroundFadeLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.3
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        [Toggle(_GAMELIT_PACKED_MASK)] _UsePackedMask("Use Packed Surface Mask", Float) = 0
        _MetallicGlossMap("Surface Mask (R Metallic, A Smoothness)", 2D) = "white" {}
        _BumpScale("Normal Scale", Float) = 1.0
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _ComicLighting("Painted Diffuse Lighting", Range(0.0, 1.0)) = 1.0
        [HDR] _EmissionColor("Emission", Color) = (0,0,0,1)
        _EmissionMap("Emission Map", 2D) = "white" {}

        [Header(Legacy Look When Painted Lighting Is Off)]
        _GrimeColor("Grime Color", Color) = (0.55, 0.50, 0.44, 1)
        _GrimeStrength("Grime", Range(0.0, 1.0)) = 0.5
        _GrimeScale("Grime Scale", Float) = 0.6
        _GroundGrimeHeight("Ground Grime Height (m)", Float) = 0.8
        [MaterialToggle] _GrimeObjectSpace("Grime In Object Space (characters)", Float) = 0
        _ShadowDesaturation("Shadow Desaturation", Range(0.0, 1.0)) = 0.55
        _RimStrength("Rim", Range(0.0, 1.0)) = 0.0
        _RimColor("Rim Color", Color) = (0.60, 0.65, 0.75, 1)

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        [HideInInspector] _Surface("__surface", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent-20"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex GroundFadeVertex
            #pragma fragment GroundFadeFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            // 2026-09-11 사용자 "빛반사가 심하다, 반짝반짝 대리석 같다" — 쿼터뷰는 바닥을 비스듬히 봐서 하늘 반사(프레넬)가 크고,
            //   머리 위 램프가 바닥에 하이라이트를 찍는다. 환경 반사는 끄고(전 머티리얼), 스펙 하이라이트는 금속이 아닌 환경에서 끈다(전환 도구).
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _GAMELIT_PACKED_MASK
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "GameLitInput.hlsl"
            #include "GameLitForwardPass.hlsl"

            // Reuse GameLit lighting; only these ground overlays consume vertex alpha.
            Varyings GroundFadeVertex(Attributes input, half4 vertexColor : COLOR, out half fade : TEXCOORD6)
            {
                fade = vertexColor.a;
                return GameLitVertex(input);
            }
            half4 GroundFadeFragment(Varyings input, half fade : TEXCOORD6) : SV_Target
            {
                half4 color = GameLitFragment(input);
                color.a *= saturate(fade);
                return color;
            }
            ENDHLSL
        }

    }
}
