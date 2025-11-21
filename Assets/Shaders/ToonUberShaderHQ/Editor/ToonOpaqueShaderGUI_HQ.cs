using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class ToonOpaqueShaderGUI_HQ : ShaderGUI
{
    public enum SurfaceType { Opaque, Metallic, Foliage, Bling, Cosmic }
    public enum RenderMode { Opaque, Cutout, Transparent }

    private MaterialProperty _SurfaceType;
    private MaterialProperty _RenderMode;
    private MaterialProperty _SrcBlend;
    private MaterialProperty _DstBlend;
    private MaterialProperty _ZWrite;
    private MaterialProperty _ZTest;
    private MaterialProperty _CullMode;
    private MaterialProperty _BaseMap;
    private MaterialProperty _BaseColor;
    private MaterialProperty _BumpMap;
    private MaterialProperty _BumpScale;
    private MaterialProperty _Cutoff;
    private MaterialProperty _EmissionMode;
    private MaterialProperty _EmissionColor;
    private MaterialProperty _EmissionMap;

    // Lighting
    private MaterialProperty _FakeLightMode;
    private MaterialProperty _FakeLightColor;
    private MaterialProperty _FakeLightDirection;

    // Toon
    private MaterialProperty _ShadowTint;
    private MaterialProperty _MidtoneColor;
    private MaterialProperty _ShadowThreshold;
    private MaterialProperty _MidtoneThreshold;
    private MaterialProperty _RampSmoothness;
    private MaterialProperty _AmbientColor;
    private MaterialProperty _ToonSpecularToggle;
    private MaterialProperty _ToonSpecularColor;
    private MaterialProperty _ToonSpecularSize;
    private MaterialProperty _ToonSpecularSmoothness;

    // Stylized Metal
    private MaterialProperty _Ramp;
    private MaterialProperty _Brightness;
    private MaterialProperty _Offset;
    private MaterialProperty _HighlightOffset;
    private MaterialProperty _SpecuColor;
    private MaterialProperty _HiColor;
    private MaterialProperty _MetalRimColor;
    private MaterialProperty _MetalRimPower;

    // Foliage
    private MaterialProperty _WindFrequency;
    private MaterialProperty _WindAmplitude;
    private MaterialProperty _WindDirection;
    private MaterialProperty _TranslucencyColor;
    private MaterialProperty _TranslucencyStrength;

    // Bling
    private MaterialProperty _BlingWorldSpace;
    private MaterialProperty _BlingColor;
    private MaterialProperty _BlingIntensity;
    private MaterialProperty _BlingScale;
    private MaterialProperty _BlingSpeed;
    private MaterialProperty _BlingThreshold;
    private MaterialProperty _BlingFresnelPower;

    // Cosmic
    private MaterialProperty _CosmicTex1;
    private MaterialProperty _CosmicColor1;
    private MaterialProperty _CosmicScale1;
    private MaterialProperty _CosmicScrollSpeed1;
    private MaterialProperty _CosmicParallaxDepth1;
    private MaterialProperty _CosmicTex2;
    private MaterialProperty _CosmicColor2;
    private MaterialProperty _CosmicScale2;
    private MaterialProperty _CosmicScrollSpeed2;
    private MaterialProperty _CosmicParallaxDepth2;
    private MaterialProperty _StarfieldTex;
    private MaterialProperty _StarfieldColor;
    private MaterialProperty _StarfieldScale;
    private MaterialProperty _StarfieldScrollSpeed;
    private MaterialProperty _StarfieldParallaxDepth;
    private MaterialProperty _TriplanarSharpness;
    private MaterialProperty _CosmicAmbientColor;

    // Outline
    private MaterialProperty _OutlineMode;
    private MaterialProperty _FresnelOutlineColor;
    private MaterialProperty _FresnelOutlineWidth;
    private MaterialProperty _FresnelOutlinePower;
    private MaterialProperty _FresnelOutlineSharpness;
    private MaterialProperty _GlintToggle;
    private MaterialProperty _GlintColor;
    private MaterialProperty _GlintScale;
    private MaterialProperty _GlintSpeed;
    private MaterialProperty _GlintThreshold;
    private MaterialProperty _OutlineColor;
    private MaterialProperty _OutlineWidth;
    private MaterialProperty _OutlineScaleWithDistance;
    private MaterialProperty _DistanceFadeStart;
    private MaterialProperty _DistanceFadeEnd;

    // Effects
    private MaterialProperty _EffectType;
    private MaterialProperty _EffectTex1;
    private MaterialProperty _RareColor1;
    private MaterialProperty _RareFloat1;
    private MaterialProperty _RareFloat2;
    private MaterialProperty _EpicColor1;
    private MaterialProperty _EpicColor2;
    private MaterialProperty _EpicFloat1;
    private MaterialProperty _EpicFloat2;
    private MaterialProperty _EpicFloat3;
    private MaterialProperty _LegendaryColor1;
    private MaterialProperty _LegendaryColor2;
    private MaterialProperty _LegendaryFloat1;
    private MaterialProperty _LegendaryFloat2;
    private MaterialProperty _LegendaryFloat3;

    // Masking
    private MaterialProperty _MaskTriplanarToggle;
    private MaterialProperty _MaskTriplanarTex;
    private MaterialProperty _MaskTriplanarScale;
    private MaterialProperty _MaskTriplanarBlend;
    private MaterialProperty _MaskTriplanarSharpness;
    private MaterialProperty _MaskDivisions;
    private MaterialProperty _MaskColor0; private MaterialProperty _MaskColor1; private MaterialProperty _MaskColor2; private MaterialProperty _MaskColor3;
    private MaterialProperty _MaskColor4; private MaterialProperty _MaskColor5; private MaterialProperty _MaskColor6; private MaterialProperty _MaskColor7;
    private MaterialProperty _MaskColor8; private MaterialProperty _MaskColor9; private MaterialProperty _MaskColor10; private MaterialProperty _MaskColor11;
    private MaterialProperty _MaskColor12; private MaterialProperty _MaskColor13; private MaterialProperty _MaskColor14; private MaterialProperty _MaskColor15;

    // Rim Light
    private MaterialProperty _RimLightToggle;
    private MaterialProperty _RimLightColor;
    private MaterialProperty _RimLightPower;
    private MaterialProperty _RimLightSmoothness;

    // Dissolve
    private MaterialProperty _EnableDissolve;
    private MaterialProperty _DissolveType;
    private MaterialProperty _DissolveThreshold;
    private MaterialProperty _RevealProgress;
    private MaterialProperty _RadialDirection;
    private MaterialProperty _UseTimeAnimation;
    private MaterialProperty _TimeScale;
    private MaterialProperty _UseLocalSpace;
    private MaterialProperty _DissolveDirection;
    private MaterialProperty _NoiseTex;
    private MaterialProperty _NoiseScale;
    private MaterialProperty _NoiseStrength;
    private MaterialProperty _DissolveEdgeWidth;
    private MaterialProperty _DissolveEdgeColor;
    private MaterialProperty _MaxDissolveDistance;
    private MaterialProperty _PatternType;
    private MaterialProperty _PatternFrequency;
    private MaterialProperty _AlphaFadeRange;
    private MaterialProperty _EnableVertexDisplacement;
    private MaterialProperty _UseSaturateDisplacement;
    private MaterialProperty _EnableShatterEffect;
    private MaterialProperty _VertexDisplacement;
    private MaterialProperty _BounceWaveWidth;
    private MaterialProperty _ShatterStrength;
    private MaterialProperty _ShatterLiftSpeed;
    private MaterialProperty _ShatterOffsetStrength;
    private MaterialProperty _ShatterTriggerRange;
    private MaterialProperty _EnableHologramReveal;
    private MaterialProperty _HologramPatternTex;
    private MaterialProperty _HologramEmissionColor;
    private MaterialProperty _HologramPatternScale;
    private MaterialProperty _HologramFlickerSpeed;

    private bool _showRenderSettings = true;
    private bool _showBaseProperties = true;
    private bool _showLighting = true;
    private bool _showSurfaceSpecific = true;
    private bool _showOutline = true;
    private bool _showDissolve = true;
    private bool _showEffects = true;
    private bool _showMasking = true;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        FindProperties(properties);
        Material material = materialEditor.target as Material;

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Bill's Toon HQ Refined", EditorStyles.boldLabel);

        _showRenderSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showRenderSettings, "Render Settings");
        if (_showRenderSettings)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(_SurfaceType, "Surface Type");
            materialEditor.ShaderProperty(_RenderMode, "Render Mode");

            if ((RenderMode)_RenderMode.floatValue == RenderMode.Cutout)
                materialEditor.ShaderProperty(_Cutoff, "Alpha Cutoff");

            materialEditor.ShaderProperty(_CullMode, "Culling");
            materialEditor.ShaderProperty(_ZWrite, "Z Write");
            materialEditor.ShaderProperty(_ZTest, "Z Test");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        _showBaseProperties = EditorGUILayout.BeginFoldoutHeaderGroup(_showBaseProperties, "Base Properties");
        if (_showBaseProperties)
        {
            EditorGUI.indentLevel++;
            materialEditor.TexturePropertySingleLine(new GUIContent("Base Map", "Albedo (RGB) Alpha (A)"), _BaseMap, _BaseColor);
            materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), _BumpMap, _BumpScale);

            materialEditor.ShaderProperty(_EmissionMode, "Enable Emission");
            if (_EmissionMode.floatValue > 0.5f)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Emission Map"), _EmissionMap, _EmissionColor);
                materialEditor.LightmapEmissionProperty();
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        _showLighting = EditorGUILayout.BeginFoldoutHeaderGroup(_showLighting, "Global Lighting");
        if (_showLighting)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(_FakeLightMode, "Fake Light Enabled");
            if (_FakeLightMode.floatValue > 0.5f)
            {
                materialEditor.ShaderProperty(_FakeLightColor, "Fake Light Color");
                materialEditor.ShaderProperty(_FakeLightDirection, "Fake Light Direction");
            }

            materialEditor.ShaderProperty(_RimLightToggle, "Enable Rim Light");
            if (_RimLightToggle.floatValue > 0.5f)
            {
                materialEditor.ShaderProperty(_RimLightColor, "Rim Color");
                materialEditor.ShaderProperty(_RimLightPower, "Rim Power");
                materialEditor.ShaderProperty(_RimLightSmoothness, "Rim Smoothness");
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        DrawSurfaceSpecificGUI(materialEditor, (SurfaceType)_SurfaceType.floatValue);

        _showMasking = EditorGUILayout.BeginFoldoutHeaderGroup(_showMasking, "Triplanar Masking");
        if (_showMasking)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(_MaskTriplanarToggle, "Enable Masking");
            if (_MaskTriplanarToggle.floatValue > 0.5f)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Triplanar Tex"), _MaskTriplanarTex);
                materialEditor.ShaderProperty(_MaskTriplanarScale, "Scale");
                materialEditor.ShaderProperty(_MaskTriplanarBlend, "Blend");
                materialEditor.ShaderProperty(_MaskTriplanarSharpness, "Sharpness");
                materialEditor.ShaderProperty(_MaskDivisions, "Grid Divisions");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Mask Colors", EditorStyles.boldLabel);

                int divisions = (int)_MaskDivisions.floatValue;
                int count = divisions * divisions;

                MaterialProperty[] maskColors = { _MaskColor0, _MaskColor1, _MaskColor2, _MaskColor3, _MaskColor4, _MaskColor5, _MaskColor6, _MaskColor7, _MaskColor8, _MaskColor9, _MaskColor10, _MaskColor11, _MaskColor12, _MaskColor13, _MaskColor14, _MaskColor15 };

                for (int i = 0; i < count; i++)
                {
                    materialEditor.ShaderProperty(maskColors[i], "Cell " + i);
                }
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        _showOutline = EditorGUILayout.BeginFoldoutHeaderGroup(_showOutline, "Outline");
        if (_showOutline)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(_OutlineMode, "Outline Mode");
            float mode = _OutlineMode.floatValue;

            if (mode > 0.5f && mode < 1.5f) // Fresnel
            {
                materialEditor.ShaderProperty(_FresnelOutlineColor, "Color");
                materialEditor.ShaderProperty(_FresnelOutlineWidth, "Width");
                materialEditor.ShaderProperty(_FresnelOutlinePower, "Power");
                materialEditor.ShaderProperty(_FresnelOutlineSharpness, "Sharpness");

                materialEditor.ShaderProperty(_GlintToggle, "Enable Glint");
                if (_GlintToggle.floatValue > 0.5f)
                {
                    materialEditor.ShaderProperty(_GlintColor, "Glint Color");
                    materialEditor.ShaderProperty(_GlintScale, "Scale");
                    materialEditor.ShaderProperty(_GlintSpeed, "Speed");
                    materialEditor.ShaderProperty(_GlintThreshold, "Threshold");
                }
            }
            else if (mode > 1.5f) // Hull
            {
                materialEditor.ShaderProperty(_OutlineColor, "Color");
                materialEditor.ShaderProperty(_OutlineWidth, "Width");
                materialEditor.ShaderProperty(_OutlineScaleWithDistance, "Scale with Distance");
                if (_OutlineScaleWithDistance.floatValue > 0.5f)
                {
                    materialEditor.ShaderProperty(_DistanceFadeStart, "Fade Start");
                    materialEditor.ShaderProperty(_DistanceFadeEnd, "Fade End");
                }
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        _showEffects = EditorGUILayout.BeginFoldoutHeaderGroup(_showEffects, "Tiered Effects");
        if (_showEffects)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(_EffectType, "Effect Type");
            int effect = (int)_EffectType.floatValue;

            if (effect > 0)
            {
                if (effect >= 3) materialEditor.TexturePropertySingleLine(new GUIContent("Effect Texture"), _EffectTex1);

                if (effect == 1 || effect == 2) // Rare
                {
                    materialEditor.ShaderProperty(_RareColor1, "Color 1");
                    materialEditor.ShaderProperty(_RareFloat1, effect == 1 ? "Speed" : "Scale");
                    materialEditor.ShaderProperty(_RareFloat2, effect == 1 ? "Pulse Amount" : "Speed");
                }
                else if (effect == 3 || effect == 4) // Epic
                {
                    materialEditor.ShaderProperty(_EpicColor1, "Color 1");
                    materialEditor.ShaderProperty(_EpicColor2, "Color 2");
                    materialEditor.ShaderProperty(_EpicFloat1, "Speed");
                    materialEditor.ShaderProperty(_EpicFloat2, "Scale");
                    materialEditor.ShaderProperty(_EpicFloat3, "Intensity");
                }
                else if (effect >= 5) // Legendary
                {
                    materialEditor.ShaderProperty(_LegendaryColor1, "Color 1");
                    materialEditor.ShaderProperty(_LegendaryColor2, "Color 2");
                    materialEditor.ShaderProperty(_LegendaryFloat1, "Speed");
                    materialEditor.ShaderProperty(_LegendaryFloat2, "Scale");
                    materialEditor.ShaderProperty(_LegendaryFloat3, "Intensity");
                }
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        _showDissolve = EditorGUILayout.BeginFoldoutHeaderGroup(_showDissolve, "Dissolve");
        if (_showDissolve)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(_EnableDissolve, "Enable Dissolve");
            if (_EnableDissolve.floatValue > 0.5f)
            {
                materialEditor.ShaderProperty(_DissolveType, "Type");
                int type = (int)_DissolveType.floatValue;

                materialEditor.ShaderProperty(_EnableHologramReveal, "Hologram Reveal Mode");
                bool isHologram = _EnableHologramReveal.floatValue > 0.5f;

                if (isHologram)
                {
                    materialEditor.ShaderProperty(_RevealProgress, "Reveal Progress");
                    materialEditor.TexturePropertySingleLine(new GUIContent("Hologram Pattern"), _HologramPatternTex, _HologramEmissionColor);
                    materialEditor.ShaderProperty(_HologramPatternScale, "Pattern Scale");
                    materialEditor.ShaderProperty(_HologramFlickerSpeed, "Flicker Speed");
                }
                else
                {
                    materialEditor.ShaderProperty(_DissolveThreshold, "Threshold");
                }

                if (type == 1 || type == 5) materialEditor.ShaderProperty(_DissolveDirection, "Direction");
                if (type == 2)
                {
                    materialEditor.ShaderProperty(_DissolveDirection, "Center Point");
                    materialEditor.ShaderProperty(_MaxDissolveDistance, "Radius");
                    materialEditor.ShaderProperty(_RadialDirection, "Invert");
                }
                if (type == 3)
                {
                    materialEditor.ShaderProperty(_PatternType, "Pattern");
                    materialEditor.ShaderProperty(_PatternFrequency, "Frequency");
                }

                materialEditor.TexturePropertySingleLine(new GUIContent("Noise Texture"), _NoiseTex);
                materialEditor.ShaderProperty(_NoiseScale, "Noise Scale");
                materialEditor.ShaderProperty(_NoiseStrength, "Noise Strength");

                if (!isHologram)
                {
                    materialEditor.ShaderProperty(_DissolveEdgeWidth, "Edge Width");
                    materialEditor.ShaderProperty(_DissolveEdgeColor, "Edge Color");
                }

                materialEditor.ShaderProperty(_UseTimeAnimation, "Animate with Time");
                if (_UseTimeAnimation.floatValue > 0.5f)
                    materialEditor.ShaderProperty(_TimeScale, "Time Scale");

                EditorGUILayout.LabelField("Vertex Effects", EditorStyles.boldLabel);
                materialEditor.ShaderProperty(_EnableVertexDisplacement, "Displacement");
                if (_EnableVertexDisplacement.floatValue > 0.5f || type == 5)
                {
                    materialEditor.ShaderProperty(_VertexDisplacement, "Intensity");
                    materialEditor.ShaderProperty(_BounceWaveWidth, "Wave Width");
                }

                if (type == 5) // Shatter
                {
                    materialEditor.ShaderProperty(_EnableShatterEffect, "Shatter Physics");
                    if (_EnableShatterEffect.floatValue > 0.5f)
                    {
                        materialEditor.ShaderProperty(_ShatterStrength, "Strength");
                        materialEditor.ShaderProperty(_ShatterLiftSpeed, "Lift");
                        materialEditor.ShaderProperty(_ShatterOffsetStrength, "Scatter");
                        materialEditor.ShaderProperty(_ShatterTriggerRange, "Trigger Range");
                    }
                }
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (EditorGUI.EndChangeCheck())
        {
            foreach (Material m in materialEditor.targets)
            {
                SetupKeywords(m);
            }
        }
    }

    private void DrawSurfaceSpecificGUI(MaterialEditor materialEditor, SurfaceType surfaceType)
    {
        _showSurfaceSpecific = EditorGUILayout.BeginFoldoutHeaderGroup(_showSurfaceSpecific, surfaceType.ToString() + " Settings");
        if (_showSurfaceSpecific)
        {
            EditorGUI.indentLevel++;
            switch (surfaceType)
            {
                case SurfaceType.Opaque:
                    materialEditor.ShaderProperty(_ShadowTint, "Shadow Tint");
                    materialEditor.ShaderProperty(_MidtoneColor, "Midtone Color");
                    materialEditor.ShaderProperty(_AmbientColor, "Ambient Color");
                    materialEditor.ShaderProperty(_ShadowThreshold, "Shadow Threshold");
                    materialEditor.ShaderProperty(_MidtoneThreshold, "Midtone Threshold");
                    materialEditor.ShaderProperty(_RampSmoothness, "Smoothness");

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Specular", EditorStyles.boldLabel);
                    materialEditor.ShaderProperty(_ToonSpecularToggle, "Enable Specular");
                    if (_ToonSpecularToggle.floatValue > 0.5f)
                    {
                        materialEditor.ShaderProperty(_ToonSpecularColor, "Color");
                        materialEditor.ShaderProperty(_ToonSpecularSize, "Size");
                        materialEditor.ShaderProperty(_ToonSpecularSmoothness, "Smoothness");
                    }
                    break;

                case SurfaceType.Metallic:
                    materialEditor.TexturePropertySingleLine(new GUIContent("Ramp Texture"), _Ramp);
                    materialEditor.ShaderProperty(_Brightness, "Brightness");
                    materialEditor.ShaderProperty(_SpecuColor, "Specular Color");
                    materialEditor.ShaderProperty(_Offset, "Specular Size");
                    materialEditor.ShaderProperty(_HiColor, "Highlight Color");
                    materialEditor.ShaderProperty(_HighlightOffset, "Highlight Size");
                    materialEditor.ShaderProperty(_MetalRimColor, "Metal Rim Color");
                    materialEditor.ShaderProperty(_MetalRimPower, "Metal Rim Power");
                    break;

                case SurfaceType.Foliage:
                    materialEditor.ShaderProperty(_TranslucencyColor, "Translucency Color");
                    materialEditor.ShaderProperty(_TranslucencyStrength, "Translucency Strength");
                    materialEditor.ShaderProperty(_WindFrequency, "Wind Frequency");
                    materialEditor.ShaderProperty(_WindAmplitude, "Wind Amplitude");
                    materialEditor.ShaderProperty(_WindDirection, "Wind Direction");
                    break;

                case SurfaceType.Bling:
                    materialEditor.ShaderProperty(_BlingWorldSpace, "World Space");
                    materialEditor.ShaderProperty(_BlingColor, "Color");
                    materialEditor.ShaderProperty(_BlingIntensity, "Intensity");
                    materialEditor.ShaderProperty(_BlingScale, "Scale");
                    materialEditor.ShaderProperty(_BlingSpeed, "Speed");
                    materialEditor.ShaderProperty(_BlingThreshold, "Threshold");
                    materialEditor.ShaderProperty(_BlingFresnelPower, "Fresnel Power");
                    break;

                case SurfaceType.Cosmic:
                    materialEditor.TexturePropertySingleLine(new GUIContent("Nebula 1"), _CosmicTex1, _CosmicColor1);
                    materialEditor.ShaderProperty(_CosmicScale1, "Scale 1");
                    materialEditor.ShaderProperty(_CosmicScrollSpeed1, "Speed 1");
                    materialEditor.ShaderProperty(_CosmicParallaxDepth1, "Parallax 1");

                    materialEditor.TexturePropertySingleLine(new GUIContent("Nebula 2"), _CosmicTex2, _CosmicColor2);
                    materialEditor.ShaderProperty(_CosmicScale2, "Scale 2");
                    materialEditor.ShaderProperty(_CosmicScrollSpeed2, "Speed 2");
                    materialEditor.ShaderProperty(_CosmicParallaxDepth2, "Parallax 2");

                    materialEditor.TexturePropertySingleLine(new GUIContent("Stars"), _StarfieldTex, _StarfieldColor);
                    materialEditor.ShaderProperty(_StarfieldScale, "Scale");
                    materialEditor.ShaderProperty(_StarfieldScrollSpeed, "Speed");
                    materialEditor.ShaderProperty(_StarfieldParallaxDepth, "Parallax");

                    materialEditor.ShaderProperty(_TriplanarSharpness, "Blend Sharpness");
                    materialEditor.ShaderProperty(_CosmicAmbientColor, "Ambient Color");
                    break;
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void SetupKeywords(Material material)
    {
        // Render Mode
        int renderMode = (int)material.GetFloat("_RenderMode");
        if (renderMode == 0) // Opaque
        {
            material.SetOverrideTag("RenderType", "Opaque");
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHACLIP_ON");
            material.renderQueue = (int)RenderQueue.Geometry;
        }
        else if (renderMode == 1) // Cutout
        {
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.EnableKeyword("_ALPHACLIP_ON");
            material.renderQueue = (int)RenderQueue.AlphaTest;
        }
        else // Transparent
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHACLIP_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        // Surface Type
        SurfaceType surface = (SurfaceType)material.GetFloat("_SurfaceType");
        SetKeyword(material, "_SURFACETYPE_OPAQUE", surface == SurfaceType.Opaque);
        SetKeyword(material, "_SURFACETYPE_METALLIC", surface == SurfaceType.Metallic);
        SetKeyword(material, "_SURFACETYPE_FOLIAGE", surface == SurfaceType.Foliage);
        SetKeyword(material, "_SURFACETYPE_BLING", surface == SurfaceType.Bling);
        SetKeyword(material, "_SURFACETYPE_COSMIC", surface == SurfaceType.Cosmic);

        // Features
        SetKeyword(material, "_EMISSION_ON", material.GetFloat("_EmissionMode") > 0.5f);
        SetKeyword(material, "_FAKELIGHT_ON", material.GetFloat("_FakeLightMode") > 0.5f);
        SetKeyword(material, "_RIMLIGHT_ON", material.GetFloat("_RimLightToggle") > 0.5f);
        SetKeyword(material, "_MASK_TRIPLANAR_ON", material.GetFloat("_MaskTriplanarToggle") > 0.5f);
        SetKeyword(material, "_TOON_SPECULAR_ON", material.GetFloat("_ToonSpecularToggle") > 0.5f && surface == SurfaceType.Opaque);

        // Outline
        float outlineMode = material.GetFloat("_OutlineMode");
        SetKeyword(material, "_OUTLINEMODE_FRESNEL", outlineMode > 0.5f && outlineMode < 1.5f);
        SetKeyword(material, "_OUTLINEMODE_HULL", outlineMode > 1.5f);
        SetKeyword(material, "_OUTLINEGLINT_ON", material.GetFloat("_GlintToggle") > 0.5f && outlineMode > 0.5f && outlineMode < 1.5f);
        SetKeyword(material, "_OUTLINE_SCALE_WITH_DISTANCE", material.GetFloat("_OutlineScaleWithDistance") > 0.5f && outlineMode > 1.5f);

        // Dissolve
        bool dissolve = material.GetFloat("_EnableDissolve") > 0.5f;
        SetKeyword(material, "_DISSOLVE_ON", dissolve);
        if (dissolve)
        {
            int type = (int)material.GetFloat("_DissolveType");
            SetKeyword(material, "_DISSOLVETYPE_NOISE", type == 0);
            SetKeyword(material, "_DISSOLVETYPE_LINEAR", type == 1);
            SetKeyword(material, "_DISSOLVETYPE_RADIAL", type == 2);
            SetKeyword(material, "_DISSOLVETYPE_PATTERN", type == 3);
            SetKeyword(material, "_DISSOLVETYPE_ALPHA_BLEND", type == 4);
            SetKeyword(material, "_DISSOLVETYPE_SHATTER", type == 5);

            SetKeyword(material, "_DISSOLVE_LOCALSPACE_ON", material.GetFloat("_UseLocalSpace") > 0.5f);
            SetKeyword(material, "_VERTEX_DISPLACEMENT_ON", material.GetFloat("_EnableVertexDisplacement") > 0.5f);
            SetKeyword(material, "_SHATTER_EFFECT_ON", material.GetFloat("_EnableShatterEffect") > 0.5f && type == 5);
            SetKeyword(material, "_DISPLACEMENT_SATURATE_ON", material.GetFloat("_UseSaturateDisplacement") > 0.5f);
            SetKeyword(material, "_HOLOGRAM_REVEAL_ON", material.GetFloat("_EnableHologramReveal") > 0.5f);
        }

        // Effects
        int effect = (int)material.GetFloat("_EffectType");
        SetKeyword(material, "_EFFECT_RARE_PULSING_GLOW", effect == 1);
        SetKeyword(material, "_EFFECT_RARE_SPARKLES", effect == 2);
        SetKeyword(material, "_EFFECT_EPIC_FIRE_AURA", effect == 3);
        SetKeyword(material, "_EFFECT_EPIC_ELECTRIC_FIELD", effect == 4);
        SetKeyword(material, "_EFFECT_LEGENDARY_COSMIC_RIFT", effect == 5);
        SetKeyword(material, "_EFFECT_LEGENDARY_HOLY_AURA", effect == 6);

        SetKeyword(material, "_BLING_WORLDSPACE_ON", material.GetFloat("_BlingWorldSpace") > 0.5f);
    }

    private void SetKeyword(Material m, string keyword, bool state)
    {
        if (state) m.EnableKeyword(keyword);
        else m.DisableKeyword(keyword);
    }

    private void FindProperties(MaterialProperty[] props)
    {
        _SurfaceType = FindProperty("_SurfaceType", props);
        _RenderMode = FindProperty("_RenderMode", props);
        _SrcBlend = FindProperty("_SrcBlend", props);
        _DstBlend = FindProperty("_DstBlend", props);
        _ZWrite = FindProperty("_ZWrite", props);
        _ZTest = FindProperty("_ZTest", props);
        _CullMode = FindProperty("_CullMode", props);
        _BaseMap = FindProperty("_BaseMap", props);
        _BaseColor = FindProperty("_BaseColor", props);
        _BumpMap = FindProperty("_BumpMap", props);
        _BumpScale = FindProperty("_BumpScale", props);
        _Cutoff = FindProperty("_Cutoff", props);
        _EmissionMode = FindProperty("_EmissionMode", props);
        _EmissionColor = FindProperty("_EmissionColor", props);
        _EmissionMap = FindProperty("_EmissionMap", props);

        _FakeLightMode = FindProperty("_FakeLightMode", props);
        _FakeLightColor = FindProperty("_FakeLightColor", props);
        _FakeLightDirection = FindProperty("_FakeLightDirection", props);

        _ShadowTint = FindProperty("_ShadowTint", props);
        _MidtoneColor = FindProperty("_MidtoneColor", props);
        _ShadowThreshold = FindProperty("_ShadowThreshold", props);
        _MidtoneThreshold = FindProperty("_MidtoneThreshold", props);
        _RampSmoothness = FindProperty("_RampSmoothness", props);
        _AmbientColor = FindProperty("_AmbientColor", props);
        _ToonSpecularToggle = FindProperty("_ToonSpecularToggle", props);
        _ToonSpecularColor = FindProperty("_ToonSpecularColor", props);
        _ToonSpecularSize = FindProperty("_ToonSpecularSize", props);
        _ToonSpecularSmoothness = FindProperty("_ToonSpecularSmoothness", props);

        _Ramp = FindProperty("_Ramp", props);
        _Brightness = FindProperty("_Brightness", props);
        _Offset = FindProperty("_Offset", props);
        _HighlightOffset = FindProperty("_HighlightOffset", props);
        _SpecuColor = FindProperty("_SpecuColor", props);
        _HiColor = FindProperty("_HiColor", props);
        _MetalRimColor = FindProperty("_MetalRimColor", props);
        _MetalRimPower = FindProperty("_MetalRimPower", props);

        _WindFrequency = FindProperty("_WindFrequency", props);
        _WindAmplitude = FindProperty("_WindAmplitude", props);
        _WindDirection = FindProperty("_WindDirection", props);
        _TranslucencyColor = FindProperty("_TranslucencyColor", props);
        _TranslucencyStrength = FindProperty("_TranslucencyStrength", props);

        _BlingWorldSpace = FindProperty("_BlingWorldSpace", props);
        _BlingColor = FindProperty("_BlingColor", props);
        _BlingIntensity = FindProperty("_BlingIntensity", props);
        _BlingScale = FindProperty("_BlingScale", props);
        _BlingSpeed = FindProperty("_BlingSpeed", props);
        _BlingThreshold = FindProperty("_BlingThreshold", props);
        _BlingFresnelPower = FindProperty("_BlingFresnelPower", props);

        _CosmicTex1 = FindProperty("_CosmicTex1", props);
        _CosmicColor1 = FindProperty("_CosmicColor1", props);
        _CosmicScale1 = FindProperty("_CosmicScale1", props);
        _CosmicScrollSpeed1 = FindProperty("_CosmicScrollSpeed1", props);
        _CosmicParallaxDepth1 = FindProperty("_CosmicParallaxDepth1", props);
        _CosmicTex2 = FindProperty("_CosmicTex2", props);
        _CosmicColor2 = FindProperty("_CosmicColor2", props);
        _CosmicScale2 = FindProperty("_CosmicScale2", props);
        _CosmicScrollSpeed2 = FindProperty("_CosmicScrollSpeed2", props);
        _CosmicParallaxDepth2 = FindProperty("_CosmicParallaxDepth2", props);
        _StarfieldTex = FindProperty("_StarfieldTex", props);
        _StarfieldColor = FindProperty("_StarfieldColor", props);
        _StarfieldScale = FindProperty("_StarfieldScale", props);
        _StarfieldScrollSpeed = FindProperty("_StarfieldScrollSpeed", props);
        _StarfieldParallaxDepth = FindProperty("_StarfieldParallaxDepth", props);
        _TriplanarSharpness = FindProperty("_TriplanarSharpness", props);
        _CosmicAmbientColor = FindProperty("_CosmicAmbientColor", props);

        _OutlineMode = FindProperty("_OutlineMode", props);
        _FresnelOutlineColor = FindProperty("_FresnelOutlineColor", props);
        _FresnelOutlineWidth = FindProperty("_FresnelOutlineWidth", props);
        _FresnelOutlinePower = FindProperty("_FresnelOutlinePower", props);
        _FresnelOutlineSharpness = FindProperty("_FresnelOutlineSharpness", props);
        _GlintToggle = FindProperty("_GlintToggle", props);
        _GlintColor = FindProperty("_GlintColor", props);
        _GlintScale = FindProperty("_GlintScale", props);
        _GlintSpeed = FindProperty("_GlintSpeed", props);
        _GlintThreshold = FindProperty("_GlintThreshold", props);
        _OutlineColor = FindProperty("_OutlineColor", props);
        _OutlineWidth = FindProperty("_OutlineWidth", props);
        _OutlineScaleWithDistance = FindProperty("_OutlineScaleWithDistance", props);
        _DistanceFadeStart = FindProperty("_DistanceFadeStart", props);
        _DistanceFadeEnd = FindProperty("_DistanceFadeEnd", props);

        _EffectType = FindProperty("_EffectType", props);
        _EffectTex1 = FindProperty("_EffectTex1", props);
        _RareColor1 = FindProperty("_RareColor1", props);
        _RareFloat1 = FindProperty("_RareFloat1", props);
        _RareFloat2 = FindProperty("_RareFloat2", props);
        _EpicColor1 = FindProperty("_EpicColor1", props);
        _EpicColor2 = FindProperty("_EpicColor2", props);
        _EpicFloat1 = FindProperty("_EpicFloat1", props);
        _EpicFloat2 = FindProperty("_EpicFloat2", props);
        _EpicFloat3 = FindProperty("_EpicFloat3", props);
        _LegendaryColor1 = FindProperty("_LegendaryColor1", props);
        _LegendaryColor2 = FindProperty("_LegendaryColor2", props);
        _LegendaryFloat1 = FindProperty("_LegendaryFloat1", props);
        _LegendaryFloat2 = FindProperty("_LegendaryFloat2", props);
        _LegendaryFloat3 = FindProperty("_LegendaryFloat3", props);

        _MaskTriplanarToggle = FindProperty("_MaskTriplanarToggle", props);
        _MaskTriplanarTex = FindProperty("_MaskTriplanarTex", props);
        _MaskTriplanarScale = FindProperty("_MaskTriplanarScale", props);
        _MaskTriplanarBlend = FindProperty("_MaskTriplanarBlend", props);
        _MaskTriplanarSharpness = FindProperty("_MaskTriplanarSharpness", props);
        _MaskDivisions = FindProperty("_MaskDivisions", props);
        _MaskColor0 = FindProperty("_MaskColor0", props); _MaskColor1 = FindProperty("_MaskColor1", props);
        _MaskColor2 = FindProperty("_MaskColor2", props); _MaskColor3 = FindProperty("_MaskColor3", props);
        _MaskColor4 = FindProperty("_MaskColor4", props); _MaskColor5 = FindProperty("_MaskColor5", props);
        _MaskColor6 = FindProperty("_MaskColor6", props); _MaskColor7 = FindProperty("_MaskColor7", props);
        _MaskColor8 = FindProperty("_MaskColor8", props); _MaskColor9 = FindProperty("_MaskColor9", props);
        _MaskColor10 = FindProperty("_MaskColor10", props); _MaskColor11 = FindProperty("_MaskColor11", props);
        _MaskColor12 = FindProperty("_MaskColor12", props); _MaskColor13 = FindProperty("_MaskColor13", props);
        _MaskColor14 = FindProperty("_MaskColor14", props); _MaskColor15 = FindProperty("_MaskColor15", props);

        _RimLightToggle = FindProperty("_RimLightToggle", props);
        _RimLightColor = FindProperty("_RimLightColor", props);
        _RimLightPower = FindProperty("_RimLightPower", props);
        _RimLightSmoothness = FindProperty("_RimLightSmoothness", props);

        _EnableDissolve = FindProperty("_EnableDissolve", props);
        _DissolveType = FindProperty("_DissolveType", props);
        _DissolveThreshold = FindProperty("_DissolveThreshold", props);
        _RevealProgress = FindProperty("_RevealProgress", props);
        _RadialDirection = FindProperty("_RadialDirection", props);
        _UseTimeAnimation = FindProperty("_UseTimeAnimation", props);
        _TimeScale = FindProperty("_TimeScale", props);
        _UseLocalSpace = FindProperty("_UseLocalSpace", props);
        _DissolveDirection = FindProperty("_DissolveDirection", props);
        _NoiseTex = FindProperty("_NoiseTex", props);
        _NoiseScale = FindProperty("_NoiseScale", props);
        _NoiseStrength = FindProperty("_NoiseStrength", props);
        _DissolveEdgeWidth = FindProperty("_DissolveEdgeWidth", props);
        _DissolveEdgeColor = FindProperty("_DissolveEdgeColor", props);
        _MaxDissolveDistance = FindProperty("_MaxDissolveDistance", props);
        _PatternType = FindProperty("_PatternType", props);
        _PatternFrequency = FindProperty("_PatternFrequency", props);
        _AlphaFadeRange = FindProperty("_AlphaFadeRange", props);
        _EnableVertexDisplacement = FindProperty("_EnableVertexDisplacement", props);
        _UseSaturateDisplacement = FindProperty("_UseSaturateDisplacement", props);
        _EnableShatterEffect = FindProperty("_EnableShatterEffect", props);
        _VertexDisplacement = FindProperty("_VertexDisplacement", props);
        _BounceWaveWidth = FindProperty("_BounceWaveWidth", props);
        _ShatterStrength = FindProperty("_ShatterStrength", props);
        _ShatterLiftSpeed = FindProperty("_ShatterLiftSpeed", props);
        _ShatterOffsetStrength = FindProperty("_ShatterOffsetStrength", props);
        _ShatterTriggerRange = FindProperty("_ShatterTriggerRange", props);
        _EnableHologramReveal = FindProperty("_EnableHologramReveal", props);
        _HologramPatternTex = FindProperty("_HologramPatternTex", props);
        _HologramEmissionColor = FindProperty("_HologramEmissionColor", props);
        _HologramPatternScale = FindProperty("_HologramPatternScale", props);
        _HologramFlickerSpeed = FindProperty("_HologramFlickerSpeed", props);
    }
}