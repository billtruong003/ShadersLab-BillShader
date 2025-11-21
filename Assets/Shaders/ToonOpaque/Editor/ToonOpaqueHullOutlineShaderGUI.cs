using UnityEngine;
using UnityEditor;

public class ToonOpaqueHullOutlineShaderGUI : ToonOpaqueShaderBase
{
    private static bool showHullOutlineSettings = true;

    private MaterialProperty outlineColorProp, outlineWidthProp, outlineScaleWithDistanceProp, distanceFadeStartProp, distanceFadeEndProp;

    protected override void FindProperties()
    {
        base.FindProperties();

        outlineColorProp = FindProperty("_OutlineColor", properties);
        outlineWidthProp = FindProperty("_OutlineWidth", properties);
        outlineScaleWithDistanceProp = FindProperty("_OutlineScaleWithDistance", properties);
        distanceFadeStartProp = FindProperty("_OutlineDistanceFadeStart", properties);
        distanceFadeEndProp = FindProperty("_OutlineDistanceFadeEnd", properties);
    }

    protected override void DrawWorkflowSettings()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Workflow", EditorStyles.boldLabel);
        DrawSurfaceTypeSelector();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Outline Mode", "Inverted Hull");
        if (GUILayout.Button("Remove Outline (Switch to Standard Opaque)"))
        {
            SwitchShader("Bill's Toon/Opaque - Full URP Compatible");
        }
        EditorGUILayout.EndVertical();
    }

    protected override void DrawMainProperties()
    {
        // Draw Outline specific settings first
        DrawFoldout("Inverted Hull Outline", ref showHullOutlineSettings, () =>
        {
            materialEditor.ShaderProperty(outlineColorProp, "Color");
            materialEditor.ShaderProperty(outlineWidthProp, "Width");
            materialEditor.ShaderProperty(outlineScaleWithDistanceProp, "Screen-Space Scaling");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("World-Space Distance Fade", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(distanceFadeStartProp, "Fade Start");
            materialEditor.ShaderProperty(distanceFadeEndProp, "Fade End");
            EditorGUI.indentLevel--;
        });

        // Then draw all standard properties (Toon, Rim, Metallic, etc.)
        base.DrawMainProperties();
    }

    protected override void ApplyKeywords()
    {
        base.ApplyKeywords();
        SetKeyword("_OUTLINE_SCALE_WITH_DISTANCE", outlineScaleWithDistanceProp.floatValue > 0.5f);
    }
}