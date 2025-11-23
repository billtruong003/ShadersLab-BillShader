Shader "Stylized/OptimizedMoon"
{
    Properties
    {
        [Header(Textures)]
        _MainTex("Moon Albedo", 2D) = "white" {}
        _BumpMap("Normal Map", 2D) = "bump" {}

        [Header(Colors)]
        [MainColor] _LitColor("Lit Color", Color) = (1, 1, 0.9, 1)
        _ShadowColor("Shadow Color", Color) = (0.1, 0.1, 0.15, 1)
        [HDR] _RimColor("Atmosphere Rim Color", Color) = (0.5, 0.7, 1, 1)

        [Header(Lighting Control)]
        _LightDir("Fake Light Direction", Vector) = (1, 0, 0, 0)
        _ShadowSoftness("Shadow Edge Softness", Range(0.01, 1)) = 0.1
        _Brightness("Brightness", Range(0, 5)) = 1.0

        [Header(Fresnel)]
        _FresnelPower("Fresnel Power", Range(0, 10)) = 4.0
        _FresnelIntensity("Fresnel Intensity", Range(0, 5)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "StylizedMoonPass"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LitColor;
                half4 _ShadowColor;
                half4 _RimColor;
                float4 _MainTex_ST;
                float4 _LightDir;
                float _ShadowSoftness;
                float _Brightness;
                float _FresnelPower;
                float _FresnelIntensity;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 viewDir = GetWorldSpaceViewDir(input.positionWS);
                
                float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 TBN = float3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                
                float3 normalMap = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));
                float3 normalWS = normalize(mul(normalMap, TBN));

                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float3 lightDir = normalize(_LightDir.xyz);
                float NdotL = dot(normalWS, lightDir);

                float shadowMask = smoothstep(-_ShadowSoftness, _ShadowSoftness, NdotL);
                
                half3 finalBaseColor = lerp(_ShadowColor.rgb, _LitColor.rgb, shadowMask);
                finalBaseColor *= albedo.rgb * _Brightness;

                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                half3 rim = _RimColor.rgb * fresnel * _FresnelIntensity;

                half3 finalColor = finalBaseColor + rim;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Simple Lit"
}