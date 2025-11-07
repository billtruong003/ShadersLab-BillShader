    // Made by an AI Assistant for Unity URP
    // Shader: Unlit/ToonTrail
Shader "Unlit/ToonTrail"
{
    Properties
    {
        [Header(Color)]
        _StartColor("Start Color", Color) = (0.5, 0.8, 1, 1)
        _EndColor("End Color", Color) = (1, 1, 1, 0)

        [Header(Texture and Noise)]
        _NoiseTex("Noise Texture", 2D) = "white"{}
        _NoiseScrollSpeed("Noise Scroll Speed", Float) = 2.0
        _NoiseStrength("Noise Strength", Range(0, 5)) = 1.0

        [Header(Erosion)]
        _ErosionTex("Erosion Texture", 2D) = "white"{}
        _ErosionStrength("Erosion Strength", Range(0, 1)) = 0.8
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent"
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite On

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
                float4 color : COLOR; // Vertex color from Trail Renderer
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _NoiseTex;
            sampler2D _ErosionTex;

            CBUFFER_START(UnityPerMaterial)
            half4 _StartColor;
            half4 _EndColor;
            float4 _NoiseTex_ST;
            float _NoiseScrollSpeed;
            float _NoiseStrength;
            float _ErosionStrength;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;  // Pass vertex color to fragment shader
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                    // -- Lifetime Gradient --
                    // The Trail Renderer passes lifetime in the vertex alpha channel
                float life = i.color.a;

                    // -- Erosion --
                    // The trail erodes from the tail (where life is close to 0)
                float erosionValue = tex2D(_ErosionTex, i.uv).r;
                float erosionThreshold = (1.0 - life) * _ErosionStrength;
                float erosionStep = smoothstep(erosionThreshold, erosionThreshold + 0.1, erosionValue);

                    // If eroded, clip the pixel
                clip(erosionStep - 0.001);

                    // -- Scrolling Noise --
                float2 scrollUV = float2(i.uv.x + _Time.y * _NoiseScrollSpeed, i.uv.y);
                half noise = tex2D(_NoiseTex, scrollUV).r * _NoiseStrength;

                    // -- Color over Lifetime --
                half4 colorOverLife = lerp(_EndColor, _StartColor, life);

                    // -- Final Composition --
                half3 finalColor = colorOverLife.rgb * noise;
                half finalAlpha = colorOverLife.a * life * erosionStep;

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
