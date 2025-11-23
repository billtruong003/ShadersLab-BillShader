Shader "Stylized/SimpleSun"
{
    Properties
    {
        [Header(Surface)]
        _MainTex("Fire Texture", 2D) = "white" {}
        [MainColor] _CoreColor("Core Color", Color) = (1, 0.9, 0.4, 1)
        [HDR] _GlowColor("Glow/Rim Color", Color) = (1, 0.4, 0, 1)
        
        [Header(Settings)]
        _SunIntensity("Intensity", Range(1, 10)) = 2.0
        _ScrollSpeed("Scroll Speed", Vector) = (0.05, 0.1, 0, 0)
        _FresnelPower("Fresnel Power", Range(0, 5)) = 2.0
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
            Name "UnlitSunPass"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _GlowColor;
                float4 _MainTex_ST;
                float4 _ScrollSpeed;
                float _SunIntensity;
                float _FresnelPower;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                float2 uvOffset = _Time.y * _ScrollSpeed.xy;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex) + uvOffset;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = GetWorldSpaceViewDir(input.positionWS);
                float3 normal = normalize(input.normalWS);
                float NdotV = saturate(dot(normal, viewDir));

                half noise = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).r;

                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                
                half3 core = _CoreColor.rgb;
                half3 glow = _GlowColor.rgb;
                
                half3 finalColor = lerp(core, glow, fresnel + (noise * 0.2));
                finalColor *= _SunIntensity;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}