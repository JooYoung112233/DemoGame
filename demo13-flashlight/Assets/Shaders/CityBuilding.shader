Shader "InkCity/CityBuilding"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5

        [Header(Tiling)]
        _TileScaleX ("Tile Scale X", Range(0.1, 10)) = 1
        _TileScaleY ("Tile Scale Y", Range(0.1, 10)) = 1

        [Header(Normal Map)]
        [Toggle(_NORMALMAP)] _NormalMapToggle ("Use Normal Map", Float) = 0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 1.0

        [Header(Window Glow)]
        [Toggle(_GLOW_ON)] _GlowToggle ("Window Glow", Float) = 1
        _GlowMask ("Glow Mask (R=glow area)", 2D) = "black" {}
        _GlowColor ("Glow Color", Color) = (1.0, 0.85, 0.5, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.5
        _GlowMin ("Glow Min Brightness", Range(0, 1)) = 0.6
        _GlowSpread ("Glow Spread", Range(0, 30)) = 8.0
        _GlowSpreadIntensity ("Spread Intensity", Range(0, 1)) = 0.3

        [Header(Glow Flicker)]
        [Toggle(_FLICKER_ON)] _FlickerToggle ("Flicker", Float) = 0
        _FlickerSpeed ("Flicker Speed", Range(0.5, 10)) = 3.0
        _FlickerAmount ("Flicker Amount", Range(0, 0.5)) = 0.15

        [Header(Surface Detail)]
        _DetailStrength ("Detail Enhance", Range(0, 1)) = 0.3
        _Desaturation ("Desaturation", Range(0, 1)) = 0.0
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _LightBoost ("Light Boost", Range(1, 5)) = 2.0

        [Header(Height Fade)]
        [Toggle(_HEIGHTFADE_ON)] _HeightFadeToggle ("Height Fade", Float) = 1
        _HeightFadeMask ("Fade Mask (R=fade area)", 2D) = "black" {}
        _HeightFadeColor ("Fade Color", Color) = (0.02, 0.02, 0.04, 1)
        _HeightFadeAmount ("Fade Amount", Range(0, 1)) = 0.6

        [Header(Weathering)]
        [Toggle(_WEATHER_ON)] _WeatherToggle ("Weathering Effect", Float) = 0
        _DirtColor ("Dirt Color", Color) = (0.25, 0.22, 0.18, 1)
        _DirtAmount ("Dirt Amount", Range(0, 1)) = 0.3
        _DirtScale ("Dirt Scale", Range(1, 30)) = 6
        _MoistureBottom ("Bottom Moisture", Range(0, 1)) = 0.3
        _MoistureHeight ("Moisture Height", Range(0, 1)) = 0.25
        _StainColor ("Stain Color", Color) = (0.15, 0.12, 0.10, 1)
        _StainAmount ("Stain Amount", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "CityBuilding"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _GLOW_ON
            #pragma shader_feature_local _FLICKER_ON
            #pragma shader_feature_local _HEIGHTFADE_ON
            #pragma shader_feature_local _WEATHER_ON
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BumpMap_ST;
                float _BumpScale;
                float4 _Color;
                float _Cutoff;
                float _TileScaleX;
                float _TileScaleY;
                float4 _GlowColor;
                float _GlowIntensity;
                float _GlowMin;
                float _GlowSpread;
                float _GlowSpreadIntensity;
                float4 _HeightFadeColor;
                float _HeightFadeAmount;
                float _FlickerSpeed;
                float _FlickerAmount;
                float _DetailStrength;
                float _Desaturation;
                float _Brightness;
                float _LightBoost;
                float4 _DirtColor;
                float _DirtAmount;
                float _DirtScale;
                float _MoistureBottom;
                float _MoistureHeight;
                float4 _StainColor;
                float _StainAmount;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_GlowMask);
            SAMPLER(sampler_GlowMask);
            float4 _GlowMask_TexelSize;
            TEXTURE2D(_HeightFadeMask);
            SAMPLER(sampler_HeightFadeMask);

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

            // ---- noise ----
            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
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
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // alpha cutout
                clip(mainTex.a - _Cutoff);

                half3 color = mainTex.rgb;
                color *= _Color.rgb * _Brightness;

                // ---- surface detail enhancement ----
                float luma = dot(color, float3(0.299, 0.587, 0.114));
                // local contrast boost
                float detail = luma - dot(color, float3(0.333, 0.333, 0.334));
                color += detail * _DetailStrength;
                // desaturation
                color = lerp(color, half3(luma, luma, luma), _Desaturation);

                // ---- weathering ----
                #ifdef _WEATHER_ON
                    float dirtNoise = fbm(uv * _DirtScale, 4);
                    float dirtMask = smoothstep(0.4 - _DirtAmount * 0.3, 0.75, dirtNoise) * _DirtAmount;
                    color = lerp(color, _DirtColor.rgb, dirtMask);

                    // bottom moisture (object-space Y: -0.5=bottom, 0.5=top for cube)
                    float heightRatio = saturate(input.positionOS.y + 0.5);
                    float moistureMask = (1.0 - smoothstep(0.0, _MoistureHeight, heightRatio)) * _MoistureBottom;
                    float moistureNoise = valueNoise(uv * 8.0 + 17.0);
                    moistureMask *= smoothstep(0.2, 0.6, moistureNoise);
                    color = lerp(color, color * 0.5, moistureMask);

                    // water stain streaks from top
                    float streakX = valueNoise(float2(uv.x * 12.0, 0.0));
                    float streakMask = smoothstep(0.6, 0.8, streakX);
                    float streakFade = smoothstep(0.3, 1.0, heightRatio);
                    streakMask *= streakFade * _StainAmount;
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

                // ---- lighting: no NdotL (Quad/billboard mesh has flat normal) ----
                half3 baseColor = color;
                half3 ambient = baseColor * 0.4;
                // 손전등용: Brightness 적용 전 원본 색
                half3 litColor = mainTex.rgb * _Color.rgb;

                Light mainLight = GetMainLight();
                color = ambient + baseColor * mainLight.color;

                // additional lights (flashlight) — 원본 색 기준
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    float addAtten = addLight.distanceAttenuation;
                    color += litColor * addAtten * addLight.color * _LightBoost;
                }
                #endif

                // ---- window glow (additive, after lighting) ----
                #ifdef _GLOW_ON
                    float2 glowUV = input.uv;
                    float glowMask = SAMPLE_TEXTURE2D(_GlowMask, sampler_GlowMask, glowUV).r;

                    // soft spread: blur-sample the glow mask to bleed light outward
                    float spreadMask = 0;
                    if (_GlowSpread > 0)
                    {
                        float2 spreadTexel = _GlowMask_TexelSize.xy * _GlowSpread;
                        // 13-tap blur (center + 12 surrounding)
                        spreadMask += SAMPLE_TEXTURE2D_LOD(_GlowMask, sampler_GlowMask, glowUV + float2(-1, 0) * spreadTexel, 0).r;
                        spreadMask += SAMPLE_TEXTURE2D_LOD(_GlowMask, sampler_GlowMask, glowUV + float2( 1, 0) * spreadTexel, 0).r;
                        spreadMask += SAMPLE_TEXTURE2D_LOD(_GlowMask, sampler_GlowMask, glowUV + float2( 0,-1) * spreadTexel, 0).r;
                        spreadMask += SAMPLE_TEXTURE2D_LOD(_GlowMask, sampler_GlowMask, glowUV + float2( 0, 1) * spreadTexel, 0).r;
                        spreadMask += SAMPLE_TEXTURE2D_LOD(_GlowMask, sampler_GlowMask, glowUV + float2(-1,-1) * spreadTexel * 0.7, 0).r;
                        spreadMask += SAMPLE_TEXTURE2D_LOD(_GlowMask, sampler_GlowMask, glowUV + float2( 1,-1) * spreadTexel * 0.7, 0).r;
                        spreadMask += SAMPLE_TEXTURE2D_LOD(_GlowMask, sampler_GlowMask, glowUV + float2(-1, 1) * spreadTexel * 0.7, 0).r;
                        spreadMask += SAMPLE_TEXTURE2D_LOD(_GlowMask, sampler_GlowMask, glowUV + float2( 1, 1) * spreadTexel * 0.7, 0).r;
                        // outer ring (wider)
                        spreadMask += SAMPLE_TEXTURE2D_LOD(_GlowMask, sampler_GlowMask, glowUV + float2(-2, 0) * spreadTexel * 0.5, 0).r;
                        spreadMask += SAMPLE_TEXTURE2D_LOD(_GlowMask, sampler_GlowMask, glowUV + float2( 2, 0) * spreadTexel * 0.5, 0).r;
                        spreadMask += SAMPLE_TEXTURE2D_LOD(_GlowMask, sampler_GlowMask, glowUV + float2( 0,-2) * spreadTexel * 0.5, 0).r;
                        spreadMask += SAMPLE_TEXTURE2D_LOD(_GlowMask, sampler_GlowMask, glowUV + float2( 0, 2) * spreadTexel * 0.5, 0).r;
                        spreadMask /= 12.0;
                    }

                    // combine: direct glow + soft spread halo
                    float combinedGlow = max(glowMask, spreadMask);

                    if (combinedGlow > 0.01)
                    {
                        float glowFactor = _GlowIntensity;

                        #ifdef _FLICKER_ON
                            float flickerSeed = floor(glowUV.x * 20.0) + floor(glowUV.y * 30.0) * 7.0;
                            float flicker = sin(_Time.y * _FlickerSpeed + flickerSeed * 3.7) * 0.5 + 0.5;
                            float flickerNoise = hash11(flickerSeed + floor(_Time.y * 0.5));
                            flicker = lerp(flicker, flickerNoise, 0.3);
                            glowFactor *= lerp(1.0 - _FlickerAmount, 1.0, flicker);
                        #endif

                        half3 windowGlow = _GlowColor.rgb * glowFactor;

                        // direct window area: full glow
                        float directBlend = glowMask * saturate(_GlowMin + (1.0 - _GlowMin) * glowFactor);
                        // spread area: softer, subtler
                        float spreadBlend = saturate(spreadMask * _GlowSpreadIntensity * glowFactor);
                        // final: direct takes priority, spread fills surrounding
                        float finalBlend = max(directBlend, spreadBlend * (1.0 - glowMask));

                        color = lerp(color, windowGlow, finalBlend);
                    }
                #endif

                // ---- height fade (texture-driven fog/darkness) ----
                #ifdef _HEIGHTFADE_ON
                    float fadeMask = SAMPLE_TEXTURE2D(_HeightFadeMask, sampler_HeightFadeMask, input.uv).r;
                    color = lerp(color, _HeightFadeColor.rgb, fadeMask * _HeightFadeAmount);
                #endif

                return half4(color, mainTex.a);
            }
            ENDHLSL
        }

    }
    FallBack "Universal Render Pipeline/Lit"
}
