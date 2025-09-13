using UnityEngine;
using UnityEditor;

public static class TieredEffectsDrawer
{
    public enum EffectTier { None, Standard, Rare, Epic, Legendary }
    public enum RareEffectType { PulsingGlow, Sparkles }
    public enum EpicEffectType { FireAura, ElectricField }
    public enum LegendaryEffectType { CosmicRift, HolyAura }

    public static void DrawTieredEffectsSettings(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.TieredEffectProps props)
    {
        editor.ShaderProperty(props.effectTier, "Effect Tier");
        if (props.effectTier.hasMixedValue) return;

        EditorGUI.indentLevel++;
        var currentTier = (EffectTier)props.effectTier.floatValue;
        switch (currentTier)
        {
            case EffectTier.Rare:
                DrawRareEffects(editor, props);
                break;
            case EffectTier.Epic:
                DrawEpicEffects(editor, props);
                break;
            case EffectTier.Legendary:
                DrawLegendaryEffects(editor, props);
                break;
        }
        EditorGUI.indentLevel--;
    }

    private static void DrawRareEffects(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.TieredEffectProps props)
    {
        editor.ShaderProperty(props.rareEffectType, "Effect Type");
        if (props.rareEffectType.hasMixedValue) return;

        var effectType = (RareEffectType)props.rareEffectType.floatValue;
        switch (effectType)
        {
            case RareEffectType.PulsingGlow:
                EditorGUILayout.LabelField("Pulsing Glow", EditorStyles.miniBoldLabel);
                editor.ShaderProperty(props.rareColor1, "Glow Color");
                editor.ShaderProperty(props.rareFloat1, "Pulse Speed");
                editor.ShaderProperty(props.rareFloat2, "Pulse Intensity");
                break;
            case RareEffectType.Sparkles:
                EditorGUILayout.LabelField("Sparkles", EditorStyles.miniBoldLabel);
                editor.ShaderProperty(props.rareColor1, "Sparkle Color");
                editor.ShaderProperty(props.rareFloat1, "Sparkle Density");
                editor.ShaderProperty(props.rareFloat2, "Flicker Speed");
                break;
        }
    }

    private static void DrawEpicEffects(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.TieredEffectProps props)
    {
        editor.ShaderProperty(props.epicEffectType, "Effect Type");
        if (props.epicEffectType.hasMixedValue) return;

        var effectType = (EpicEffectType)props.epicEffectType.floatValue;
        switch (effectType)
        {
            case EpicEffectType.FireAura:
                EditorGUILayout.LabelField("Fire Aura", EditorStyles.miniBoldLabel);
                editor.TexturePropertySingleLine(new GUIContent("Noise Texture"), props.effectTex1, props.epicColor1);
                editor.ShaderProperty(props.epicColor2, "Aura Color 2");
                editor.ShaderProperty(props.epicFloat1, "Scroll Speed");
                editor.ShaderProperty(props.epicFloat2, "Noise Scale");
                editor.ShaderProperty(props.epicFloat3, "Fresnel Power");
                break;
            case EpicEffectType.ElectricField:
                EditorGUILayout.LabelField("Electric Field", EditorStyles.miniBoldLabel);
                editor.TexturePropertySingleLine(new GUIContent("Line Pattern"), props.effectTex1, props.epicColor1);
                editor.ShaderProperty(props.epicFloat1, "Scroll Speed");
                editor.ShaderProperty(props.epicFloat2, "Line Density");
                editor.ShaderProperty(props.epicFloat3, "Jitter Intensity");
                break;
        }
    }

    private static void DrawLegendaryEffects(MaterialEditor editor, ToonOpaqueShaderGUI_HQ.TieredEffectProps props)
    {
        editor.ShaderProperty(props.legendaryEffectType, "Effect Type");
        if (props.legendaryEffectType.hasMixedValue) return;

        var effectType = (LegendaryEffectType)props.legendaryEffectType.floatValue;
        switch (effectType)
        {
            case LegendaryEffectType.CosmicRift:
                EditorGUILayout.LabelField("Cosmic Rift", EditorStyles.miniBoldLabel);
                editor.TexturePropertySingleLine(new GUIContent("Nebula Texture"), props.effectTex1, props.legendaryColor1);
                editor.ShaderProperty(props.legendaryColor2, "Rift Edge Color");
                editor.ShaderProperty(props.legendaryFloat1, "Scroll Speed");
                editor.ShaderProperty(props.legendaryFloat2, "Nebula Scale");
                editor.ShaderProperty(props.legendaryFloat3, "Rift Sharpness");
                break;
            case LegendaryEffectType.HolyAura:
                EditorGUILayout.LabelField("Holy Aura", EditorStyles.miniBoldLabel);
                editor.ShaderProperty(props.legendaryColor1, "Aura Color");
                editor.ShaderProperty(props.legendaryFloat1, "Wave Speed");
                editor.ShaderProperty(props.legendaryFloat2, "Wave Frequency");
                editor.ShaderProperty(props.legendaryFloat3, "Fresnel Power");
                break;
        }
    }
}