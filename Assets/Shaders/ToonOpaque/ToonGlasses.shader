Shader "Custom/Toon/LitGlass_URP"
{
    Properties
    {
        [Header(Main)]
        _Color("Main Color", Color) = (1,1,1,0.2)
        _SColor("Specular Color", Color) = (1,1,1,1)
        
        [Header(Light Direction Specular)]
        _SpecSize("Light Direction Specular Size", Range(0.65, 0.999)) = 0.9
        _SpecOffset("Light Direction Specular Offset", Range(0.5, 1)) = 0.6
        _Offset2("LightDir Spec Smoothness", Range(0, 1)) = 0.05
        
        [Header(View Direction Specular)]
        _SpecSize2("View Specular", Range(0.65, 0.999)) = 0.9
        _Offset("View Spec Smoothness", Range(0, 1)) = 0.1
        
        [Header(Outer Rim)]
        _RimPower2("Rim Offset Out Rim", Range(0, 4)) = 0.7
        _RimColor2("Outer Rim Color", Color) = (0.49, 0.94, 0.64, 1)
        _OutRimCutoff("Out Rim Cutoff Inner", Range(0, 1)) = 0
        
        [Header(Inner Fresnel)]
        _RimPower("Rim Offset Inner Fresnel", Range(0, 4)) = 1.2
        _RimColor("Inner Fresnel Rim Color", Color) = (0.49, 0.94, 0.64, 1)
        _FresnelInner("Fresnell Rim Cutoff", Range(0, 2)) = 0.7

        [Header(System)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 10
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2
        [Toggle] _ZWrite("Z Write", Float) = 0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            Cull [_Cull]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _SColor;
                half4 _RimColor;
                half4 _RimColor2;
                float _SpecSize;
                float _SpecSize2;
                float _SpecOffset;
                float _Offset;
                float _Offset2;
                float _RimPower;
                float _RimPower2;
                float _FresnelInner;
                float _OutRimCutoff;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                float3 viewDirWS = SafeNormalize(input.viewDirWS);
                
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;

                half lightDot = dot(normalWS, lightDir - viewDirWS) * 0.5 + _SpecOffset;
                
                float NdotV = saturate(dot(viewDirWS, normalWS));
                
                float outerRimTerm = _RimPower2 - NdotV;
                float innerFresnelTerm = _RimPower - NdotV;

                float innerGlow = smoothstep(0.5, 0.5 + _OutRimCutoff, innerFresnelTerm) * _RimColor.a;
                float outerRim = (1.0 - smoothstep(0.5 - _FresnelInner, 0.5, outerRimTerm)) * _RimColor2.a;

                float3 emission = 0;
                emission += _RimColor2.rgb * pow(abs(outerRim), 1.5);
                emission += _RimColor.rgb * pow(abs(innerGlow), 1.5);

                half viewSpecBase = saturate(dot(normalWS, viewDirWS));
                float viewSpecLine = smoothstep(_SpecSize2, _SpecSize2 + _Offset, viewSpecBase) * 10.0;
                
                float lightDirSpecular = smoothstep(_SpecSize, _SpecSize + _Offset2, lightDot) * 10.0;

                float totalSpecular = saturate(viewSpecLine + lightDirSpecular);
                emission += totalSpecular * _SColor.rgb * 2.0;

                half finalAlpha = saturate(_Color.a + (viewSpecLine + lightDirSpecular + outerRim + innerGlow));
                half3 finalColor = saturate(_Color.rgb + emission);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return float4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
}