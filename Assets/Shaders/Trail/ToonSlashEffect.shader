Shader "Unlit/StaticToonSlash"
{
    Properties
    {
        [Header(Appearance)]
        _BaseColor("Base Color", Color) = (0.5, 0.8, 1, 1)
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(0.1, 10)) = 3.5
        _GradientSharpness("Edge Gradient Sharpness", Range(0, 10)) = 2.0

        [Header(Energy Texture)]
        _EnergyTexture("Energy Texture (Grayscale)", 2D) = "white"{}
        _ScrollSpeed("Scroll Speed", Float) = 1.0
        _EnergyIntensity("Energy Intensity", Range(0, 5)) = 1.5
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldViewDir : TEXCOORD2;
            };

            sampler2D _EnergyTexture;

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _RimColor;
            half _RimPower;
            half _GradientSharpness;
            float4 _EnergyTexture_ST;
            half _ScrollSpeed;
            half _EnergyIntensity;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.worldNormal = TransformObjectToWorldNormal(v.normalOS);
                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.worldViewDir = normalize(_WorldSpaceCameraPos.xyz - worldPos);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                    // -- Rim Light --
                    // Calculates a rim effect based on the viewing angle against the mesh's normals.
                half rimDot = 1.0 - saturate(dot(i.worldViewDir, i.worldNormal));
                half rim = pow(rimDot, _RimPower);
                half3 rimColor = _RimColor.rgb * rim * _RimColor.a;

                    // -- Scrolling Energy --
                    // A simple scrolling texture to give the slash a sense of flowing energy.
                float2 scrollUV = i.uv;
                scrollUV.x -= _Time.y * _ScrollSpeed;
                half energy = tex2D(_EnergyTexture, scrollUV).r * _EnergyIntensity;

                    // -- Base Color Composition --
                half3 baseColor = _BaseColor.rgb * energy;
                half3 finalColor = baseColor + rimColor;

                    // -- Alpha Gradient --
                    // Fades out the alpha at the edges of the mesh based on UVs.
                    // Assumes the mesh is UV-mapped with Y=0.5 at the center and 0/1 at the edges.
                half gradient = pow(1.0 - abs(i.uv.y * 2.0 - 1.0), _GradientSharpness);
                half finalAlpha = gradient * _BaseColor.a;

                    // Discard pixels that are almost fully transparent to avoid artifacts.
                clip(finalAlpha - 0.001);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
