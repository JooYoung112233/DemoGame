Shader "InkCity/RoadFloor"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}

        [Header(Road)]
        _RoadColor ("Road Color", Color) = (0.12, 0.12, 0.13, 1)
        _SidewalkColor ("Sidewalk Color", Color) = (0.22, 0.21, 0.20, 1)
        _LineColor ("Line Color", Color) = (0.85, 0.85, 0.80, 1)
        _RoadWidth ("Road Width", Range(0.1, 0.9)) = 0.6
        _LineWidth ("Line Width", Range(0.002, 0.03)) = 0.008
        _CenterDash ("Center Dash Length", Range(0.05, 0.5)) = 0.15
        _CenterGap ("Center Gap Length", Range(0.05, 0.5)) = 0.1

        [Header(Direction)]
        [Toggle(_VERTICAL)] _Vertical ("Vertical Road", Float) = 1

        [Header(Curb)]
        _CurbWidth ("Curb Width", Range(0, 0.03)) = 0.01
        _CurbColor ("Curb Color", Color) = (0.35, 0.34, 0.33, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "RoadFloor"
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
            #pragma shader_feature_local _VERTICAL
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _RoadColor;
                float4 _SidewalkColor;
                float4 _LineColor;
                float _RoadWidth;
                float _LineWidth;
                float _CenterDash;
                float _CenterGap;
                float _CurbWidth;
                float4 _CurbColor;
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
                float2 uv = input.uv;

                // road axis: across = perpendicular to road, along = parallel
                #ifdef _VERTICAL
                    float across = uv.x;
                    float along = uv.y;
                #else
                    float across = uv.y;
                    float along = uv.x;
                #endif

                // base texture for noise/detail
                half3 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                float texDetail = dot(baseTex, float3(0.299, 0.587, 0.114));

                // road boundaries
                float roadStart = 0.5 - _RoadWidth * 0.5;
                float roadEnd = 0.5 + _RoadWidth * 0.5;
                bool onRoad = across >= roadStart && across <= roadEnd;

                // base color
                half3 color;
                if (onRoad)
                    color = _RoadColor.rgb;
                else
                    color = _SidewalkColor.rgb;

                // mix in texture detail
                color *= (0.8 + texDetail * 0.4);

                // curb lines (road edge)
                float distToRoadEdge = min(abs(across - roadStart), abs(across - roadEnd));
                if (distToRoadEdge < _CurbWidth && _CurbWidth > 0)
                    color = _CurbColor.rgb;

                // edge lines (white lines at road border)
                float edgeLineInner = _LineWidth * 1.5;
                if (onRoad)
                {
                    float distInner = min(across - roadStart, roadEnd - across);
                    if (distInner < edgeLineInner)
                        color = _LineColor.rgb;
                }

                // center dashed line
                if (onRoad)
                {
                    float distToCenter = abs(across - 0.5);
                    float dashCycle = _CenterDash + _CenterGap;
                    float dashPos = frac(along / dashCycle);
                    bool inDash = dashPos < (_CenterDash / dashCycle);

                    if (distToCenter < _LineWidth * 0.5 && inDash)
                        color = _LineColor.rgb;
                }

                // lighting: no NdotL (billboard sprite)
                half3 baseColor = color;
                half3 ambient = baseColor * 0.4;

                Light mainLight = GetMainLight();
                color = ambient + baseColor * mainLight.color;

                // additional lights (flashlight)
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    float addAtten = addLight.distanceAttenuation;
                    color += baseColor * addAtten * addLight.color;
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
            Varyings2 DepthVert2(Attributes2 input) { Varyings2 o; o.positionCS = TransformObjectToHClip(input.positionOS.xyz); return o; }
            half4 DepthFrag2(Varyings2 input) : SV_Target { return 0; }
            ENDHLSL
        }

    }
    FallBack Off
}
