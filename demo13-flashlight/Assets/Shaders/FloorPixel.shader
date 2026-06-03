Shader "BRB/FloorPixel"
{
    // 바닥(Tilemap/스프라이트)용 픽셀 셰이더. 도트/색단계/Light2D 처리.
    // 바닥은 불투명이라 알파 컷아웃 없음. Tilemap 정점컬러 지원.
    // 타일 반복 깨기(_BREAKUP_ON): 월드 좌표 대형 노이즈로 명암을 변주 → 같은 타일이 깔려도 격자감 사라짐.
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _LightBoost ("Light Boost", Range(1, 5)) = 2.0

        [Header(Tiling Breakup)]
        [Toggle(_BREAKUP_ON)] _BreakupToggle ("타일 반복 깨기 ON", Float) = 0
        _BreakupScale ("Breakup Scale (월드 빈도)", Range(0.05, 4)) = 0.6
        _BreakupAmount ("Breakup Amount (세기)", Range(0, 1)) = 0.35

        [Header(Pixelation)]
        [Toggle(_PIXELATE_ON)] _PixelateToggle ("픽셀화 ON", Float) = 1
        _PixelDensity ("Pixel Density (격자 수)", Range(8, 2048)) = 64

        [Header(Color Quantize)]
        [Toggle(_QUANTIZE_ON)] _QuantizeToggle ("색 단계화 (포스터라이즈)", Float) = 1
        _ColorLevels ("Color Levels", Range(2, 32)) = 8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        // ============ 3D URP Forward 패스 ============
        Pass
        {
            Name "FloorPixelForward"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _BREAKUP_ON
            #pragma shader_feature_local _PIXELATE_ON
            #pragma shader_feature_local _QUANTIZE_ON
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Brightness;
                float _LightBoost;
                float _BreakupScale;
                float _BreakupAmount;
                float _PixelDensity;
                float _ColorLevels;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }
            float VNoise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            float Fbm(float2 p)
            {
                float s = 0.0, a = 0.5;
                [unroll] for (int i = 0; i < 4; i++) { s += a * VNoise(p); p *= 2.0; a *= 0.5; }
                return s;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
            };

            float2 PixelateUV(float2 uv, float density)
            {
                float2 grid = float2(density, density);
                return (floor(uv * grid) + 0.5) / grid;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz).xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                #ifdef _PIXELATE_ON
                    float2 puv = PixelateUV(input.uv, _PixelDensity);
                #else
                    float2 puv = input.uv;
                #endif
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, puv);

                half3 rgb = mainTex.rgb;
                #ifdef _QUANTIZE_ON
                    float levels = max(_ColorLevels, 2.0);
                    rgb = floor(rgb * levels + 0.5) / levels;
                #endif

                #ifdef _BREAKUP_ON
                    // 월드 좌표 노이즈로 명암 변주(타일 반복 깨기). 타일 경계와 무관하게 연속.
                    float bn = Fbm(input.positionWS.xy * _BreakupScale);
                    rgb *= lerp(1.0, 0.55 + 0.9 * bn, _BreakupAmount);
                #endif

                half3 color = rgb * _Color.rgb * input.color.rgb * _Brightness;

                half3 baseColor = color;
                half3 ambient = baseColor * 0.4;
                half3 litColor = rgb * _Color.rgb * input.color.rgb;

                Light mainLight = GetMainLight();
                color = ambient + baseColor * mainLight.color;

                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    float addAtten = addLight.distanceAttenuation;
                    color += litColor * addAtten * addLight.color * _LightBoost;
                }
                #endif

                return half4(color, mainTex.a * input.color.a);
            }
            ENDHLSL
        }

        // ============ URP 2D 렌더러 패스 (Light2D 반응) ============
        Pass
        {
            Name "FloorPixel2D"
            Tags { "LightMode"="Universal2D" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert2d
            #pragma fragment frag2d
            #pragma shader_feature_local _BREAKUP_ON
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
                float _LightBoost;
                float _BreakupScale;
                float _BreakupAmount;
                float _PixelDensity;
                float _ColorLevels;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }
            float VNoise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            float Fbm(float2 p)
            {
                float s = 0.0, a = 0.5;
                [unroll] for (int i = 0; i < 4; i++) { s += a * VNoise(p); p *= 2.0; a *= 0.5; }
                return s;
            }

            struct Attributes2D { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings2D
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                half2 lightingUV : TEXCOORD1;
                float2 worldXY : TEXCOORD2;
            };

            float2 PixelateUV2D(float2 uv, float density)
            {
                float2 grid = float2(density, density);
                return (floor(uv * grid) + 0.5) / grid;
            }

            Varyings2D vert2d(Attributes2D input)
            {
                Varyings2D output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.lightingUV = half2(ComputeScreenPos(output.positionCS / output.positionCS.w).xy);
                output.worldXY = TransformObjectToWorld(input.positionOS.xyz).xy;
                return output;
            }

            half4 frag2d(Varyings2D input) : SV_Target
            {
                #ifdef _PIXELATE_ON
                    float2 puv = PixelateUV2D(input.uv, _PixelDensity);
                #else
                    float2 puv = input.uv;
                #endif
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, puv);

                half3 rgb = mainTex.rgb;
                #ifdef _QUANTIZE_ON
                    float levels = max(_ColorLevels, 2.0);
                    rgb = floor(rgb * levels + 0.5) / levels;
                #endif

                #ifdef _BREAKUP_ON
                    // 월드 좌표 노이즈로 명암 변주(타일 반복 깨기). 화면이 아니라 월드 기준이라 카메라 이동에도 안정.
                    float bn = Fbm(input.worldXY * _BreakupScale);
                    rgb *= lerp(1.0, 0.55 + 0.9 * bn, _BreakupAmount);
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
