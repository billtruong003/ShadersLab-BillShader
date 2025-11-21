#ifndef BILLS_TOON_FUNCTIONS_HQ_INCLUDED
#define BILLS_TOON_FUNCTIONS_HQ_INCLUDED

#include "../../Others/MathUtils.hlsl"

#if defined(_OUTLINEGLINT_ON)
float CalculateGlintFactor(float3 worldPos)
{
    float noiseTime = _Time.y * _GlintSpeed;
    float2 noiseUV = worldPos.xy * _GlintScale * 0.1;
    noiseUV.y += noiseTime;
    float noise = MU_SimplexNoise(noiseUV) * 0.5 + 0.5;
    return smoothstep(_GlintThreshold, _GlintThreshold + 0.05, noise);
}
#endif

half TriplanarSample(TEXTURE2D_PARAM(tex, samp), float3 position, float3 normal, float scale, float sharpness)
{
    float2 uvX = position.yz * scale;
    float2 uvY = position.xz * scale;
    float2 uvZ = position.xy * scale;
    half sampleX = SAMPLE_TEXTURE2D_LOD(tex, samp, uvX, 0).r;
    half sampleY = SAMPLE_TEXTURE2D_LOD(tex, samp, uvY, 0).r;
    half sampleZ = SAMPLE_TEXTURE2D_LOD(tex, samp, uvZ, 0).r;
    float3 blendWeights = pow(abs(normal), sharpness);
    blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);
    return sampleX * blendWeights.x + sampleY * blendWeights.y + sampleZ * blendWeights.z;
}

float3 ApplyThreeStepToonRamp(float NdotL, float3 lightColor)
{
    half shadowRamp = smoothstep(_ShadowThreshold - _RampSmoothness, _ShadowThreshold + _RampSmoothness, NdotL);
    half3 colorAfterShadow = lerp(_ShadowTint.rgb, _MidtoneColor.rgb, shadowRamp);
    half midtoneRamp = smoothstep(_MidtoneThreshold - _RampSmoothness, _MidtoneThreshold + _RampSmoothness, NdotL);
    return lerp(colorAfterShadow, lightColor, midtoneRamp);
}

float3 ApplyAdditionalLightToonRamp(float NdotL, float3 lightColor)
{
    half shadowRamp = smoothstep(_AddLightShadowThreshold - _AddLightRampSmoothness, _AddLightShadowThreshold + _AddLightRampSmoothness, NdotL);
    half3 colorAfterShadow = lerp(_AddLightShadowTint.rgb, _AddLightMidtoneColor.rgb, shadowRamp);
    half midtoneRamp = smoothstep(_AddLightMidtoneThreshold - _AddLightRampSmoothness, _AddLightMidtoneThreshold + _AddLightRampSmoothness, NdotL);
    return lerp(colorAfterShadow, lightColor, midtoneRamp);
}

float3 CalculateToonLighting(float3 normalWS, float3 worldPos, Light mainLight)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float3 mainLightContribution = ApplyThreeStepToonRamp(NdotL, mainLight.color) * mainLight.shadowAttenuation;
    float3 additionalLightContribution = 0.0h;
    #ifdef _ADDITIONAL_LIGHTS
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light additionalLight = GetAdditionalLight(i, worldPos);
            float addNdotL = dot(normalWS, additionalLight.direction) * 0.5 + 0.5;
            additionalLightContribution += ApplyAdditionalLightToonRamp(addNdotL, additionalLight.color) * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
        }
    #endif
    return mainLightContribution + additionalLightContribution;
}

float3 CalculateStylizedSurfaceLighting(float3 normalWS, float3 viewDir, Light mainLight)
{
    float3 halfVec = SafeNormalize(viewDir + mainLight.direction);
    float NdotH = saturate(dot(normalWS, halfVec));
    float NdotL = saturate(dot(normalWS, mainLight.direction));
    half3 rampColor = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, half2(NdotL, 0.5h)).rgb;
    half specularRamp = MU_FastSmoothstep(_Offset, _Offset + 0.05, NdotH);
    half highlightRamp = MU_FastSmoothstep(_HighlightOffset, _HighlightOffset + 0.05, NdotH);
    half3 specular = specularRamp * _SpecuColor.rgb;
    half3 highlight = highlightRamp * _HiColor.rgb;
    float3 lighting = (rampColor + specular + highlight) * _Brightness * mainLight.color * mainLight.shadowAttenuation;
    return lighting;
}

float3 CalculateStylizedSpecular(float3 normalWS, float3 viewDir, Light mainLight)
{
    float3 halfVec = SafeNormalize(viewDir + mainLight.direction);
    float NdotH = saturate(dot(normalWS, halfVec));
    float specSize = 1.0 - _Offset; 
    float specSmooth = _SpecularSmoothness;
    float specIntensity = smoothstep(specSize, specSize + specSmooth, NdotH);
    return _SpecuColor.rgb * specIntensity * _Brightness * mainLight.shadowAttenuation;
}

float3 CalculateMetallicLighting(float3 normalWS, float3 viewDir, Light mainLight)
{
    return CalculateStylizedSurfaceLighting(normalWS, viewDir, mainLight);
}

float3 CalculateCosmicLighting(float3 normalWS, float3 viewDir, float3 positionWS, Light mainLight)
{
    float3 surfaceLighting = CalculateStylizedSurfaceLighting(normalWS, viewDir, mainLight);
    float3 parallaxOffset1 = viewDir.xyz * _CosmicParallaxDepth1;
    float3 parallaxOffset2 = viewDir.xyz * _CosmicParallaxDepth2;
    float3 parallaxOffset3 = viewDir.xyz * _StarfieldParallaxDepth;
    float3 pos1 = positionWS + parallaxOffset1 + _Time.y * _CosmicScrollSpeed1;
    float3 pos2 = positionWS + parallaxOffset2 + _Time.y * _CosmicScrollSpeed2;
    float3 pos3 = positionWS + parallaxOffset3 + _Time.y * _StarfieldScrollSpeed;
    half cosmicLayer1 = TriplanarSample(TEXTURE2D_ARGS(_CosmicTex1, sampler_CosmicTex1), pos1, normalWS, _CosmicScale1, _TriplanarSharpness);
    half cosmicLayer2 = TriplanarSample(TEXTURE2D_ARGS(_CosmicTex2, sampler_CosmicTex2), pos2, normalWS, _CosmicScale2, _TriplanarSharpness);
    half starfieldLayer = TriplanarSample(TEXTURE2D_ARGS(_StarfieldTex, sampler_StarfieldTex), pos3, normalWS, _StarfieldScale, _TriplanarSharpness);
    half3 cosmicColor = cosmicLayer1 * _CosmicColor1.rgb + cosmicLayer2 * _CosmicColor2.rgb + starfieldLayer * _StarfieldColor.rgb;
    return surfaceLighting + cosmicColor;
}

float3 CalculateFoliageLighting(float3 normalWS, float3 worldPos, Light mainLight)
{
    float NdotL = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
    float3 lambert = mainLight.color * NdotL;
    float3 backLightDir = -mainLight.direction;
    float backNdotL = dot(normalWS, backLightDir) * 0.5 + 0.5;
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
    return totalLight;
}

void ApplyWind(inout float3 positionOS, float4 vertexColor)
{
    float3 worldPos = TransformObjectToWorld(positionOS);
    float windPhase = dot(worldPos.xz, float2(0.2, 0.1));
    float windSine = MU_FastSin(_Time.y * _WindFrequency + windPhase);
    float3 windVector = normalize(_WindDirection) * windSine * _WindAmplitude;
    float windMask = vertexColor.a;
    positionOS.xyz += windVector * windMask;
}

float3 ApplyRimLight(float3 surfaceColor, float3 normalWS, float3 viewDir)
{
    #if defined(_RIMLIGHT_ON)
        float NdotV = 1.0h - saturate(dot(normalWS, viewDir));
        float rimIntensity = pow(NdotV, _RimLightPower);
        rimIntensity = smoothstep(_RimLightSmoothness, 1.0h, rimIntensity);
        surfaceColor += _RimLightColor.rgb * rimIntensity;
    #endif
    return surfaceColor;
}

float3 ApplyDynamicTriplanarMask(float3 surfaceColor, float2 uv, float3 worldPos, float3 normalWS)
{
    #if defined(_MASK_TRIPLANAR_ON)
        float2 cellCoords = floor(uv * _MaskDivisions);
        int cellIndex = int(cellCoords.y * _MaskDivisions + cellCoords.x);
        cellIndex = clamp(cellIndex, 0, 15);
        half4 maskColor = _MaskColor0;
        if(cellIndex == 1) maskColor = _MaskColor1;
        else if(cellIndex == 2) maskColor = _MaskColor2;
        else if(cellIndex == 3) maskColor = _MaskColor3;
        else if(cellIndex == 4) maskColor = _MaskColor4;
        else if(cellIndex == 5) maskColor = _MaskColor5;
        else if(cellIndex == 6) maskColor = _MaskColor6;
        else if(cellIndex == 7) maskColor = _MaskColor7;
        else if(cellIndex == 8) maskColor = _MaskColor8;
        else if(cellIndex == 9) maskColor = _MaskColor9;
        else if(cellIndex == 10) maskColor = _MaskColor10;
        else if(cellIndex == 11) maskColor = _MaskColor11;
        else if(cellIndex == 12) maskColor = _MaskColor12;
        else if(cellIndex == 13) maskColor = _MaskColor13;
        else if(cellIndex == 14) maskColor = _MaskColor14;
        else if(cellIndex == 15) maskColor = _MaskColor15;
        half triplanarSample = TriplanarSample(TEXTURE2D_ARGS(_MaskTriplanarTex, sampler_MaskTriplanarTex), worldPos, normalWS, _MaskTriplanarScale, _MaskTriplanarSharpness);
        half3 tintedTriplanar = triplanarSample * maskColor.rgb;
        surfaceColor = lerp(surfaceColor, tintedTriplanar, maskColor.a * _MaskTriplanarBlend);
    #endif
    return surfaceColor;
}

#if defined(_DISSOLVE_ON)
float CalculateDissolveValue(float3 position, float3 normal, float2 uv, float perVertexNoise)
{
    float dissolveValue = 0.0h;
    #if defined(_DISSOLVETYPE_NOISE)
        dissolveValue = TriplanarSample(TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), position, normal, _NoiseScale, 2.0f);
    #elif defined(_DISSOLVETYPE_LINEAR)
        dissolveValue = dot(position, normalize(_DissolveDirection.xyz));
    #elif defined(_DISSOLVETYPE_RADIAL)
        half dist = distance(position, _DissolveDirection.xyz);
        half gradient = saturate(dist / max(_MaxDissolveDistance, 0.001h));
        dissolveValue = (_RadialDirection < 0.0) ? (1.0h - gradient) : gradient;
    #elif defined(_DISSOLVETYPE_PATTERN)
        if (_PatternType == 0) dissolveValue = (sin(position.x * _PatternFrequency) * cos(position.y * _PatternFrequency) + 1.0h) * 0.5h;
        else if (_PatternType == 1) { float2 grid = floor(position.xy * _PatternFrequency); dissolveValue = fmod(grid.x + grid.y, 2.0h); }
        else { float2 gridUV = frac(position.xy * _PatternFrequency); float lineThickness = 0.1h; float2 lines = step(1.0h - lineThickness, gridUV); dissolveValue = 1.0h - max(lines.x, lines.y); }
    #elif defined(_DISSOLVETYPE_SHATTER)
        dissolveValue = perVertexNoise;
    #endif
    return dissolveValue;
}

float3 ApplyShatterEffect(float3 positionOS, float threshold, float perturbedDissolveValue, float perVertexNoise)
{
    #if defined(_SHATTER_EFFECT_ON)
        float shatterProgress = saturate(MU_Remap(perturbedDissolveValue, threshold - _ShatterTriggerRange, threshold, 1.0h, 0.0h));
        float lift = pow(shatterProgress, 2.0h) * _ShatterLiftSpeed;
        float3 direction = normalize(_DissolveDirection.xyz);
        float3 offset = (MU_Hash22(positionOS.xy).xy - 0.5h).xyy * _ShatterOffsetStrength;
        positionOS += direction * lift * _ShatterStrength;
        positionOS += offset * shatterProgress;
        positionOS += perVertexNoise * _VertexDisplacement * shatterProgress;
    #endif
    return positionOS;
}

float3 ApplyStandardVertexDisplacement(float3 positionOS, float3 normalOS, float threshold, float perturbedDissolveValue)
{
    #if defined(_VERTEX_DISPLACEMENT_ON)
        float displacementFactor = 1.0h - smoothstep(threshold - _BounceWaveWidth, threshold, perturbedDissolveValue);
        #if defined(_DISPLACEMENT_SATURATE_ON) 
            displacementFactor = saturate(displacementFactor); 
        #endif
        positionOS.xyz += normalOS * displacementFactor * _VertexDisplacement;
    #endif
    return positionOS;
}

float3 ApplyDissolveVertexEffects(float3 positionOS, float3 normalOS, float dissolveValue, float perVertexNoise)
{
    #if defined(_VERTEX_DISPLACEMENT_ON) || defined(_SHATTER_EFFECT_ON)
        half timeAnimOffset = _UseTimeAnimation > 0.5h ? sin(_Time.y * _TimeScale) * 0.05h : 0.0h;
        
        half threshold;
        #if defined(_HOLOGRAM_REVEAL_ON)
            threshold = 1.0h - saturate(_RevealProgress * 0.5h);
        #else
            threshold = _DissolveThreshold;
        #endif
        
        threshold += timeAnimOffset;
        half vertexPerturbation = (perVertexNoise - 0.5h) * _NoiseStrength;
        half perturbedDissolveValueForVertex = dissolveValue + vertexPerturbation;

        #if defined(_DISSOLVETYPE_SHATTER)
            positionOS = ApplyShatterEffect(positionOS, threshold, perturbedDissolveValueForVertex, perVertexNoise);
        #else
            positionOS = ApplyStandardVertexDisplacement(positionOS, normalOS, threshold, perturbedDissolveValueForVertex);
        #endif
    #endif
    return positionOS;
}

half4 GetHologramPixelData(float3 worldPos)
{
    float2 patternUV = worldPos.xy * _HologramPatternScale * 0.1h;
    patternUV.y += _Time.y * 0.1h;
    float pattern = SAMPLE_TEXTURE2D(_HologramPatternTex, sampler_HologramPatternTex, patternUV).a;
    float flicker = (sin(_Time.y * _HologramFlickerSpeed) * 0.5h + 0.5h) * 0.3h + 0.7h;
    half3 emission = _HologramEmissionColor.rgb * flicker * pattern;
    return half4(emission, pattern);
}

half4 CalculateHologramRevealEffect(half4 currentPixel, half perturbedDissolveValue, float3 worldPos, half timeAnimOffset)
{
    half progress = _RevealProgress + timeAnimOffset;
    half hologramReveal = saturate(progress);
    half materialReveal = saturate(progress - 1.0h);
    half hologramThreshold = 1.0h - hologramReveal;
    half materialThreshold = 1.0h - materialReveal;
    half hologramVisibility = smoothstep(hologramThreshold - _DissolveEdgeWidth, hologramThreshold, perturbedDissolveValue);
    half materialVisibility = smoothstep(materialThreshold - _DissolveEdgeWidth, materialThreshold, perturbedDissolveValue);
    half hologramEdgeFactor = hologramVisibility - smoothstep(hologramThreshold, hologramThreshold + 0.001h, perturbedDissolveValue);
    half materialEdgeFactor = materialVisibility - smoothstep(materialThreshold, materialThreshold + 0.001h, perturbedDissolveValue);
    half4 hologramPixel = GetHologramPixelData(worldPos);
    hologramPixel.rgb = lerp(hologramPixel.rgb, _DissolveEdgeColor.rgb, hologramEdgeFactor);
    currentPixel.rgb = lerp(currentPixel.rgb, _DissolveEdgeColor.rgb, materialEdgeFactor);
    half4 finalPixel;
    finalPixel.rgb = lerp(hologramPixel.rgb, currentPixel.rgb, materialVisibility);
    finalPixel.a = lerp(hologramPixel.a, currentPixel.a, materialVisibility) * hologramVisibility;
    return finalPixel;
}

half4 CalculateStandardDissolveEffect(half4 currentPixel, half perturbedDissolveValue, half timeAnimOffset)
{
    half threshold = _DissolveThreshold + timeAnimOffset;
    #if defined(_DISSOLVETYPE_ALPHA_BLEND)
        half remappedAlpha = 1.0h - saturate((perturbedDissolveValue - threshold) / max(_AlphaFadeRange, 0.001h));
        currentPixel.a *= remappedAlpha;
    #else
        half edgeStart = threshold - _DissolveEdgeWidth;
        half dissolveAlpha = smoothstep(edgeStart, threshold, perturbedDissolveValue);
        half edgeColorFactor = dissolveAlpha - smoothstep(threshold, threshold + 0.001h, perturbedDissolveValue);
        currentPixel.rgb = lerp(currentPixel.rgb, _DissolveEdgeColor.rgb, edgeColorFactor);
        currentPixel.a *= dissolveAlpha;
    #endif
    return currentPixel;
}

half4 ApplyDissolveFragmentEffect(half4 currentPixel, float baseDissolveValue, float2 uv, float3 worldPos, float3 normalWS)
{
    half timeAnimOffset = _UseTimeAnimation > 0.5h ? sin(_Time.y * _TimeScale) * 0.05h : 0.0h;
    half perturbedDissolveValue = baseDissolveValue;
    #if !defined(_DISSOLVETYPE_NOISE)
        half noiseTexSample = TriplanarSample(TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), worldPos, normalWS, _NoiseScale, 2.0f);
        half pixelPerturbation = (noiseTexSample - 0.5h) * _NoiseStrength;
        perturbedDissolveValue += pixelPerturbation;
    #endif
    #if defined(_HOLOGRAM_REVEAL_ON)
        return CalculateHologramRevealEffect(currentPixel, perturbedDissolveValue, worldPos, timeAnimOffset);
    #else
        return CalculateStandardDissolveEffect(currentPixel, perturbedDissolveValue, timeAnimOffset);
    #endif
}
#endif
#endif