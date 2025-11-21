using UnityEngine;
using UnityEditor;

public class BillToonProEditor : ShaderGUI
{
    bool showGeneral = true;
    bool showLighting = true;
    bool showMasking = true;
    bool showDissolve = true;
    bool showOutline = true;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        DrawHeader();

        // --- General Settings ---
        showGeneral = EditorGUILayout.BeginFoldoutHeaderGroup(showGeneral, "Base Properties");
        if (showGeneral)
        {
            MaterialProperty surfaceProp = FindProperty("_BaseMap", properties);
            MaterialProperty colorProp = FindProperty("_BaseColor", properties);
            MaterialProperty bumpProp = FindProperty("_BumpMap", properties);
            MaterialProperty bumpScaleProp = FindProperty("_BumpScale", properties);
            MaterialProperty cutoffProp = FindProperty("_Cutoff", properties);
            MaterialProperty emissionMapProp = FindProperty("_EmissionMap", properties);
            MaterialProperty emissionColorProp = FindProperty("_EmissionColor", properties);

            materialEditor.TexturePropertySingleLine(new GUIContent("Base Map"), surfaceProp, colorProp);
            materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), bumpProp, bumpScaleProp);
            materialEditor.TexturePropertySingleLine(new GUIContent("Emission"), emissionMapProp, emissionColorProp);

            materialEditor.ShaderProperty(cutoffProp, "Alpha Cutoff");

            if (bumpProp.textureValue != null) material.EnableKeyword("_NORMALMAP");
            else material.DisableKeyword("_NORMALMAP");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // --- Lighting ---
        showLighting = EditorGUILayout.BeginFoldoutHeaderGroup(showLighting, "Toon Lighting & Specular");
        if (showLighting)
        {
            DrawProperty(materialEditor, properties, "_ToonRamp");
            DrawProperty(materialEditor, properties, "_RampThreshold");
            DrawProperty(materialEditor, properties, "_RampSmoothness");
            DrawProperty(materialEditor, properties, "_ShadowColor");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Specular & Rim", EditorStyles.boldLabel);
            DrawProperty(materialEditor, properties, "_SpecularColor");
            DrawProperty(materialEditor, properties, "_SpecularSize");
            DrawProperty(materialEditor, properties, "_SpecularFalloff");
            DrawProperty(materialEditor, properties, "_RimColor");
            DrawProperty(materialEditor, properties, "_RimPower");
            DrawProperty(materialEditor, properties, "_RimThreshold");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("MatCap", EditorStyles.boldLabel);
            DrawProperty(materialEditor, properties, "_MatCapTex");
            DrawProperty(materialEditor, properties, "_MatCapStrength");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // --- Masking ---
        showMasking = EditorGUILayout.BeginFoldoutHeaderGroup(showMasking, "Multi-Layer Masking");
        if (showMasking)
        {
            MaterialProperty maskToggle = FindProperty("_MaskingToggle", properties);
            materialEditor.ShaderProperty(maskToggle, "Enable Masking");

            if (maskToggle.floatValue > 0.5f)
            {
                material.EnableKeyword("_MASKING_ON");
                EditorGUI.indentLevel++;

                MaterialProperty triplanarToggle = FindProperty("_TriplanarToggle", properties);
                materialEditor.ShaderProperty(triplanarToggle, "Use Triplanar Projection");
                if (triplanarToggle.floatValue > 0.5f)
                {
                    material.EnableKeyword("_TRIPLANAR_MASK");
                    DrawProperty(materialEditor, properties, "_TriplanarScale");
                    DrawProperty(materialEditor, properties, "_TriplanarBlendSharpness");
                }
                else
                {
                    material.DisableKeyword("_TRIPLANAR_MASK");
                }

                EditorGUILayout.Space();
                DrawProperty(materialEditor, properties, "_MaskControlMap");
                EditorGUILayout.HelpBox("Mask RGB controls Layers 1-3", MessageType.Info);

                EditorGUILayout.LabelField("Layer 1 (Red Channel)", EditorStyles.boldLabel);
                materialEditor.TexturePropertySingleLine(new GUIContent("Texture"), FindProperty("_Layer1Tex", properties), FindProperty("_Layer1Color", properties));

                EditorGUILayout.LabelField("Layer 2 (Green Channel)", EditorStyles.boldLabel);
                materialEditor.TexturePropertySingleLine(new GUIContent("Texture"), FindProperty("_Layer2Tex", properties), FindProperty("_Layer2Color", properties));

                EditorGUILayout.LabelField("Layer 3 (Blue Channel)", EditorStyles.boldLabel);
                materialEditor.TexturePropertySingleLine(new GUIContent("Texture"), FindProperty("_Layer3Tex", properties), FindProperty("_Layer3Color", properties));

                EditorGUI.indentLevel--;
            }
            else
            {
                material.DisableKeyword("_MASKING_ON");
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // --- Dissolve ---
        showDissolve = EditorGUILayout.BeginFoldoutHeaderGroup(showDissolve, "Dissolve Effect");
        if (showDissolve)
        {
            MaterialProperty dissolveToggle = FindProperty("_DissolveToggle", properties);
            materialEditor.ShaderProperty(dissolveToggle, "Enable Dissolve");

            if (dissolveToggle.floatValue > 0.5f)
            {
                material.EnableKeyword("_DISSOLVE_ON");
                EditorGUI.indentLevel++;
                DrawProperty(materialEditor, properties, "_DissolveMap");
                DrawProperty(materialEditor, properties, "_DissolveScale");
                DrawProperty(materialEditor, properties, "_DissolveAmount");
                DrawProperty(materialEditor, properties, "_DissolveEdgeWidth");
                DrawProperty(materialEditor, properties, "_DissolveEdgeColor");
                EditorGUI.indentLevel--;
            }
            else
            {
                material.DisableKeyword("_DISSOLVE_ON");
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // --- Outline ---
        showOutline = EditorGUILayout.BeginFoldoutHeaderGroup(showOutline, "Hull Outline");
        if (showOutline)
        {
            DrawProperty(materialEditor, properties, "_OutlineWidth");
            DrawProperty(materialEditor, properties, "_OutlineColor");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DrawProperty(MaterialEditor editor, MaterialProperty[] props, string name)
    {
        MaterialProperty prop = FindProperty(name, props);
        if (prop != null) editor.ShaderProperty(prop, prop.displayName);
    }

    void DrawHeader()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 14;
        EditorGUILayout.BeginVertical("helpBox");
        EditorGUILayout.LabelField("Bill's Pro Toon Extended", style);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }
}