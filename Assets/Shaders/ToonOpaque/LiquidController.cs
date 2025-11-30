using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Renderer))]
public class LiquidController : MonoBehaviour
{
    [Title("General Settings")]
    [SerializeField, Range(0f, 1f), OnValueChanged(nameof(UpdateVisuals))]
    private float fillAmount = 0.5f;

    [Title("Colors")]
    [SerializeField, ColorUsage(true, true), OnValueChanged(nameof(UpdateVisuals))]
    private Color liquidColor = new Color(0.3f, 0.7f, 1f, 1f);

    [SerializeField, ColorUsage(true, true), OnValueChanged(nameof(UpdateVisuals))]
    private Color surfaceColor = new Color(0.6f, 0.9f, 1f, 1f);

    [Title("Simulation")]
    [SerializeField, Min(0), OnValueChanged(nameof(UpdateVisuals))]
    private float wobbleFrequency = 1f;

    [SerializeField, Min(0), OnValueChanged(nameof(UpdateVisuals))]
    private float wobbleAmplitude = 0.05f;

    [SerializeField, Min(0), OnValueChanged(nameof(UpdateVisuals))]
    private float wobbleSpeed = 1f;

    [Title("Mesh Data (Baked)")]
    [SerializeField, ReadOnly]
    private float minY = -1f;

    [SerializeField, ReadOnly]
    private float maxY = 1f;

    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;

    private static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");
    private static readonly int LiquidColorID = Shader.PropertyToID("_LiquidColor");
    private static readonly int SurfaceColorID = Shader.PropertyToID("_SurfaceColor");
    private static readonly int WobbleFreqID = Shader.PropertyToID("_WobbleFrequency");
    private static readonly int WobbleAmpID = Shader.PropertyToID("_WobbleAmplitude");
    private static readonly int WobbleSpeedID = Shader.PropertyToID("_WobbleSpeed");
    private static readonly int MinYID = Shader.PropertyToID("_MinY");
    private static readonly int MaxYID = Shader.PropertyToID("_MaxY");

    private void Awake()
    {
        Initialize();
    }

    private void OnValidate()
    {
        if (_renderer == null) Initialize();
        UpdateVisuals();
    }

    private void LateUpdate()
    {
        UpdateVisuals();
    }

    private void Initialize()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    public void UpdateVisuals()
    {
        if (_renderer == null) return;

        _renderer.GetPropertyBlock(_propBlock);

        _propBlock.SetFloat(FillAmountID, fillAmount);
        _propBlock.SetColor(LiquidColorID, liquidColor);
        _propBlock.SetColor(SurfaceColorID, surfaceColor);
        _propBlock.SetFloat(WobbleFreqID, wobbleFrequency);
        _propBlock.SetFloat(WobbleAmpID, wobbleAmplitude);
        _propBlock.SetFloat(WobbleSpeedID, wobbleSpeed);
        _propBlock.SetFloat(MinYID, minY);
        _propBlock.SetFloat(MaxYID, maxY);

        _renderer.SetPropertyBlock(_propBlock);
    }

    [Button("Bake Mesh Bounds", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    public void BakeBounds()
    {
        Mesh mesh = null;

        if (GetComponent<MeshFilter>() is MeshFilter mf)
        {
            mesh = mf.sharedMesh;
        }
        else if (GetComponent<SkinnedMeshRenderer>() is SkinnedMeshRenderer smr)
        {
            mesh = smr.sharedMesh;
        }

        if (mesh == null)
        {
            Debug.LogError("No Mesh found to bake bounds!");
            return;
        }

        Bounds bounds = mesh.bounds;
        minY = bounds.min.y;
        maxY = bounds.max.y;

        UpdateVisuals();
    }

    public void SetFillAmount(float value)
    {
        fillAmount = Mathf.Clamp01(value);
        UpdateVisuals();
    }

    public void SetColors(Color liquid, Color surface)
    {
        liquidColor = liquid;
        surfaceColor = surface;
        UpdateVisuals();
    }

    public void SetWobble(float frequency, float amplitude, float speed)
    {
        wobbleFrequency = frequency;
        wobbleAmplitude = amplitude;
        wobbleSpeed = speed;
        UpdateVisuals();
    }

    public float GetFillAmount() => fillAmount;
}