using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Camera))]
public class IsometricCameraController : MonoBehaviour
{
    [Header("=== Target ===")]
    public Transform target;                    // Kéo Player vào đây

    [Header("=== Angle & Distance ===")]
    [SerializeField] private Vector3 offset = new Vector3(0, 12, -12); // Khoảng cách + độ cao
    [SerializeField, Range(0f, 90f)] private float pitchAngle = 35f;  // Góc nhìn xuống

    [Header("=== Follow Smoothness ===")]
    [SerializeField, Range(0.01f, 1f)] private float positionSmoothTime = 0.12f;
    [SerializeField, Range(0.01f, 1f)] private float rotationSmoothTime = 0.2f;

    [Header("=== Zoom ===")]
    [SerializeField] private float zoomSpeed = 4f;
    [SerializeField] private float minDistance = 6f;
    [SerializeField] private float maxDistance = 20f;

    [Header("=== Rotation (Right Mouse Button) ===")]
    [SerializeField] private bool allowRotation = true;
    [SerializeField] private float rotationSpeed = 120f;

    private float currentZoom = 1f;
    private float currentYaw = 45f;             // Góc xoay ngang ban đầu (45° là isometric chuẩn)

    private Vector3 positionVelocity;
    private float rotationVelocity;

    private void Awake()
    {
        if (target == null)
        {
            Debug.LogError("[IsometricCamera] Không tìm thấy Player!");
            enabled = false;
            return;
        }

        currentZoom = offset.magnitude;
        ApplyZoomAndAngle();
    }

    [Button]
    private void SetupOffset()
    {
        Awake();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleZoom();
        HandleRotation();

        // Tính toán vị trí và góc mong muốn
        Quaternion camRotation = Quaternion.Euler(pitchAngle, currentYaw, 0f);
        Vector3 desiredPosition = target.position + camRotation * (Vector3.back * currentZoom);

        // Smooth follow
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, camRotation, Time.deltaTime / rotationSmoothTime);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentZoom -= scroll * zoomSpeed * currentZoom * 0.5f;
            currentZoom = Mathf.Clamp(currentZoom, minDistance, maxDistance);
            ApplyZoomAndAngle();
        }
    }

    private void HandleRotation()
    {
        if (!allowRotation) return;

        if (Input.GetMouseButton(1)) // Chuột phải giữ
        {
            float mouseX = Input.GetAxis("Mouse X");
            currentYaw += mouseX * rotationSpeed * Time.deltaTime;
        }
    }

    // Cập nhật offset theo zoom và góc pitch hiện tại
    private void ApplyZoomAndAngle()
    {
        float vertical = currentZoom * Mathf.Tan(pitchAngle * Mathf.Deg2Rad);
        offset = new Vector3(0, vertical, -currentZoom);
    }

    // Vẽ gizmo để dễ chỉnh trong Scene view
    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.5f);
            Quaternion rot = Quaternion.Euler(pitchAngle, currentYaw, 0);
            Vector3 pos = target.position + rot * new Vector3(0, offset.y, -currentZoom);
            Gizmos.DrawLine(target.position, pos);
            Gizmos.DrawWireSphere(pos, 1f);
        }
    }

    // Optional: Reset góc về isometric chuẩn bằng phím R
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentYaw = 45f;
        }
    }
}