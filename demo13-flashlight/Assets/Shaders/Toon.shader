// 카툰(셀) 셰이더 — 진한 외곽선 + 계단식 명암.
//
// 왜 만드나: 모델이 아직 완성도가 들쭉날쭉해서, 사실적 음영으로 가면 그 어색함이 그대로 드러난다.
// 카툰 룩은 **형태를 선으로 규정**하고 음영을 몇 단으로 뭉개서, 모델의 미세한 결함을 덮는다.
// (2026-09-10 사용자: "후처리로 모델 애매한 걸 커버하고 싶다, 아웃라인 진하게 카툰처럼")
//
// 두 조각으로 만든다:
//   ① **외곽선 = 인버티드 헐**. 앞면을 버리고(Cull Front) 뒷면을 법선 방향으로 살짝 부풀려
//      어둡게 칠한다. 화면공간 엣지검출과 달리 **렌더러 기능이 필요 없고**, 실루엣이 굵고
//      확실하게 나온다. 대신 내부 주름선은 안 생긴다(그건 나중에 후처리로 얹을 수 있다).
//      부풀리는 양은 **클립공간**에서 준다 — 오브젝트공간에서 밀면 큰 모델일수록 선이 굵어져
//      중장 밴딧(1.3배)만 테두리가 두꺼워진다.
//   ② **셀 음영 = 밴딩**. wrapped diffuse를 그대로 쓰되 몇 단으로 양자화한다. 단 경계는
//      완전히 각지게 두지 않고 아주 좁게 풀어(_BandSoft) 계단 노이즈를 막는다.
//
// 나머지(림·그림자 틴트·구운 AO·추가 광원·SSAO)는 BRB/Stylized와 같은 규약을 따른다 —
// 두 셰이더를 섞어 쓰는 장면에서 룩이 갈리지 않게.
Shader "BRB/Toon"
{
    Properties
    {
        _BaseColor    ("Base Color", Color) = (0.5, 0.5, 0.5, 1)

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.02, 0.02, 0.03, 1)
        _OutlineWidth ("Outline Width (px)", Range(0, 12)) = 5.5

        [Header(Cel)]
        _Bands        ("Light Bands", Range(2, 6)) = 3
        _BandSoft     ("Band Softness", Range(0.001, 0.25)) = 0.035
        _Wrap         ("Light Wrap", Range(0, 1)) = 0.28

        [Header(Shading)]
        _ShadowTint   ("Shadow Tint", Color) = (0.42, 0.47, 0.58, 1)
        _ShadowDepth  ("Shadow Depth", Range(0, 1)) = 0.45
        _RimColor     ("Rim Color", Color) = (1, 0.96, 0.88, 1)
        _RimPower     ("Rim Power", Range(0.5, 8)) = 3.0
        _RimStrength  ("Rim Strength", Range(0, 1)) = 0.22
        _VertexAO     ("Vertex AO Strength", Range(0, 1)) = 0.50
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // ── ① 외곽선 (인버티드 헐) ─────────────────────────────────────
        //  뒷면만 그리므로 앞면(본체)이 덮어써서, 실루엣 바깥으로 삐져나온 테두리만 남는다.
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex outlineVert
            #pragma fragment outlineFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _OutlineColor; float _OutlineWidth;
                float _Bands; float _BandSoft; float _Wrap;
                float4 _ShadowTint; float _ShadowDepth;
                float4 _RimColor; float _RimPower; float _RimStrength; float _VertexAO;
            CBUFFER_END

            struct OA { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct OV { float4 positionCS : SV_POSITION; };

            OV outlineVert (OA IN)
            {
                OV o;
                float4 clip = TransformObjectToHClip(IN.positionOS.xyz);

                // 법선을 **뷰공간**으로 옮겨 화면상의 밀 방향을 얻는다. 뒤통수를 보고 있는 정점은
                // xy 성분이 0에 가까워 normalize가 터지므로 안전값을 둔다.
                float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 nVS = TransformWorldToViewDir(nWS);
                float2 dir = nVS.xy;
                float  len = max(length(dir), 1e-4);
                dir /= len;

                // 화면 픽셀 기준 두께 — 오소 카메라라 clip.w는 1이지만, 원근으로 바뀌어도
                // 굵기가 유지되도록 w를 곱해 둔다. _ScreenParams.zw = 1 + 1/해상도.
                float2 px = _OutlineWidth * (_ScreenParams.zw - 1.0) * 2.0;
                clip.xy += dir * px * clip.w;

                o.positionCS = clip;
                return o;
            }

            half4 outlineFrag (OV IN) : SV_Target { return half4(_OutlineColor.rgb, 1); }
            ENDHLSL
        }

        // ── ② 셀 음영 본체 ────────────────────────────────────────────
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
            // 추가 광원 키워드는 URP 기본 Lit과 **글자 그대로 같아야** 한다(Stylized와 동일 이유).
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _OutlineColor; float _OutlineWidth;
                float _Bands; float _BandSoft; float _Wrap;
                float4 _ShadowTint; float _ShadowDepth;
                float4 _RimColor; float _RimPower; float _RimStrength; float _VertexAO;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 screenPos  : TEXCOORD2;
                float  bakedAO    : TEXCOORD3;
            };

            Varyings vert (Attributes IN)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                o.screenPos  = ComputeScreenPos(p.positionCS);
                o.bakedAO    = IN.color.r;
                return o;
            }

            // 0~1 밝기를 N단으로 계단화. 경계는 _BandSoft만큼만 부드럽게 —
            // 완전히 각지면 곡면에서 계단이 지글거린다.
            float Posterize(float x, float bands, float soft)
            {
                float n = max(2.0, floor(bands));
                float s = x * n;
                float f = floor(s);
                float frac0 = s - f;
                return (f + smoothstep(0.5 - soft * n, 0.5 + soft * n, frac0)) / n;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float4 sc = TransformWorldToShadowCoord(IN.positionWS);
                Light L = GetMainLight(sc);

                float ndl  = dot(N, L.direction);
                float wrap = saturate((ndl + _Wrap) / (1.0 + _Wrap));
                float lit  = Posterize(saturate(wrap * L.shadowAttenuation), _Bands, _BandSoft);

                float3 shadowCol = _BaseColor.rgb * lerp(1.0, 1.0 - _ShadowDepth, 1.0 - lit) * _ShadowTint.rgb;
                float3 litCol    = _BaseColor.rgb * L.color;
                float3 col = lerp(shadowCol, litCol, lit);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint addCount = GetAdditionalLightsCount();
                    for (uint li = 0u; li < addCount; li++)
                    {
                        Light AL = GetAdditionalLight(li, IN.positionWS);
                        float andl  = dot(N, AL.direction);
                        float awrap = saturate((andl + _Wrap) / (1.0 + _Wrap));
                        // 감쇠까지 계단화하면 램프 테두리가 동심원으로 끊긴다 — 각도만 계단화한다.
                        float aband = Posterize(awrap, _Bands, _BandSoft);
                        col += _BaseColor.rgb * AL.color * aband * AL.distanceAttenuation * AL.shadowAttenuation;
                    }
                #endif

                col += _BaseColor.rgb * SampleSH(N) * 1.25;

                float rim = pow(1.0 - saturate(dot(N, V)), _RimPower);
                col += _RimColor.rgb * rim * _RimStrength;

                col *= lerp(1.0, saturate(IN.bakedAO), _VertexAO);

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
                float4 _BaseColor;
                float4 _OutlineColor; float _OutlineWidth;
                float _Bands; float _BandSoft; float _Wrap;
                float4 _ShadowTint; float _ShadowDepth;
                float4 _RimColor; float _RimPower; float _RimStrength; float _VertexAO;
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
                float4 _BaseColor;
                float4 _OutlineColor; float _OutlineWidth;
                float _Bands; float _BandSoft; float _Wrap;
                float4 _ShadowTint; float _ShadowDepth;
                float4 _RimColor; float _RimPower; float _RimStrength; float _VertexAO;
            CBUFFER_END

            struct DA { float4 positionOS : POSITION; };
            struct DV { float4 positionCS : SV_POSITION; };
            DV dVert (DA IN) { DV o; o.positionCS = TransformObjectToHClip(IN.positionOS.xyz); return o; }
            half4 dFrag (DV IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // DepthNormals — SSAO가 법선을 읽는다. 없으면 이 셰이더를 쓴 것만 AO가 빠진다.
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
                float4 _BaseColor;
                float4 _OutlineColor; float _OutlineWidth;
                float _Bands; float _BandSoft; float _Wrap;
                float4 _ShadowTint; float _ShadowDepth;
                float4 _RimColor; float _RimPower; float _RimStrength; float _VertexAO;
            CBUFFER_END

            struct DNA { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct DNV { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            DNV dnVert (DNA IN)
            {
                DNV o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return o;
            }
            half4 dnFrag (DNV IN) : SV_Target { return half4(normalize(IN.normalWS) * 0.5 + 0.5, 0); }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
