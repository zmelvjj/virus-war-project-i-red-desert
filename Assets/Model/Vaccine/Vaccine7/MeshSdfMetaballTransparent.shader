Shader "Custom/Mesh SDF Metaball Transparent"
{
    Properties
    {
        _Color ("Raymarch Tint", Color) = (1, 1, 1, 1)
        _Blend ("Smooth Union", Float) = 0.25
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        [HideInInspector] _HasBridgeCapsule ("Has Bridge Capsule", Float) = 0
        [HideInInspector] _BridgeStart ("Bridge Start", Vector) = (0, 0, 0, 0)
        [HideInInspector] _BridgeEnd ("Bridge End", Vector) = (0, 0, 0, 0)
        [HideInInspector] _BridgeRadius ("Bridge Radius", Float) = 0
        [HideInInspector] _BridgeSmoothness ("Bridge Smoothness", Float) = 0.25
        [NoScaleOffset] _SdfTextureA ("Mesh SDF A", 3D) = "" {}
        [NoScaleOffset] _SdfTextureB ("Mesh SDF B", 3D) = "" {}
        [HideInInspector] [NoScaleOffset] _MaterialIndexA ("Material IDs A", 3D) = "" {}
        [HideInInspector] [NoScaleOffset] _MaterialIndexB ("Material IDs B", 3D) = "" {}
        [NoScaleOffset] _SdfAlbedoA ("Projected Albedo A", 2D) = "white" {}
        [NoScaleOffset] _SdfAlbedoA1 ("Projected Albedo A Slot 1", 2D) = "white" {}
        [NoScaleOffset] _SdfAlbedoA2 ("Projected Albedo A Slot 2", 2D) = "white" {}
        [NoScaleOffset] _SdfAlbedoA3 ("Projected Albedo A Slot 3", 2D) = "white" {}
        [NoScaleOffset] _SdfAlbedoA4 ("Projected Albedo A Slot 4", 2D) = "white" {}
        [NoScaleOffset] _SdfAlbedoB ("Projected Albedo B", 2D) = "white" {}
        [NoScaleOffset] _SdfAlbedoB1 ("Projected Albedo B Slot 1", 2D) = "white" {}
        [NoScaleOffset] _SdfAlbedoB2 ("Projected Albedo B Slot 2", 2D) = "white" {}
        [NoScaleOffset] _SdfAlbedoB3 ("Projected Albedo B Slot 3", 2D) = "white" {}
        [NoScaleOffset] _SdfAlbedoB4 ("Projected Albedo B Slot 4", 2D) = "white" {}
        [HideInInspector] _SdfAlbedoColorA ("Albedo A Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _SdfAlbedoColorA1 ("Albedo A Slot 1 Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _SdfAlbedoColorA2 ("Albedo A Slot 2 Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _SdfAlbedoColorA3 ("Albedo A Slot 3 Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _SdfAlbedoColorA4 ("Albedo A Slot 4 Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _SdfAlbedoColorB ("Albedo B Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _SdfAlbedoColorB1 ("Albedo B Slot 1 Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _SdfAlbedoColorB2 ("Albedo B Slot 2 Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _SdfAlbedoColorB3 ("Albedo B Slot 3 Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _SdfAlbedoColorB4 ("Albedo B Slot 4 Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _SdfAlbedoStA ("Albedo A Tiling Offset", Vector) = (1, 1, 0, 0)
        [HideInInspector] _SdfAlbedoStA1 ("Albedo A Slot 1 Tiling Offset", Vector) = (1, 1, 0, 0)
        [HideInInspector] _SdfAlbedoStA2 ("Albedo A Slot 2 Tiling Offset", Vector) = (1, 1, 0, 0)
        [HideInInspector] _SdfAlbedoStA3 ("Albedo A Slot 3 Tiling Offset", Vector) = (1, 1, 0, 0)
        [HideInInspector] _SdfAlbedoStA4 ("Albedo A Slot 4 Tiling Offset", Vector) = (1, 1, 0, 0)
        [HideInInspector] _SdfAlbedoStB ("Albedo B Tiling Offset", Vector) = (1, 1, 0, 0)
        [HideInInspector] _SdfAlbedoStB1 ("Albedo B Slot 1 Tiling Offset", Vector) = (1, 1, 0, 0)
        [HideInInspector] _SdfAlbedoStB2 ("Albedo B Slot 2 Tiling Offset", Vector) = (1, 1, 0, 0)
        [HideInInspector] _SdfAlbedoStB3 ("Albedo B Slot 3 Tiling Offset", Vector) = (1, 1, 0, 0)
        [HideInInspector] _SdfAlbedoStB4 ("Albedo B Slot 4 Tiling Offset", Vector) = (1, 1, 0, 0)
        [HideInInspector] _SdfTriplanarScaleA ("Albedo A Projection Scale", Float) = 1
        [HideInInspector] _SdfTriplanarScaleB ("Albedo B Projection Scale", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "MeshSdfMetaballPass"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE3D(_SdfTextureA);
            SAMPLER(sampler_SdfTextureA);
            TEXTURE3D(_SdfTextureB);
            SAMPLER(sampler_SdfTextureB);
            TEXTURE3D(_MaterialIndexA);
            SAMPLER(sampler_MaterialIndexA);
            TEXTURE3D(_MaterialIndexB);
            SAMPLER(sampler_MaterialIndexB);
            TEXTURE2D(_SdfAlbedoA);
            SAMPLER(sampler_SdfAlbedoA);
            TEXTURE2D(_SdfAlbedoA1);
            SAMPLER(sampler_SdfAlbedoA1);
            TEXTURE2D(_SdfAlbedoA2);
            SAMPLER(sampler_SdfAlbedoA2);
            TEXTURE2D(_SdfAlbedoA3);
            SAMPLER(sampler_SdfAlbedoA3);
            TEXTURE2D(_SdfAlbedoA4);
            SAMPLER(sampler_SdfAlbedoA4);
            TEXTURE2D(_SdfAlbedoB);
            SAMPLER(sampler_SdfAlbedoB);
            TEXTURE2D(_SdfAlbedoB1);
            SAMPLER(sampler_SdfAlbedoB1);
            TEXTURE2D(_SdfAlbedoB2);
            SAMPLER(sampler_SdfAlbedoB2);
            TEXTURE2D(_SdfAlbedoB3);
            SAMPLER(sampler_SdfAlbedoB3);
            TEXTURE2D(_SdfAlbedoB4);
            SAMPLER(sampler_SdfAlbedoB4);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Blend;
                float _Metallic;
                float _Smoothness;
                float _HasBridgeCapsule;
                float4 _BridgeStart;
                float4 _BridgeEnd;
                float _BridgeRadius;
                float _BridgeSmoothness;
                float4 _SdfBoundsMinA;
                float4 _SdfBoundsMinB;
                float4 _SdfBoundsSizeA;
                float4 _SdfBoundsSizeB;
                float4 _ProxyBoundsMin;
                float4 _ProxyBoundsMax;
                float4 _SdfAlbedoColorA;
                float4 _SdfAlbedoColorA1;
                float4 _SdfAlbedoColorA2;
                float4 _SdfAlbedoColorA3;
                float4 _SdfAlbedoColorA4;
                float4 _SdfAlbedoColorB;
                float4 _SdfAlbedoColorB1;
                float4 _SdfAlbedoColorB2;
                float4 _SdfAlbedoColorB3;
                float4 _SdfAlbedoColorB4;
                float4 _SdfAlbedoStA;
                float4 _SdfAlbedoStA1;
                float4 _SdfAlbedoStA2;
                float4 _SdfAlbedoStA3;
                float4 _SdfAlbedoStA4;
                float4 _SdfAlbedoStB;
                float4 _SdfAlbedoStB1;
                float4 _SdfAlbedoStB2;
                float4 _SdfAlbedoStB3;
                float4 _SdfAlbedoStB4;
                float4x4 _WorldToLocalA;
                float4x4 _WorldToLocalB;
                float4x4 _LocalToWorldA;
                float4x4 _LocalToWorldB;
                float _DistanceScaleA;
                float _DistanceScaleB;
                float _HasSdfA;
                float _HasSdfB;
                float _HasMaterialIndexA;
                float _HasMaterialIndexB;
                float _SdfTriplanarScaleA;
                float _SdfTriplanarScaleB;
                float _NormalStep;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            float SmoothMin(float a, float b, float k)
            {
                if (k <= 0.00001)
                    return min(a, b);

                float h = saturate(0.5 + 0.5 * (b - a) / k);
                return lerp(b, a, h) - k * h * (1.0 - h);
            }

            float SampleSdfA(float3 positionWS)
            {
                if (_HasSdfA < 0.5)
                    return 1e5;

                float3 localPosition = mul(_WorldToLocalA, float4(positionWS, 1.0)).xyz;
                float3 localMax = _SdfBoundsMinA.xyz + _SdfBoundsSizeA.xyz;
                float3 clampedPosition = clamp(localPosition, _SdfBoundsMinA.xyz, localMax);
                float3 uvw = (clampedPosition - _SdfBoundsMinA.xyz) / _SdfBoundsSizeA.xyz;
                float outsideDistance = length(localPosition - clampedPosition);
                if (outsideDistance > 0.0)
                {
                    float edgeDistance = SAMPLE_TEXTURE3D_LOD(_SdfTextureA, sampler_SdfTextureA, uvw, 0).r;
                    return max(outsideDistance, edgeDistance - outsideDistance) * _DistanceScaleA;
                }

                return SAMPLE_TEXTURE3D_LOD(_SdfTextureA, sampler_SdfTextureA, uvw, 0).r * _DistanceScaleA;
            }

            float SampleSdfB(float3 positionWS)
            {
                if (_HasSdfB < 0.5)
                    return 1e5;

                float3 localPosition = mul(_WorldToLocalB, float4(positionWS, 1.0)).xyz;
                float3 localMax = _SdfBoundsMinB.xyz + _SdfBoundsSizeB.xyz;
                float3 clampedPosition = clamp(localPosition, _SdfBoundsMinB.xyz, localMax);
                float3 uvw = (clampedPosition - _SdfBoundsMinB.xyz) / _SdfBoundsSizeB.xyz;
                float outsideDistance = length(localPosition - clampedPosition);
                if (outsideDistance > 0.0)
                {
                    float edgeDistance = SAMPLE_TEXTURE3D_LOD(_SdfTextureB, sampler_SdfTextureB, uvw, 0).r;
                    return max(outsideDistance, edgeDistance - outsideDistance) * _DistanceScaleB;
                }

                return SAMPLE_TEXTURE3D_LOD(_SdfTextureB, sampler_SdfTextureB, uvw, 0).r * _DistanceScaleB;
            }

            float SampleMaterialIndexA(float3 positionWS)
            {
                if (_HasMaterialIndexA < 0.5)
                    return 0.0;

                float3 localPosition = mul(_WorldToLocalA, float4(positionWS, 1.0)).xyz;
                float3 localMax = _SdfBoundsMinA.xyz + _SdfBoundsSizeA.xyz;
                float3 uvw = (clamp(localPosition, _SdfBoundsMinA.xyz, localMax) - _SdfBoundsMinA.xyz) / _SdfBoundsSizeA.xyz;
                return SAMPLE_TEXTURE3D_LOD(_MaterialIndexA, sampler_MaterialIndexA, uvw, 0).r;
            }

            float SampleMaterialIndexB(float3 positionWS)
            {
                if (_HasMaterialIndexB < 0.5)
                    return 0.0;

                float3 localPosition = mul(_WorldToLocalB, float4(positionWS, 1.0)).xyz;
                float3 localMax = _SdfBoundsMinB.xyz + _SdfBoundsSizeB.xyz;
                float3 uvw = (clamp(localPosition, _SdfBoundsMinB.xyz, localMax) - _SdfBoundsMinB.xyz) / _SdfBoundsSizeB.xyz;
                return SAMPLE_TEXTURE3D_LOD(_MaterialIndexB, sampler_MaterialIndexB, uvw, 0).r;
            }

            float CapsuleSdf(float3 positionWS)
            {
                float3 startToEnd = _BridgeEnd.xyz - _BridgeStart.xyz;
                float segmentLengthSquared = max(dot(startToEnd, startToEnd), 1e-6);
                float alongSegment = saturate(dot(positionWS - _BridgeStart.xyz, startToEnd) / segmentLengthSquared);
                float3 closestPoint = _BridgeStart.xyz + startToEnd * alongSegment;
                return length(positionWS - closestPoint) - _BridgeRadius;
            }

            float SceneSdf(float3 positionWS)
            {
                float distanceA = SampleSdfA(positionWS);
                float distanceB = SampleSdfB(positionWS);
                float unionDistance = SmoothMin(distanceA, distanceB, _Blend);
                if (_HasBridgeCapsule > 0.5)
                    unionDistance = SmoothMin(unionDistance, CapsuleSdf(positionWS), _BridgeSmoothness);
                return unionDistance;
            }

            bool IntersectProxyBounds(float3 rayOrigin, float3 rayDirection, out float nearDistance, out float farDistance)
            {
                float3 inverseDirection = 1.0 / rayDirection;
                float3 first = (_ProxyBoundsMin.xyz - rayOrigin) * inverseDirection;
                float3 second = (_ProxyBoundsMax.xyz - rayOrigin) * inverseDirection;
                float3 axisNear = min(first, second);
                float3 axisFar = max(first, second);
                nearDistance = max(max(axisNear.x, axisNear.y), axisNear.z);
                farDistance = min(min(axisFar.x, axisFar.y), axisFar.z);
                return farDistance >= max(nearDistance, 0.0);
            }

            float Raymarch(float3 rayOrigin, float3 rayDirection, float startDistance, float endDistance)
            {
                float travel = startDistance;
                const float marchSafety = 0.8;

                [loop]
                for (int stepIndex = 0; stepIndex < 160; stepIndex++)
                {
                    float distanceToSurface = SceneSdf(rayOrigin + rayDirection * travel);
                    if (distanceToSurface < 0.0005)
                        return travel;

                    travel += max(distanceToSurface * marchSafety, 0.0001);
                    if (travel > endDistance)
                        break;
                }

                return -1.0;
            }

            float3 EstimateNormal(float3 positionWS)
            {
                float e = max(_NormalStep, 0.0005);
                float3 normal;
                normal.x = SceneSdf(positionWS + float3(e, 0, 0)) - SceneSdf(positionWS - float3(e, 0, 0));
                normal.y = SceneSdf(positionWS + float3(0, e, 0)) - SceneSdf(positionWS - float3(0, e, 0));
                normal.z = SceneSdf(positionWS + float3(0, 0, e)) - SceneSdf(positionWS - float3(0, 0, e));
                return normalize(normal);
            }

            float3 TriplanarAlbedo(TEXTURE2D_PARAM(albedoMap, albedoSampler), float3 localPosition,
                float3 localNormal, float scale, float4 st)
            {
                float3 weights = pow(abs(normalize(localNormal)), 4.0);
                weights /= max(weights.x + weights.y + weights.z, 0.0001);

                float3 orientation = sign(localNormal);
                float2 uvX = float2(localPosition.y * orientation.x, localPosition.z) * scale;
                float2 uvY = float2(localPosition.x, localPosition.z * orientation.y) * scale;
                float2 uvZ = float2(localPosition.x * orientation.z, localPosition.y) * scale;
                uvX = uvX * st.xy + st.zw;
                uvY = uvY * st.xy + st.zw;
                uvZ = uvZ * st.xy + st.zw;

                float3 colorX = SAMPLE_TEXTURE2D(albedoMap, albedoSampler, uvX).rgb;
                float3 colorY = SAMPLE_TEXTURE2D(albedoMap, albedoSampler, uvY).rgb;
                float3 colorZ = SAMPLE_TEXTURE2D(albedoMap, albedoSampler, uvZ).rgb;
                return colorX * weights.x + colorY * weights.y + colorZ * weights.z;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDirection = normalize(input.positionWS - rayOrigin);
                float nearDistance;
                float farDistance;
                if (!IntersectProxyBounds(rayOrigin, rayDirection, nearDistance, farDistance))
                    discard;

                float hitDistance = Raymarch(rayOrigin, rayDirection, max(nearDistance, 0.0), farDistance);
                if (hitDistance < 0.0)
                    discard;

                float3 hitPositionWS = rayOrigin + rayDirection * hitDistance;
                float3 normalWS = EstimateNormal(hitPositionWS);
                float distanceA = SampleSdfA(hitPositionWS);
                float distanceB = SampleSdfB(hitPositionWS);
                float blendWeight = _Blend > 0.00001
                    ? saturate(0.5 + 0.5 * (distanceB - distanceA) / _Blend)
                    : (distanceA <= distanceB ? 1.0 : 0.0);

                float3 localA = mul(_WorldToLocalA, float4(hitPositionWS, 1.0)).xyz;
                float3 localB = mul(_WorldToLocalB, float4(hitPositionWS, 1.0)).xyz;
                float3 normalA = mul((float3x3)transpose(_LocalToWorldA), normalWS);
                float3 normalB = mul((float3x3)transpose(_LocalToWorldB), normalWS);
                float materialIndexA = floor(SampleMaterialIndexA(hitPositionWS) + 0.5);
                float materialIndexB = floor(SampleMaterialIndexB(hitPositionWS) + 0.5);
                float3 albedoA;
                float3 albedoB;
                if (materialIndexA < 0.5)
                    albedoA = TriplanarAlbedo(TEXTURE2D_ARGS(_SdfAlbedoA, sampler_SdfAlbedoA), localA, normalA,
                        _SdfTriplanarScaleA, _SdfAlbedoStA) * _SdfAlbedoColorA.rgb;
                else if (materialIndexA < 1.5)
                    albedoA = TriplanarAlbedo(TEXTURE2D_ARGS(_SdfAlbedoA1, sampler_SdfAlbedoA1), localA, normalA,
                        _SdfTriplanarScaleA, _SdfAlbedoStA1) * _SdfAlbedoColorA1.rgb;
                else if (materialIndexA < 2.5)
                    albedoA = TriplanarAlbedo(TEXTURE2D_ARGS(_SdfAlbedoA2, sampler_SdfAlbedoA2), localA, normalA,
                        _SdfTriplanarScaleA, _SdfAlbedoStA2) * _SdfAlbedoColorA2.rgb;
                else if (materialIndexA < 3.5)
                    albedoA = TriplanarAlbedo(TEXTURE2D_ARGS(_SdfAlbedoA3, sampler_SdfAlbedoA3), localA, normalA,
                        _SdfTriplanarScaleA, _SdfAlbedoStA3) * _SdfAlbedoColorA3.rgb;
                else
                    albedoA = TriplanarAlbedo(TEXTURE2D_ARGS(_SdfAlbedoA4, sampler_SdfAlbedoA4), localA, normalA,
                        _SdfTriplanarScaleA, _SdfAlbedoStA4) * _SdfAlbedoColorA4.rgb;

                if (materialIndexB < 0.5)
                    albedoB = TriplanarAlbedo(TEXTURE2D_ARGS(_SdfAlbedoB, sampler_SdfAlbedoB), localB, normalB,
                        _SdfTriplanarScaleB, _SdfAlbedoStB) * _SdfAlbedoColorB.rgb;
                else if (materialIndexB < 1.5)
                    albedoB = TriplanarAlbedo(TEXTURE2D_ARGS(_SdfAlbedoB1, sampler_SdfAlbedoB1), localB, normalB,
                        _SdfTriplanarScaleB, _SdfAlbedoStB1) * _SdfAlbedoColorB1.rgb;
                else if (materialIndexB < 2.5)
                    albedoB = TriplanarAlbedo(TEXTURE2D_ARGS(_SdfAlbedoB2, sampler_SdfAlbedoB2), localB, normalB,
                        _SdfTriplanarScaleB, _SdfAlbedoStB2) * _SdfAlbedoColorB2.rgb;
                else if (materialIndexB < 3.5)
                    albedoB = TriplanarAlbedo(TEXTURE2D_ARGS(_SdfAlbedoB3, sampler_SdfAlbedoB3), localB, normalB,
                        _SdfTriplanarScaleB, _SdfAlbedoStB3) * _SdfAlbedoColorB3.rgb;
                else
                    albedoB = TriplanarAlbedo(TEXTURE2D_ARGS(_SdfAlbedoB4, sampler_SdfAlbedoB4), localB, normalB,
                        _SdfTriplanarScaleB, _SdfAlbedoStB4) * _SdfAlbedoColorB4.rgb;
                float3 albedo = lerp(albedoB, albedoA, blendWeight) * _Color.rgb;

                InputData inputData = (InputData)0;
                inputData.positionWS = hitPositionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = SafeNormalize(_WorldSpaceCameraPos - hitPositionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(hitPositionWS);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1.0, 1.0, 1.0, 1.0);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0.0, 0.0, 1.0);
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = _Color.a;
                surfaceData.clearCoatSmoothness = 1.0;
                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }
    }
}
