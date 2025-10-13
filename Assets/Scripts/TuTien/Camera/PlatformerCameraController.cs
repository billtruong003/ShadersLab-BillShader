using UnityEngine;
using Sirenix.OdinInspector;

namespace VoTanTuTien.Cam
{
    public class PlatformerCameraController : MonoBehaviour
    {
        [BoxGroup("Target")]
        [Required("Bạn phải gán mục tiêu cho camera!")]
        [SceneObjectsOnly]
        [SerializeField] private Transform target;

        [Title("Position Following")]
        [BoxGroup("Following Parameters")]
        [Range(0.01f, 1f)]
        [SerializeField] private float positionSmoothSpeed = 0.125f;
        [BoxGroup("Following Parameters")]
        [Tooltip("Khoảng cách mặc định từ camera đến mục tiêu.")]
        [SerializeField] private Vector3 offset;

        [Title("Look Ahead")]
        [BoxGroup("Look Ahead Parameters")]
        [InfoBox("Camera sẽ nhìn về phía trước một chút theo hướng di chuyển của nhân vật.")]
        [SerializeField] private float lookAheadFactor = 2f;
        [BoxGroup("Look Ahead Parameters")]
        [SerializeField] private float lookAheadReturnSpeed = 0.5f;
        [BoxGroup("Look Ahead Parameters")]
        [Tooltip("Tên trục ngang được định nghĩa trong Input Manager.")]
        [SerializeField] private string horizontalAxisName = "Horizontal";

        [Title("Rotation Following")]
        [BoxGroup("Rotation Parameters")]
        [InfoBox("Khiến camera luôn xoay và nhìn về phía mục tiêu một cách mượt mà.")]
        [Range(0.1f, 15f)]
        [SerializeField] private float rotationSmoothSpeed = 8f;

        [BoxGroup("Rotation Parameters")]
        [Tooltip("Góc xoay bổ sung (pitch, yaw, roll) để tinh chỉnh góc nhìn camera. Ví dụ: X=15 để camera hơi cúi xuống.")]
        [SerializeField] private Vector3 rotationOffset;

        // --- Private Variables ---
        private Vector3 currentPositionVelocity;
        private float lookAheadOffset;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            HandlePosition();
            HandleRotation();
        }

        private void HandlePosition()
        {
            UpdateLookAhead();
            FollowTargetPosition();
        }

        private void UpdateLookAhead()
        {
            float horizontalInput = Input.GetAxis(horizontalAxisName);
            float targetLookAhead = lookAheadFactor * Mathf.Sign(horizontalInput);
            lookAheadOffset = Mathf.Lerp(lookAheadOffset, targetLookAhead, Time.deltaTime * lookAheadReturnSpeed);
        }

        private void FollowTargetPosition()
        {
            Vector3 desiredPosition = target.position + offset;
            desiredPosition.x += lookAheadOffset;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref currentPositionVelocity,
                positionSmoothSpeed
            );
        }

        private void HandleRotation()
        {
            // 1. Tính toán góc quay cơ bản để nhìn thẳng vào mục tiêu
            Vector3 directionToTarget = target.position - transform.position;
            Quaternion lookAtRotation = Quaternion.LookRotation(directionToTarget);

            // 2. Chuyển đổi offset từ Vector3 (Euler angles) sang Quaternion
            Quaternion offsetQuaternion = Quaternion.Euler(rotationOffset);

            // 3. Kết hợp góc quay cơ bản với góc quay offset
            // Phép nhân Quaternion sẽ áp dụng offset *sau khi* đã xoay về phía mục tiêu
            Quaternion finalRotation = lookAtRotation * offsetQuaternion;

            // 4. Xoay camera đến góc quay cuối cùng một cách mượt mà
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                finalRotation,
                rotationSmoothSpeed * Time.deltaTime
            );
        }
    }
}