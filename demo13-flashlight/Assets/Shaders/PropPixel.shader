Shader "BRB/PropPixel"
{
    // 프롭용 픽셀 셰이더. 픽셀화/색단계/외곽선/알파 컷아웃 + Light2D 반응.
    // 그림자는 FlatShadow 컴포넌트(정적 발밑 + 동적 투영)로 별도 처리.
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Pixelation)]
        [Toggle(_PIXELATE_ON)] _PixelateToggle ("픽셀화 ON", Float) = 1
        _PixelDensity ("Pixel Density (격자 수)", Range(8, 2048)) = 64

        [Header(Color Quantize)]
        [Toggle(_QUANTIZE_ON)] _QuantizeToggle ("색 단계화 (포스터라이즈)", Float) = 1
        _ColorLevels ("Color Levels", Range(2, 32)) = 8

        [Header(Outline)]
        [Toggle(_OUTLINE_ON)] _OutlineToggle ("픽셀 외곽선", Float) = 0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThreshold ("Outline Threshold", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        // ============ 3D URP Forward 패스 ============
        Pass
        {
            Name "PropPixelForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off  ZWrite On  Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _PIXELATE_ON
            #pragma shader_feature_local _QUANTIZE_ON
            #pragma shader_feature_local _OUTLINE_ON
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Brightness;
                float _Cutoff;
                float _PixelDensity;
                float _ColorLevels;
                float4 _OutlineColor;
                float _OutlineThreshold;
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

                half3 color = rgb * _Color.rgb * input.color.rgb * _Brightness;

                #ifdef _OUTLINE_ON
                    float2 texel = 1.0 / float2(_PixelDensity, _PixelDensity);
                    float aL = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, PixelateUV(input.uv + float2(-texel.x, 0), _PixelDensity)).a;
                    float aR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, PixelateUV(input.uv + float2( texel.x, 0), _PixelDensity)).a;
                    float aD = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, PixelateUV(input.uv + float2(0, -texel.y), _PixelDensity)).a;
                    float aU = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, PixelateUV(input.uv + float2(0,  texel.y), _PixelDensity)).a;
                    float minNeighbor = min(min(aL, aR), min(aD, aU));
                    float edge = step(minNeighbor, _OutlineThreshold) * step(_Cutoff, mainTex.a);
                    color = lerp(color, _OutlineColor.rgb, edge);
                #endif

                half3 litColor = rgb * _Color.rgb * input.color.rgb;
                half3 ambient = color * 0.4;
                Light mainLight = GetMainLight();
                half3 outc = ambient + color * mainLight.color;

                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    outc += litColor * addLight.distanceAttenuation * addLight.color * 2.0;
                }
                #endif

                return half4(outc, mainTex.a * input.color.a);
            }
            ENDHLSL
        }

        // ============ URP 2D 렌더러 패스 (Light2D 반응) ============
        Pass
        {
            Name "PropPixel2D"
            Tags { "LightMode"="Universal2D" }
            Cull Off  ZWrite Off  Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert2d
            #pragma fragment frag2d
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
                float _Cutoff;
                float _PixelDensity;
                float _ColorLevels;
                float4 _OutlineColor;
                float _OutlineThreshold;
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

                half3 albedo = rgb * _Color.rgb * input.color.rgb * _Brightness;

                SurfaceData2D sd;
                InputData2D id;
                InitializeSurfaceData(albedo, mainTex.a * input.color.a, half4(1,1,1,1), sd);
                InitializeInputData(puv, input.lightingUV, id);
                return CombinedShapeLightShared(sd, id);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
