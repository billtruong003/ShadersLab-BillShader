using ExternPropertyAttributes;
using UnityEngine;

namespace DungeonRush
{
    public class CameraController : MonoBehaviour
    {
        [Header("Target & Following")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 followOffset = new Vector3(0, 15f, -10f);
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0, 1.5f, 0);
        [SerializeField, Min(0)] private float smoothSpeed = 0.125f;

        [Header("Shake Settings")]
        [SerializeField] private float defaultShakeIntensity = 0.5f;
        [SerializeField] private float defaultShakeDuration = 0.2f;

        private Vector3 velocity = Vector3.zero;
        private Transform cameraPivot;

        private void Awake()
        {
            InitializeCameraPivot();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleFollowing();
            HandleRotation();
        }

        private void InitializeCameraPivot()
        {
            cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(transform, false);
            cameraPivot.localPosition = Vector3.zero;
            cameraPivot.localRotation = Quaternion.identity;

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.SetParent(cameraPivot);
                mainCamera.transform.localPosition = Vector3.zero;
                mainCamera.transform.localRotation = Quaternion.identity;
            }
        }

        private void HandleFollowing()
        {
            Vector3 desiredPosition = target.position + followOffset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
        }

        private void HandleRotation()
        {
            Vector3 lookAtPoint = target.position + lookAtOffset;
            transform.LookAt(lookAtPoint);
        }


        [Button]
        public void TriggerShake()
        {
            TriggerShake(defaultShakeIntensity, defaultShakeDuration);
        }

        public void TriggerShake(float intensity, float duration)
        {
            LeanTween.cancel(cameraPivot.gameObject);
            cameraPivot.localPosition = Vector3.zero;

            LeanTween.move(cameraPivot.gameObject, (Vector3.right + Vector3.up) * intensity, duration)
                .setEaseShake();
        }
    }
}