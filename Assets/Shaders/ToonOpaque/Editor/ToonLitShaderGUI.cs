using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System;

public class ToonLitShaderGUI : ShaderGUI
{
    private MaterialEditor editor;
    private MaterialProperty[] properties;
    private Material target;

    // --- Enums for clarity ---
    private enum RenderMode { Opaque, Cutout, Transparent }
    private enum ToonStyle { Smooth, Hard }

    // --- Foldout states ---
    private static bool showRenderStates = true;
    private static bool showBaseProperties = true;
    private static bool showEmission = true;
    private static bool showLighting = true;
    private static bool showIndirectLighting = true;

    // --- Cached MaterialProperties ---
    private MaterialProperty renderMode, srcBlend, dstBlend, zWrite, cullMode;
    private MaterialProperty baseMap, baseColor, bumpMap, bumpScale, cutoff;
    private MaterialProperty emissionToggle, emissionColor, emissionMap;
    private MaterialProperty forceFakeLight, fakeLightMode, fakeLightColor, fakeLightDirection;
    private MaterialProperty ambientColor, maxBrightness;
    private MaterialProperty indirectSpecular, indirectSpecularIntensity;
    private MaterialProperty toonStyle, shadowTint, midtoneColor, shadowThreshold, midtoneThreshold, toonRampSmoothness;
    private MaterialProperty addLightShadowTint, addLightMidtoneColor, addLightShadowThreshold, addLightMidtoneThreshold, addLightRampSmoothness;

    // --- Static GUIContent for performance and reusability ---
    private static class Content
    {
        public static readonly GUIContent renderMode = new GUIContent("Render Mode", "Controls how the material is rendered.\nOpaque: Solid, no transparency.\nCutout: Portions are fully transparent or fully opaque based on alpha.\nTransparent: Blends with the background based on alpha.");
        public static readonly GUIContent albedo = new GUIContent("Albedo", "The main color map (RGB) and transparency (A).");
        public static readonly GUIContent normalMap = new GUIContent("Normal Map", "Provides surface detail and lighting variations.");
        public static readonly GUIContent alphaCutoff = new GUIContent("Alpha Cutoff", "The alpha value below which pixels are discarded in Cutout mode.");
        public static readonly GUIContent emission = new GUIContent("Emission", "Controls light emitted from the material's surface.");
        public static readonly GUIContent forceFakeLight = new GUIContent("Force Fake Light", "Always use the fake light direction and color, ignoring scene lights.");
        public static readonly GUIContent fakeLightFallback = new GUIContent("Enable Fake Light Fallback", "Use the fake light only when no main light exists in the scene.");
        public static readonly GUIContent ambientColor = new GUIContent("Ambient Color", "The color of the darkest areas. Alpha channel controls blending between GI ambient and this custom color.");
        public static readonly GUIContent toonStyle = new GUIContent("Style", "Smooth: Soft transition between light and shadow.\nHard: Sharp, cel-shaded transition.");
        public static readonly GUIContent environmentReflections = new GUIContent("Enable Environment Reflections", "Allows the material to reflect the skybox or reflection probes.");

        public static readonly GUIContent mainLightHeader = new GUIContent("Main Light Toon Shading");
        public static readonly GUIContent additionalLightsHeader = new GUIContent("Additional Lights Toon Shading");
    }

    private static class PropNames
    {
        public static readonly string RenderMode = "_RenderMode";
        public static readonly string SrcBlend = "_SrcBlend";
        public static readonly string DstBlend = "_DstBlend";
        public static readonly string ZWrite = "_ZWrite";
        public static readonly string CullMode = "_CullMode";

        public static readonly string BaseMap = "_BaseMap";
        public static readonly string BaseColor = "_BaseColor";
        public static readonly string BumpMap = "_BumpMap";
        public static readonly string BumpScale = "_BumpScale";
        public static readonly string Cutoff = "_Cutoff";

        public static readonly string EmissionToggle = "_EmissionToggle";
        public static readonly string EmissionColor = "_EmissionColor";
        public static readonly string EmissionMap = "_EmissionMap";

        public static readonly string ForceFakeLight = "_ForceFakeLight";
        public static readonly string FakeLightMode = "_FakeLightMode";
        public static readonly string FakeLightColor = "_FakeLightColor";
        public static readonly string FakeLightDirection = "_FakeLightDirection";
        public static readonly string AmbientColor = "_AmbientColor";
        public static readonly string MaxBrightness = "_MaxBrightness";

        public static readonly string IndirectSpecular = "_IndirectSpecular";
        public static readonly string IndirectSpecularIntensity = "_IndirectSpecularIntensity";

        public static readonly string ToonStyle = "_ToonStyle";
        public static readonly string ShadowTint = "_ShadowTint";
        public static readonly string MidtoneColor = "_MidtoneColor";
        public static readonly string ShadowThreshold = "_ShadowThreshold";
        public static readonly string MidtoneThreshold = "_MidtoneThreshold";
        public static readonly string ToonRampSmoothness = "_ToonRampSmoothness";

        public static readonly string AddLightShadowTint = "_AddLightShadowTint";
        public static readonly string AddLightMidtoneColor = "_AddLightMidtoneColor";
        public static readonly string AddLightShadowThreshold = "_AddLightShadowThreshold";
        public static readonly string AddLightMidtoneThreshold = "_AddLightMidtoneThreshold";
        public static readonly string AddLightRampSmoothness = "_AddLightRampSmoothness";
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        this.editor = materialEditor;
        this.properties = properties;
        this.target = materialEditor.target as Material;

        FindProperties();

        EditorGUI.BeginChangeCheck();
        {
            DrawRenderStates();
            DrawBaseProperties();
            DrawEmission();
            DrawLighting();
            DrawIndirectLighting();
        }
        if (EditorGUI.EndChangeCheck())
        {
            SetKeywordsAndRenderStates();
        }
    }

    private void FindProperties()
    {
        renderMode = FindProperty(PropNames.RenderMode, properties);
        srcBlend = FindProperty(PropNames.SrcBlend, properties);
        dstBlend = FindProperty(PropNames.DstBlend, properties);
        zWrite = FindProperty(PropNames.ZWrite, properties);
        cullMode = FindProperty(PropNames.CullMode, properties);

        baseMap = FindProperty(PropNames.BaseMap, properties);
        baseColor = FindProperty(PropNames.BaseColor, properties);
        bumpMap = FindProperty(PropNames.BumpMap, properties);
        bumpScale = FindProperty(PropNames.BumpScale, properties);
        cutoff = FindProperty(PropNames.Cutoff, properties);

        emissionToggle = FindProperty(PropNames.EmissionToggle, properties);
        emissionColor = FindProperty(PropNames.EmissionColor, properties);
        emissionMap = FindProperty(PropNames.EmissionMap, properties);

        forceFakeLight = FindProperty(PropNames.ForceFakeLight, properties);
        fakeLightMode = FindProperty(PropNames.FakeLightMode, properties);
        fakeLightColor = FindProperty(PropNames.FakeLightColor, properties);
        fakeLightDirection = FindProperty(PropNames.FakeLightDirection, properties);
        ambientColor = FindProperty(PropNames.AmbientColor, properties);
        maxBrightness = FindProperty(PropNames.MaxBrightness, properties);

        indirectSpecular = FindProperty(PropNames.IndirectSpecular, properties);
        indirectSpecularIntensity = FindProperty(PropNames.IndirectSpecularIntensity, properties);

        toonStyle = FindProperty(PropNames.ToonStyle, properties);
        shadowTint = FindProperty(PropNames.ShadowTint, properties);
        midtoneColor = FindProperty(PropNames.MidtoneColor, properties);
        shadowThreshold = FindProperty(PropNames.ShadowThreshold, properties);
        midtoneThreshold = FindProperty(PropNames.MidtoneThreshold, properties);
        toonRampSmoothness = FindProperty(PropNames.ToonRampSmoothness, properties);

        addLightShadowTint = FindProperty(PropNames.AddLightShadowTint, properties);
        addLightMidtoneColor = FindProperty(PropNames.AddLightMidtoneColor, properties);
        addLightShadowThreshold = FindProperty(PropNames.AddLightShadowThreshold, properties);
        addLightMidtoneThreshold = FindProperty(PropNames.AddLightMidtoneThreshold, properties);
        addLightRampSmoothness = FindProperty(PropNames.AddLightRampSmoothness, properties);
    }

    private void DrawRenderStates()
    {
        showRenderStates = EditorGUILayout.BeginFoldoutHeaderGroup(showRenderStates, "Render States");
        if (showRenderStates)
        {
            editor.ShaderProperty(renderMode, Content.renderMode);

            var currentMode = (RenderMode)renderMode.floatValue;
            if (currentMode == RenderMode.Transparent)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    editor.ShaderProperty(srcBlend, "Source Blend");
                    editor.ShaderProperty(dstBlend, "Destination Blend");
                }
            }

            editor.ShaderProperty(zWrite, "ZWrite");
            editor.ShaderProperty(cullMode, "Culling Mode");

            EditorGUILayout.Space();
            editor.RenderQueueField();
            editor.EnableInstancingField();
            editor.DoubleSidedGIField();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawBaseProperties()
    {
        showBaseProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showBaseProperties, "Base Properties");
        if (showBaseProperties)
        {
            editor.TexturePropertySingleLine(Content.albedo, baseMap, baseColor);
            editor.TexturePropertySingleLine(Content.normalMap, bumpMap, bumpScale);
            editor.TextureScaleOffsetProperty(baseMap);

            if ((RenderMode)renderMode.floatValue == RenderMode.Cutout)
            {
                editor.ShaderProperty(cutoff, Content.alphaCutoff);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawEmission()
    {
        showEmission = EditorGUILayout.BeginFoldoutHeaderGroup(showEmission, "Emission");
        if (showEmission)
        {
            editor.ShaderProperty(emissionToggle, "Enable Emission");
            if (IsPropertyEnabled(emissionToggle))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    editor.TexturePropertySingleLine(Content.emission, emissionMap, emissionColor);
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawLighting()
    {
        showLighting = EditorGUILayout.BeginFoldoutHeaderGroup(showLighting, "Lighting");
        if (showLighting)
        {
            editor.ShaderProperty(forceFakeLight, Content.forceFakeLight);
            bool isForceFakeLight = IsPropertyEnabled(forceFakeLight);

            if (!isForceFakeLight)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    editor.ShaderProperty(fakeLightMode, Content.fakeLightFallback);
                }
            }

            if (isForceFakeLight || IsPropertyEnabled(fakeLightMode))
            {
                using (new EditorGUI.IndentLevelScope(2))
                {
                    editor.ShaderProperty(fakeLightColor, "Color");
                    editor.ShaderProperty(fakeLightDirection, "Direction");
                }
            }

            editor.ShaderProperty(ambientColor, Content.ambientColor);
            editor.ShaderProperty(maxBrightness, "Max Brightness");

            EditorGUILayout.Space();
            DrawToonSettings(Content.mainLightHeader, toonStyle, shadowTint, midtoneColor, shadowThreshold, midtoneThreshold, toonRampSmoothness);

            EditorGUILayout.Space();
            DrawToonSettings(Content.additionalLightsHeader, null, addLightShadowTint, addLightMidtoneColor, addLightShadowThreshold, addLightMidtoneThreshold, addLightRampSmoothness);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawToonSettings(GUIContent header, MaterialProperty styleProp, MaterialProperty tintProp, MaterialProperty midtoneProp, MaterialProperty shadowT, MaterialProperty midtoneT, MaterialProperty smoothnessProp)
    {
        EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
        using (new EditorGUI.IndentLevelScope())
        {
            if (styleProp != null)
            {
                editor.ShaderProperty(styleProp, Content.toonStyle);
            }

            editor.ShaderProperty(tintProp, "Shadow Tint");
            editor.ShaderProperty(midtoneProp, "Mid-tone Color");
            editor.ShaderProperty(shadowT, "Shadow Threshold");
            editor.ShaderProperty(midtoneT, "Mid-tone Threshold");

            bool isSmooth = (styleProp != null && (ToonStyle)styleProp.floatValue == ToonStyle.Smooth) || styleProp == null;
            if (isSmooth)
            {
                editor.ShaderProperty(smoothnessProp, "Ramp Smoothness");
            }
        }
    }

    private void DrawIndirectLighting()
    {
        showIndirectLighting = EditorGUILayout.BeginFoldoutHeaderGroup(showIndirectLighting, "Indirect Lighting");
        if (showIndirectLighting)
        {
            editor.ShaderProperty(indirectSpecular, Content.environmentReflections);
            if (IsPropertyEnabled(indirectSpecular))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    editor.ShaderProperty(indirectSpecularIntensity, "Intensity");
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void SetKeywordsAndRenderStates()
    {
        foreach (var obj in editor.targets)
        {
            var mat = (Material)obj;

            var currentMode = (RenderMode)mat.GetFloat(PropNames.RenderMode);
            switch (currentMode)
            {
                case RenderMode.Opaque:
                    mat.SetOverrideTag("RenderType", "Opaque");
                    mat.renderQueue = (int)RenderQueue.Geometry;
                    mat.SetInt(PropNames.SrcBlend, (int)BlendMode.One);
                    mat.SetInt(PropNames.DstBlend, (int)BlendMode.Zero);
                    mat.SetInt(PropNames.ZWrite, 1);
                    break;
                case RenderMode.Cutout:
                    mat.SetOverrideTag("RenderType", "TransparentCutout");
                    mat.renderQueue = (int)RenderQueue.AlphaTest;
                    mat.SetInt(PropNames.SrcBlend, (int)BlendMode.One);
                    mat.SetInt(PropNames.DstBlend, (int)BlendMode.Zero);
                    mat.SetInt(PropNames.ZWrite, 1);
                    break;
                case RenderMode.Transparent:
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                    // ZWrite is user-configurable in transparent mode
                    break;
            }

            SetKeyword(mat, "_ALPHACLIP_ON", currentMode == RenderMode.Cutout);
            SetKeyword(mat, "_NORMALMAP_ON", mat.GetTexture(PropNames.BumpMap) != null);
            SetKeyword(mat, "_EMISSION_ON", IsPropertyEnabled(mat, PropNames.EmissionToggle));

            bool isForceFakeLight = IsPropertyEnabled(mat, PropNames.ForceFakeLight);
            bool isFakeLightFallback = IsPropertyEnabled(mat, PropNames.FakeLightMode);
            SetKeyword(mat, "_FORCE_FAKELIGHT_ON", isForceFakeLight);
            SetKeyword(mat, "_FAKELIGHT_ON", isForceFakeLight || isFakeLightFallback);

            SetKeyword(mat, "_INDIRECTSPECULAR_ON", IsPropertyEnabled(mat, PropNames.IndirectSpecular));
            SetKeyword(mat, "_TOON_STYLE_HARD", (ToonStyle)mat.GetFloat(PropNames.ToonStyle) == ToonStyle.Hard);
        }
    }

    private bool IsPropertyEnabled(MaterialProperty prop) => prop.floatValue > 0.5f;
    private bool IsPropertyEnabled(Material mat, string propName) => mat.GetFloat(propName) > 0.5f;

    private void SetKeyword(Material m, string keyword, bool state)
    {
        if (state) m.EnableKeyword(keyword);
        else m.DisableKeyword(keyword);
    }
}