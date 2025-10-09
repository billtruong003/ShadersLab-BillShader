// File: Assets/FANTASTIC - Village Pack/BrushHit/Script/Shader/InteractiveDisplacement.shader
Shader "CleanCode/InteractiveGrassCollectible_V19.0_AlphaThreshold"
{
    Properties
    {
        [Header(Toon Shading)]
        _LitColorTint("Lit Color Tint (Unlit Additive)", Color) = (0.1, 0.1, 0.1, 1)
        _HighlightColor("Highlight Color", Color) = (1,1,1,1)
        _MidToneColor("Mid-tone Color", Color) = (0.5,0.5,0.5,1)
        _ShadowColor("Shadow Color", Color) = (0.0, 0.0, 0.0,1)
        _ShadowStep("Shadow Step", Range(0, 1)) = 0.4
        _HighlightStep("Highlight Step", Range(0, 1)) = 0.8
        _StepSmoothness("Step Smoothness", Range(0.001, 0.2)) = 0.05
        
        [Header(Idle Animation)]
        _IdleSwayFrequency("Idle Sway Frequency", Range(0, 10)) = 1.0
        _IdleSwayAmplitude("Idle Sway Amplitude", Range(0, 0.5)) = 0.1

        [Header(Alpha Occlusion)]
        // THAY ĐỔI MỚI 1: Thêm thuộc tính ngưỡng Alpha tối thiểu
        _MinFadeAlpha("Minimum Fade Alpha", Range(0, 1)) = 0.5
        // THAY ĐỔI MỚI 2: Tăng giới hạn của Fade Softness
        _FadeSoftness("Fade Softness", Range(0.01, 5.0)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma target 4.5
            
            #pragma multi_compile_instancing
            #pragma multi_compile_fwdbase

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct VertexInput
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexToFragment
            {
                float4 positionCS             : SV_POSITION;
                float3 worldNormal            : TEXCOORD0;
                float3 originalWorldPos       : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };
            
            UNITY_INSTANCING_BUFFER_START(PerInstanceProps)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InteractionColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _InteractionProgress)
                UNITY_DEFINE_INSTANCED_PROP(float, _UniqueID)
            UNITY_INSTANCING_BUFFER_END(PerInstanceProps)

            CBUFFER_START(UnityPerMaterial)
                half4 _HighlightColor, _MidToneColor, _ShadowColor, _LitColorTint;
                float _ShadowStep, _HighlightStep, _StepSmoothness;
                float _IdleSwayFrequency, _IdleSwayAmplitude;
                float _FadeSoftness;
                // THAY ĐỔI MỚI 3: Thêm biến vào CBUFFER để HLSL có thể truy cập
                float _MinFadeAlpha;
            CBUFFER_END
            
            float4 _GlobalInteractorPosition;
            float3 _GlobalInteractorBounds;
            float _GlobalDisplacementStrength;
            float _GlobalMaxInteractionDistance;

            float3 ApplyIdleSway(float3 positionOS, float uniqueID)
            {
                float time = _Time.y * _IdleSwayFrequency + uniqueID * 0.5;
                float swayX = sin(time) * _IdleSwayAmplitude;
                float swayZ = cos(time * 0.7) * _IdleSwayAmplitude;
                return float3(swayX, 0, swayZ) * positionOS.y;
            }
            
            float3 CalculateContinuousPush(float3 vertexWorldPos)
            {
                float3 diff = vertexWorldPos - _GlobalInteractorPosition.xyz;
                float3 halfBounds = _GlobalInteractorBounds * 0.5;
                float3 q = abs(diff) - halfBounds;
                float distanceToBox = length(max(q, 0.0));
                float attenuation = 1.0 - saturate(distanceToBox / max(_GlobalMaxInteractionDistance, 1e-5));
                attenuation = pow(attenuation, 2);

                if (attenuation < 0.01) return float3(0,0,0);

                float3 pushDirection = normalize(float3(diff.x, 0, diff.z) + 1e-5);
                
                return pushDirection * attenuation * _GlobalDisplacementStrength;
            }

            half3 CalculateToonLighting(float3 worldNormal, Light mainLight)
            {
                float NdotL = saturate(dot(worldNormal, mainLight.direction));
                float shadow = 1.0 - smoothstep(_ShadowStep - _StepSmoothness, _ShadowStep + _StepSmoothness, NdotL);
                float highlight = smoothstep(_HighlightStep - _StepSmoothness, _HighlightStep + _StepSmoothness, NdotL);
                
                half3 toonLight = lerp(_MidToneColor.rgb, _ShadowColor.rgb, shadow);
                toonLight = lerp(toonLight, _HighlightColor.rgb, highlight);
                
                half3 ambient = SampleSH(worldNormal);
                return (toonLight * mainLight.color + ambient);
            }

            // Hàm này vẫn trả về giá trị từ 0 đến 1
            float CalculateOcclusionFactor(float3 worldPos)
            {
                float3 diff = worldPos - _GlobalInteractorPosition.xyz;
                float3 distanceToFaces = abs(diff) - _GlobalInteractorBounds;
                float sdf = length(max(distanceToFaces, 0.0)) + min(max(distanceToFaces.x, max(distanceToFaces.y, distanceToFaces.z)), 0.0);
                return saturate(sdf / max(_FadeSoftness, 0.01));
            }


            VertexToFragment vert(VertexInput IN)
            {
                VertexToFragment OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                
                float uniqueID = UNITY_ACCESS_INSTANCED_PROP(PerInstanceProps, _UniqueID);
                
                float3 positionOS = IN.positionOS.xyz;
                positionOS += ApplyIdleSway(positionOS, uniqueID);
                
                float3 worldPos = TransformObjectToWorld(positionOS);
                OUT.originalWorldPos = worldPos;
                
                float3 displacement = CalculateContinuousPush(worldPos);
                float vertexHeightFactor = saturate(IN.positionOS.y);
                worldPos += displacement * vertexHeightFactor;
                
                OUT.positionCS = TransformWorldToHClip(worldPos);
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                
                return OUT;
            }
            
            half4 frag(VertexToFragment IN, float facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                
                // THAY ĐỔI MỚI 4: Logic tính toán alpha cuối cùng
                // 1. Lấy hệ số fade gốc (0 -> 1)
                float occlusionFactor = CalculateOcclusionFactor(IN.originalWorldPos);
                
                // 2. Ánh xạ lại hệ số đó vào khoảng [_MinFadeAlpha, 1.0]
                float finalOcclusionAlpha = lerp(_MinFadeAlpha, 1.0, occlusionFactor);
                
                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(PerInstanceProps, _BaseColor);
                float4 interactionColor = UNITY_ACCESS_INSTANCED_PROP(PerInstanceProps, _InteractionColor);
                float progress = UNITY_ACCESS_INSTANCED_PROP(PerInstanceProps, _InteractionProgress);
                
                float3 worldNormal = normalize(IN.worldNormal) * (facing > 0 ? 1 : -1);
                
                Light mainLight = GetMainLight();
                half3 lighting = CalculateToonLighting(worldNormal, mainLight);
                
                half4 finalColor = lerp(baseColor, interactionColor, progress);
                
                half3 litColor = finalColor.rgb * lighting;
                half3 finalComposite = litColor + (_LitColorTint.rgb * finalColor.rgb);
                
                // Gán giá trị alpha đã được điều chỉnh vào kết quả cuối cùng
                return float4(finalComposite, finalColor.a * finalOcclusionAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}