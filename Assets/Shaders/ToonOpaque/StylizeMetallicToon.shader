Shader "Custom/Toon/StylizedMetal_URP"
{
    Properties
    {
        [Header(Base)]
        [MainColor] _BaseColor("Base Color (Shadow)", Color) = (0.8, 0.4, 0.15, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {} // Added for compatibility
        
        [Header(Metal Specular)]
        _SpecuColor("Specular Color (Lit)", Color) = (0.8, 0.45, 0.2, 1)
        _Brightness("Specular Brightness", Range(0, 2)) = 1.3
        _Offset("Specular Size Threshold", Range(0, 1)) = 0.8
        
        [Header(Highlight)]
        _HiColor("Highlight Color", Color) = (1, 1, 1, 1)
        _HighlightOffset("Highlight Size Threshold", Range(0, 1)) = 0.9
        
        [Header(Rim)]
        _RimColor("Rim Color", Color) = (1, 0.3, 0.3, 1)
        _RimPower("Rim Power", Range(0, 20)) = 6
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _SpecuColor;
                half4 _HiColor;
                half4 _RimColor;
                float _Brightness;
                float _Offset;
                float _HighlightOffset;
                float _RimPower;
            CBUFFER_END
            
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half3 CalculateMetalLighting(Light light, float3 normalWS, float3 viewDirWS, half3 baseColor, float shadowAtten)
            {
                // Blinn-Phong Half Vector
                float3 halfVector = normalize(viewDirWS + light.direction);
                
                // Stylized Metal Logic
                float specDot = dot(halfVector, normalWS);
                float cutOff = step(specDot, _Offset); // 1 if Base, 0 if Specular
                
                half3 metalBase = baseColor * cutOff;
                half3 metalSpec = _SpecuColor.rgb * (1.0 - cutOff) * _Brightness;
                
                // Highlight Logic
                float NdotL = saturate(dot(light.direction, normalWS));
                float highlightVal = step(_HighlightOffset, NdotL);
                half3 highlightAlbedo = highlightVal * _HiColor.rgb;

                // Combine and apply shadow attenuation
                half3 mixedColor = (metalBase + metalSpec + highlightAlbedo) * light.color * shadowAtten;
                return mixedColor;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                
                // Base texture maps to BaseColor (shadow area of metal)
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 baseAlbedo = _BaseColor.rgb * texColor.rgb;

                // Main Light
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                half3 finalColor = CalculateMetalLighting(mainLight, normalWS, viewDirWS, baseAlbedo, mainLight.shadowAttenuation * mainLight.distanceAttenuation);

                // Additional Lights
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < pixelLightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, input.positionWS);
                    finalColor += CalculateMetalLighting(light, normalWS, viewDirWS, baseAlbedo, light.shadowAttenuation * light.distanceAttenuation);
                }

                // Rim Light
                half rim = 1.0 - saturate(dot(viewDirWS, normalWS));
                half3 rimEmission = _RimColor.rgb * pow(rim, _RimPower);

                finalColor += rimEmission;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // ShadowCaster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            
            Varyings ShadowPassVertex(Attributes input) {
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, float3(0,0,0)));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }
            half4 ShadowPassFragment(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        // DepthNormals for Outline
        Pass { Name "DepthNormals" Tags { "LightMode" = "DepthNormals" } ZWrite On HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            Varyings DepthNormalsVertex(Attributes input) {
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz); output.normalWS = TransformObjectToWorldNormal(input.normalOS); return output;
            }
            float4 DepthNormalsFragment(Varyings input) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(input); return float4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }

        // DepthOnly
        Pass { Name "DepthOnly" Tags { "LightMode" = "DepthOnly" } ZWrite On ColorMask 0 HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            Varyings DepthOnlyVertex(Attributes input) {
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz); return output;
            }
            half4 DepthOnlyFragment(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        // Basic Meta Pass for GI (returns base color)
        Pass { Name "Meta" Tags { "LightMode" = "Meta" } Cull Off HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex MetaVert
            #pragma fragment MetaFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float2 lightmapUV : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            CBUFFER_START(UnityPerMaterial) float4 _BaseMap_ST; half4 _BaseColor; CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            Varyings MetaVert(Attributes input) {
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = MetaVertexPosition(input.positionOS, input.lightmapUV, input.uv, unity_LightmapST, unity_DynamicLightmapST); output.uv = TRANSFORM_TEX(input.uv, _BaseMap); return output;
            }
            half4 MetaFrag(Varyings input) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                MetaInput metaInput; metaInput.Albedo = baseColor.rgb; metaInput.Emission = 0;
                return MetaFragment(metaInput);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}