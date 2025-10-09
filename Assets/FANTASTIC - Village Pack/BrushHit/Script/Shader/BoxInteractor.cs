using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
[ExecuteInEditMode]
public class BoxInteractor : SerializedMonoBehaviour
{
    [Title("Thiết lập tương tác")]
    [InfoBox("Kích thước của vùng tương tác hình hộp.")]
    [SerializeField] private Vector3 interactionBounds = Vector3.one;

    [InfoBox("Sức mạnh tổng thể của hiệu ứng đẩy.")]
    [Range(0f, 5f)][SerializeField] private float displacementStrength = 1.5f;

    [InfoBox("Vùng ảnh hưởng lan tỏa ra bên ngoài rìa của hộp. Giá trị > 0 để hiệu ứng mượt mà hơn.")]
    [Range(0f, 5f)][SerializeField] private float maxInteractionDistance = 1.0f;

    private Transform cachedTransform;

    private static readonly int InteractorPositionID = Shader.PropertyToID("_GlobalInteractorPosition");
    private static readonly int InteractorBoundsID = Shader.PropertyToID("_GlobalInteractorBounds");
    private static readonly int DisplacementStrengthID = Shader.PropertyToID("_GlobalDisplacementStrength");
    private static readonly int MaxInteractionDistanceID = Shader.PropertyToID("_GlobalMaxInteractionDistance");

    private void OnEnable()
    {
        cachedTransform = transform;
    }

    private void Update()
    {
        UpdateGlobalShaderProperties();
    }

    private void UpdateGlobalShaderProperties()
    {
        Shader.SetGlobalVector(InteractorPositionID, cachedTransform.position);
        Shader.SetGlobalVector(InteractorBoundsID, interactionBounds);
        Shader.SetGlobalFloat(DisplacementStrengthID, displacementStrength);
        Shader.SetGlobalFloat(MaxInteractionDistanceID, maxInteractionDistance);
    }

#if UNITY_EDITOR
    [Title("Debug Visuals")]
    [SerializeField] private bool enableGizmos = true;
    [ShowIf("enableGizmos")][SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.5f);
    [ShowIf("enableGizmos")][SerializeField] private Color falloffColor = new Color(0.2f, 0.8f, 1f, 0.2f);

    private void OnDrawGizmos()
    {
        if (!enableGizmos) return;
        if (cachedTransform == null) cachedTransform = transform;

        // Vẽ hộp tương tác chính
        Gizmos.color = gizmoColor;
        Gizmos.matrix = cachedTransform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, interactionBounds * 2);

        // Vẽ hộp falloff bên ngoài
        Gizmos.color = falloffColor;
        Vector3 falloffBounds = interactionBounds + new Vector3(maxInteractionDistance, maxInteractionDistance, maxInteractionDistance);
        Gizmos.DrawCube(Vector3.zero, falloffBounds * 2);
    }
#endif
}