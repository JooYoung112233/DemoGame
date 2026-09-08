// 스타일라이즈드 로우폴리 — 레퍼런스(각진 저폴리 + 무광 + 부드러운 명암 + 틈새 AO) 대응.
//
// 이 룩을 만드는 네 요소 중 셰이더가 맡는 것:
//   ① **무광** — 스페큘러 없음. URP/Lit의 하이라이트가 플라스틱처럼 보이던 것을 없앤다.
//   ② **부드러운 명암 경계** — wrapped diffuse. NdotL을 그대로 쓰면 각진 면에서 경계가
//      너무 딱딱하다. (NdotL + w)/(1+w)로 빛을 뒤쪽까지 감아 손그림 느낌을 낸다.
//   ③ **림라이트** — 실루엣 가장자리를 살짝 들어올려 배경에서 분리한다. 어두운 팔레트가
//      배경에 묻히던 문제(3d-migration.md Stage 0 함정 ③)에도 직접 듣는다.
//   ④ **바닥 그림자색 틴트** — 그림자를 검정이 아니라 차가운 색으로 물들인다.
// 나머지(틈새 AO)는 렌더러의 SSAO가 맡는다.
//
// 컷어웨이가 필요한 오브젝트는 Spike/OccluderFX를 쓴다. 이 셰이더는 캐릭터·소품용이다.
Shader "BRB/Stylized"
{
    Properties
    {
        _BaseColor   ("Base Color", Color) = (0.5, 0.5, 0.5, 1)
        _Wrap        ("Light Wrap", Range(0, 1)) = 0.28
        _ShadowTint  ("Shadow Tint", Color) = (0.42, 0.47, 0.58, 1)
        _ShadowDepth ("Shadow Depth", Range(0, 1)) = 0.42
        _RimColor    ("Rim Color", Color) = (1, 0.96, 0.88, 1)
        _RimPower    ("Rim Power", Range(0.5, 8)) = 3.0
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.18
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _Wrap;
                float4 _ShadowTint;
                float  _ShadowDepth;
                float4 _RimColor;
                float  _RimPower;
                float  _RimStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 screenPos  : TEXCOORD2;
            };

            Varyings vert (Attributes IN)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                o.screenPos  = ComputeScreenPos(p.positionCS);
                return o;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float4 sc = TransformWorldToShadowCoord(IN.positionWS);
                Light L = GetMainLight(sc);

                // ② wrapped diffuse — 각진 면에서 명암 경계를 부드럽게
                float ndl  = dot(N, L.direction);
                float wrap = saturate((ndl + _Wrap) / (1.0 + _Wrap));
                float lit  = wrap * L.shadowAttenuation;

                // ④ 그림자를 검정이 아니라 찬 색으로
                float3 shadowCol = _BaseColor.rgb * lerp(1.0, 1.0 - _ShadowDepth, 1.0 - lit) * _ShadowTint.rgb;
                float3 litCol    = _BaseColor.rgb * L.color;
                float3 col = lerp(shadowCol, litCol, lit);

                // 앰비언트 — 하늘/땅 그라디언트가 형태를 읽히게 한다
                col += _BaseColor.rgb * SampleSH(N) * 1.25;

                // ③ 림 — 실루엣을 배경에서 떼어낸다
                float rim = pow(1.0 - saturate(dot(N, V)), _RimPower);
                col += _RimColor.rgb * rim * _RimStrength;

                // ⑤ 틈새 AO(렌더러의 SSAO)
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    float2 nuv = IN.screenPos.xy / max(IN.screenPos.w, 1e-6);
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(nuv);
                    col *= ao.indirectAmbientOcclusion;
                #endif

                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float _Wrap; float4 _ShadowTint; float _ShadowDepth;
                float4 _RimColor; float _RimPower; float _RimStrength;
            CBUFFER_END

            struct SA { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct SV { float4 positionCS : SV_POSITION; };

            SV shadowVert (SA IN)
            {
                SV o;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nrmWS = TransformObjectToWorldNormal(IN.normalOS);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, nrmWS, _MainLightPosition.xyz));
                return o;
            }
            half4 shadowFrag (SV IN) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex dVert
            #pragma fragment dFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float _Wrap; float4 _ShadowTint; float _ShadowDepth;
                float4 _RimColor; float _RimPower; float _RimStrength;
            CBUFFER_END

            struct DA { float4 positionOS : POSITION; };
            struct DV { float4 positionCS : SV_POSITION; };
            DV dVert (DA IN) { DV o; o.positionCS = TransformObjectToHClip(IN.positionOS.xyz); return o; }
            half4 dFrag (DV IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // DepthNormals — SSAO가 법선을 읽어 틈새를 찾는다. 없으면 AO가 안 나온다.
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex dnVert
            #pragma fragment dnFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float _Wrap; float4 _ShadowTint; float _ShadowDepth;
                float4 _RimColor; float _RimPower; float _RimStrength;
            CBUFFER_END

            struct NA { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct NV { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            NV dnVert (NA IN)
            {
                NV o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return o;
            }
            half4 dnFrag (NV IN) : SV_Target { return half4(normalize(IN.normalWS) * 0.5 + 0.5, 0); }
            ENDHLSL
        }
    }
}
