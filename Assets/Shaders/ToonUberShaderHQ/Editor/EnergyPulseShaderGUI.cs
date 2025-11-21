using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class EnergyPulseShaderGUI : ShaderGUI
{
    // UI State
    bool showMainSettings = true;
    bool showFlowSettings = true;
    bool showPulseSettings = true;
    bool showRenderingSettings = true;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material targetMat = materialEditor.target as Material;

        // Find Properties
        var mainTex = FindProperty("_MainTex", properties);
        var baseColor = FindProperty("_BaseColor", properties);
        var energyIntensity = FindProperty("_EnergyIntensity", properties);
        var cutoff = FindProperty("_Cutoff", properties);

        var useWorldSpace = FindProperty("_UseWorldSpace", properties);
        var useGrayscale = FindProperty("_UseGrayscaleFlow", properties);
        var flowDir = FindProperty("_FlowDirection", properties);
        var flowSpeed = FindProperty("_FlowSpeed", properties);

        var pulseDensity = FindProperty("_PulseDensity", properties);
        var pulseWidth = FindProperty("_PulseWidth", properties);
        var pulseSoftness = FindProperty("_PulseSoftness", properties);

        var useRamp = FindProperty("_UseRamp", properties);
        var rampTex = FindProperty("_RampTex", properties);

        var srcBlend = FindProperty("_SrcBlend", properties);
        var dstBlend = FindProperty("_DstBlend", properties);
        var cullMode = FindProperty("_Cull", properties);
        var zWrite = FindProperty("_ZWrite", properties);

        // --- Header ---
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("⚡ Energy Pulse Pro", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // --- 1. Main Settings ---
        showMainSettings = EditorGUILayout.Foldout(showMainSettings, "Base Visuals", true);
        if (showMainSettings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Pattern Mask"), mainTex, baseColor);
                materialEditor.ShaderProperty(energyIntensity, "Intensity (HDR)");

                // Conditional Ramp Logic
                materialEditor.ShaderProperty(useRamp, "Use Ramp Gradient");
                if (useRamp.floatValue > 0.5f)
                {
                    materialEditor.TexturePropertySingleLine(new GUIContent("Ramp Texture"), rampTex);
                }

                materialEditor.ShaderProperty(cutoff, "Alpha Cutoff (Depth)");
            }
        }

        // --- 2. Flow Logic ---
        showFlowSettings = EditorGUILayout.Foldout(showFlowSettings, "Flow & Animation", true);
        if (showFlowSettings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                materialEditor.ShaderProperty(useGrayscale, "Mode: Grayscale as Path");
                if (useGrayscale.floatValue < 0.5f)
                {
                    materialEditor.ShaderProperty(useWorldSpace, "Use World Space (3D)");
                    materialEditor.ShaderProperty(flowDir, "Flow Direction");
                }
                else
                {
                    EditorGUILayout.HelpBox("Electricity flows from Black -> White pixels.", MessageType.Info);
                }

                materialEditor.ShaderProperty(flowSpeed, "Flow Speed");
            }
        }

        // --- 3. Pulse Shape ---
        showPulseSettings = EditorGUILayout.Foldout(showPulseSettings, "Pulse Shape", true);
        if (showPulseSettings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                materialEditor.ShaderProperty(pulseDensity, "Frequency (Density)");
                materialEditor.ShaderProperty(pulseWidth, "Width (Sharpness)");
                materialEditor.ShaderProperty(pulseSoftness, "Edge Softness");
            }
        }

        // --- 4. Rendering (Depth & Blending) ---
        showRenderingSettings = EditorGUILayout.Foldout(showRenderingSettings, "Rendering & Depth", true);
        if (showRenderingSettings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Blending", EditorStyles.miniBoldLabel);
                materialEditor.ShaderProperty(srcBlend, "Source Blend");
                materialEditor.ShaderProperty(dstBlend, "Dest Blend");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Depth & Culling", EditorStyles.miniBoldLabel);
                materialEditor.ShaderProperty(zWrite, "ZWrite (Depth Buffer)");
                materialEditor.ShaderProperty(cullMode, "Culling");

                materialEditor.RenderQueueField();
                materialEditor.EnableInstancingField();
                materialEditor.DoubleSidedGIField();
            }
        }
    }
}