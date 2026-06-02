Shader "BRB/SpriteFlash"
{
    // 탑다운 2D 캐릭터용 스프라이트 셰이더 (URP 2D 렌더러).
    // - Light2D 반응 (CombinedShapeLight)
    // - 알파 블렌딩 (일반 스프라이트)
    // - 피격 흰 플래시: 조명 적용 후 rgb를 _FlashColor로 lerp (_FlashAmount 0~1)
    //   → 어둠 속에서도 풀 화이트로 번쩍. SpriteRenderer.color(틴트/플립)와 독립.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Hit Flash)]
        _FlashColor  ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0,1)) = 0
        // 틴트/플립은 SpriteRenderer.color(정점 컬러)·메시가 처리하므로 별도 프로퍼티 불필요.
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        // ============ URP 2D 렌더러 패스 (Light2D 반응) ============
        Pass
        {
            Name "SpriteFlash2D"
            Tags { "LightMode"="Universal2D" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _FlashColor;
                float  _FlashAmount;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 vertexColor : COLOR;
                half2  lightingUV  : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS  = TransformObjectToHClip(input.positionOS.xyz);
                o.uv          = TRANSFORM_TEX(input.uv, _MainTex);
                o.vertexColor = input.color;
                o.lightingUV  = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color * input.vertexColor;

                // Light2D 적용
                SurfaceData2D sd;
                InputData2D id;
                InitializeSurfaceData(col.rgb, col.a, half4(1,1,1,1), sd);
                InitializeInputData(input.uv, input.lightingUV, id);
                half4 lit = CombinedShapeLightShared(sd, id);

                // 피격 플래시 — 조명 후 rgb를 흰색으로 (알파 유지)
                lit.rgb = lerp(lit.rgb, _FlashColor.rgb, _FlashAmount);
                return lit;
            }
            ENDHLSL
        }

        // ============ 폴백(라이트 없는 카메라/프리뷰) ============
        Pass
        {
            Name "SpriteFlashUnlit"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _FlashColor;
                float  _FlashAmount;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 vertexColor : COLOR; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS  = TransformObjectToHClip(input.positionOS.xyz);
                o.uv          = TRANSFORM_TEX(input.uv, _MainTex);
                o.vertexColor = input.color;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color * input.vertexColor;
                col.rgb = lerp(col.rgb, _FlashColor.rgb, _FlashAmount);
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
