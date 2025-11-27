using UnityEngine;
using Sirenix.OdinInspector;

namespace Nebulanook.Core
{
    [RequireComponent(typeof(Camera))]
    public class IsometricCameraController : MonoBehaviour
    {
        public static IsometricCameraController Instance { get; private set; }

        [Title("Targeting")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 12, -12);
        [SerializeField, Range(0f, 90f)] private float pitchAngle = 45f;

        [Title("Smoothing")]
        [SerializeField] private float followSmoothTime = 0.12f;
        [SerializeField] private float rotationSmoothTime = 0.2f;

        [Title("Occlusion")]
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private float obstacleCheckRadius = 0.5f;
        [SerializeField] private float occlusionSmoothTime = 0.1f;

        [Title("Input")]
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 20f;
        [SerializeField] private float rotationSpeed = 120f;

        [Title("Dialogue Settings")]
        [SerializeField] private float dialogueZoom = 7f;
        [SerializeField] private float dialogueZoomSpeed = 10f;

        private float currentZoom;
        private float targetZoomValue;
        private float currentYaw = 45f;
        private Vector3 currentVelocity;
        private float targetDistance;
        private float occlusionVelocity;

        private float shakeTimer;
        private float shakePower;
        private Vector3 shakeOffset;

        private bool isControlLocked;
        private float storedUserZoom;

        private void Awake()
        {
            Instance = this;
            if (target == null) return;

            currentZoom = offset.magnitude;
            targetZoomValue = currentZoom;
            targetDistance = currentZoom;
            UpdateCameraPosition(true);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(bool isLocked)
        {
            isControlLocked = isLocked;
            if (isLocked)
            {
                storedUserZoom = targetZoomValue;
            }
            else
            {
                targetZoomValue = storedUserZoom;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleInput();
            HandleZoomLogic();
            CalculateOcclusion();
            UpdateCameraPosition(false);
            UpdateShake();
        }

        private void HandleInput()
        {
            if (isControlLocked) return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                targetZoomValue -= scroll * zoomSpeed;
                targetZoomValue = Mathf.Clamp(targetZoomValue, minZoom, maxZoom);
            }

            if (Input.GetMouseButton(1))
            {
                currentYaw += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            }
        }

        private void HandleZoomLogic()
        {
            float targetZ = isControlLocked ? dialogueZoom : targetZoomValue;
            float speed = isControlLocked ? dialogueZoomSpeed : zoomSpeed;
            currentZoom = Mathf.MoveTowards(currentZoom, targetZ, speed * Time.deltaTime);
        }

        private void CalculateOcclusion()
        {
            Vector3 targetPos = target.position + Vector3.up;
            Quaternion rotation = Quaternion.Euler(pitchAngle, currentYaw, 0);
            Vector3 direction = rotation * Vector3.back;

            Ray ray = new Ray(targetPos, direction);
            float desiredDist = currentZoom;

            if (Physics.SphereCast(ray, obstacleCheckRadius, out RaycastHit hit, currentZoom, obstacleMask))
            {
                desiredDist = hit.distance - 0.5f;
                if (desiredDist < minZoom) desiredDist = minZoom;
            }

            targetDistance = Mathf.SmoothDamp(targetDistance, desiredDist, ref occlusionVelocity, occlusionSmoothTime);
        }

        private void UpdateCameraPosition(bool instant)
        {
            Quaternion targetRotation = Quaternion.Euler(pitchAngle, currentYaw, 0);
            Vector3 targetPos = target.position + targetRotation * (Vector3.back * targetDistance);

            if (instant)
            {
                transform.position = targetPos;
                transform.rotation = targetRotation;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, targetPos + shakeOffset, ref currentVelocity, followSmoothTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / rotationSmoothTime);
            }
        }

        public void Shake(float duration, float power)
        {
            shakeTimer = duration;
            shakePower = power;
        }

        private void UpdateShake()
        {
            if (shakeTimer > 0)
            {
                shakeOffset = Random.insideUnitSphere * shakePower;
                shakeTimer -= Time.deltaTime;
            }
            else
            {
                shakeOffset = Vector3.zero;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (target == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, obstacleCheckRadius);
            Gizmos.DrawLine(target.position, transform.position);
        }
    }
}