Shader "BRB/DecalPixel"
{
    // 데칼(바닥 오버레이)용 픽셀 셰이더 — 핏자국/그을음/발자국/금/포스터/이상현상 등.
    // 프롭/바닥과 별개. Transparent 큐, 오버레이로 깔리고 sortingOrder로 앞뒤 정함.
    // 블렌드 프리셋(머티리얼별): Alpha=일반 스프라이트, Multiply=얼룩(바닥 어둡게), Additive=발광.
    //   → DecalPixelGUI 인스펙터의 "Blend Preset"으로 원클릭 전환(또는 Src/Dst/멀티플라이 수동).
    // 픽셀화/색단계는 BRB 패밀리 공통. _Alpha + 정점컬러(SpriteRenderer.color)로 시간 페이드.
    Properties
    {
        _MainTex ("Decal Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _Alpha ("Opacity Fade", Range(0, 1)) = 1.0
        _Cutoff ("Alpha Cutoff (0=소프트)", Range(0, 1)) = 0.0

        [Header(Lighting)]
        [Toggle(_LIT_ON)] _LitToggle ("Light2D 반응 ON", Float) = 1

        [Header(Mask Mode)]
        [Toggle(_MASK_MODE)] _MaskMode ("흑백 마스크→틴트 (낡음/녹/폐허)", Float) = 0

        [Header(Blend Preset)]
        [Toggle(_MULTIPLY_ON)] _MultiplyToggle ("멀티플라이 얼룩 모드", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10

        [Header(Pixelation)]
        [Toggle(_PIXELATE_ON)] _PixelateToggle ("픽셀화 ON", Float) = 1
        _PixelDensity ("Pixel Density (격자 수)", Range(8, 2048)) = 64

        [Header(Color Quantize)]
        [Toggle(_QUANTIZE_ON)] _QuantizeToggle ("색 단계화 (포스터라이즈)", Float) = 1
        _ColorLevels ("Color Levels", Range(2, 32)) = 8
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
        }

        // ============ 3D URP Forward 패스 (폴백/머티리얼 프리뷰용) ============
        Pass
        {
            Name "DecalPixelForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off  ZWrite Off  Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _LIT_ON
            #pragma shader_feature_local _MULTIPLY_ON
            #pragma shader_feature_local _MASK_MODE
            #pragma shader_feature_local _PIXELATE_ON
            #pragma shader_feature_local _QUANTIZE_ON
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Brightness;
                float _Alpha;
                float _Cutoff;
                float _PixelDensity;
                float _ColorLevels;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; float3 positionWS : TEXCOORD1; };

            float2 PixelateUV(float2 uv, float density)
            {
                float2 grid = float2(density, density);
                return (floor(uv * grid) + 0.5) / grid;
            }

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.color = input.color;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz).xyz;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                #ifdef _PIXELATE_ON
                    float2 puv = PixelateUV(input.uv, _PixelDensity);
                #else
                    float2 puv = input.uv;
                #endif
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, puv);
                clip(mainTex.a - _Cutoff);

                half3 rgb = mainTex.rgb;
                #ifdef _QUANTIZE_ON
                    float levels = max(_ColorLevels, 2.0);
                    rgb = floor(rgb * levels + 0.5) / levels;
                #endif

                #ifdef _MASK_MODE
                    // 흑백 마스크: 텍스처 밝기(루미넌스)=세기, 색은 _Color(틴트). 검정 배경=효과 없음.
                    half lum = dot(saturate(rgb), half3(0.299, 0.587, 0.114));
                    half3 albedo = _Color.rgb * input.color.rgb * _Brightness;
                    half outA = lum * _Color.a * input.color.a * _Alpha;
                #else
                    half3 albedo = rgb * _Color.rgb * input.color.rgb * _Brightness;
                    half outA = mainTex.a * input.color.a * _Alpha;
                #endif

                #ifdef _MULTIPLY_ON
                    // 알파 인식 멀티플라이: 알파 낮은 곳은 흰색(1)=바닥 변화 없음. (Blend DstColor Zero 전제)
                    return half4(lerp(half3(1,1,1), albedo, outA), 1.0);
                #else
                    #ifdef _LIT_ON
                        half3 ambient = albedo * 0.4;
                        Light mainLight = GetMainLight();
                        half3 outc = ambient + albedo * mainLight.color;
                        #ifdef _ADDITIONAL_LIGHTS
                        uint lightCount = GetAdditionalLightsCount();
                        for (uint i = 0; i < lightCount; i++)
                        {
                            Light addLight = GetAdditionalLight(i, input.positionWS);
                            outc += albedo * addLight.distanceAttenuation * addLight.color * 2.0;
                        }
                        #endif
                        return half4(outc, outA);
                    #else
                        return half4(albedo, outA);
                    #endif
                #endif
            }
            ENDHLSL
        }

        // ============ URP 2D 렌더러 패스 (Light2D 반응) ============
        Pass
        {
            Name "DecalPixel2D"
            Tags { "LightMode"="Universal2D" }
            Cull Off  ZWrite Off  Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma vertex vert2d
            #pragma fragment frag2d
            #pragma shader_feature_local _LIT_ON
            #pragma shader_feature_local _MULTIPLY_ON
            #pragma shader_feature_local _MASK_MODE
            #pragma shader_feature_local _PIXELATE_ON
            #pragma shader_feature_local _QUANTIZE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Brightness;
                float _Alpha;
                float _Cutoff;
                float _PixelDensity;
                float _ColorLevels;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes2D { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings2D { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; half2 lightingUV : TEXCOORD1; };

            float2 PixelateUV2D(float2 uv, float density)
            {
                float2 grid = float2(density, density);
                return (floor(uv * grid) + 0.5) / grid;
            }

            Varyings2D vert2d(Attributes2D input)
            {
                Varyings2D o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.color = input.color;
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                return o;
            }

            half4 frag2d(Varyings2D input) : SV_Target
            {
                #ifdef _PIXELATE_ON
                    float2 puv = PixelateUV2D(input.uv, _PixelDensity);
                #else
                    float2 puv = input.uv;
                #endif
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, puv);
                clip(mainTex.a - _Cutoff);

                half3 rgb = mainTex.rgb;
                #ifdef _QUANTIZE_ON
                    float levels = max(_ColorLevels, 2.0);
                    rgb = floor(rgb * levels + 0.5) / levels;
                #endif

                #ifdef _MASK_MODE
                    // 흑백 마스크: 텍스처 밝기(루미넌스)=세기, 색은 _Color(틴트). 검정 배경=효과 없음.
                    half lum = dot(saturate(rgb), half3(0.299, 0.587, 0.114));
                    half3 albedo = _Color.rgb * input.color.rgb * _Brightness;
                    half outA = lum * _Color.a * input.color.a * _Alpha;
                #else
                    half3 albedo = rgb * _Color.rgb * input.color.rgb * _Brightness;
                    half outA = mainTex.a * input.color.a * _Alpha;
                #endif

                #ifdef _MULTIPLY_ON
                    // 알파 인식 멀티플라이(언릿): 알파 낮은 곳=흰색=바닥 변화 없음. (Blend DstColor Zero 전제)
                    return half4(lerp(half3(1,1,1), albedo, outA), 1.0);
                #else
                    #ifdef _LIT_ON
                        SurfaceData2D sd;
                        InputData2D id;
                        InitializeSurfaceData(albedo, outA, half4(1,1,1,1), sd);
                        InitializeInputData(puv, input.lightingUV, id);
                        return CombinedShapeLightShared(sd, id);
                    #else
                        return half4(albedo, outA);
                    #endif
                #endif
            }
            ENDHLSL
        }
    }
    CustomEditor "DecalPixelGUI"
    FallBack "Universal Render Pipeline/Lit"
}
