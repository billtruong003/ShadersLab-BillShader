Shader "Custom/CrystalOptimized"
{
    Properties
    {
        [HDR] _BaseColor("Base Color", Color) = (0.5, 0.5, 1.0, 0.5)
        _CrystalColor1("Crystal Color 1", Color) = (1, 0, 1, 1)
        _CrystalColor2("Crystal Color 2", Color) = (0, 1, 1, 1)

        [Header(Noise Texture Settings)]
        _NoiseMap("Noise Map (Grayscale)", 2D) = "white"{}
        _NoiseTiling("Noise Tiling", Float) = 1.0
        _NoiseScrollSpeed("Noise Scroll Speed (X1, Y1, X2, Y2)", Vector) = (0.05, 0.02, -0.03, 0.04)
        _NoiseColorIntensity("Noise Color Intensity", Range(0.0, 2.0)) = 1.0

        [Header(Parallax Settings)]
        _ParallaxMap("Parallax Map (Height)", 2D) = "white"{}
        _ParallaxStrength("Parallax Strength", Range(-1, 1)) = 0.02

        [Header(Sparkle Effect)]
        _SparkleIntensity("Sparkle Intensity", Range(0.0, 50.0)) = 20.0
        _SparkleThreshold("Sparkle Threshold", Range(0.0, 1.0)) = 0.95

        [Header(Fresnel Effect)]
        _FresnelColor("Fresnel Color", Color) = (1, 1, 1, 1)
        _FresnelPower("Fresnel Power", Range(0.1, 10.0)) = 2.5
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : NORMAL;
                float3 viewDirTS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _CrystalColor1;
            float4 _CrystalColor2;
            float4 _NoiseScrollSpeed;
            float _NoiseTiling;
            float _NoiseColorIntensity;
            float _ParallaxStrength;
            float _SparkleIntensity;
            float _SparkleThreshold;
            float4 _FresnelColor;
            float _FresnelPower;
            CBUFFER_END

            TEXTURE2D(_ParallaxMap);
            SAMPLER(sampler_ParallaxMap);
            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);

            float2 GetParallaxOffset(float height, float strength, float3 viewDirTS)
            {
                return viewDirTS.xy * (height * strength);
            }

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                float3 bitangentWS = cross(normalWS, tangentWS) * IN.tangentOS.w;
                OUT.normalWS = normalWS;

                float3 viewDirWS = GetWorldSpaceViewDir(OUT.positionWS);
                OUT.viewDirTS = float3(dot(viewDirWS, tangentWS), dot(viewDirWS, bitangentWS), dot(viewDirWS, normalWS));

                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));

                float height = SAMPLE_TEXTURE2D(_ParallaxMap, sampler_ParallaxMap, IN.uv).r;
                float2 parallaxUV = GetParallaxOffset(height, _ParallaxStrength, normalize(IN.viewDirTS));
                float2 finalUV = IN.uv + parallaxUV;

                float2 noiseUV1 = finalUV * _NoiseTiling + _Time.y * _NoiseScrollSpeed.xy;
                float2 noiseUV2 = finalUV * _NoiseTiling * 0.7 + _Time.y * _NoiseScrollSpeed.zw;

                float noiseValue1 = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUV1).r;
                float noiseValue2 = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUV2).r;
                float finalNoise = (noiseValue1 + noiseValue2) * 0.5;

                half3 noiseColor = lerp(_CrystalColor1.rgb, _CrystalColor2.rgb, finalNoise) * _NoiseColorIntensity;

                half fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                fresnel = pow(fresnel, _FresnelPower);
                half3 fresnelColor = _FresnelColor.rgb * fresnel;

                float sparkleNoise = rand(finalUV * 25.0 + _Time.y * 0.1);
                float sparkle = pow(sparkleNoise, _SparkleIntensity);
                sparkle = step(_SparkleThreshold, sparkle);
                half3 sparkleColor = sparkle * _FresnelColor.rgb;

                half3 finalColor = _BaseColor.rgb + noiseColor + fresnelColor + sparkleColor;

                return half4(finalColor, _BaseColor.a * (1.0 - fresnel * 0.5));
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}
