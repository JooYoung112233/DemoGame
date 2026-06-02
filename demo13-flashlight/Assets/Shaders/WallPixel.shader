Shader "BRB/WallPixel"
{
    // 벽 전용 픽셀(도트) 셰이더. BRB/Pixelated와 동일한 픽셀화/색단계/Light2D 처리 +
    // 벽 전용: 문(흰색) 뚫기, 오클루전 알파 페이드.
    // _SHADOW_MODE 켜면 같은 셰이더가 "방향성 투영 + 밑동 접지" 그림자로 동작
    //   (그림자 자식 오브젝트가 이 머티리얼을 모드 ON으로 사용 — FlatShadow.cs가 자동 처리).
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _LightBoost ("Light Boost", Range(1, 5)) = 2.0
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

        [Header(Doorway Cutout)]
        [Toggle(_WHITE_CUTOUT)] _WhiteCutoutToggle ("흰색 투명화 (문 뚫기)", Float) = 0
        _WhiteThreshold ("White Threshold", Range(0.5, 1)) = 0.85
        _WhiteSoftness ("White Edge Softness", Range(0, 0.3)) = 0.08

        [Header(Occlusion Fade)]
        _Alpha ("Alpha (오클루전 페이드)", Range(0, 1)) = 1.0

        [Header(Shadow Mode (그림자 자식용))]
        _ShadowColor ("Shadow Color", Color) = (0,0,0,1)
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 0.55
        _TipFade ("Tip Fade", Range(0,1)) = 0.35
        _ContactStrength ("Contact Strength (밑동)", Range(0,1)) = 0.5
        _ContactHeight ("Contact Height", Range(0.01,0.6)) = 0.15
        [Toggle] _ShadowFlipV ("Shadow Flip V", Float) = 0
        _ShadowDirWS ("Shadow Dir WS (xy)", Vector) = (1,0,0,0)
        _ShadowLength ("Shadow Length", Range(0,5)) = 1.2
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        // ============ 3D URP Forward 패스 ============
        Pass
        {
            Name "WallPixelForward"
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
            #pragma shader_feature_local _WHITE_CUTOUT
            #pragma multi_compile_local _ _SHADOW_MODE
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
                float _WhiteThreshold;
                float _WhiteSoftness;
                float _Alpha;
                float4 _ShadowColor;
                float _ShadowStrength;
                float _TipFade;
                float _ContactStrength;
                float _ContactHeight;
                float _ShadowFlipV;
                float4 _ShadowDirWS;
                float _ShadowLength;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float height : TEXCOORD2;
            };

            float2 PixelateUV(float2 uv, float density)
            {
                float2 grid = float2(density, density);
                return (floor(uv * grid) + 0.5) / grid;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float h = (_ShadowFlipV > 0.5) ? (1.0 - input.uv.y) : input.uv.y;
                #ifdef _SHADOW_MODE
                    posWS.x += _ShadowDirWS.x * h * _ShadowLength;
                    posWS.y += _ShadowDirWS.y * h * _ShadowLength;
                #endif
                output.positionWS = posWS;
                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.height = h;
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
                clip(mainTex.a - _Cutoff);

                half3 rgb = mainTex.rgb;

                // ---- 문(흰색) 뚫기 — 그림자도 문은 안 드리움 ----
                float cutoutAlpha = 1.0;
                #ifdef _WHITE_CUTOUT
                    float whiteness = min(rgb.r, min(rgb.g, rgb.b));
                    cutoutAlpha = 1.0 - smoothstep(_WhiteThreshold - _WhiteSoftness, _WhiteThreshold, whiteness);
                    clip(cutoutAlpha - 0.01);
                #endif

                // ===== 그림자 모드 =====
                #ifdef _SHADOW_MODE
                    float h = saturate(input.height);
                    float proj = _ShadowStrength * lerp(1.0, _TipFade, h);
                    float contact = _ContactStrength * (1.0 - smoothstep(0.0, _ContactHeight, h));
                    return half4(_ShadowColor.rgb, mainTex.a * cutoutAlpha * max(proj, contact));
                #else

                #ifdef _QUANTIZE_ON
                    float levels = max(_ColorLevels, 2.0);
                    rgb = floor(rgb * levels + 0.5) / levels;
                #endif

                half3 color = rgb * _Color.rgb * _Brightness;

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

                half3 baseColor = color;
                half3 ambient = baseColor * 0.4;
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

                return half4(color, mainTex.a * cutoutAlpha * _Alpha);
                #endif // _SHADOW_MODE
            }
            ENDHLSL
        }

        // ============ URP 2D 렌더러 패스 ============
        Pass
        {
            Name "WallPixel2D"
            Tags { "LightMode"="Universal2D" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert2d
            #pragma fragment frag2d
            #pragma shader_feature_local _PIXELATE_ON
            #pragma shader_feature_local _QUANTIZE_ON
            #pragma shader_feature_local _WHITE_CUTOUT
            #pragma multi_compile_local _ _SHADOW_MODE

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
                float _WhiteThreshold;
                float _WhiteSoftness;
                float _Alpha;
                float4 _ShadowColor;
                float _ShadowStrength;
                float _TipFade;
                float _ContactStrength;
                float _ContactHeight;
                float _ShadowFlipV;
                float4 _ShadowDirWS;
                float _ShadowLength;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes2D { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings2D
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                half2 lightingUV : TEXCOORD1;
                float height : TEXCOORD2;
            };

            float2 PixelateUV2D(float2 uv, float density)
            {
                float2 grid = float2(density, density);
                return (floor(uv * grid) + 0.5) / grid;
            }

            Varyings2D vert2d(Attributes2D input)
            {
                Varyings2D output;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float h = (_ShadowFlipV > 0.5) ? (1.0 - input.uv.y) : input.uv.y;
                #ifdef _SHADOW_MODE
                    posWS.x += _ShadowDirWS.x * h * _ShadowLength;
                    posWS.y += _ShadowDirWS.y * h * _ShadowLength;
                #endif
                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.lightingUV = half2(ComputeScreenPos(output.positionCS / output.positionCS.w).xy);
                output.height = h;
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

                float cutoutAlpha = 1.0;
                #ifdef _WHITE_CUTOUT
                    float whiteness = min(rgb.r, min(rgb.g, rgb.b));
                    cutoutAlpha = 1.0 - smoothstep(_WhiteThreshold - _WhiteSoftness, _WhiteThreshold, whiteness);
                    clip(cutoutAlpha - 0.01);
                #endif

                // ===== 그림자 모드 =====
                #ifdef _SHADOW_MODE
                    float h = saturate(input.height);
                    float proj = _ShadowStrength * lerp(1.0, _TipFade, h);
                    float contact = _ContactStrength * (1.0 - smoothstep(0.0, _ContactHeight, h));
                    return half4(_ShadowColor.rgb, mainTex.a * cutoutAlpha * max(proj, contact));
                #else

                #ifdef _QUANTIZE_ON
                    float levels = max(_ColorLevels, 2.0);
                    rgb = floor(rgb * levels + 0.5) / levels;
                #endif

                half3 albedo = rgb * _Color.rgb * input.color.rgb * _Brightness;

                SurfaceData2D sd;
                InputData2D id;
                InitializeSurfaceData(albedo, mainTex.a * input.color.a * cutoutAlpha * _Alpha, half4(1,1,1,1), sd);
                InitializeInputData(puv, input.lightingUV, id);
                return CombinedShapeLightShared(sd, id);
                #endif // _SHADOW_MODE
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
