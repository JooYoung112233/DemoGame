Shader "InkCity/RuinFloor"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _LightBoost ("Light Boost", Range(1, 5)) = 2.0

        [Header(Normal Map)]
        [Toggle(_NORMALMAP)] _NormalMapToggle ("Use Normal Map", Float) = 0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 1.0

        [Header(Ruin Effect)]
        [Toggle(_RUIN_ON)] _RuinToggle ("Ruin Effect", Float) = 1

        [Header(Dust)]
        _DustAmount ("Dust Amount", Range(0, 1)) = 0.4
        _DustColor ("Dust Color", Color) = (0.35, 0.32, 0.25, 1)
        _DustScale ("Dust Noise Scale", Range(1, 50)) = 12
        _DustRoughness ("Dust Roughness", Range(0, 1)) = 0.6

        [Header(Cracks)]
        _CrackAmount ("Crack Amount", Range(0, 1)) = 0.5
        _CrackColor ("Crack Color", Color) = (0.05, 0.04, 0.03, 1)
        _CrackScale ("Crack Scale", Range(1, 30)) = 8
        _CrackWidth ("Crack Width", Range(0.01, 0.15)) = 0.06

        [Header(Weathering)]
        _WeatherTint ("Weather Tint", Color) = (0.6, 0.55, 0.45, 1)
        _WeatherAmount ("Weather Amount", Range(0, 1)) = 0.3
        _DarkenEdges ("Edge Darkening", Range(0, 1)) = 0.4
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "RuinFloor"
            Tags { "LightMode"="UniversalForward" }

            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _RUIN_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BumpMap_ST;
                float _BumpScale;
                float4 _Color;
                float _Brightness;
                float _LightBoost;
                float _DustAmount;
                float4 _DustColor;
                float _DustScale;
                float _DustRoughness;
                float _CrackAmount;
                float4 _CrackColor;
                float _CrackScale;
                float _CrackWidth;
                float4 _WeatherTint;
                float _WeatherAmount;
                float _DarkenEdges;
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
                #ifdef _NORMALMAP
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                #endif
            };

            // ---- noise functions (procedural) ----

            float2 hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // value noise
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // fractal brownian motion (layered noise)
            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;

                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * valueNoise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            // voronoi for cracks
            float voronoi(float2 p, out float2 nearestDiff)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float minDist = 1.0;
                float secondDist = 1.0;
                nearestDiff = float2(0, 0);

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 pt = hash22(i + neighbor);
                        float2 diff = neighbor + pt - f;
                        float d = dot(diff, diff);

                        if (d < minDist)
                        {
                            secondDist = minDist;
                            minDist = d;
                            nearestDiff = diff;
                        }
                        else if (d < secondDist)
                        {
                            secondDist = d;
                        }
                    }
                }

                // edge distance (crack-like)
                return secondDist - minDist;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz).xyz;
                output.normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                #ifdef _NORMALMAP
                output.tangentWS = normalize(TransformObjectToWorldDir(input.tangentOS.xyz));
                output.bitangentWS = cross(output.normalWS, output.tangentWS) * input.tangentOS.w;
                #endif
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
                half3 color = baseTex * _Color.rgb * _Brightness;

                #ifdef _RUIN_ON
                    float2 uv = input.uv;

                    // ---- dust layer ----
                    float dustNoise = fbm(uv * _DustScale, 4);
                    // make dust patchy (not uniform)
                    float dustMask = smoothstep(0.3 - _DustAmount * 0.3, 0.7, dustNoise);
                    // roughen dust with high-freq noise
                    float dustDetail = valueNoise(uv * _DustScale * 4.0);
                    dustMask *= lerp(1.0, dustDetail, _DustRoughness);
                    color = lerp(color, _DustColor.rgb, dustMask * _DustAmount);

                    // ---- cracks (voronoi edge) ----
                    float2 nearDiff;
                    float crackEdge = voronoi(uv * _CrackScale, nearDiff);
                    float crackLine = 1.0 - smoothstep(0.0, _CrackWidth, crackEdge);
                    // modulate crack visibility with amount
                    float crackNoise = valueNoise(uv * _CrackScale * 0.5);
                    crackLine *= step(1.0 - _CrackAmount, crackNoise);
                    color = lerp(color, _CrackColor.rgb, crackLine);

                    // darken inside cracks slightly for depth
                    float crackDepth = saturate(crackLine * 0.5);
                    color *= (1.0 - crackDepth * 0.3);

                    // ---- weathering / aging tint ----
                    float weatherNoise = fbm(uv * 5.0 + 42.0, 3);
                    float weatherMask = smoothstep(0.3, 0.8, weatherNoise) * _WeatherAmount;
                    color = lerp(color, color * _WeatherTint.rgb, weatherMask);

                    // ---- edge darkening (vignette-like per tile) ----
                    float2 edgeDist = abs(uv - 0.5) * 2.0; // 0 center, 1 edge
                    float edgeFactor = max(edgeDist.x, edgeDist.y);
                    edgeFactor = smoothstep(0.6, 1.0, edgeFactor);
                    color *= (1.0 - edgeFactor * _DarkenEdges * 0.5);
                #endif

                // lighting: no NdotL (billboard sprite has flat normal facing wrong dir)
                half3 baseColor = color;
                half3 ambient = baseColor * 0.4;

                // litColor: 손전등용 — Brightness 무시한 원본 텍스쳐 (CityBuilding과 동일 패턴)
                half3 litColor = baseTex * _Color.rgb;

                Light mainLight = GetMainLight();
                color = ambient + baseColor * mainLight.color;

                // additional lights (flashlight) — LightBoost로 건물과 반응 일치
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    float addAtten = addLight.distanceAttenuation;
                    color += litColor * addAtten * addLight.color * _LightBoost;
                }
                #endif

                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // DepthOnly — depth prepass에서도 ZWrite Off 강제
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite Off
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings DepthVert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }
            half4 DepthFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        // DepthNormals — depth normals prepass에서도 ZWrite Off 강제
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite Off
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthVert2
            #pragma fragment DepthFrag2
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes2 { float4 positionOS : POSITION; };
            struct Varyings2 { float4 positionCS : SV_POSITION; };
            Varyings2 DepthVert2(Attributes2 input)
            {
                Varyings2 o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }
            half4 DepthFrag2(Varyings2 input) : SV_Target { return 0; }
            ENDHLSL
        }

    }
    FallBack Off
}
