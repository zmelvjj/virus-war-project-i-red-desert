Shader "SpaceShockwaveURP/Refraction" {
Properties {
_Progress("Progress",Range(0,1))=0
_Strength("Distortion Pixels",Range(0,80))=22
_Width("Wave Width",Range(0.02,0.3))=0.12
_Glow("Edge Light",Range(0,1))=0.025
}
SubShader {
Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
Pass {
Tags { "LightMode"="UniversalForward" }
Blend SrcAlpha OneMinusSrcAlpha
ZWrite Off
Cull Off
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
CBUFFER_START(UnityPerMaterial)
float _Progress, _Strength, _Width, _Glow;
CBUFFER_END
struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
Varyings vert(Attributes v) {
Varyings o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
o.positionWS=TransformObjectToWorld(v.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS); o.uv=v.uv; return o;
}
half4 frag(Varyings i):SV_Target {
UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
float2 p=(i.uv-.5)*2; float radius=length(p);
float t=saturate(_Progress); float waveRadius=lerp(.025,.91,1-pow(1-t,1.6));
float d=(radius-waveRadius)/max(_Width,.02);
float envelope=exp(-d*d*1.8);
float onset=smoothstep(0,.065,t); float fade=pow(1-t,0.85)*onset;
float edge=1-smoothstep(.91,.99,radius);
float mask=envelope*fade*edge;
clip(mask-.001);
float2 uv=GetNormalizedScreenSpaceUV(i.positionCS);
float3 centerWS=TransformObjectToWorld(float3(0,0,0));
float4 centerCS=TransformWorldToHClip(centerWS);
float4 nearCS=TransformWorldToHClip(i.positionWS);
float2 directionNDC=nearCS.xy/nearCS.w-centerCS.xy/centerCS.w;
#if UNITY_UV_STARTS_AT_TOP
 directionNDC.y=-directionNDC.y;
#endif
float2 directionPixels=directionNDC*_ScaledScreenParams.xy;
float2 direction=directionPixels/max(length(directionPixels),.001);
float displacement=sin(d*4.2)*envelope*fade*edge*_Strength;
float2 offset=direction*displacement/_ScaledScreenParams.xy;
float2 border=min(uv,1-uv); float boundary=saturate(min(border.x,border.y)*35);
float3 color=SampleSceneColor(clamp(uv+offset*boundary,.001,.999));
color+=_Glow*envelope*fade;
return half4(color,saturate(mask*2.5));
}
ENDHLSL
}
}
}
