using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Mechanics
{
    public class MechHandPro : MonoBehaviour
    {
        [System.Serializable]
        public class FingerConfig
        {
            public string name = "Finger";
            public Transform rootBone;

            [Header("Axis Configuration")]
            [Tooltip("Trục xoay của khớp (Local Space). Đỏ=X, XanhLá=Y, XanhDương=Z")]
            public Vector3 rotationAxis = new Vector3(1, 0, 0);

            [Tooltip("Hướng mặt của ngón tay (phần thịt) dùng để hướng về PalmCenter")]
            public Vector3 faceDirection = new Vector3(0, -1, 0);

            [Header("Limits")]
            [Range(-180, 180)] public float minAngle = -10f;
            [Range(-180, 180)] public float maxAngle = 90f;
        }

        [Header("Targeting")]
        [SerializeField] private Transform palmCenter;

        [Header("Controls")]
        [Range(0f, 1f)] public float gripStrength = 0f;
        [SerializeField] private float smoothSpeed = 15f;

        [Header("Fingers Setup")]
        [SerializeField] private List<FingerConfig> fingers = new List<FingerConfig>();

        // --- Runtime Cache (Zero GC) ---
        private class RuntimeJoint
        {
            public Transform transform;
            public Quaternion initialLocalRot;
            public Vector3 axis;
            public Vector3 faceDir;
            public float min, max;
        }
        private List<List<RuntimeJoint>> runtimeFingers = new List<List<RuntimeJoint>>();
        private float currentGrip = 0f;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            runtimeFingers.Clear();
            if (palmCenter == null)
            {
                // Fallback: Nếu không có PalmCenter, tạo tạm 1 cái ở phía trước wrist để tránh crash
                Debug.LogWarning("Missing PalmCenter! Using implicit forward point.");
            }

            foreach (var config in fingers)
            {
                if (config.rootBone == null) continue;

                List<RuntimeJoint> chain = new List<RuntimeJoint>();
                Transform current = config.rootBone;

                // Normalize input vectors để đảm bảo math đúng
                Vector3 axisNorm = config.rotationAxis.normalized;
                Vector3 faceNorm = config.faceDirection.normalized;

                while (current != null)
                {
                    chain.Add(new RuntimeJoint
                    {
                        transform = current,
                        initialLocalRot = current.localRotation,
                        axis = axisNorm,
                        faceDir = faceNorm,
                        min = config.minAngle,
                        max = config.maxAngle
                    });

                    // Move to next child
                    if (current.childCount > 0) current = current.GetChild(0);
                    else current = null;
                }
                runtimeFingers.Add(chain);
            }
        }

        private void LateUpdate()
        {
            if (runtimeFingers.Count == 0) return;

            // Lerp Grip value
            currentGrip = Mathf.Lerp(currentGrip, gripStrength, Time.deltaTime * smoothSpeed);

            // Nếu grip = 0, trả về trạng thái mở hoàn toàn cho nhẹ math
            if (currentGrip <= 0.001f)
            {
                ResetToOpen();
                return;
            }

            Vector3 targetPos = palmCenter != null ? palmCenter.position : (transform.position + transform.forward * 0.1f);

            foreach (var chain in runtimeFingers)
            {
                foreach (var joint in chain)
                {
                    // 1. Calculate Target Angle based on Palm Center
                    // Vector từ khớp tới Palm Center
                    Vector3 toTarget = targetPos - joint.transform.position;
                    // Chuyển sang Local Space của khớp
                    Vector3 localToTarget = joint.transform.InverseTransformDirection(toTarget);

                    // Chiếu vector đích lên mặt phẳng xoay (Plane vuông góc với Axis)
                    Vector3 projectedDir = Vector3.ProjectOnPlane(localToTarget, joint.axis).normalized;

                    // Tính góc giữa "mặt ngón tay" và hướng đích
                    float angle = Vector3.SignedAngle(joint.faceDir, projectedDir, joint.axis);

                    // Clamp góc xoay theo giới hạn vật lý
                    angle = Mathf.Clamp(angle, joint.min, joint.max);

                    // 2. Apply Rotation
                    // Grip càng lớn thì càng xoay tiệm cận về góc mục tiêu
                    Quaternion targetRot = joint.initialLocalRot * Quaternion.AngleAxis(angle, joint.axis);
                    joint.transform.localRotation = Quaternion.Slerp(joint.initialLocalRot, targetRot, currentGrip);
                }
            }
        }

        private void ResetToOpen()
        {
            foreach (var chain in runtimeFingers)
            {
                foreach (var joint in chain)
                {
                    joint.transform.localRotation = Quaternion.Slerp(joint.transform.localRotation, joint.initialLocalRot, Time.deltaTime * smoothSpeed);
                }
            }
        }

        // --- EDITOR HELPERS ---

        [ContextMenu("Auto Setup From Children")]
        public void AutoSetup()
        {
            fingers.Clear();
            foreach (Transform child in transform)
            {
                // Bỏ qua các object không phải xương (ví dụ PalmCenter, Mesh)
                if (child.name.Contains("Center") || child.name.Contains("Mesh")) continue;

                // Tạo config mặc định
                FingerConfig config = new FingerConfig
                {
                    name = child.name,
                    rootBone = child,
                    rotationAxis = new Vector3(1, 0, 0), // Default X Axis
                    faceDirection = new Vector3(0, -1, 0) // Default Down (-Y)
                };
                fingers.Add(config);
            }
            Debug.Log($"<color=green>Auto Setup found {fingers.Count} finger roots. Check axis via Gizmos now!</color>");
        }

        private void OnDrawGizmosSelected()
        {
            if (fingers == null) return;

            foreach (var f in fingers)
            {
                if (f.rootBone == null) continue;
                Transform cur = f.rootBone;
                while (cur != null)
                {
                    Gizmos.matrix = cur.localToWorldMatrix;

                    // Vẽ trục xoay (Axis) - Màu vàng
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(Vector3.zero, f.rotationAxis.normalized * 0.02f);

                    // Vẽ hướng mặt ngón (Face) - Màu xanh cyan
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawRay(Vector3.zero, f.faceDirection.normalized * 0.01f);

                    // Vẽ đĩa xoay tượng trưng
#if UNITY_EDITOR
                    Handles.color = new Color(1, 1, 0, 0.1f);
                    Handles.matrix = cur.localToWorldMatrix;
                    Handles.DrawSolidArc(Vector3.zero, f.rotationAxis, f.faceDirection, f.maxAngle, 0.015f);
#endif

                    if (cur.childCount > 0) cur = cur.GetChild(0);
                    else cur = null;
                }
            }

            // Draw Palm Center
            if (palmCenter != null)
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(palmCenter.position, 0.01f);
            }
        }
    }
}