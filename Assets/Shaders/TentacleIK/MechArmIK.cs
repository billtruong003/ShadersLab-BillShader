using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Mechanics
{
    public class MechArmIK : MonoBehaviour
    {
        [Header("Targeting")]
        [SerializeField] private Transform target;
        [SerializeField] private Transform poleTarget; // Optional: để chỉnh hướng khuỷu tay

        [Header("Settings")]
        [SerializeField] private int iterations = 10;
        [SerializeField, Range(0f, 1f)] private float smoothTime = 0.05f;
        [SerializeField, Range(0f, 1f)] private float stiffness = 0.8f; // Càng cao càng cứng form ban đầu

        [Header("Setup (Auto-filled)")]
        [SerializeField] private Transform rootBone;
        [SerializeField] private Transform wristBone; // Bone.007
        [SerializeField] private List<Transform> armBones = new List<Transform>();

        // Cache
        private float[] boneLengths;
        private float totalLength;
        private Vector3[] positions;
        private Vector3[] startDirections;
        private Quaternion[] startRotations;
        private int boneCount;

        // Runtime
        private Vector3[] solverPositions; // Double buffer để tính toán logic
        private Quaternion targetWristRot;

        private void Awake()
        {
            if (armBones.Count == 0) InitBones();
        }

        private void LateUpdate()
        {
            if (target == null || boneCount == 0) return;
            SolveIK();
            ApplyToTransforms();
        }

        private void SolveIK()
        {
            // 1. Copy current positions to solver
            for (int i = 0; i < boneCount; i++) solverPositions[i] = armBones[i].position;

            Vector3 rootPos = armBones[0].position;
            Vector3 targetPos = target.position;
            Quaternion targetRot = target.rotation;

            // 2. Distance Check (Hard Clamp Logic)
            float distToTarget = Vector3.Distance(rootPos, targetPos);

            // Nếu xa quá tầm với -> Duỗi thẳng tắp về hướng target (NO STRETCH)
            if (distToTarget >= totalLength)
            {
                Vector3 dir = (targetPos - rootPos).normalized;
                for (int i = 0; i < boneCount - 1; i++)
                {
                    solverPositions[i + 1] = solverPositions[i] + dir * boneLengths[i];
                }
            }
            else
            {
                // FABRIK Loop
                for (int iter = 0; iter < iterations; iter++)
                {
                    // Backward (Tip -> Root)
                    solverPositions[boneCount - 1] = targetPos;
                    for (int i = boneCount - 2; i >= 0; i--)
                    {
                        Vector3 dir = (solverPositions[i] - solverPositions[i + 1]).normalized;
                        solverPositions[i] = solverPositions[i + 1] + dir * boneLengths[i];
                    }

                    // Forward (Root -> Tip)
                    solverPositions[0] = rootPos;
                    for (int i = 1; i < boneCount; i++)
                    {
                        Vector3 dir = (solverPositions[i] - solverPositions[i - 1]).normalized;

                        // Mechanical Stiffness: Níu hướng về hướng ban đầu để tạo cảm giác robot
                        if (stiffness > 0)
                        {
                            // Tính toán vector cứng dựa trên rotation của bone cha
                            // Đây là trick để tay không bị mềm oặt như xúc tu
                            Vector3 stiffDir = (armBones[i].position - armBones[i - 1].position).normalized;
                            dir = Vector3.Lerp(dir, stiffDir, stiffness / (iter + 1));
                        }

                        solverPositions[i] = solverPositions[i - 1] + dir.normalized * boneLengths[i - 1];
                    }
                }
            }
        }

        private void ApplyToTransforms()
        {
            for (int i = 0; i < boneCount - 1; i++)
            {
                // Rotate bone để hướng về solver position tiếp theo
                Vector3 targetDir = (solverPositions[i + 1] - solverPositions[i]).normalized;

                // Pole constraint (Optional - đơn giản hóa)
                if (poleTarget != null && i == 1) // Ví dụ áp dụng pole cho khuỷu tay
                {
                    // Logic pole nâng cao nằm ở đây, nhưng với arm nhiều khúc, 
                    // ta dùng Quaternion.FromToRotation là đủ nhanh và đẹp.
                }

                // Preserving twist: Dùng startDirections để tính rotation offset
                Quaternion aim = Quaternion.FromToRotation(startDirections[i], targetDir);
                armBones[i].rotation = Quaternion.Slerp(armBones[i].rotation, aim * startRotations[i], 1f - smoothTime);
            }

            // WRIST LOCK: Bone cuối cùng (Bone.007) copy hoàn toàn rotation của Target
            // Đây là mấu chốt để tay cầm đồ vật đúng hướng
            armBones[boneCount - 1].rotation = Quaternion.Slerp(armBones[boneCount - 1].rotation, target.rotation, 1f - smoothTime);

            // Sync position của wrist vào target để tránh visual drift
            // (Chỉ apply position cho bone cuối nếu cần độ chính xác tuyệt đối, còn lại xương nối nhau bằng hierarchy)
        }

        [ContextMenu("Auto Setup Arm")]
        public void InitBones()
        {
            armBones.Clear();
            if (rootBone == null) rootBone = transform;

            Transform current = rootBone;
            armBones.Add(current);

            // Tìm đường đi đến Wrist (Bone.007)
            // Lưu ý: Logic này giả định chain đi thẳng (GetChild(0)). 
            // Nếu hierarchy phức tạp hơn, hãy kéo tay vào list armBones.
            while (current != wristBone && current.childCount > 0)
            {
                // Ưu tiên tìm child có tên khớp pattern hoặc đi thẳng
                current = current.GetChild(0);
                armBones.Add(current);
            }

            boneCount = armBones.Count;
            boneLengths = new float[boneCount - 1];
            startDirections = new Vector3[boneCount];
            startRotations = new Quaternion[boneCount];
            solverPositions = new Vector3[boneCount];
            totalLength = 0;

            for (int i = 0; i < boneCount; i++)
            {
                startRotations[i] = armBones[i].rotation;
                if (i < boneCount - 1)
                {
                    Vector3 vectorToChild = armBones[i + 1].position - armBones[i].position;
                    boneLengths[i] = vectorToChild.magnitude;
                    startDirections[i] = vectorToChild.normalized;
                    totalLength += boneLengths[i];
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (armBones.Count == 0) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < armBones.Count - 1; i++)
                Gizmos.DrawLine(armBones[i].position, armBones[i + 1].position);
        }
    }
}