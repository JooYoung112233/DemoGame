Shader "BRB/FlashlightBeam"
{
    Properties
    {
        _Color ("Beam Color", Color) = (1, 0.95, 0.8, 0.25)
        _EdgeFade ("Edge Fade", Range(0.01, 1)) = 0.4
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "FlashlightBeam"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _EdgeFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // uv.y = 0(원점) ~ 1(끝) 방향 페이드
                float lengthFade = smoothstep(0.0, 0.15, uv.y) * smoothstep(1.0, 0.7, uv.y);

                // uv.x = 0~1, 중심 0.5에서 가장자리로 페이드
                float centerDist = abs(uv.x - 0.5) * 2.0;
                // 끝으로 갈수록 원뿔이 넓어지므로 가장자리 페이드를 y에 따라 조절
                float widthFade = smoothstep(1.0, 1.0 - _EdgeFade, centerDist);

                float alpha = _Color.a * lengthFade * widthFade;

                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
