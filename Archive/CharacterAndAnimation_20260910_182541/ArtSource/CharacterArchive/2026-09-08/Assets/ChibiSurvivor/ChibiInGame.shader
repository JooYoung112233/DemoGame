Shader "BRB/ChibiInGame"
{
    Properties
    {
        _Color ("Palette Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ChibiLit2D"
            Tags { "LightMode"="Universal2D" }
            Cull Back
            ZWrite On
            Blend Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; half2 lightingUV : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.lightingUV = ComputeScreenPos(output.positionCS / output.positionCS.w).xy;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // A fixed soft key reveals low-poly facets; the real scene's Light2D
                // textures supply brightness and color (including day/night changes).
                half facet = .55h + .45h * saturate(dot(normalize(input.normalWS), normalize(half3(-.4h,.7h,-.6h))));
                SurfaceData2D surface;
                InputData2D lighting;
                InitializeSurfaceData(_Color.rgb * facet, 1, half4(1,1,1,1), surface);
                InitializeInputData(float2(0,0), input.lightingUV, lighting);
                return CombinedShapeLightShared(surface, lighting);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
