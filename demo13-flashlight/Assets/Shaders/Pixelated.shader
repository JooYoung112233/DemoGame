Shader "BRB/Pixelated"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _LightBoost ("Light Boost", Range(1, 5)) = 2.0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Pixelation)]
        [Toggle(_PIXELATE_ON)] _PixelateToggle ("픽셀화 ON", Float) = 1
        // 가로/세로 픽셀 해상도 — 낮을수록 도트가 굵어짐
        _PixelDensity ("Pixel Density (격자 수)", Range(8, 2048)) = 64

        [Header(Color Quantize)]
        [Toggle(_QUANTIZE_ON)] _QuantizeToggle ("색 단계화 (포스터라이즈)", Float) = 1
        // 채널당 색 단계 수 — 낮을수록 레트로함
        _ColorLevels ("Color Levels", Range(2, 32)) = 8

        [Header(Outline)]
        [Toggle(_OUTLINE_ON)] _OutlineToggle ("픽셀 외곽선", Float) = 0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThreshold ("Outline Threshold", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Pixelated"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

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
                float _LightBoost;
                float _Cutoff;
                float _PixelDensity;
                float _ColorLevels;
                float4 _OutlineColor;
                float _OutlineThreshold;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz).xyz;
                return output;
            }

            // UV를 격자 중심으로 스냅 → 픽셀화
            float2 PixelateUV(float2 uv, float density)
            {
                float2 grid = float2(density, density);
                return (floor(uv * grid) + 0.5) / grid;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // ---- 픽셀화: UV를 격자에 스냅 (OFF면 원본 UV) ----
                #ifdef _PIXELATE_ON
                    float2 puv = PixelateUV(input.uv, _PixelDensity);
                #else
                    float2 puv = input.uv;
                #endif
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, puv);

                // 알파 컷아웃 (도트는 하드 엣지)
                clip(mainTex.a - _Cutoff);

                half3 rgb = mainTex.rgb;

                // ---- 색 단계화 (포스터라이즈) ----
                #ifdef _QUANTIZE_ON
                    float levels = max(_ColorLevels, 2.0);
                    rgb = floor(rgb * levels + 0.5) / levels;
                #endif

                half3 color = rgb * _Color.rgb * _Brightness;

                // ---- 픽셀 외곽선 (인접 픽셀 알파 차이 검출) ----
                #ifdef _OUTLINE_ON
                    float2 texel = 1.0 / float2(_PixelDensity, _PixelDensity);
                    float aL = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, PixelateUV(input.uv + float2(-texel.x, 0), _PixelDensity)).a;
                    float aR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, PixelateUV(input.uv + float2( texel.x, 0), _PixelDensity)).a;
                    float aD = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, PixelateUV(input.uv + float2(0, -texel.y), _PixelDensity)).a;
                    float aU = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, PixelateUV(input.uv + float2(0,  texel.y), _PixelDensity)).a;
                    float minNeighbor = min(min(aL, aR), min(aD, aU));
                    // 자신은 불투명인데 이웃이 비면 외곽선
                    float edge = step(minNeighbor, _OutlineThreshold) * step(_Cutoff, mainTex.a);
                    color = lerp(color, _OutlineColor.rgb, edge);
                #endif

                // ---- lighting: NdotL 없음 (빌보드 평면) ----
                half3 baseColor = color;
                half3 ambient = baseColor * 0.4;
                // litColor: 손전등용 — Brightness 무시한 원본 (다른 셰이더와 동일 패턴)
                half3 litColor = rgb * _Color.rgb;

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

                return half4(color, mainTex.a);
            }
            ENDHLSL
        }

        // ---- URP 2D 렌더러(Renderer2D)용 패스 ----
        Pass
        {
            Name "Pixelated2D"
            Tags { "LightMode"="Universal2D" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

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
                float _LightBoost;
                float _Cutoff;
                float _PixelDensity;
                float _ColorLevels;
                float4 _OutlineColor;
                float _OutlineThreshold;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes2D
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings2D
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                half2 lightingUV : TEXCOORD1;
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
                clip(mainTex.a - _Cutoff);

                half3 rgb = mainTex.rgb;
                #ifdef _QUANTIZE_ON
                    float levels = max(_ColorLevels, 2.0);
                    rgb = floor(rgb * levels + 0.5) / levels;
                #endif

                // SpriteRenderer 정점 컬러 틴트 × 머티리얼 틴트 × 밝기
                half3 albedo = rgb * _Color.rgb * input.color.rgb * _Brightness;

                // 2D 라이트(손전등) 적용
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
