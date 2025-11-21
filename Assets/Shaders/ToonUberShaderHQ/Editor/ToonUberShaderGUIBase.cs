using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class ToonUber_Opaque_HQ_GUI : ShaderGUI
{
    static class Styles
    {
        public static GUIContent mainSettingsText = new GUIContent("Main Configuration");
        public static GUIContent baseSurfaceText = new GUIContent("Base Surface");
        public static GUIContent surfaceOptionsText = new GUIContent("Surface Type & Effects");
        public static GUIContent lightingText = new GUIContent("Lighting & Toon Ramp");
        public static GUIContent emissionText = new GUIContent("Emission");
        public static GUIContent outlineText = new GUIContent("Outlines");
        public static GUIContent dissolveText = new GUIContent("Dissolve & Hologram");
        public static GUIContent triplanarText = new GUIContent("Dynamic Triplanar Masking");
        public static GUIContent advancedText = new GUIContent("Advanced Render Settings");
    }

    bool m_ExpandMain = true;
    bool m_ExpandBase = true;
    bool m_ExpandSurface = false;
    bool m_ExpandLighting = false;
    bool m_ExpandEmission = false;
    bool m_ExpandOutline = false;
    bool m_ExpandDissolve = true;
    bool m_ExpandTriplanar = false;
    bool m_ExpandAdvanced = false;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material targetMat = materialEditor.target as Material;

        DrawHeaderLabel("TOON UBER SHADER HQ");
        DrawSplitter();

        m_ExpandMain = DrawSection(Styles.mainSettingsText, m_ExpandMain, () => DrawMainSettings(materialEditor, properties, targetMat));
        m_ExpandBase = DrawSection(Styles.baseSurfaceText, m_ExpandBase, () => DrawBaseSurface(materialEditor, properties, targetMat));
        m_ExpandSurface = DrawSection(Styles.surfaceOptionsText, m_ExpandSurface, () => DrawSurfaceOptions(materialEditor, properties, targetMat));
        m_ExpandLighting = DrawSection(Styles.lightingText, m_ExpandLighting, () => DrawLighting(materialEditor, properties));
        m_ExpandEmission = DrawSection(Styles.emissionText, m_ExpandEmission, () => DrawEmission(materialEditor, properties));
        m_ExpandOutline = DrawSection(Styles.outlineText, m_ExpandOutline, () => DrawOutline(materialEditor, properties));
        m_ExpandDissolve = DrawSection(Styles.dissolveText, m_ExpandDissolve, () => DrawDissolve(materialEditor, properties, targetMat));
        m_ExpandTriplanar = DrawSection(Styles.triplanarText, m_ExpandTriplanar, () => DrawTriplanarMasking(materialEditor, properties));
        m_ExpandAdvanced = DrawSection(Styles.advancedText, m_ExpandAdvanced, () => DrawAdvanced(materialEditor, properties));

        EditorGUILayout.Space();
        materialEditor.RenderQueueField();
        materialEditor.EnableInstancingField();
        materialEditor.DoubleSidedGIField();
    }

    void DrawMainSettings(MaterialEditor editor, MaterialProperty[] props, Material targetMat)
    {
        EditorGUI.BeginChangeCheck();
        MaterialProperty renderMode = FindProperty("_RenderMode", props);
        editor.ShaderProperty(renderMode, "Render Mode");

        MaterialProperty cullMode = FindProperty("_CullMode", props);
        editor.ShaderProperty(cullMode, "Culling Mode");

        if (EditorGUI.EndChangeCheck())
        {
            SetupRenderMode(targetMat, (int)renderMode.floatValue);
        }

        EditorGUILayout.Space();
        MaterialProperty surfaceType = FindProperty("_SurfaceType", props);
        string[] surfaceOptions = { "Standard Opaque", "Metallic", "Foliage", "Bling / Sparkle", "Cosmic Nebula" };

        EditorGUI.BeginChangeCheck();
        int selection = EditorGUILayout.Popup("Surface Material Type", (int)surfaceType.floatValue, surfaceOptions);
        if (EditorGUI.EndChangeCheck())
        {
            surfaceType.floatValue = selection;
            UpdateSurfaceKeywords(targetMat, selection);
        }
    }

    void DrawBaseSurface(MaterialEditor editor, MaterialProperty[] props, Material targetMat)
    {
        MaterialProperty baseMap = FindProperty("_BaseMap", props);
        MaterialProperty baseColor = FindProperty("_BaseColor", props);
        editor.TexturePropertySingleLine(new GUIContent("Albedo & Color"), baseMap, baseColor);

        MaterialProperty bumpMap = FindProperty("_BumpMap", props);
        MaterialProperty bumpScale = FindProperty("_BumpScale", props);
        editor.TexturePropertySingleLine(new GUIContent("Normal Map"), bumpMap, bumpScale);

        MaterialProperty renderMode = FindProperty("_RenderMode", props);
        if (renderMode.floatValue > 0.5f)
        {
            MaterialProperty cutoff = FindProperty("_Cutoff", props);
            editor.ShaderProperty(cutoff, "Alpha Cutoff");
            targetMat.EnableKeyword("_ALPHACLIP_ON");
        }
        else
        {
            targetMat.DisableKeyword("_ALPHACLIP_ON");
        }
    }

    void DrawSurfaceOptions(MaterialEditor editor, MaterialProperty[] props, Material targetMat)
    {
        int surfaceType = (int)FindProperty("_SurfaceType", props).floatValue;

        if (surfaceType == 2)
        {
            DrawSubtitle("Foliage Settings");
            editor.ShaderProperty(FindProperty("_WindFrequency", props), "Wind Frequency");
            editor.ShaderProperty(FindProperty("_WindAmplitude", props), "Wind Amplitude");
            editor.ShaderProperty(FindProperty("_WindDirection", props), "Wind Direction");
            editor.ShaderProperty(FindProperty("_TranslucencyColor", props), "Translucency Color");
            editor.ShaderProperty(FindProperty("_TranslucencyStrength", props), "Translucency Strength");
        }
        else if (surfaceType == 3)
        {
            DrawSubtitle("Bling Settings");
            editor.ShaderProperty(FindProperty("_BlingWorldSpace", props), "World Space Noise");
            editor.ShaderProperty(FindProperty("_BlingColor", props), "Sparkle Color");
            editor.ShaderProperty(FindProperty("_BlingIntensity", props), "Intensity");
            editor.ShaderProperty(FindProperty("_BlingScale", props), "Noise Scale");
            editor.ShaderProperty(FindProperty("_BlingSpeed", props), "Animation Speed");
            editor.ShaderProperty(FindProperty("_BlingThreshold", props), "Threshold");
            editor.ShaderProperty(FindProperty("_BlingFresnelPower", props), "Fresnel Falloff");
        }
        else if (surfaceType == 4)
        {
            DrawSubtitle("Cosmic Nebula Settings");
            editor.ShaderProperty(FindProperty("_CosmicAmbientColor", props), "Cosmic Ambient");
            editor.ShaderProperty(FindProperty("_TriplanarSharpness", props), "Triplanar Blend Sharpness");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Layer 1", EditorStyles.miniBoldLabel);
            editor.TexturePropertySingleLine(new GUIContent("Nebula Texture 1"), FindProperty("_CosmicTex1", props), FindProperty("_CosmicColor1", props));
            editor.ShaderProperty(FindProperty("_CosmicScale1", props), "Scale");
            editor.ShaderProperty(FindProperty("_CosmicScrollSpeed1", props), "Scroll Speed");
            editor.ShaderProperty(FindProperty("_CosmicParallaxDepth1", props), "Parallax Depth");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Layer 2", EditorStyles.miniBoldLabel);
            editor.TexturePropertySingleLine(new GUIContent("Nebula Texture 2"), FindProperty("_CosmicTex2", props), FindProperty("_CosmicColor2", props));
            editor.ShaderProperty(FindProperty("_CosmicScale2", props), "Scale");
            editor.ShaderProperty(FindProperty("_CosmicScrollSpeed2", props), "Scroll Speed");
            editor.ShaderProperty(FindProperty("_CosmicParallaxDepth2", props), "Parallax Depth");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Star Field", EditorStyles.miniBoldLabel);
            editor.TexturePropertySingleLine(new GUIContent("Stars Texture"), FindProperty("_StarfieldTex", props), FindProperty("_StarfieldColor", props));
            editor.ShaderProperty(FindProperty("_StarfieldScale", props), "Scale");
            editor.ShaderProperty(FindProperty("_StarfieldScrollSpeed", props), "Scroll Speed");
            editor.ShaderProperty(FindProperty("_StarfieldParallaxDepth", props), "Parallax Depth");
        }

        EditorGUILayout.Space();
        DrawSubtitle("Stylized Specular");
        MaterialProperty specToggle = FindProperty("_SpecularToggle", props);
        editor.ShaderProperty(specToggle, "Enable Specular");
        if (specToggle.floatValue > 0.5f)
        {
            targetMat.EnableKeyword("_SPECULAR_ON");
            EditorGUI.indentLevel++;
            editor.ShaderProperty(FindProperty("_SpecuColor", props), "Color");
            editor.ShaderProperty(FindProperty("_Brightness", props), "Intensity");
            editor.ShaderProperty(FindProperty("_Offset", props), "Size");
            editor.ShaderProperty(FindProperty("_SpecularSmoothness", props), "Falloff Softness");
            EditorGUI.indentLevel--;
        }
        else
        {
            targetMat.DisableKeyword("_SPECULAR_ON");
        }

        EditorGUILayout.Space();
        DrawSubtitle("Rim Light");
        MaterialProperty rimToggle = FindProperty("_RimLightToggle", props);
        editor.ShaderProperty(rimToggle, "Enable Rim Light");
        if (rimToggle.floatValue > 0.5f)
        {
            targetMat.EnableKeyword("_RIMLIGHT_ON");
            EditorGUI.indentLevel++;
            editor.ShaderProperty(FindProperty("_RimLightColor", props), "Color");
            editor.ShaderProperty(FindProperty("_RimLightPower", props), "Power");
            editor.ShaderProperty(FindProperty("_RimLightSmoothness", props), "Smoothness");
            EditorGUI.indentLevel--;
        }
        else
        {
            targetMat.DisableKeyword("_RIMLIGHT_ON");
        }
    }

    void DrawLighting(MaterialEditor editor, MaterialProperty[] props)
    {
        MaterialProperty fakeMode = FindProperty("_FakeLightMode", props);
        editor.ShaderProperty(fakeMode, "Use Fake Light Source");
        if (fakeMode.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            editor.ShaderProperty(FindProperty("_FakeLightColor", props), "Fake Light Color");
            editor.ShaderProperty(FindProperty("_FakeLightDirection", props), "Fake Light Direction");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();

        DrawSubtitle("Main Light Ramp");
        editor.ShaderProperty(FindProperty("_ShadowTint", props), "Shadow Tint");
        editor.ShaderProperty(FindProperty("_MidtoneColor", props), "Midtone Color");
        editor.ShaderProperty(FindProperty("_ShadowThreshold", props), "Shadow Threshold");
        editor.ShaderProperty(FindProperty("_MidtoneThreshold", props), "Midtone Threshold");
        editor.ShaderProperty(FindProperty("_RampSmoothness", props), "Ramp Smoothness");
        editor.ShaderProperty(FindProperty("_AmbientColor", props), "Ambient Overlay");

        EditorGUILayout.Space();
        DrawSubtitle("Additional Lights Ramp");
        editor.ShaderProperty(FindProperty("_AddLightShadowTint", props), "Shadow Tint");
        editor.ShaderProperty(FindProperty("_AddLightMidtoneColor", props), "Midtone Color");
        editor.ShaderProperty(FindProperty("_AddLightShadowThreshold", props), "Shadow Threshold");
        editor.ShaderProperty(FindProperty("_AddLightMidtoneThreshold", props), "Midtone Threshold");
        editor.ShaderProperty(FindProperty("_AddLightRampSmoothness", props), "Ramp Smoothness");
    }

    void DrawEmission(MaterialEditor editor, MaterialProperty[] props)
    {
        MaterialProperty emissionMode = FindProperty("_EmissionMode", props);
        editor.ShaderProperty(emissionMode, "Enable Emission");
        if (emissionMode.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            editor.TexturePropertySingleLine(new GUIContent("Emission Map"), FindProperty("_EmissionMap", props), FindProperty("_EmissionColor", props));
            EditorGUI.indentLevel--;
        }
    }

    void DrawOutline(MaterialEditor editor, MaterialProperty[] props)
    {
        MaterialProperty outlineMode = FindProperty("_OutlineMode", props);
        editor.ShaderProperty(outlineMode, "Outline Mode");

        if (outlineMode.floatValue == 1)
        {
            EditorGUI.indentLevel++;
            editor.ShaderProperty(FindProperty("_FresnelOutlineColor", props), "Color");
            editor.ShaderProperty(FindProperty("_FresnelOutlineWidth", props), "Width");
            editor.ShaderProperty(FindProperty("_FresnelOutlinePower", props), "Fresnel Power");
            editor.ShaderProperty(FindProperty("_FresnelOutlineSharpness", props), "Edge Sharpness");

            EditorGUILayout.Space();
            MaterialProperty glintToggle = FindProperty("_GlintToggle", props);
            editor.ShaderProperty(glintToggle, "Enable Glint Animation");
            if (glintToggle.floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProperty("_GlintColor", props), "Glint Color");
                editor.ShaderProperty(FindProperty("_GlintScale", props), "Scale");
                editor.ShaderProperty(FindProperty("_GlintSpeed", props), "Speed");
                editor.ShaderProperty(FindProperty("_GlintThreshold", props), "Threshold");
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }
        else if (outlineMode.floatValue == 2)
        {
            EditorGUI.indentLevel++;
            editor.ShaderProperty(FindProperty("_OutlineColor", props), "Color");
            editor.ShaderProperty(FindProperty("_OutlineWidth", props), "Width");

            MaterialProperty scaleDist = FindProperty("_OutlineScaleWithDistance", props);
            editor.ShaderProperty(scaleDist, "Scale with Distance");
            if (scaleDist.floatValue > 0.5f)
            {
                editor.ShaderProperty(FindProperty("_DistanceFadeStart", props), "Fade Start Dist");
                editor.ShaderProperty(FindProperty("_DistanceFadeEnd", props), "Fade End Dist");
            }
            EditorGUI.indentLevel--;
        }
    }

    void DrawDissolve(MaterialEditor editor, MaterialProperty[] props, Material targetMat)
    {
        EditorGUI.BeginChangeCheck();
        MaterialProperty dissolveOn = FindProperty("_EnableDissolve", props);
        editor.ShaderProperty(dissolveOn, "Enable Dissolve System");

        if (EditorGUI.EndChangeCheck())
        {
            if (dissolveOn.floatValue > 0.5f) targetMat.EnableKeyword("_DISSOLVE_ON");
            else targetMat.DisableKeyword("_DISSOLVE_ON");
        }

        if (dissolveOn.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            MaterialProperty dissolveType = FindProperty("_DissolveType", props);

            EditorGUI.BeginChangeCheck();
            editor.ShaderProperty(dissolveType, "Dissolve Type");
            if (EditorGUI.EndChangeCheck())
            {
                UpdateDissolveKeywords(targetMat, (int)dissolveType.floatValue);
            }

            int type = (int)dissolveType.floatValue;

            EditorGUILayout.Space();
            DrawSubtitle("Method Configuration");

            if (type == 0)
            {
                editor.TexturePropertySingleLine(new GUIContent("Noise Texture"), FindProperty("_NoiseTex", props));
                editor.ShaderProperty(FindProperty("_NoiseScale", props), "Noise Scale");
            }
            else
            {
                editor.TexturePropertySingleLine(new GUIContent("Edge Noise (Optional)"), FindProperty("_NoiseTex", props));
                editor.ShaderProperty(FindProperty("_NoiseScale", props), "Noise Scale");
                editor.ShaderProperty(FindProperty("_NoiseStrength", props), "Noise Distortion Strength");

                if (type == 1 || type == 5)
                {
                    editor.ShaderProperty(FindProperty("_DissolveDirection", props), "Direction (XYZ)");
                    EditorGUI.BeginChangeCheck();
                    MaterialProperty localSpace = FindProperty("_UseLocalSpace", props);
                    editor.ShaderProperty(localSpace, "Use Local Space");
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (localSpace.floatValue > 0.5f) targetMat.EnableKeyword("_DISSOLVE_LOCALSPACE_ON");
                        else targetMat.DisableKeyword("_DISSOLVE_LOCALSPACE_ON");
                    }
                }
                else if (type == 2)
                {
                    editor.ShaderProperty(FindProperty("_DissolveDirection", props), "Center Point (XYZ)");
                    editor.ShaderProperty(FindProperty("_RadialDirection", props), "Invert Direction");
                    editor.ShaderProperty(FindProperty("_MaxDissolveDistance", props), "Max Radius");
                }
                else if (type == 3)
                {
                    editor.ShaderProperty(FindProperty("_PatternType", props), "Pattern Shape");
                    editor.ShaderProperty(FindProperty("_PatternFrequency", props), "Pattern Frequency");
                }
            }

            EditorGUILayout.Space();
            DrawSubtitle("Look & Feel");

            MaterialProperty hologramMode = FindProperty("_EnableHologramReveal", props);
            editor.ShaderProperty(hologramMode, "Hologram Reveal Mode");

            if (hologramMode.floatValue > 0.5f)
            {
                targetMat.EnableKeyword("_HOLOGRAM_REVEAL_ON");
                EditorGUI.indentLevel++;
                editor.TexturePropertySingleLine(new GUIContent("Hologram Pattern"), FindProperty("_HologramPatternTex", props), FindProperty("_HologramEmissionColor", props));
                editor.ShaderProperty(FindProperty("_HologramPatternScale", props), "Pattern Scale");
                editor.ShaderProperty(FindProperty("_HologramFlickerSpeed", props), "Flicker Speed");
                editor.ShaderProperty(FindProperty("_RevealProgress", props), "Reveal Progress (-1 to 2)");
                EditorGUI.indentLevel--;
            }
            else
            {
                targetMat.DisableKeyword("_HOLOGRAM_REVEAL_ON");
                editor.ShaderProperty(FindProperty("_DissolveThreshold", props), "Dissolve Threshold");
            }

            editor.ShaderProperty(FindProperty("_UseTimeAnimation", props), "Animate Time Sine");
            if (FindProperty("_UseTimeAnimation", props).floatValue > 0.5f)
            {
                editor.ShaderProperty(FindProperty("_TimeScale", props), "Time Speed");
            }

            EditorGUILayout.Space();
            if (type != 4)
            {
                editor.ShaderProperty(FindProperty("_DissolveEdgeWidth", props), "Edge Width");
                editor.ShaderProperty(FindProperty("_DissolveEdgeColor", props), "Edge Color (HDR)");
            }
            else
            {
                editor.ShaderProperty(FindProperty("_AlphaFadeRange", props), "Alpha Fade Softness");
            }

            EditorGUILayout.Space();
            DrawSubtitle("Vertex Manipulation");

            EditorGUI.BeginChangeCheck();
            MaterialProperty vDisp = FindProperty("_EnableVertexDisplacement", props);
            editor.ShaderProperty(vDisp, "Enable Edge Displacement");
            if (EditorGUI.EndChangeCheck())
            {
                if (vDisp.floatValue > 0.5f) targetMat.EnableKeyword("_VERTEX_DISPLACEMENT_ON");
                else targetMat.DisableKeyword("_VERTEX_DISPLACEMENT_ON");
            }

            if (vDisp.floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                MaterialProperty satDisp = FindProperty("_UseSaturateDisplacement", props);
                editor.ShaderProperty(satDisp, "Saturate Displacement");
                if (EditorGUI.EndChangeCheck())
                {
                    if (satDisp.floatValue > 0.5f) targetMat.EnableKeyword("_DISPLACEMENT_SATURATE_ON");
                    else targetMat.DisableKeyword("_DISPLACEMENT_SATURATE_ON");
                }

                editor.ShaderProperty(FindProperty("_VertexDisplacement", props), "Displacement Amount");
                editor.ShaderProperty(FindProperty("_BounceWaveWidth", props), "Wave Width");
                EditorGUI.indentLevel--;
            }

            if (type == 5)
            {
                EditorGUILayout.Space();
                DrawSubtitle("Shatter Physics");
                EditorGUI.BeginChangeCheck();
                MaterialProperty shatterFx = FindProperty("_EnableShatterEffect", props);
                editor.ShaderProperty(shatterFx, "Enable Shatter Logic");
                if (EditorGUI.EndChangeCheck())
                {
                    if (shatterFx.floatValue > 0.5f) targetMat.EnableKeyword("_SHATTER_EFFECT_ON");
                    else targetMat.DisableKeyword("_SHATTER_EFFECT_ON");
                }

                if (shatterFx.floatValue > 0.5f)
                {
                    EditorGUI.indentLevel++;
                    editor.ShaderProperty(FindProperty("_ShatterStrength", props), "Strength");
                    editor.ShaderProperty(FindProperty("_ShatterLiftSpeed", props), "Lift Speed");
                    editor.ShaderProperty(FindProperty("_ShatterOffsetStrength", props), "Random Offset");
                    editor.ShaderProperty(FindProperty("_ShatterTriggerRange", props), "Trigger Range");
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
        }
    }

    void DrawTriplanarMasking(MaterialEditor editor, MaterialProperty[] props)
    {
        MaterialProperty maskToggle = FindProperty("_MaskTriplanarToggle", props);
        editor.ShaderProperty(maskToggle, "Enable Masking System");

        if (maskToggle.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            editor.TexturePropertySingleLine(new GUIContent("Triplanar Pattern"), FindProperty("_MaskTriplanarTex", props));
            editor.ShaderProperty(FindProperty("_MaskTriplanarScale", props), "Global Scale");
            editor.ShaderProperty(FindProperty("_MaskTriplanarBlend", props), "Blend Intensity");
            editor.ShaderProperty(FindProperty("_MaskTriplanarSharpness", props), "Triplanar Sharpness");
            editor.ShaderProperty(FindProperty("_MaskDivisions", props), "Grid Divisions");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mask Colors (Grid Index 0-15)", EditorStyles.boldLabel);

            for (int i = 0; i < 16; i += 4)
            {
                GUILayout.BeginHorizontal();
                editor.ShaderProperty(FindProperty("_MaskColor" + i, props), GUIContent.none);
                editor.ShaderProperty(FindProperty("_MaskColor" + (i + 1), props), GUIContent.none);
                editor.ShaderProperty(FindProperty("_MaskColor" + (i + 2), props), GUIContent.none);
                editor.ShaderProperty(FindProperty("_MaskColor" + (i + 3), props), GUIContent.none);
                GUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
    }

    void DrawAdvanced(MaterialEditor editor, MaterialProperty[] props)
    {
        editor.ShaderProperty(FindProperty("_SrcBlend", props), "Source Blend");
        editor.ShaderProperty(FindProperty("_DstBlend", props), "Destination Blend");
        editor.ShaderProperty(FindProperty("_ZWrite", props), "ZWrite");
        editor.ShaderProperty(FindProperty("_ZTest", props), "ZTest");
    }

    void UpdateSurfaceKeywords(Material targetMat, int selection)
    {
        targetMat.DisableKeyword("_SURFACETYPE_OPAQUE");
        targetMat.DisableKeyword("_SURFACETYPE_METALLIC");
        targetMat.DisableKeyword("_SURFACETYPE_FOLIAGE");
        targetMat.DisableKeyword("_SURFACETYPE_BLING");
        targetMat.DisableKeyword("_SURFACETYPE_COSMIC");

        switch (selection)
        {
            case 0: targetMat.EnableKeyword("_SURFACETYPE_OPAQUE"); break;
            case 1: targetMat.EnableKeyword("_SURFACETYPE_METALLIC"); break;
            case 2: targetMat.EnableKeyword("_SURFACETYPE_FOLIAGE"); break;
            case 3: targetMat.EnableKeyword("_SURFACETYPE_BLING"); break;
            case 4: targetMat.EnableKeyword("_SURFACETYPE_COSMIC"); break;
        }
    }

    void UpdateDissolveKeywords(Material material, int typeIndex)
    {
        material.DisableKeyword("_DISSOLVETYPE_NOISE");
        material.DisableKeyword("_DISSOLVETYPE_LINEAR");
        material.DisableKeyword("_DISSOLVETYPE_RADIAL");
        material.DisableKeyword("_DISSOLVETYPE_PATTERN");
        material.DisableKeyword("_DISSOLVETYPE_ALPHA_BLEND");
        material.DisableKeyword("_DISSOLVETYPE_SHATTER");

        switch (typeIndex)
        {
            case 0: material.EnableKeyword("_DISSOLVETYPE_NOISE"); break;
            case 1: material.EnableKeyword("_DISSOLVETYPE_LINEAR"); break;
            case 2: material.EnableKeyword("_DISSOLVETYPE_RADIAL"); break;
            case 3: material.EnableKeyword("_DISSOLVETYPE_PATTERN"); break;
            case 4: material.EnableKeyword("_DISSOLVETYPE_ALPHA_BLEND"); break;
            case 5: material.EnableKeyword("_DISSOLVETYPE_SHATTER"); break;
        }
    }

    void SetupRenderMode(Material material, int renderMode)
    {
        switch (renderMode)
        {
            case 0:
                material.SetOverrideTag("RenderType", "Opaque");
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.renderQueue = -1;
                break;
            case 1:
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.renderQueue = (int)RenderQueue.AlphaTest;
                break;
            case 2:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.renderQueue = (int)RenderQueue.Transparent;
                break;
        }
    }

    bool DrawSection(GUIContent title, bool expanded, System.Action drawer)
    {
        var style = new GUIStyle("ShurikenModuleTitle")
        {
            font = EditorStyles.label.font,
            border = new RectOffset(15, 7, 4, 4),
            fixedHeight = 22,
            contentOffset = new Vector2(20f, -2f)
        };

        var rect = GUILayoutUtility.GetRect(16f, 22f, style);
        GUI.Box(rect, title, style);

        var e = Event.current;
        var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
        if (e.type == EventType.Repaint)
        {
            EditorStyles.foldout.Draw(toggleRect, false, false, expanded, false);
        }

        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            expanded = !expanded;
            e.Use();
        }

        if (expanded)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            drawer?.Invoke();
            GUILayout.EndVertical();
        }

        return expanded;
    }

    void DrawSubtitle(string text)
    {
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    }

    void DrawHeaderLabel(string text)
    {
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        GUILayout.Label(text, titleStyle);
    }

    void DrawSplitter()
    {
        var rect = GUILayoutUtility.GetRect(1f, 1f);
        rect.y += 5;
        rect.height = 2;
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1));
        EditorGUILayout.Space(8);
    }
}