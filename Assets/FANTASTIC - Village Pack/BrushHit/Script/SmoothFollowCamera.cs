// File: Assets/FANTASTIC - Village Pack/BrushHit/Script/SmoothFollowCamera.cs
using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class SmoothFollowCamera : SerializedMonoBehaviour
{
    [Title("Mục tiêu & Vị trí")]
    [Required][SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 5f, -10f);

    [Title("Thông số chuyển động")]
    [Range(0.01f, 1f)][SerializeField] private float positionSmoothTime = 0.15f;
    [Range(0.1f, 20f)][SerializeField] private float lookRotationSpeed = 8f;


    [Title("Debug Info")]
    [ShowInInspector, ReadOnly] private Transform lookAtTarget;

    private Vector3 currentVelocity = Vector3.zero;
    private Transform cachedTransform;

    private void Awake()
    {
        cachedTransform = transform;
        if (followTarget == null)
        {
            Debug.LogError("Chưa gán Follow Target cho camera!");
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;

        UpdatePosition();

        if (lookAtTarget != null)
        {
            UpdateRotation();
        }
    }

    private void UpdatePosition()
    {
        Vector3 desiredPosition = followTarget.position + followOffset;
        cachedTransform.position = Vector3.SmoothDamp(cachedTransform.position, desiredPosition, ref currentVelocity, positionSmoothTime);
    }


    private void UpdateRotation()
    {
        Quaternion targetRotation = Quaternion.LookRotation(lookAtTarget.position - cachedTransform.position);
        cachedTransform.rotation = Quaternion.Slerp(cachedTransform.rotation, targetRotation, lookRotationSpeed * Time.deltaTime);
    }

    public void UpdateLookAtTarget(Transform newLookAtTarget)
    {
        this.lookAtTarget = newLookAtTarget;
    }

}