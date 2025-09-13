#ifndef BILLS_TOON_TIERED_EFFECTS_HQ_INCLUDED
#define BILLS_TOON_TIERED_EFFECTS_HQ_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "../../Others/MathUtils.hlsl" 

half3 CalculateRarePulsingGlow(float3 normalWS, float3 viewDir)
{
    half fresnel = 1.0h - saturate(dot(normalWS, viewDir));
    fresnel = pow(fresnel, 2.0h);
    
    half pulse = (sin(_Time.y * (float)_RareFloat1) * 0.5h + 0.5h) * _RareFloat2;
    return _RareColor1.rgb * fresnel * pulse;
}

half3 CalculateRareSparkles(float3 worldPos)
{
    float2 noiseUV = worldPos.xy * (float)_RareFloat1;
    noiseUV.x += _Time.y * (float)_RareFloat2 * 0.2h;
    noiseUV.y -= _Time.y * (float)_RareFloat2 * 0.1h;
    
    float sparkleNoise = MU_Hash21(noiseUV);
    half sparkle = step(0.995h, sparkleNoise);
    
    return _RareColor1.rgb * sparkle;
}

half3 CalculateEpicFireAura(float3 normalWS, float3 viewDir, float3 worldPos)
{
    half fresnel = 1.0h - saturate(dot(normalWS, viewDir));
    fresnel = pow(fresnel, (float)_EpicFloat3);

    float2 noiseUV = worldPos.xy * (float)_EpicFloat2;
    noiseUV.y += _Time.y * (float)_EpicFloat1;
    half noise1 = SAMPLE_TEXTURE2D(_EffectTex1, sampler_EffectTex1, noiseUV).r;
    
    float2 noiseUV2 = worldPos.xz * (float)_EpicFloat2 * 0.7h;
    noiseUV2.x -= _Time.y * (float)_EpicFloat1 * 0.8h;
    half noise2 = SAMPLE_TEXTURE2D(_EffectTex1, sampler_EffectTex1, noiseUV2).r;
    
    half combinedNoise = saturate(pow(noise1 * noise2, 2.0h));
    
    half3 fireColor = lerp(_EpicColor1.rgb, _EpicColor2.rgb, combinedNoise);
    
    return fireColor * fresnel * _EpicColor1.a;
}

half3 CalculateEpicElectricField(Varyings i)
{
    float jitter = (MU_SimplexNoise(i.positionCS.xy * 0.5 + _Time.y * 20.0h) - 0.5h) * _EpicFloat3 * 0.1h;
    
    float2 electricUV = i.uv * _EpicFloat2;
    electricUV.x += _Time.y * _EpicFloat1 + jitter;
    
    half linePattern = SAMPLE_TEXTURE2D(_EffectTex1, sampler_EffectTex1, electricUV).r;
    
    float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);
    half fresnel = 1.0h - saturate(dot(i.normalWS, viewDir));
    fresnel = pow(fresnel, 3.0h);
    
    return _EpicColor1.rgb * linePattern * fresnel;
}

half3 CalculateLegendaryCosmicRift(Varyings i, float3 viewDir)
{
    float2 noiseUV = i.uv * _LegendaryFloat2;
    noiseUV.y += _Time.y * _LegendaryFloat1;
    half nebula = SAMPLE_TEXTURE2D(_EffectTex1, sampler_EffectTex1, noiseUV).r;
    
    half fresnel = 1.0h - saturate(dot(i.normalWS, viewDir));
    fresnel = pow(fresnel, 2.0h);
    
    half riftMask = smoothstep(0.1h, 0.8h, fresnel);
    
    half3 nebulaColor = _LegendaryColor1.rgb * nebula;
    
    half riftEdge = 1.0h - smoothstep(0.75h, 0.8h, fresnel);
    riftEdge *= smoothstep(0.8h, 0.85h, fresnel);
    
    half3 finalColor = lerp(0, nebulaColor, riftMask);
    finalColor = lerp(finalColor, _LegendaryColor2.rgb, riftEdge * _LegendaryFloat3);
    
    return finalColor;
}

half3 CalculateLegendaryHolyAura(float3 normalWS, float3 viewDir)
{
    half fresnel = 1.0h - saturate(dot(normalWS, viewDir));
    fresnel = pow(fresnel, (float)_LegendaryFloat3);
    
    float sineWave = sin(fresnel * _LegendaryFloat2 - _Time.y * _LegendaryFloat1) * 0.5 + 0.5;
    sineWave = pow(sineWave, 4.0h);
    
    half3 aura = _LegendaryColor1.rgb * fresnel;
    aura += _LegendaryColor1.rgb * sineWave * 0.5h;
    
    return aura;
}

half3 CalculateTieredEffect(Varyings i)
{
    half3 effectEmission = 0.0h;
    float3 viewDir = SafeNormalize(_WorldSpaceCameraPos.xyz - i.positionWS);

    #if defined(_EFFECT_TIER_RARE)
        #if defined(_RARE_EFFECT_PULSING_GLOW)
            effectEmission = CalculateRarePulsingGlow(i.normalWS, viewDir);
        #elif defined(_RARE_EFFECT_SPARKLES)
            effectEmission = CalculateRareSparkles(i.positionWS);
        #endif
    #elif defined(_EFFECT_TIER_EPIC)
        #if defined(_EPIC_EFFECT_FIRE_AURA)
            effectEmission = CalculateEpicFireAura(i.normalWS, viewDir, i.positionWS);
        #elif defined(_EPIC_EFFECT_ELECTRIC_FIELD)
            effectEmission = CalculateEpicElectricField(i);
        #endif
    #elif defined(_EFFECT_TIER_LEGENDARY)
        #if defined(_LEGENDARY_EFFECT_COSMIC_RIFT)
            effectEmission = CalculateLegendaryCosmicRift(i, viewDir);
        #elif defined(_LEGENDARY_EFFECT_HOLY_AURA)
            effectEmission = CalculateLegendaryHolyAura(i.normalWS, viewDir);
        #endif
    #endif

    return effectEmission;
}

#endif