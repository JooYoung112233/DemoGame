Shader "BRB/SpriteSheet"
{
    Properties
    {
        _MainTex ("Sprite Sheet", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Columns ("Columns", Float) = 2
        _Rows ("Rows", Float) = 8
        _CurrentFrame ("Current Frame", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "SpriteSheet"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite On
            Blend One Zero
            AlphaToMask On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Columns;
                float _Rows;
                float _CurrentFrame;
                float _Cutoff;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

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

                uint frame = (uint)_CurrentFrame;
                uint cols = (uint)_Columns;
                uint col = frame % cols;
                uint row = frame / cols;

                float cellW = 1.0 / _Columns;
                float cellH = 1.0 / _Rows;

                float u = (col + input.uv.x) * cellW;
                float v = 1.0 - (row + 1.0 - input.uv.y) * cellH;

                output.uv = float2(u, v);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                col *= _Color;
                clip(col.a - _Cutoff);
                return col;
            }
            ENDHLSL
        }

        // ============ URP 2D 렌더러 패스 (Light2D 반응) ============
        Pass
        {
            Name "SpriteSheet2D"
            Tags { "LightMode"="Universal2D" }

            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert2d
            #pragma fragment frag2d

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Columns;
                float _Rows;
                float _CurrentFrame;
                float _Cutoff;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes2D { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings2D { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half2 lightingUV : TEXCOORD1; };

            Varyings2D vert2d(Attributes2D input)
            {
                Varyings2D o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                uint frame = (uint)_CurrentFrame;
                uint cols = (uint)_Columns;
                uint col = frame % cols;
                uint row = frame / cols;
                float cellW = 1.0 / _Columns;
                float cellH = 1.0 / _Rows;
                float u = (col + input.uv.x) * cellW;
                float v = 1.0 - (row + 1.0 - input.uv.y) * cellH;
                o.uv = float2(u, v);

                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                return o;
            }

            half4 frag2d(Varyings2D input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                clip(col.a - _Cutoff);

                SurfaceData2D sd;
                InputData2D id;
                InitializeSurfaceData(col.rgb, col.a, half4(1,1,1,1), sd);
                InitializeInputData(input.uv, input.lightingUV, id);
                return CombinedShapeLightShared(sd, id);
            }
            ENDHLSL
        }
    }
}
