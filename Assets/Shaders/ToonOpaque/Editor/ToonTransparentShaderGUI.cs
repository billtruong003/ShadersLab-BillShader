using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// Giao diện tùy chỉnh cho ToonTransparentShader, cung cấp một Inspector được sắp xếp gọn gàng
/// với các mục có thể thu gọn (foldout) và các nhóm thuộc tính có điều kiện.
/// </summary>
public class ToonTransparentShaderGUI : ShaderGUI
{
    private MaterialEditor materialEditor;
    private Material material;

    // Foldout states
    private static bool showBaseSettings = true;
    private static bool showLightingSettings = true;
    private static bool showGlassSettings = true;
    private static bool showOutlineSettings = true;

    // Properties
    private MaterialProperty baseMapProp, baseColorProp;
    private MaterialProperty emissionModeProp, emissionColorProp, emissionMapProp;
    private MaterialProperty fakeLightModeProp, fakeLightColorProp, fakeLightDirectionProp;
    private MaterialProperty glassColorProp, fresnelColorProp, fresnelPowerProp;
    private MaterialProperty refractionStrengthProp, glassSpecularPowerProp, glassSpecularIntensityProp;
    private MaterialProperty fresnelOutlineToggleProp, fresnelOutlineColorProp, fresnelOutlineWidthProp, fresnelOutlinePowerProp, fresnelOutlineSharpnessProp;
    private MaterialProperty glintToggleProp, glintColorProp, glintScaleProp, glintSpeedProp, glintThresholdProp;

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        this.materialEditor = editor;
        this.material = editor.target as Material;

        FindProperties(properties);
        DrawGUI();
        ApplyKeywords();
    }

    private void FindProperties(MaterialProperty[] properties)
    {
        baseMapProp = FindProperty("_BaseMap", properties);
        baseColorProp = FindProperty("_BaseColor", properties);
        emissionModeProp = FindProperty("_EmissionMode", properties);
        emissionColorProp = FindProperty("_EmissionColor", properties);
        emissionMapProp = FindProperty("_EmissionMap", properties);
        fakeLightModeProp = FindProperty("_FakeLightMode", properties, false);
        fakeLightColorProp = FindProperty("_FakeLightColor", properties, false);
        fakeLightDirectionProp = FindProperty("_FakeLightDirection", properties, false);
        glassColorProp = FindProperty("_GlassColor", properties);
        fresnelColorProp = FindProperty("_FresnelColor", properties);
        fresnelPowerProp = FindProperty("_FresnelPower", properties);
        refractionStrengthProp = FindProperty("_RefractionStrength", properties);
        glassSpecularPowerProp = FindProperty("_GlassSpecularPower", properties);
        glassSpecularIntensityProp = FindProperty("_GlassSpecularIntensity", properties);
        fresnelOutlineToggleProp = FindProperty("_FresnelOutlineToggle", properties);
        fresnelOutlineColorProp = FindProperty("_FresnelOutlineColor", properties);
        fresnelOutlineWidthProp = FindProperty("_FresnelOutlineWidth", properties);
        fresnelOutlinePowerProp = FindProperty("_FresnelOutlinePower", properties);
        fresnelOutlineSharpnessProp = FindProperty("_FresnelOutlineSharpness", properties);
        glintToggleProp = FindProperty("_GlintToggle", properties);
        glintColorProp = FindProperty("_GlintColor", properties);
        glintScaleProp = FindProperty("_GlintScale", properties);
        glintSpeedProp = FindProperty("_GlintSpeed", properties);
        glintThresholdProp = FindProperty("_GlintThreshold", properties);
    }

    private void DrawGUI()
    {
        DrawWorkflowSettings();
        DrawMainProperties();
    }

    private void DrawWorkflowSettings()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Workflow: Stylized Glass", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This shader simulates a stylized transparent glass effect with refraction, fresnel, and outlines.", MessageType.Info);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawMainProperties()
    {
        DrawFoldout("Base Properties", ref showBaseSettings, () =>
        {
            materialEditor.TexturePropertySingleLine(new GUIContent(baseMapProp.displayName, "Albedo (RGB) Alpha (A)"), baseMapProp, baseColorProp);
            DrawPropertyGroup(emissionModeProp, "Enable Emission", () =>
            {
                materialEditor.ShaderProperty(emissionColorProp, "Color");
                materialEditor.TexturePropertySingleLine(new GUIContent(emissionMapProp.displayName), emissionMapProp);
            });
        });

        DrawFoldout("Lighting", ref showLightingSettings, () =>
        {
            if (fakeLightModeProp != null)
            {
                DrawPropertyGroup(fakeLightModeProp, "Enable Fake Light", () =>
                {
                    EditorGUILayout.HelpBox("Fake Light acts as a fallback when no main Directional Light is present, ensuring the object is never completely black.", MessageType.Info);
                    materialEditor.ShaderProperty(fakeLightColorProp, "Color");
                    materialEditor.ShaderProperty(fakeLightDirectionProp, "Direction");
                });
            }
        });

        DrawFoldout("Stylized Glass", ref showGlassSettings, () =>
        {
            materialEditor.ShaderProperty(glassColorProp, "Glass Color & Opacity");
            materialEditor.ShaderProperty(refractionStrengthProp, "Refraction Strength");
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Fresnel (Edge Effect)", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(fresnelColorProp, "Edge Color");
            materialEditor.ShaderProperty(fresnelPowerProp, "Edge Power");
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Specular", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(glassSpecularPowerProp, "Power");
            materialEditor.ShaderProperty(glassSpecularIntensityProp, "Intensity");
        });

        DrawFoldout("Outline & Glint", ref showOutlineSettings, () =>
        {
            DrawPropertyGroup(fresnelOutlineToggleProp, "Enable Fresnel Outline", () =>
            {
                materialEditor.ShaderProperty(fresnelOutlineColorProp, "Color");
                materialEditor.ShaderProperty(fresnelOutlineWidthProp, "Width");
                materialEditor.ShaderProperty(fresnelOutlinePowerProp, "Power");
                materialEditor.ShaderProperty(fresnelOutlineSharpnessProp, "Sharpness");
                EditorGUILayout.Space();
            });

            // Glint effect is dependent on the outline being enabled
            EditorGUI.BeginDisabledGroup(fresnelOutlineToggleProp.floatValue == 0);
            DrawPropertyGroup(glintToggleProp, "Enable Glint Effect", () =>
            {
                materialEditor.ShaderProperty(glintColorProp, "Color");
                materialEditor.ShaderProperty(glintScaleProp, "Scale");
                materialEditor.ShaderProperty(glintSpeedProp, "Speed");
                materialEditor.ShaderProperty(glintThresholdProp, "Threshold");
            });
            EditorGUI.EndDisabledGroup();
        });
    }

    private void ApplyKeywords()
    {
        SetKeyword("_EMISSION_ON", emissionModeProp.floatValue > 0);
        if (fakeLightModeProp != null)
        {
            SetKeyword("_FAKELIGHT_ON", fakeLightModeProp.floatValue > 0);
        }

        bool fresnelEnabled = fresnelOutlineToggleProp.floatValue > 0;
        SetKeyword("_OUTLINEMODE_FRESNEL", fresnelEnabled);

        bool glintEnabled = fresnelEnabled && glintToggleProp.floatValue > 0;
        SetKeyword("_OUTLINEGLINT_ON", glintEnabled);
    }

    // --- Helper Methods ---

    private void DrawFoldout(string title, ref bool display, Action contents)
    {
        var style = new GUIStyle("ShurikenModuleTitle")
        {
            font = new GUIStyle(EditorStyles.label).font,
            border = new RectOffset(15, 7, 4, 4),
            fixedHeight = 22,
            contentOffset = new Vector2(20f, -2f)
        };

        var rect = EditorGUILayout.GetControlRect(false, 22, style);
        GUI.Box(rect, title, style);

        var e = Event.current;
        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            display = !display;
            e.Use();
        }

        if (display)
        {
            EditorGUILayout.BeginVertical("box");
            contents();
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawPropertyGroup(MaterialProperty toggle, string title, Action contents)
    {
        materialEditor.ShaderProperty(toggle, title);

        EditorGUI.indentLevel++;
        EditorGUI.BeginDisabledGroup(toggle.floatValue == 0);
        contents();
        EditorGUI.EndDisabledGroup();
        EditorGUI.indentLevel--;
    }

    private void SetKeyword(string keyword, bool enabled)
    {
        if (enabled)
        {
            if (!material.IsKeywordEnabled(keyword))
            {
                material.EnableKeyword(keyword);
            }
        }
        else
        {
            if (material.IsKeywordEnabled(keyword))
            {
                material.DisableKeyword(keyword);
            }
        }
    }
}