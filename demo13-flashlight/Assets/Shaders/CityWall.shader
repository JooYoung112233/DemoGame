Shader "InkCity/CityWall"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}

        [Header(Normal Map)]
        [Toggle(_NORMALMAP)] _NormalMapToggle ("Use Normal Map", Float) = 0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 1.0

        [Header(Tiling)]
        _TileScaleX ("Tile Scale X", Range(0.1, 10)) = 1
        _TileScaleY ("Tile Scale Y", Range(0.1, 10)) = 1

        [Header(Surface)]
        _Tint ("Surface Tint", Color) = (1, 1, 1, 1)
        _Brightness ("Brightness", Range(0, 2)) = 1.0

        [Header(Weathering)]
        [Toggle(_WEATHER_ON)] _WeatherToggle ("Weathering Effect", Float) = 0
        _DirtColor ("Dirt Color", Color) = (0.25, 0.22, 0.18, 1)
        _DirtAmount ("Dirt Amount", Range(0, 1)) = 0.3
        _DirtScale ("Dirt Scale", Range(1, 30)) = 6
        _MoistureBottom ("Bottom Moisture", Range(0, 1)) = 0.3
        _MoistureHeight ("Moisture Height", Range(0, 1)) = 0.25
        _StainColor ("Stain Color", Color) = (0.15, 0.12, 0.10, 1)
        _StainAmount ("Stain Amount", Range(0, 1)) = 0.2

        [Header(Occlusion Fade)]
        _Alpha ("Alpha", Range(0, 1)) = 1.0

        [Header(Doorway Cutout)]
        [Toggle(_WHITE_CUTOUT)] _WhiteCutoutToggle ("흰색 투명화 (문 뚫기)", Float) = 0
        _WhiteThreshold ("White Threshold", Range(0.5, 1)) = 0.9
        _WhiteSoftness ("White Edge Softness", Range(0, 0.3)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "CityWall"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _WEATHER_ON
            #pragma shader_feature_local _WHITE_CUTOUT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BumpMap_ST;
                float _BumpScale;
                float _TileScaleX;
                float _TileScaleY;
                float4 _Tint;
                float _Brightness;
                float4 _DirtColor;
                float _DirtAmount;
                float _DirtScale;
                float _MoistureBottom;
                float _MoistureHeight;
                float4 _StainColor;
                float _StainAmount;
                float _Alpha;
                float _WhiteThreshold;
                float _WhiteSoftness;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                #ifdef _NORMALMAP
                float3 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                #endif
            };

            // ---- procedural noise ----
            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                for (int i = 0; i < octaves; i++)
                {
                    value += amp * valueNoise(p * freq);
                    freq *= 2.0;
                    amp *= 0.5;
                }
                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * float2(_TileScaleX, _TileScaleY);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz).xyz;
                output.normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                output.positionOS = input.positionOS.xyz;
                #ifdef _NORMALMAP
                output.tangentWS = normalize(TransformObjectToWorldDir(input.tangentOS.xyz));
                output.bitangentWS = cross(output.normalWS, output.tangentWS) * input.tangentOS.w;
                #endif
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                half3 rawTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                half3 color = rawTex;
                color *= _Tint.rgb * _Brightness;

                // ---- white doorway cutout ----
                float cutoutAlpha = 1.0;
                #ifdef _WHITE_CUTOUT
                    // 흰색에 가까울수록(모든 채널이 높을수록) 투명
                    float whiteness = min(rawTex.r, min(rawTex.g, rawTex.b));
                    cutoutAlpha = 1.0 - smoothstep(_WhiteThreshold - _WhiteSoftness, _WhiteThreshold, whiteness);
                    clip(cutoutAlpha - 0.01);
                #endif

                #ifdef _WEATHER_ON
                    // ---- dirt / grime patches ----
                    float dirtNoise = fbm(uv * _DirtScale, 4);
                    float dirtMask = smoothstep(0.4 - _DirtAmount * 0.3, 0.75, dirtNoise) * _DirtAmount;
                    color = lerp(color, _DirtColor.rgb, dirtMask);

                    // ---- bottom moisture (ground dampness creeping up) ----
                    // use object-space Y: bottom of cube = -0.5, top = 0.5
                    float heightRatio = saturate((input.positionOS.y + 0.5)); // 0=bottom, 1=top
                    float moistureMask = (1.0 - smoothstep(0.0, _MoistureHeight, heightRatio)) * _MoistureBottom;
                    // add noise to moisture edge
                    float moistureNoise = valueNoise(uv * 8.0 + 17.0);
                    moistureMask *= smoothstep(0.2, 0.6, moistureNoise);
                    color = lerp(color, color * 0.5, moistureMask);

                    // ---- water stains (drip streaks from top) ----
                    float streakX = valueNoise(float2(uv.x * 12.0, 0.0)); // vertical streaks
                    float streakMask = smoothstep(0.6, 0.8, streakX);
                    float streakFade = smoothstep(0.3, 1.0, heightRatio); // stronger near top
                    streakMask *= streakFade * _StainAmount;
                    // make streaks narrow and drippy
                    float dripNoise = valueNoise(float2(uv.x * 20.0, uv.y * 3.0));
                    streakMask *= smoothstep(0.3, 0.7, dripNoise);
                    color = lerp(color, _StainColor.rgb, streakMask);
                #endif

                // ---- normal map ----
                float3 N = normalize(input.normalWS);
                #ifdef _NORMALMAP
                    float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                    float3 T = normalize(input.tangentWS);
                    float3 B = normalize(input.bitangentWS);
                    N = normalize(T * normalTS.x + B * normalTS.y + N * normalTS.z);
                #endif

                // ---- lighting: no NdotL (billboard/flat mesh) ----
                half3 baseColor = color;
                half3 ambient = baseColor * 0.4;

                Light mainLight = GetMainLight();
                color = ambient + baseColor * mainLight.color;

                // additional lights (flashlight)
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    float addAtten = addLight.distanceAttenuation;
                    color += baseColor * addAtten * addLight.color;
                }
                #endif

                return half4(color, _Alpha * cutoutAlpha);
            }
            ENDHLSL
        }

    }
    FallBack "Universal Render Pipeline/Lit"
}
