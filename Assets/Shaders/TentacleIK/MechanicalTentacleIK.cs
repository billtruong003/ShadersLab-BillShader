using UnityEngine;
using System.Collections.Generic;

namespace Game.Mechanics
{
    public class MechanicalTentacleIK : MonoBehaviour
    {
        [Header("Servo Configuration")]
        [SerializeField] private float servoSpeed = 200f; // Tăng lên chút cho nhanh
        [SerializeField] private float jointLimitAngle = 60f;

        [Header("Solver Settings")]
        [SerializeField] private int iterations = 10;

        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private Transform rootBone;
        [SerializeField] private List<Transform> bones = new List<Transform>();

        // Cache Data
        private float[] boneLengths;
        private float totalLength;
        private Vector3[] virtualPositions;

        // Rotation Correction Data
        private Vector3[] initialLocalBoneDirs; // Lưu hướng xương cục bộ (Local Direction)

        private int boneCount;

        private void Start() // Đổi sang Start để đảm bảo Transform đã init xong
        {
            InitializeSkeleton();
        }

        private void InitializeSkeleton()
        {
            if (rootBone == null) rootBone = transform;
            if (bones.Count == 0) AutoAssignBones();

            boneCount = bones.Count;
            if (boneCount == 0) return;

            boneLengths = new float[boneCount - 1];
            virtualPositions = new Vector3[boneCount];
            initialLocalBoneDirs = new Vector3[boneCount - 1];

            totalLength = 0;

            for (int i = 0; i < boneCount; i++)
            {
                virtualPositions[i] = bones[i].position;

                if (i < boneCount - 1)
                {
                    Vector3 worldDir = bones[i + 1].position - bones[i].position;
                    boneLengths[i] = worldDir.magnitude;
                    totalLength += boneLengths[i];

                    // Tính hướng xương cục bộ relative với Rotation
                    // Công thức này giúp lưu hướng mà KHÔNG lưu Scale
                    initialLocalBoneDirs[i] = Quaternion.Inverse(bones[i].rotation) * worldDir.normalized;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null || boneCount == 0) return;

            SolveFABRIK();
            ApplyMechanicalMovement();
        }

        private void SolveFABRIK()
        {
            virtualPositions[0] = bones[0].position;
            Vector3 targetPos = target.position;

            float dist = Vector3.Distance(virtualPositions[0], targetPos);

            // Logic duỗi thẳng nếu target quá xa (Mechanical Limit)
            if (dist >= totalLength)
            {
                Vector3 dir = (targetPos - virtualPositions[0]).normalized;
                for (int i = 1; i < boneCount; i++)
                    virtualPositions[i] = virtualPositions[i - 1] + dir * boneLengths[i - 1];
            }
            else
            {
                for (int i = 0; i < boneCount; i++) virtualPositions[i] = bones[i].position;

                for (int it = 0; it < iterations; it++)
                {
                    // Backward
                    virtualPositions[boneCount - 1] = targetPos;
                    for (int i = boneCount - 2; i >= 0; i--)
                    {
                        Vector3 dir = (virtualPositions[i] - virtualPositions[i + 1]).normalized;
                        virtualPositions[i] = virtualPositions[i + 1] + dir * boneLengths[i];
                    }

                    // Forward
                    virtualPositions[0] = bones[0].position;
                    for (int i = 1; i < boneCount; i++)
                    {
                        Vector3 dir = (virtualPositions[i] - virtualPositions[i - 1]).normalized;
                        virtualPositions[i] = virtualPositions[i - 1] + dir * boneLengths[i - 1];
                    }
                }
            }
        }

        private void ApplyMechanicalMovement()
        {
            // 1. Root Rotation
            if (boneCount > 1)
            {
                Vector3 targetDir = (virtualPositions[1] - bones[0].position).normalized;
                RotateBoneSafe(0, targetDir);
            }

            // 2. Chain Rotation & Position Fix
            for (int i = 0; i < boneCount - 1; i++)
            {
                Transform currentBone = bones[i];
                Transform nextBone = bones[i + 1];

                Vector3 desiredDir = (virtualPositions[i + 1] - virtualPositions[i]).normalized;

                // Xoay xương
                RotateBoneSafe(i, desiredDir);

                // --- FIX QUAN TRỌNG Ở ĐÂY ---
                // Thay vì dùng TransformPoint (bị ảnh hưởng bởi Scale), ta dùng Rotation thuần túy + Độ dài gốc
                // Công thức: Pos mới = Pos cũ + (Hướng xoay hiện tại * Vector hướng gốc * Độ dài gốc)
                nextBone.position = currentBone.position + (currentBone.rotation * initialLocalBoneDirs[i] * boneLengths[i]);
            }

            // 3. Tip Rotation
            Transform tipBone = bones[boneCount - 1];
            tipBone.rotation = Quaternion.RotateTowards(tipBone.rotation, target.rotation, servoSpeed * Time.deltaTime);
        }

        private void RotateBoneSafe(int index, Vector3 targetWorldDir)
        {
            Transform bone = bones[index];

            // Lấy hướng hiện tại của xương trong World Space dựa trên Rotation hiện tại
            Vector3 currentHeading = bone.rotation * initialLocalBoneDirs[index];

            // Tính góc xoay cần thiết để đưa hướng hiện tại về hướng target
            Quaternion swing = Quaternion.FromToRotation(currentHeading, targetWorldDir);
            Quaternion targetRotation = swing * bone.rotation;

            // Constraint: Giới hạn góc gập
            if (index > 0)
            {
                Quaternion parentRot = bones[index - 1].rotation;
                if (Quaternion.Angle(parentRot, targetRotation) > jointLimitAngle)
                {
                    targetRotation = Quaternion.RotateTowards(parentRot, targetRotation, jointLimitAngle);
                }
            }

            // Apply Servo Speed
            bone.rotation = Quaternion.RotateTowards(bone.rotation, targetRotation, servoSpeed * Time.deltaTime);
        }

        [ContextMenu("Auto Assign Bones")]
        public void AutoAssignBones()
        {
            bones.Clear();
            if (rootBone == null) rootBone = transform;

            Transform current = rootBone;
            while (current.childCount > 0)
            {
                bones.Add(current);
                current = current.GetChild(0);
            }
            bones.Add(current);
        }
    }
}