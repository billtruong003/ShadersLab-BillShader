using UnityEngine;
using UnityEditor;
using System;

public static class ToonOpaqueDrawerUtils
{
    public enum SurfaceType { Opaque, Metallic, Foliage, Bling }

    private static bool showToonSettings = true;
    private static bool showMetallicSettings = true;
    private static bool showFoliageSettings = true;
    private static bool showBlingSettings = true;
    private static bool showRimLightSettings = true;

    public static void DrawToggleGroup(MaterialEditor editor, MaterialProperty toggle, string title, Action contents)
    {
        editor.ShaderProperty(toggle, title);
        if (toggle.floatValue > 0.5f || toggle.hasMixedValue)
        {
            EditorGUI.indentLevel++;
            contents.Invoke();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
    }

    private static void DrawFoldout(string title, ref bool state, Action contents)
    {
        state = EditorGUILayout.BeginFoldoutHeaderGroup(state, title);
        if (state)
        {
            EditorGUILayout.BeginVertical("box");
            contents.Invoke();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(2);
    }

    public static void DrawTextureMaskSettings(MaterialEditor editor, MaterialProperty toggle, MaterialProperty divisions, MaterialProperty blend, MaterialProperty[] colors, ref bool state)
    {
        DrawFoldout("Dynamic Texture Mask", ref state, () =>
        {
            DrawToggleGroup(editor, toggle, "Enable Texture Mask", () =>
            {
                editor.ShaderProperty(divisions, divisions.displayName);
                editor.ShaderProperty(blend, blend.displayName);
                EditorGUILayout.Space();

                if (!divisions.hasMixedValue && colors[0] != null)
                {
                    EditorGUILayout.LabelField("Mask Colors", EditorStyles.boldLabel);
                    int divs = (int)divisions.floatValue;
                    int colorIndex = 0;
                    float originalLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 1f;

                    for (int y = 0; y < divs; y++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        for (int x = 0; x < divs; x++)
                        {
                            if (colorIndex < colors.Length && colors[colorIndex] != null)
                            {
                                editor.ColorProperty(colors[colorIndex], "");
                            }
                            colorIndex++;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUIUtility.labelWidth = originalLabelWidth;
                }
            });
        });
    }

    public static void DrawToonSettings(MaterialEditor editor, MaterialProperty toonStyle, MaterialProperty shadowThreshold, MaterialProperty midtoneThreshold, MaterialProperty smoothness, MaterialProperty shadowTint, MaterialProperty midtoneColor)
    {
        DrawFoldout("Toon Shading", ref showToonSettings, () =>
        {
            editor.ShaderProperty(toonStyle, "Style");
            editor.ShaderProperty(shadowThreshold, "Shadow Threshold");
            editor.ShaderProperty(midtoneThreshold, "Mid-tone Threshold");

            bool isHardStyle = toonStyle.floatValue > 0.5f;
            EditorGUI.BeginDisabledGroup(isHardStyle && !toonStyle.hasMixedValue);
            editor.ShaderProperty(smoothness, "Ramp Smoothness");
            EditorGUI.EndDisabledGroup();

            editor.ShaderProperty(shadowTint, "Shadow Tint");
            editor.ShaderProperty(midtoneColor, "Mid-tone Color");
        });
    }

    public static void DrawRimLightSettings(MaterialEditor editor, MaterialProperty toggle, MaterialProperty color, MaterialProperty power)
    {
        DrawFoldout("Rim Light", ref showRimLightSettings, () =>
        {
            if (toggle != null)
            {
                DrawToggleGroup(editor, toggle, "Enable Rim Light", () =>
                {
                    editor.ShaderProperty(color, "Rim Color");
                    editor.ShaderProperty(power, "Rim Power");
                });
            }
            else
            {
                editor.ShaderProperty(color, "Rim Color");
                editor.ShaderProperty(power, "Rim Power");
            }
        });
    }

    public static void DrawMetallicSettings(MaterialEditor editor, MaterialProperty ramp, MaterialProperty brightness, MaterialProperty offset, MaterialProperty specColor, MaterialProperty hiOffset, MaterialProperty hiColor, MaterialProperty rimColor, MaterialProperty rimPower)
    {
        DrawFoldout("Stylized Metal", ref showMetallicSettings, () =>
        {
            editor.TexturePropertySingleLine(new GUIContent("Ramp Texture"), ramp);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Specular", EditorStyles.boldLabel);
            editor.ShaderProperty(brightness, "Brightness");
            editor.ShaderProperty(offset, "Size");
            editor.ShaderProperty(specColor, "Color");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Highlight", EditorStyles.boldLabel);
            editor.ShaderProperty(hiOffset, "Size");
            editor.ShaderProperty(hiColor, "Color");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rim Light", EditorStyles.boldLabel);
            editor.ShaderProperty(rimColor, "Color");
            editor.ShaderProperty(rimPower, "Power");
        });
    }

    public static void DrawFoliageSettings(MaterialEditor editor, MaterialProperty windNoiseTex, MaterialProperty windSpeed, MaterialProperty windAmp, MaterialProperty windNoiseScale, MaterialProperty windDir, MaterialProperty windFadeStart, MaterialProperty windFadeEnd, MaterialProperty transColor, MaterialProperty transStrength)
    {
        DrawFoldout("Foliage", ref showFoliageSettings, () =>
        {
            EditorGUILayout.LabelField("Wind", EditorStyles.boldLabel);
            editor.TexturePropertySingleLine(new GUIContent(windNoiseTex.displayName), windNoiseTex);
            editor.ShaderProperty(windSpeed, "Speed");
            editor.ShaderProperty(windAmp, "Amplitude");
            editor.ShaderProperty(windNoiseScale, "Noise Scale");
            editor.ShaderProperty(windDir, "Direction");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Distance Fade", EditorStyles.boldLabel);
            editor.ShaderProperty(windFadeStart, "Fade Start");
            editor.ShaderProperty(windFadeEnd, "Fade End");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
            editor.ShaderProperty(transColor, "Translucency Color");
            editor.ShaderProperty(transStrength, "Translucency Strength");
        });
    }

    public static void DrawBlingSettings(MaterialEditor editor, MaterialProperty noiseTex, MaterialProperty worldSpace, MaterialProperty color, MaterialProperty intensity, MaterialProperty scale, MaterialProperty speed, MaterialProperty fresnelPower, MaterialProperty threshold)
    {
        DrawFoldout("Bling Effect", ref showBlingSettings, () =>
        {
            editor.TexturePropertySingleLine(new GUIContent(noiseTex.displayName), noiseTex);
            EditorGUILayout.Space();
            editor.ShaderProperty(worldSpace, worldSpace.displayName);
            editor.ShaderProperty(color, color.displayName);
            editor.ShaderProperty(intensity, intensity.displayName);
            editor.ShaderProperty(scale, scale.displayName);
            editor.ShaderProperty(speed, speed.displayName);
            editor.ShaderProperty(fresnelPower, fresnelPower.displayName);
            editor.ShaderProperty(threshold, threshold.displayName);
        });
    }
}