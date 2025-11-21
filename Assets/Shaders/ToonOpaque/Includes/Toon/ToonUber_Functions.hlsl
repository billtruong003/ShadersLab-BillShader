#ifndef BILLS_TOON_FUNCTIONS_INCLUDED
#define BILLS_TOON_FUNCTIONS_INCLUDED

#include "../../../Others/MathUtils_Core.hlsl"

#if defined(_OUTLINEGLINT_ON)
float CalculateGlintFactor(float3 worldPos)
{
    float2 noiseUV = worldPos.xy * _GlintScale * 0.1;
    half spatialNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
    float timeStep = floor(_Time.y * _GlintSpeed * 10.0);
    half temporalNoise = MU_Hash31(float3(worldPos.xy * 0.1, timeStep));
    half combinedNoise = spatialNoise * temporalNoise;
    return smoothstep(_GlintThreshold, _GlintThreshold + 0.05, combinedNoise);
}
#endif

float3 ApplyConfigurableToonRamp(float NdotL, float3 lightColor, float3 shadowTint, float3 midtoneColor, float shadowThreshold, float midtoneThreshold, float smoothness)
{
    // Logic Fix: Saturate NdotL to avoid ramp errors
    NdotL = saturate(NdotL);
    
    half3 rampedLight;
    #if defined(_TOON_STYLE_HARD)
        half shadowFactor = step(shadowThreshold, NdotL);
        half midtoneFactor = step(midtoneThreshold, NdotL);
        rampedLight = lerp(shadowTint, midtoneColor, shadowFactor);
        rampedLight = lerp(rampedLight, lightColor, midtoneFactor);
    #else
        half shadowFactor = smoothstep(shadowThreshold, shadowThreshold + smoothness, NdotL);
        half midtoneFactor = smoothstep(midtoneThreshold, midtoneThreshold + smoothness, NdotL);
        rampedLight = lerp(shadowTint, midtoneColor, shadowFactor);
        rampedLight = lerp(rampedLight, lightColor, midtoneFactor);
    #endif
    return rampedLight;
}

// UPDATED: Added viewDir for Rim Light calculation
float3 CalculateToonLighting(float3 normalWS, float3 worldPos, Light mainLight, float3 viewDir)
{
    // 1. Main Light
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5; // Half-Lambert
    float3 mainLightRamp = ApplyConfigurableToonRamp(NdotL, mainLight.color, _ShadowTint.rgb, _MidtoneColor.rgb, _ShadowThreshold, _MidtoneThreshold, _ToonRampSmoothness);
    float3 mainLightContribution = mainLightRamp * mainLight.shadowAttenuation;

    // 2. Additional Lights
    float3 additionalLightContribution = 0.0h;
    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light additionalLight = GetAdditionalLight(i, worldPos);
            float addNdotL = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
            float3 addLightRamp = ApplyConfigurableToonRamp(addNdotL, additionalLight.color, _AddLightShadowTint.rgb, _AddLightMidtoneColor.rgb, _AddLightShadowThreshold, _AddLightMidtoneThreshold, _AddLightRampSmoothness);
            additionalLightContribution += addLightRamp * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
        }
    #endif
    
    float3 totalLighting = mainLightContribution + additionalLightContribution;

    // 3. New Rim Light Logic
    #if defined(_RIMLIGHT_ON)
        float NdotV = 1.0 - saturate(dot(normalWS, viewDir));
        // Use standard Fresnel power
        float rimIntensity = pow(NdotV, _RimPower);
        // Smoothstep for a cleaner Toon Rim edge
        rimIntensity = smoothstep(0.5, 0.8, rimIntensity); 
        
        float3 rim = rimIntensity * _RimColor.rgb;
        // Optional: Mask rim by main light shadow to avoid glowing in occlusion
        // rim *= mainLight.shadowAttenuation; 
        
        totalLighting += rim;
    #endif

    return min(totalLighting, _MaxBrightness);
}

float3 CalculateMetallicLighting(float3 normalWS, float3 viewDir, Light mainLight)
{
    float3 halfVec = SafeNormalize(viewDir + mainLight.direction);
    float NdotH = saturate(dot(normalWS, halfVec));
    float NdotL = saturate(dot(normalWS, mainLight.direction));
    float NdotV = saturate(dot(normalWS, viewDir));

    // Logic Fix: Clamp UV to prevent texture wrap artifacts
    half3 rampColor = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, half2(NdotL, 0.5h)).rgb;
    
    half specularRamp = smoothstep(_Offset, _Offset + 0.05, NdotH);
    half highlightRamp = smoothstep(_HighlightOffset, _HighlightOffset + 0.05, NdotH);

    half3 specular = specularRamp * _SpecuColor.rgb;
    half3 highlight = highlightRamp * _HiColor.rgb;
    
    float3 rim = pow(1.0h - NdotV, _RimPower) * _RimColor.rgb;

    float3 lighting = (rampColor + specular + highlight) * _Brightness * mainLight.color * mainLight.shadowAttenuation;
    lighting += rim;

    return min(lighting, _MaxBrightness);
}

float3 CalculateFoliageLighting(float3 normalWS, float3 worldPos, Light mainLight)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float3 lambert = mainLight.color * NdotL;

    float3 backLightDir = -mainLight.direction;
    // Logic Fix: Saturate back lighting
    float backNdotL = saturate(dot(normalWS, backLightDir) * 0.5 + 0.5);
    float3 translucency = pow(backNdotL, 2) * mainLight.color * _TranslucencyStrength * _TranslucencyColor;
    float3 totalLight = (lambert + translucency) * mainLight.shadowAttenuation;

    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light additionalLight = GetAdditionalLight(i, worldPos);
            float addNdotL = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
            totalLight += additionalLight.color * addNdotL * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
        }
    #endif

    return min(totalLight, _MaxBrightness);
}

void ApplyWind(inout float3 positionOS, float4 vertexColor)
{
    float3 worldPos = TransformObjectToWorld(positionOS);
    
    float camDist = distance(worldPos, _WorldSpaceCameraPos);
    float distFade = 1.0h - saturate((camDist - _WindFadeStart) / max(0.001h, _WindFadeEnd - _WindFadeStart));

    float2 noiseUV = worldPos.xz * _WindNoiseScale * 0.1h;
    float2 windFlow = _WindDirection.xy * _Time.y * _WindSpeed * 0.1h;
    
    half noiseA = SAMPLE_TEXTURE2D_LOD(_WindNoiseTex, sampler_WindNoiseTex, noiseUV + windFlow, 0).r;
    half noiseB = SAMPLE_TEXTURE2D_LOD(_WindNoiseTex, sampler_WindNoiseTex, noiseUV * 0.4h - windFlow * 0.6h, 0).r;
    half finalNoise = (noiseA + noiseB) * 0.5h;

    half displacement = (finalNoise * 2.0h - 1.0h);
    float3 windOffset = _WindDirection.xyz * displacement * _WindAmplitude * vertexColor.a * distFade;

    positionOS.xyz += windOffset;
}

float3 CalculateBlingLighting(float3 baseColor, float3 normalWS, float3 worldPos, Light mainLight, float3 viewDirWS, float4 positionCS)
{
    // Re-use Toon Calculation (without rim enabled)
    float3 baseLighting = CalculateToonLighting(normalWS, worldPos, mainLight, viewDirWS);
    float3 shadedColor = baseColor * baseLighting;

    float2 noiseUV;
    #if defined(_BLING_WORLDSPACE_ON)
        noiseUV = worldPos.xy * _BlingScale * 0.1h;
    #else
        noiseUV = (positionCS.xy / positionCS.w) * _BlingScale;
        noiseUV.x *= _ScreenParams.x / _ScreenParams.y;
    #endif
    
    half spatialNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
    float timeStep = floor(_Time.y * _BlingSpeed * 10.0);
    half temporalNoise = MU_Hash31(float3(worldPos.xy * 0.1, timeStep));
    half combinedNoise = spatialNoise * temporalNoise;
    
    float3 halfVec = SafeNormalize(viewDirWS + mainLight.direction);
    float NdotH = saturate(dot(normalWS, halfVec));
    half specularFactor = pow(NdotH, 32.0h); 
    half NdotV = 1.0h - saturate(dot(normalWS, viewDirWS));
    half fresnelFactor = pow(NdotV, _BlingFresnelPower);
    
    half sparkleMask = smoothstep(_BlingThreshold, _BlingThreshold + 0.05h, combinedNoise);
    half finalSparkleStrength = sparkleMask * saturate(specularFactor + fresnelFactor);
    
    half3 bling = finalSparkleStrength * _BlingColor.rgb * _BlingIntensity * mainLight.color;
    bling *= mainLight.shadowAttenuation;
    return shadedColor + bling;
}

#endif