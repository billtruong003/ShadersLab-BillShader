    // Dành cho Unity Universal Render Pipeline (URP)
    // Tác giả: AI (Dựa trên yêu cầu của người dùng)
    // Phiên bản: 1.0
    // Mô tả: Shader tạo hiệu ứng băng tuyết (ice) theo phong cách toon.
    //        Bao gồm các tính năng:
    //        - Toon shading dựa trên ramp texture.
    //        - Gradient màu theo chiều dọc của object.
    //        - Hiệu ứng Rim Light (viền sáng) có thể tùy chỉnh.

Shader "AI/Toon Ice URP"
{
    Properties
    {
        [Header(Surface Colors)]
        [MainColor] _TopColor("Top Color", Color) = (0.64, 0.94, 0.64, 1.0)
        _BottomColor("Bottom Color", Color) = (0.23, 0.0, 0.95, 1.0)

        [Header(Toon Shading)]
        _ToonRamp("Toon Ramp (RGB)", 2D) = "gray"{}

        [Header(Rim Lighting)]
        _RimBrightness("Rim Brightness", Range(1, 10)) = 3.2
        _RimHardness("Rim Hardness", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        LOD 200

        Pass
        {
            Name "ToonIceForward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

                // Bao gồm các thư viện cốt lõi của URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

                // Khai báo các thuộc tính trong Properties block
            CBUFFER_START(UnityPerMaterial)
            float4 _TopColor;
            float4 _BottomColor;
            float _RimBrightness;
            float _RimHardness;
            CBUFFER_END

            TEXTURE2D(_ToonRamp);
            SAMPLER(sampler_ToonRamp);

                // Dữ liệu truyền từ Vertex Shader sang Fragment Shader
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float normalizedYPos : TEXCOORD3;
            };

                // Hàm tính toán Fresnel/Rim Light
            float CalculateRim(float3 normalWS, float3 viewDirWS, float hardness)
            {
                float dotProduct = 1.0 - saturate(dot(viewDirWS, normalWS));
                return smoothstep(1.0 - hardness, 1.0, dotProduct);
            }

                // Vertex Shader: Xử lý thông tin cho từng đỉnh
            Varyings Vertex(Attributes IN)
            {
                Varyings OUT;

                    // Chuyển đổi vị trí và normal từ không gian object sang không gian thế giới (world space)
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);

                    // Chuyển đổi vị trí sang không gian clip (cần thiết cho việc render)
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);

                    // Lấy hướng nhìn từ camera
                OUT.viewDirWS = normalize(_WorldSpaceCameraPos.xyz - OUT.positionWS);

                    // Tính toán vị trí Y đã được chuẩn hóa (0-1) dựa trên
                    // vị trí local của object để tạo gradient.
                    // Giả định pivot của object ở trung tâm đáy (0,0,0)
                OUT.normalizedYPos = saturate(IN.positionOS.y + 0.5);

                return OUT;
            }

                // Fragment Shader: Xử lý màu sắc cho từng pixel
            half4 Fragment(Varyings IN) : SV_Target
            {
                    // Chuẩn hóa lại các vector để đảm bảo tính chính xác
                float3 normalizedNormalWS = normalize(IN.normalWS);
                float3 normalizedViewDirWS = normalize(IN.viewDirWS);

                    // --- Toon Shading ---
                Light mainLight = GetMainLight();
                float dotNL = dot(normalizedNormalWS, mainLight.direction);
                float lightIntensity = saturate(dotNL * 0.5 + 0.5);
                float3 toonRamp = SAMPLE_TEXTURE2D(_ToonRamp, sampler_ToonRamp, float2(lightIntensity, lightIntensity)).rgb;
                float3 lighting = toonRamp * mainLight.color;

                    // --- Albedo & Gradient ---
                float3 albedo = lerp(_BottomColor.rgb, _TopColor.rgb, IN.normalizedYPos);

                    // --- Rim Light / Emission ---
                float softRim = CalculateRim(normalizedNormalWS, normalizedViewDirWS, 1.0);
                float hardRim = CalculateRim(normalizedNormalWS, normalizedViewDirWS, 0.01);
                float rimMix = lerp(hardRim, softRim, _RimHardness);
                float3 emission = _TopColor.rgb * rimMix * _RimBrightness * IN.normalizedYPos;

                    // --- Kết hợp màu cuối cùng ---
                float3 finalColor = (albedo * lighting) + emission;

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
