#ifndef BRB_GAMELIT_INPUT_INCLUDED
#define BRB_GAMELIT_INPUT_INCLUDED

// BRB/GameLit 재질 입력 — 모든 패스가 이 파일 하나를 쓴다(SRP Batcher: UnityPerMaterial 레이아웃이 패스마다 같아야 한다).
// 속성 이름은 URP/Lit과 같게 둔다 → 기존 머티리얼을 셰이더만 바꿔도 값이 그대로 넘어오고,
// MaterialPropertyBlock `_BaseColor`(피격 틴트·시체 표시 등)도 그대로 먹는다.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
    half4  _EmissionColor;
    half   _Cutoff;
    half   _Smoothness;
    half   _Metallic;
    half   _UsePackedMask;
    half   _BumpScale;
    half   _OcclusionStrength;
    // 어두운 사실풍 (docs/rendering.md §게임 전용 셰이더)
    half4  _GrimeColor;
    half   _GrimeStrength;
    half   _GrimeScale;
    half   _GroundGrimeHeight;
    half   _GrimeObjectSpace;
    half   _ShadowDesaturation;
    half   _RimStrength;
    half4  _RimColor;
    half   _Surface;
CBUFFER_END

TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);

#endif
