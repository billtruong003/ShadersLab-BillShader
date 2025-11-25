Shader "Skybox/GalaxyCelestial"
{
    Properties
    {
        [Header(Base Layers)]
        [NoScaleOffset] _MainNoiseTex ("Structure Noise (A)", 2D) = "black"{}
        [NoScaleOffset] _DetailNoiseTex ("Detail Noise (B)", 2D) = "black"{}
        [NoScaleOffset] _StarNoiseTex ("Star Map (C)", 2D) = "black"{}
        [NoScaleOffset] _ColorRamp ("Color Ramp", 2D) = "white"{}

        [Header(Celestial Bodies)]
        [NoScaleOffset] _SunTex ("Sun Texture", 2D) = "black"{}
        _SunSize ("Sun Size", Range(0.01, 0.5)) = 0.1
        _SunTint ("Sun Tint", Color) = (1, 1, 1, 1)

        [NoScaleOffset] _MoonTex ("Moon Texture", 2D) = "black"{}
        _MoonSize ("Moon Size", Range(0.01, 0.5)) = 0.1
        _MoonTint ("Moon Tint", Color) = (1, 1, 1, 1)

        [Header(Nebula Settings)]
        _NebulaScale ("Scale", Float) = 1.0
        _NebulaSpeed ("Scroll Speed", Vector) = (0.02, 0.01, 0, 0)
        _Exposure ("Exposure", Range(0, 5)) = 1.2
        _Density ("Density", Range(0, 3)) = 1.0

        [Header(Star Settings)]
        _StarScale ("Star Tiling", Float) = 5.0
        _StarBrightness ("Star Brightness", Range(0, 10)) = 2.0
        _StarThreshold ("Star Threshold", Range(0, 1)) = 0.75
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox"
        }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _MainNoiseTex;
            sampler2D _DetailNoiseTex;
            sampler2D _StarNoiseTex;
            sampler2D _ColorRamp;

            sampler2D _SunTex;
            float _SunSize;
            float4 _SunTint;

            sampler2D _MoonTex;
            float _MoonSize;
            float4 _MoonTint;

            float _NebulaScale;
            float4 _NebulaSpeed;
            float _Exposure;
            float _Density;

            float _StarScale;
            float _StarBrightness;
            float _StarThreshold;

            float3 _SunDir;
            float3 _MoonDir;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 viewDir : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.viewDir = v.texcoord;
                return o;
            }

            float3 GetTriplanarWeights(float3 normal)
            {
                float3 blend = abs(normal);
                blend = pow(blend, 4.0);
                return blend / (blend.x + blend.y + blend.z);
            }

            float SampleTriplanar(sampler2D tex, float3 dir, float scale, float2 offset)
            {
                float3 blend = GetTriplanarWeights(dir);
                float2 uvX = dir.zy * scale + offset;
                float2 uvY = dir.xz * scale + offset;
                float2 uvZ = dir.xy * scale + offset;
                float3 col;
                col.x = tex2D(tex, uvX).r;
                col.y = tex2D(tex, uvY).r;
                col.z = tex2D(tex, uvZ).r;
                return dot(col, blend);
            }

            float4 RenderCelestialBody(sampler2D tex, float3 viewDir, float3 lightDir, float size, float4 tint)
            {
                float3 zAxis = normalize(lightDir);
                float3 yAxis = float3(0, 1, 0);
                float3 xAxis = normalize(cross(yAxis, zAxis));
                yAxis = cross(zAxis, xAxis);

                float xVal = dot(viewDir, xAxis);
                float yVal = dot(viewDir, yAxis);
                float zVal = dot(viewDir, zAxis);

                if (zVal > 0)
                {
                    float2 uv = float2(xVal, yVal) / size;
                    uv = uv * 0.5 + 0.5;

                    if (uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1)
                    {
                        float4 col = tex2D(tex, uv);
                        return col * tint;
                    }
                }
                return float4(0, 0, 0, 0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.viewDir);

                float2 scrollMain = _Time.y * _NebulaSpeed.xy;
                float2 scrollDetail = _Time.y * _NebulaSpeed.zw * 1.5;

                float noiseA = SampleTriplanar(_MainNoiseTex, dir, _NebulaScale, scrollMain);
                float noiseB = SampleTriplanar(_DetailNoiseTex, dir, _NebulaScale * 2.0, -scrollMain * 0.5);

                float combinedNoise = saturate(noiseA + noiseB * 0.5);
                float pattern = pow(combinedNoise, _Density);

                float3 nebulaColor = tex2D(_ColorRamp, float2(pattern, 0.5)).rgb;
                nebulaColor *= pattern * _Exposure;

                float starNoise = SampleTriplanar(_StarNoiseTex, dir, _StarScale, float2(0, 0));
                float starVal = max(0, starNoise - _StarThreshold) / (1.0 - _StarThreshold);
                float3 stars = starVal * _StarBrightness;

                float4 sunCol = RenderCelestialBody(_SunTex, dir, _SunDir, _SunSize, _SunTint);
                float4 moonCol = RenderCelestialBody(_MoonTex, dir, _MoonDir, _MoonSize, _MoonTint);

                float3 finalColor = nebulaColor + stars;
                finalColor = lerp(finalColor, sunCol.rgb, sunCol.a);
                finalColor = lerp(finalColor, moonCol.rgb, moonCol.a);

                return float4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}
