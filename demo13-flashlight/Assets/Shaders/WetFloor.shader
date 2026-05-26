Shader "InkCity/WetFloor"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.22, 0.21, 0.20, 1)

        [Header(Wetness)]
        _Wetness ("Wetness", Range(0, 1)) = 0
        _WetDarken ("Wet Darken", Range(0, 0.6)) = 0.35
        _WetReflect ("Wet Reflectivity", Range(0, 1)) = 0.6

        [Header(Ripples)]
        _RippleSpeed ("Ripple Speed", Range(0.1, 5)) = 1.0
        _RippleDensity ("Ripple Density", Range(1, 20)) = 8
        _RippleStrength ("Ripple Strength", Range(0, 0.5)) = 0.15
        _RippleColor ("Ripple Highlight", Color) = (0.6, 0.65, 0.8, 0.3)

        [Header(Puddle)]
        _PuddleThreshold ("Puddle Threshold", Range(0, 1)) = 0.4
        _PuddleColor ("Puddle Color", Color) = (0.08, 0.09, 0.12, 1)
        _PuddleReflect ("Puddle Reflectivity", Range(0, 1)) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "WetFloor"
            Tags { "LightMode"="UniversalForward" }

            ZWrite Off

            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float _Wetness;
                float _WetDarken;
                float _WetReflect;
                float _RippleSpeed;
                float _RippleDensity;
                float _RippleStrength;
                float4 _RippleColor;
                float _PuddleThreshold;
                float4 _PuddleColor;
                float _PuddleReflect;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            // ===== 리플(물결) 함수 =====
            // 원형 물결 하나 계산
            float CircleRipple(float2 center, float2 pos, float time, float phase)
            {
                float dist = length(pos - center);
                float wave = sin((dist - time + phase) * 6.283 * 3.0) * 0.5 + 0.5;
                // 감쇠: 거리에 따라 약해짐
                float atten = saturate(1.0 - dist * 4.0);
                // 시간 감쇠: 생겼다 사라짐
                float life = frac(time * 0.3 + phase);
                float lifeAtten = life * (1.0 - life) * 4.0;
                return wave * atten * lifeAtten;
            }

            // 여러 리플 합산
            float RippleEffect(float2 worldXZ, float time, float density)
            {
                float ripple = 0;
                float scale = density;

                // 4개의 랜덤 위치에서 물결
                float2 cell = floor(worldXZ * scale);

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        float2 offset = float2(x, y);
                        float2 cellId = cell + offset;

                        // 셀 기반 pseudo-random 위치
                        float2 rnd = frac(sin(float2(
                            dot(cellId, float2(127.1, 311.7)),
                            dot(cellId, float2(269.5, 183.3))
                        )) * 43758.5453);

                        float2 rippleCenter = (cellId + rnd) / scale;
                        float phase = frac(sin(dot(cellId, float2(12.9898, 78.233))) * 43758.5453);

                        ripple += CircleRipple(rippleCenter, worldXZ, time, phase);
                    }
                }

                return saturate(ripple);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz).xyz;
                output.normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 베이스 텍스처
                half3 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
                float texDetail = dot(baseTex, float3(0.299, 0.587, 0.114));
                half3 color = _BaseColor.rgb * (0.8 + texDetail * 0.4);

                float wet = _Wetness;

                // ===== 웅덩이(Puddle) =====
                // Perlin 노이즈로 웅덩이 분포
                float2 wpos = input.positionWS.xz;
                float puddleNoise = frac(sin(dot(floor(wpos * 3.0), float2(12.9898, 78.233))) * 43758.5453);
                float puddleMask = smoothstep(_PuddleThreshold, _PuddleThreshold + 0.2, puddleNoise) * wet;

                // ===== 젖은 바닥 =====
                // 젖으면 어두워짐
                color *= lerp(1.0, 1.0 - _WetDarken, wet);

                // 웅덩이 영역
                color = lerp(color, _PuddleColor.rgb, puddleMask * 0.7);

                // ===== 리플(빗방울 물결) =====
                float ripple = 0;
                if (wet > 0.01)
                {
                    float time = _Time.y * _RippleSpeed;
                    ripple = RippleEffect(wpos, time, _RippleDensity);
                    ripple *= wet * _RippleStrength;

                    // 리플 하이라이트
                    color += _RippleColor.rgb * ripple * _RippleColor.a;

                    // 웅덩이 위 리플 더 강하게
                    color += _RippleColor.rgb * ripple * puddleMask * 0.3;
                }

                // ===== 라이팅 =====
                half3 baseColor = color;
                half3 ambient = baseColor * 0.4;

                Light mainLight = GetMainLight();
                // 젖은 표면: 메인 라이트 반사 강화
                float reflectBoost = lerp(1.0, 1.0 + _WetReflect, wet);
                color = ambient + baseColor * mainLight.color * reflectBoost;

                // Additional lights (flashlight)
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    float addAtten = addLight.distanceAttenuation;
                    // 젖은 바닥에 손전등 반사 더 강하게
                    float wetReflect = lerp(1.0, 1.0 + _PuddleReflect * 2.0, puddleMask);
                    color += baseColor * addAtten * addLight.color * wetReflect;
                }
                #endif

                return half4(color, 1.0);
            }
            ENDHLSL
        }

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
            Varyings DepthVert(Attributes input) { Varyings o; o.positionCS = TransformObjectToHClip(input.positionOS.xyz); return o; }
            half4 DepthFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack Off
}
