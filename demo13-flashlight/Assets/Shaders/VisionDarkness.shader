// 시야 콘 어둠 오버레이 전용 — **정점 색만 쓰는 언릿 반투명**.
//
// 왜 전용 셰이더인가: 2D 시절엔 `Sprites/Default`로 충분했다. URP-3D로 넘어오면 그건
// 빌트인 파이프라인 셰이더라 **분홍 에러 머티리얼**이 되고, URP 기본 Unlit은 정점 색을
// 읽지 않아 부채꼴의 밝음↔어둠 그라디언트가 통째로 사라진다. 둘 다 못 쓰므로 직접 쓴다.
//
// 조명을 받지 않아야 한다 — 어둠이 낮밤이나 근처 램프에 따라 흔들리면 "안 보이는 구역"이라는
// 신호가 아니라 그냥 얼룩으로 읽힌다.
//
// 설계: docs/rendering.md 가시성, docs/3d-migration.md Stage 2
Shader "BRB/VisionDarkness"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Transparent"
            // Overlay로 둔다. 지면 부채꼴이라 깊이로는 바닥과 거의 같은 높이인데,
            // 다른 투명체와 순서를 다투면 프레임마다 깜빡인다.
            "Queue"          = "Overlay"
        }

        Pass
        {
            Name "VisionDarkness"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            // ⚠️ ZTest Always. 지면 바로 위에 깔리므로 z-fighting으로 지글거리고,
            //    풀·턱 같은 낮은 지오메트리에 부분적으로 먹혀 구멍이 뚫린다.
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(IN.color.rgb, IN.color.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
