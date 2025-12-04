Shader "Stylized/PortalFX_URP"
{
    Properties
    {
        [Header(General)]
        [MainTexture] _NoiseTex ("Noise Texture", 2D) = "white"{}
        [HDR] _ColorCore ("Core Color", Color) = (0, 1, 1, 1)
        [HDR] _ColorEdge ("Edge Color", Color) = (0, 0.2, 1, 1)

        [Header(Motion)]
        _TwistStrength ("Twist Strength", Float) = 5.0
        _RotationSpeed ("Rotation Speed", Float) = 1.0
        _ScrollSpeed ("Scroll Speed", Float) = 0.5

        [Header(Shape)]
        _Parallax ("Parallax Depth", Range(0, 0.5)) = 0.1
        _EdgeFade ("Circle Mask Softness", Range(0.1, 2.0)) = 1.0
        _CoreSize ("Core Size", Range(0, 1)) = 0.2

        [Header(Blending)]
        _SoftParticleFactor ("Soft Particle Strength", Range(0, 10)) = 1.0
        _Opacity ("Master Opacity", Range(0, 1)) = 1.0
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
            Name "PortalFX"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 viewDirOS : TEXCOORD3;
            };

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _NoiseTex_ST;
            float4 _ColorCore;
            float4 _ColorEdge;
            float _TwistStrength;
            float _RotationSpeed;
            float _ScrollSpeed;
            float _Parallax;
            float _EdgeFade;
            float _CoreSize;
            float _SoftParticleFactor;
            float _Opacity;
            CBUFFER_END

            float2 RotateUV(float2 uv, float rotation)
            {
                float s = sin(rotation);
                float c = cos(rotation);
                return float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = vertexInput.positionCS;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.uv = input.uv;

                float3 cameraPosOS = TransformWorldToObject(GetCameraPositionWS());
                output.viewDirOS = normalize(cameraPosOS - input.positionOS.xyz);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centeredUV = input.uv - 0.5;
                float dist = length(centeredUV);

                float2 parallaxOffset = input.viewDirOS.xy * _Parallax * (1.0 - dist);
                float2 uv = centeredUV + parallaxOffset;
                float newDist = length(uv);

                float angle = atan2(uv.y, uv.x);
                float twist = _TwistStrength / (newDist + 0.1);
                float curAngle = angle + twist;

                float2 polarUV;
                polarUV.x = curAngle / (2.0 * PI);
                polarUV.y = newDist;

                float time = _Time.y;
                float2 uvLayer1 = polarUV * _NoiseTex_ST.xy + float2(time * _RotationSpeed, time * -_ScrollSpeed);
                float2 uvLayer2 = polarUV * (_NoiseTex_ST.xy * 0.7) + float2(time * -_RotationSpeed * 0.5, time * -_ScrollSpeed * 1.2);

                half noise1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uvLayer1).r;
                half noise2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uvLayer2).r;

                half combinedNoise = noise1 * noise2 * 2.0;

                float edgeMask = smoothstep(0.5, 0.5 - _EdgeFade * 0.1, newDist);
                float coreMask = smoothstep(_CoreSize, 0.0, newDist);

                half4 color = lerp(_ColorEdge, _ColorCore, combinedNoise + coreMask);

                float sceneZ = SampleSceneDepth(input.screenPos.xy / input.screenPos.w);
                float bufferZ = LinearEyeDepth(sceneZ, _ZBufferParams);
                float partZ = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float softFactor = saturate((bufferZ - partZ) * _SoftParticleFactor);

                color.a *= edgeMask * softFactor * _Opacity;
                color.rgb *= color.a;

                return color;
            }
            ENDHLSL
        }
    }
}
