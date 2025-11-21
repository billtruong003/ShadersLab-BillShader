Shader "Bill's Toon/Pro Extended (Mask + Dissolve)"
{
    Properties
    {
        [Header(Base)]
        [MainTexture] _BaseMap("Albedo", 2D) = "white"{}
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        _BumpMap("Normal Map", 2D) = "bump"{}
        _BumpScale("Normal Strength", Range(0, 2)) = 1
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Toon Lighting)]
        [NoScaleOffset] _ToonRamp("Ramp Texture (RGB)", 2D) = "white"{}
        _RampThreshold("Ramp Threshold", Range(-1, 1)) = 0.0
        _RampSmoothness("Ramp Smoothness", Range(0.001, 1)) = 0.01
        _ShadowColor("Shadow Tint", Color) = (0.6, 0.6, 0.7, 1)

        [Header(Specular)]
        [HDR] _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularSize("Specular Size", Range(0, 1)) = 0.1
        _SpecularFalloff("Specular Falloff", Range(0.001, 0.5)) = 0.05

        [Header(Rim Light)]
        [HDR] _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(0.1, 10)) = 4
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.5

        [Header(MatCap Metal)]
        [NoScaleOffset] _MatCapTex("MatCap Texture", 2D) = "black"{}
        _MatCapStrength("MatCap Strength", Range(0, 2)) = 0

        [Header(Dissolve Effect)]
        [Toggle(_DISSOLVE_ON)] _DissolveToggle("Enable Dissolve", Float) = 0
        _DissolveMap("Dissolve Noise", 2D) = "white"{}
        _DissolveAmount("Dissolve Amount", Range(0, 1)) = 0
        _DissolveEdgeWidth("Edge Width", Range(0, 0.2)) = 0.05
        [HDR] _DissolveEdgeColor("Edge Color", Color) = (1, 0.5, 0, 1)
        _DissolveScale("Noise Scale", Float) = 1.0

        [Header(Advanced Masking)]
        [Toggle(_MASKING_ON)] _MaskingToggle("Enable Masking", Float) = 0
        [NoScaleOffset] _MaskControlMap("Control Map (RGB)", 2D) = "black"{}
        [Toggle(_TRIPLANAR_MASK)] _TriplanarToggle("Use Triplanar Projection", Float) = 0
        _TriplanarScale("Triplanar Scale", Float) = 1.0
        _TriplanarBlendSharpness("Blend Sharpness", Range(1, 10)) = 2.0

        [Header(Layer 1 Red Channel)]
        _Layer1Tex("Layer 1 Texture", 2D) = "white"{}
        _Layer1Color("Layer 1 Color", Color) = (1, 1, 1, 1)

        [Header(Layer 2 Green Channel)]
        _Layer2Tex("Layer 2 Texture", 2D) = "white"{}
        _Layer2Color("Layer 2 Color", Color) = (1, 1, 1, 1)

        [Header(Layer 3 Blue Channel)]
        _Layer3Tex("Layer 3 Texture", 2D) = "white"{}
        _Layer3Color("Layer 3 Color", Color) = (1, 1, 1, 1)

        [Header(Hull Outline)]
        _OutlineWidth("Outline Width", Range(0, 2.0)) = 0.02
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)

        [Header(Emission)]
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 0)
        _EmissionMap("Emission Map", 2D) = "black"{}

        [HideInInspector] _Surface("Surface", Float) = 0.0
        [HideInInspector] _Blend("Blend", Float) = 0.0
        [HideInInspector] _Cull("Cull", Float) = 2.0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1.0
        [HideInInspector] _SrcBlend("Src", Float) = 1.0
        [HideInInspector] _DstBlend("Dst", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Outline"
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }
            Cull Front
            ZWrite On
            ZTest Less
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex VertOutline
            #pragma fragment FragOutline
            #pragma shader_feature_local _DISSOLVE_ON

            #include "ToonLighting_Extended.hlsl"

            struct AttributesOutline
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct VaryingsOutline
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            VaryingsOutline VertOutline(AttributesOutline v)
            {
                VaryingsOutline o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                float dist = distance(positionWS, _WorldSpaceCameraPos);
                float width = _OutlineWidth * 0.01 * clamp(dist, 1.0, 100.0);
                positionWS += normalWS * width;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 FragOutline(VaryingsOutline i) : SV_Target
            {
                #if defined(_DISSOLVE_ON)
                    float noise = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, i.uv * _DissolveScale).r;
                    clip(noise - _DissolveAmount);
                #endif
                return _OutlineColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "UniversalForward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _NORMALMAP
            #pragma shader_feature_local _DISSOLVE_ON
            #pragma shader_feature_local _MASKING_ON
            #pragma shader_feature_local _TRIPLANAR_MASK

            #include "ToonLighting_Extended.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);

                real sign = v.tangentOS.w * GetOddNegativeScale();
                o.tangentWS = float4(TransformObjectToWorldDir(v.tangentOS.xyz), sign);

                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                #if defined(_ALPHATEST_ON)
                    clip(baseMap.a - _Cutoff);
                #endif

                half3 albedo = baseMap.rgb * _BaseColor.rgb;
                half alpha = baseMap.a;

                #if defined(_DISSOLVE_ON)
                    ApplyDissolve(i.uv, albedo, alpha);
                #endif

                half3 normalWS = normalize(i.normalWS);
                #if defined(_NORMALMAP)
                    half3 tangentWS = i.tangentWS.xyz;
                    half3 bitangentWS = cross(normalWS, tangentWS) * i.tangentWS.w;
                    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, i.uv), _BumpScale);
                    normalWS = mul(normalTS, float3x3(tangentWS, bitangentWS, normalWS));
                #endif

                #if defined(_MASKING_ON)
                    albedo = ApplyMultiLayerMasking(i.uv, i.positionWS, normalWS, albedo);
                #endif

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(i.positionWS);

                ToonSurfaceData s;
                s.albedo = albedo;
                s.normalWS = normalWS;
                s.viewDirWS = viewDirWS;
                s.positionWS = i.positionWS;
                s.alpha = alpha;
                s.emission = 0;

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 color = CalculateToonLight(mainLight, s);

                #ifdef _ADDITIONAL_LIGHTS
                    uint pixelLightCount = GetAdditionalLightsCount();
                    for (uint j = 0;
                    j < pixelLightCount;
                    ++j)
                    {
                        Light addLight = GetAdditionalLight(j, i.positionWS);
                        color += CalculateToonLight(addLight, s);
                    }
                #endif

                color += SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, i.uv).rgb * _EmissionColor.rgb;
                color += CalculateRimLight(normalWS, viewDirWS);
                color += ApplyMatCap(viewDirWS, normalWS);

                return half4(color, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            ZWrite On ZTest LEqual ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _DISSOLVE_ON

            #include "ToonLighting_Extended.hlsl"

            struct AttributesShadow
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct VaryingsShadow
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            VaryingsShadow ShadowVert(AttributesShadow v)
            {
                VaryingsShadow o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 ShadowFrag(VaryingsShadow i) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a;
                    clip(alpha - _Cutoff);
                #endif

                #if defined(_DISSOLVE_ON)
                    float noise = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, i.uv * _DissolveScale).r;
                    clip(noise - _DissolveAmount);
                #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
            ZWrite On ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _DISSOLVE_ON

            #include "ToonLighting_Extended.hlsl"

            struct AttributesDepth
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            VaryingsDepth DepthVert(AttributesDepth v)
            {
                VaryingsDepth o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 DepthFrag(VaryingsDepth i) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a;
                    clip(alpha - _Cutoff);
                #endif

                #if defined(_DISSOLVE_ON)
                    float noise = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, i.uv * _DissolveScale).r;
                    clip(noise - _DissolveAmount);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }
    CustomEditor "BillToonProEditor"
}
