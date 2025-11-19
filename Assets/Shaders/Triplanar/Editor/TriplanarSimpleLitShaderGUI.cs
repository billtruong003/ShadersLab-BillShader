using UnityEditor;
using UnityEngine;
using System;

public class CustomToonTriplanarShaderGUI : ShaderGUI
{
    private bool showMainProps = true;
    private bool showTriplanarTextures = true;
    private bool showTriplanarSettings = true;
    private bool showBlending = true;
    private bool showLighting = true;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material targetMat = materialEditor.target as Material;

        // Find properties
        MaterialProperty mainColor = FindProperty("_Color", properties);
        MaterialProperty tintColor = FindProperty("_Tint", properties);
        MaterialProperty ambientColor = FindProperty("_AmbientColor", properties);

        MaterialProperty topTex = FindProperty("_MainTex", properties);
        MaterialProperty topNormal = FindProperty("_NormalT", properties);
        MaterialProperty sideTex = FindProperty("_MainTexSide", properties);
        MaterialProperty sideNormal = FindProperty("_Normal", properties);
        MaterialProperty normalStrength = FindProperty("_NormalStrength", properties);

        MaterialProperty topScale = FindProperty("_Scale", properties);
        MaterialProperty sideScale = FindProperty("_SideScale", properties);
        MaterialProperty noiseTex = FindProperty("_Noise", properties);
        MaterialProperty noiseScale = FindProperty("_NoiseScale", properties);

        MaterialProperty topSpread = FindProperty("_TopSpread", properties);
        MaterialProperty edgeWidth = FindProperty("_EdgeWidth", properties);

        MaterialProperty ramp = FindProperty("_Ramp", properties);
        MaterialProperty specColor = FindProperty("_SpecColor", properties);
        MaterialProperty smoothness = FindProperty("_Smoothness", properties);
        MaterialProperty rimPower = FindProperty("_RimPower", properties);
        MaterialProperty rimColorTop = FindProperty("_RimColor", properties);
        MaterialProperty rimColorSide = FindProperty("_RimColor2", properties);

        // --- GUI Layout ---

        showMainProps = EditorGUILayout.BeginFoldoutHeaderGroup(showMainProps, "Main Properties");
        if (showMainProps)
        {
            materialEditor.ShaderProperty(mainColor, "Main Color");
            materialEditor.ShaderProperty(tintColor, "Tint Color");
            materialEditor.ShaderProperty(ambientColor, "Ambient Color");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();

        showTriplanarTextures = EditorGUILayout.BeginFoldoutHeaderGroup(showTriplanarTextures, "Triplanar Textures");
        if (showTriplanarTextures)
        {
            materialEditor.TexturePropertySingleLine(new GUIContent("Top Texture", "Albedo for upward-facing surfaces."), topTex);
            materialEditor.TexturePropertySingleLine(new GUIContent("Top Normal", "Normal map for upward-facing surfaces."), topNormal);
            EditorGUILayout.Space();
            materialEditor.TexturePropertySingleLine(new GUIContent("Side/Bottom Texture", "Albedo for side/bottom surfaces."), sideTex);
            materialEditor.TexturePropertySingleLine(new GUIContent("Side/Bottom Normal", "Normal map for side/bottom surfaces."), sideNormal);
            EditorGUILayout.Space();
            materialEditor.ShaderProperty(normalStrength, "Normal Strength");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();

        showTriplanarSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showTriplanarSettings, "Triplanar Settings");
        if (showTriplanarSettings)
        {
            materialEditor.ShaderProperty(topScale, "Top Texture Scale");
            materialEditor.ShaderProperty(sideScale, "Side/Bottom Texture Scale");
            materialEditor.TexturePropertySingleLine(new GUIContent("Noise Texture", "Used to break up the blend line."), noiseTex);
            materialEditor.ShaderProperty(noiseScale, "Noise Scale");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();

        showBlending = EditorGUILayout.BeginFoldoutHeaderGroup(showBlending, "Blending Control");
        if (showBlending)
        {
            materialEditor.ShaderProperty(topSpread, new GUIContent("Top Blend Start", "Controls the Y-axis point where the top texture starts blending in."));
            materialEditor.ShaderProperty(edgeWidth, new GUIContent("Blend Smoothness", "Controls how soft the transition is between top and side textures."));
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();

        showLighting = EditorGUILayout.BeginFoldoutHeaderGroup(showLighting, "Toon Lighting");
        if (showLighting)
        {
            materialEditor.TexturePropertySingleLine(new GUIContent("Toon Ramp", "Gradient to control lighting steps."), ramp);
            materialEditor.ShaderProperty(specColor, "Specular Color");
            materialEditor.ShaderProperty(smoothness, "Smoothness");
            EditorGUILayout.Space();
            materialEditor.ShaderProperty(rimPower, "Rim Power");
            materialEditor.ShaderProperty(rimColorTop, "Rim Color (Top)");
            materialEditor.ShaderProperty(rimColorSide, "Rim Color (Side/Bottom)");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
}