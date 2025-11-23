using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenu("Post-processing/Custom/Outline")]
public class OutlineVolume : VolumeComponent, IPostProcessComponent
{
    public enum OutlineMode { FullScreen, SelectionOnly, Mixed }
    public enum OutlineAlgorithm { RobertsCross, Sobel }
    public enum DebugMode { None, Depth, Normals, Color, EdgeOnly, MaskOnly }

    public BoolParameter isActive = new BoolParameter(false);
    public EnumParameter<OutlineMode> mode = new EnumParameter<OutlineMode>(OutlineMode.FullScreen);
    public LayerMaskParameter selectionLayer = new LayerMaskParameter(-1);

    public EnumParameter<DebugMode> debugMode = new EnumParameter<DebugMode>(DebugMode.None);
    public EnumParameter<OutlineAlgorithm> algorithm = new EnumParameter<OutlineAlgorithm>(OutlineAlgorithm.Sobel);

    public ClampedIntParameter thickness = new ClampedIntParameter(2, 1, 10);
    [Tooltip("Use HDR Color for Luminous/Glow Effect")]
    public ColorParameter outlineColor = new ColorParameter(new Color(0, 1, 0, 1), true, false, true);

    public ClampedFloatParameter depthThreshold = new ClampedFloatParameter(1.5f, 0f, 10f);
    public ClampedFloatParameter normalThreshold = new ClampedFloatParameter(0.4f, 0f, 1f);
    public ClampedFloatParameter colorThreshold = new ClampedFloatParameter(0.2f, 0f, 1f);

    public BoolParameter useDepth = new BoolParameter(true);
    public BoolParameter useNormals = new BoolParameter(true);
    public BoolParameter useColor = new BoolParameter(false);

    public bool IsActive() => isActive.value;
    public bool IsTileCompatible() => false;
}