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
        // 알베도 맵. 모델에 UV가 없던 동안에는 파트마다 단색 머티리얼이라 팔뚝·소매·방망이가
        // 통째로 한 색으로 칠해졌고, 그늘 쪽으로 돌면 그대로 검게 읽혔다(2026-09-10 사용자 지적).
        // UV가 붙은 뒤에도 이 셰이더에 **텍스처 입력이 아예 없어서** 그 작업이 통째로 버려지고 있었다.
        // ⚠️ 노멀맵은 일부러 안 받는다 — OutlineNormals가 외곽선 밀기 방향을 tangent에 덮어써서
        //    탄젠트 공간이 이미 깨져 있다. 쿼터뷰 거리에서 노멀맵으로 얻는 것도 거의 없다.
        _BaseMap      ("Base Map (알베도)", 2D) = "white" {}
        // 자체발광. 랜턴 액센트처럼 **스스로 빛나야 하는 파트**가 여기 없으면 그냥 단색 얼룩으로
        // 보인다(플레이어 허리의 보라 조각이 그거였다). 램프 계산 뒤에 그대로 더한다 —
        // 셀 음영에 섞이면 빛나는 느낌이 사라진다.
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)

        [Header(Outline)]
        // 순검정이 아니라 아주 어두운 남색 — 순검정 외곽선은 만화적이고 밝다.
        _OutlineColor ("Outline Color", Color) = (0.035, 0.04, 0.055, 1)
        _OutlineWidth ("Outline Width (px)", Range(0, 12)) = 4.5
        // 헐을 시선 방향으로 뒤로 미는 거리(미터). 얇은 껍데기에서 외곽선이 모델을 덮는 것을 막는다.
        // 너무 키우면 지면 뒤로 들어가 외곽선이 사라진다 — 캐릭터 두께(≈0.2m)보다 작게.
        _OutlinePush  ("Outline Depth Push (m)", Range(0, 0.3)) = 0.03

        [Header(Cel)]
        // 램프 텍스처가 이 룩의 핵심이다. 밴딩은 "밝기"만 계단으로 자르지만, 램프는
        // **그림자 쪽 색(色)을 따로 지정**한다 — 따뜻한 광원에 차가운 보랏빛 그림자 같은
        // 색 전이가 Flat Kit류 카툰 룩을 만드는 실제 요소다. 비워 두면 아래 밴딩으로 폴백.
        [NoScaleOffset] _RampTex ("Light Ramp (가로 = 어두움→밝음)", 2D) = "white" {}
        _RampStrength ("Ramp Strength", Range(0, 1)) = 1
        _Bands        ("Light Bands (램프 없을 때)", Range(2, 6)) = 3
        _BandSoft     ("Band Softness", Range(0.001, 0.25)) = 0.035
        _Wrap         ("Light Wrap", Range(0, 1)) = 0.20

        [Header(Shading)]
        _ShadowTint   ("Shadow Tint", Color) = (0.42, 0.47, 0.58, 1)
        _ShadowDepth  ("Shadow Depth", Range(0, 1)) = 0.30
        // 림 = 어둠 속에서 실루엣을 배경에서 떼어내는 장치. 이 게임은 밤 비중이 커서
        // 이게 없으면 캐릭터가 배경에 그대로 먹힌다. 색은 달빛/형광등 쪽 차가운 계열.
        _RimColor     ("Rim Color", Color) = (0.62, 0.72, 0.85, 1)
        _RimPower     ("Rim Power", Range(0.5, 8)) = 2.6
        _RimStrength  ("Rim Strength", Range(0, 1)) = 0.30
        // 채도 억제 — 모델 색을 일괄로 바래게 한다. "붕괴된 사회" 톤을 모델마다
        // 다시 칠하지 않고 한 노브로 맞추는 장치.
        // 텍스처가 붙은 뒤로는 세게 뺄 필요가 없다 — 알베도 자체가 이미 바랜 색이라
        // 0.30이면 옷·피부 구분까지 같이 죽는다.
        _Desaturate   ("Desaturate", Range(0, 1)) = 0.18
        // ⚠️ 기본 0. 이 값은 **버텍스 컬러에 AO를 구워 둔 메시**에만 의미가 있는데,
        //    현재 캐릭터 모델(치비·밴딧)은 버텍스 컬러가 아예 없다(실측: colors.Length == 0).
        //    그 상태에서 곱하면 IN.color가 정의되지 않은 값이라 모델이 통째로 어두워지거나 얼룩진다.
        //    AO를 구운 메시를 쓸 때만 머티리얼에서 올린다.
        _VertexAO     ("Vertex AO Strength", Range(0, 1)) = 0
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
            // 깊이 바이어스를 렌더 상태로도 조금 준다. URP는 SRPDefaultUnlit을 UniversalForward
            // **뒤에** 그리므로 깊이가 비기면 외곽선이 이긴다 — 확실히 지도록 살짝 뒤로 민다.
            // (어깨가 통째로 검던 건 이것 때문이 아니라 메시 안팎이 뒤집혀 있어서였다.
            //  그건 MeshNormalRepair가 임포트 시점에 잡는다. 여기 값은 z-파이팅 보험용이라 작게.)
            Offset 1, 4

            HLSLPROGRAM
            #pragma vertex outlineVert
            #pragma fragment outlineFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float4 _BaseMap_ST; float4 _EmissionColor;
                float4 _OutlineColor; float _OutlineWidth;
                float _Bands; float _BandSoft; float _Wrap; float _RampStrength; float _OutlinePush;
                float4 _ShadowTint; float _ShadowDepth;
                float4 _RimColor; float _RimPower; float _RimStrength; float _VertexAO; float _Desaturate;
            CBUFFER_END

            // ⚠️ 밀기 방향은 NORMAL이 아니라 **TANGENT**에서 읽는다 — OutlineNormals가 거기에
            //    "위치를 공유하는 정점들의 평균 법선"을 구워 넣는다. 하드 엣지 모델은 같은 자리
            //    정점들의 법선이 갈려 있어, NORMAL로 밀면 테두리가 조각조각 찢어진다.
            //    탄젠트가 비어 있으면(안 구운 메시) 법선으로 자동 폴백한다.
            struct OA { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; };
            struct OV { float4 positionCS : SV_POSITION; };

            OV outlineVert (OA IN)
            {
                OV o;

                // ★ **헐을 시선 방향으로 조금 뒤로 물린다 — 단위는 미터.**
                //   얇은 껍데기(후드·마스크·배트)는 앞뒤 면이 거의 붙어 있어, 깊이를 안 밀면
                //   헐이 앞면을 이겨 모델 위에 검은 얼룩이 덮인다(사용자 지적).
                //   NDC로 밀면 카메라 near/far에 따라 실제 거리가 달라져 예측이 안 된다 —
                //   너무 밀면 지면 뒤로 들어가 **외곽선이 통째로 사라진다**(실제로 그랬다).
                //   월드 공간 미터로 밀면 "3cm 뒤"가 어떤 카메라에서도 3cm 뒤다.
                //
                //   ⚠️ 밀 방향은 **카메라의 정면 축**이어야 한다. 예전엔 GetWorldSpaceViewDir
                //   (= 그 점에서 카메라 '위치'로 가는 방향)를 썼는데, 이 게임은 오소 카메라라
                //   화면 중앙에서 멀어질수록 그 방향이 옆으로 기울어 헐이 **가로로 밀렸다**.
                //   UNITY_MATRIX_V의 3번째 행이 카메라의 뒤쪽 축(월드)이라, 그 반대가 정면이다.
                float3 posWS   = TransformObjectToWorld(IN.positionOS.xyz);
                float3 viewFwd = -UNITY_MATRIX_V[2].xyz;   // 카메라가 향하는 방향
                posWS += viewFwd * _OutlinePush;           // 카메라에서 멀어지는 쪽
                float4 clip = TransformWorldToHClip(posWS);

                // 법선을 **뷰공간**으로 옮겨 화면상의 밀 방향을 얻는다. 뒤통수를 보고 있는 정점은
                // xy 성분이 0에 가까워 normalize가 터지므로 안전값을 둔다.
                float3 pushOS = (dot(IN.tangentOS.xyz, IN.tangentOS.xyz) > 1e-6) ? IN.tangentOS.xyz : IN.normalOS;
                float3 nWS = TransformObjectToWorldNormal(pushOS);
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
                float4 _BaseColor; float4 _BaseMap_ST; float4 _EmissionColor;
                float4 _OutlineColor; float _OutlineWidth;
                float _Bands; float _BandSoft; float _Wrap; float _RampStrength; float _OutlinePush;
                float4 _ShadowTint; float _ShadowDepth;
                float4 _RimColor; float _RimPower; float _RimStrength; float _VertexAO; float _Desaturate;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 screenPos  : TEXCOORD2;
                float  bakedAO    : TEXCOORD3;
                float2 uv         : TEXCOORD4;
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
                o.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                return o;
            }

            // 램프 텍스처 — 가로축이 "어두움(0) → 밝음(1)". 텍스처는 CBUFFER 밖에 둔다.
            TEXTURE2D(_RampTex);
            SAMPLER(sampler_RampTex);
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

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

            /// 빛의 세기 t(0~1)를 **색**으로 바꾼다.
            ///   램프가 있으면 그 색을, 없으면(기본 흰색 텍스처) 밴딩한 회색을 돌려준다.
            ///   가장자리 픽셀을 피하려고 0.5픽셀 안쪽을 샘플한다 — 안 그러면 clamp 때문에
            ///   양 끝 밴드가 한 줄 두껍게 잡힌다.
            float3 LightRamp(float t, float bands, float soft, float strength)
            {
                float u = clamp(t, 0.004, 0.996);
                float3 ramp = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(u, 0.5)).rgb;
                float3 banded = Posterize(saturate(t), bands, soft).xxx;
                return lerp(banded, ramp, saturate(strength));
            }


            half4 frag (Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float4 sc = TransformWorldToShadowCoord(IN.positionWS);
                Light L = GetMainLight(sc);

                float ndl  = dot(N, L.direction);
                float wrap = saturate((ndl + _Wrap) / (1.0 + _Wrap));
                float t    = saturate(wrap * L.shadowAttenuation);

                // 램프가 밝기와 **색**을 동시에 준다. 예전엔 밝기(회색)만 계단으로 자르고
                // 그림자 색은 _ShadowTint 하나로 눌렀는데, 그러면 어두운 쪽이 "같은 색의 어두움"이라
                // 카툰 특유의 색 전이가 안 나온다(Flat Kit류와 갈리는 지점이 바로 여기다).
                // 채도 억제 — 밝기(휘도)는 지키고 색만 바래게 한다. 톤을 한 노브로 잡는다.
                // UV가 없는 메시는 uv가 (0,0)이라 텍스처의 한 점만 읽는다 — 기본값이 흰색이므로
                // 맵을 안 물린 머티리얼은 예전과 똑같이 _BaseColor만 나온다(폴백이 안전하다).
                float3 albedo = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;
                float  lum  = dot(albedo, float3(0.299, 0.587, 0.114));
                float3 baseC = lerp(albedo, lum.xxx, saturate(_Desaturate));

                float3 ramp = LightRamp(t, _Bands, _BandSoft, _RampStrength);
                float3 col  = baseC * L.color * ramp;

                #if defined(_ADDITIONAL_LIGHTS)
                    uint addCount = GetAdditionalLightsCount();
                    for (uint li = 0u; li < addCount; li++)
                    {
                        Light AL = GetAdditionalLight(li, IN.positionWS);
                        float andl  = dot(N, AL.direction);
                        float awrap = saturate((andl + _Wrap) / (1.0 + _Wrap));
                        // 감쇠까지 계단화하면 램프 테두리가 동심원으로 끊긴다 — 각도만 계단화한다.
                        float3 aramp = LightRamp(awrap, _Bands, _BandSoft, _RampStrength);
                        col += baseC * AL.color * aramp * AL.distanceAttenuation * AL.shadowAttenuation;
                    }
                #endif

                col += baseC * SampleSH(N) * 1.25;

                col += _EmissionColor.rgb;

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
                float4 _BaseColor; float4 _BaseMap_ST; float4 _EmissionColor;
                float4 _OutlineColor; float _OutlineWidth;
                float _Bands; float _BandSoft; float _Wrap; float _RampStrength; float _OutlinePush;
                float4 _ShadowTint; float _ShadowDepth;
                float4 _RimColor; float _RimPower; float _RimStrength; float _VertexAO; float _Desaturate;
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
                float4 _BaseColor; float4 _BaseMap_ST; float4 _EmissionColor;
                float4 _OutlineColor; float _OutlineWidth;
                float _Bands; float _BandSoft; float _Wrap; float _RampStrength; float _OutlinePush;
                float4 _ShadowTint; float _ShadowDepth;
                float4 _RimColor; float _RimPower; float _RimStrength; float _VertexAO; float _Desaturate;
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
                float4 _BaseColor; float4 _BaseMap_ST; float4 _EmissionColor;
                float4 _OutlineColor; float _OutlineWidth;
                float _Bands; float _BandSoft; float _Wrap; float _RampStrength; float _OutlinePush;
                float4 _ShadowTint; float _ShadowDepth;
                float4 _RimColor; float _RimPower; float _RimStrength; float _VertexAO; float _Desaturate;
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
