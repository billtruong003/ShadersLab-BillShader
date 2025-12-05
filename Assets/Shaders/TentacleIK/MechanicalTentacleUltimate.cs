using UnityEngine;
using System.Collections.Generic;

namespace Game.Mechanics
{
    public class MechanicalTentacleUltimate : MonoBehaviour
    {
        [System.Serializable]
        public struct ClawData
        {
            public Transform root;
            public Transform[] segments;
            public Quaternion[] bindPoses;
            public Vector3 localHinge;
            public Transform tip;
        }

        [Header("Core References")]
        [SerializeField] private Transform target;
        [SerializeField] private Transform palmCenter;
        [SerializeField] private Transform armRoot;

        [Header("Arm Config")]
        [SerializeField] private int armIterations = 10;
        [Range(0f, 1f)][SerializeField] private float armDamping = 0.1f;
        [SerializeField] private float jointLimitAngle = 45f;
        [SerializeField] private Vector3 armForward = Vector3.up;
        [SerializeField] private Vector3 armUp = Vector3.forward;

        [Header("Hand Config")]
        [SerializeField] private int clawIterations = 10;
        [Range(0f, 1f)][SerializeField] private float grabStrength = 0f;
        [SerializeField] private float wristSpinSpeed = 180f;

        // Runtime Cache (Arrays > Lists for performance)
        private Transform[] armBones;
        private float[] armLengths;
        private Vector3[] solverPositions;
        private ClawData[] claws;
        private int armCount;
        private float armTotalLen;
        private Quaternion axisCorrection;
        private float currentSpin;

        private void Awake()
        {
            CacheArm();
            CacheClaws();
            axisCorrection = Quaternion.Inverse(Quaternion.LookRotation(armForward, armUp));
        }

        private void LateUpdate()
        {
            if (target == null || armCount == 0) return;

            SolveArmFABRIK();
            ApplyArmToScene();
            SolveHandCCD();
        }

        private void CacheArm()
        {
            if (armRoot == null) return;

            var bones = new List<Transform>();
            var curr = armRoot;
            while (curr.childCount > 0)
            {
                bones.Add(curr);
                // Stop if we hit the wrist/palm splitter
                if (curr.name.Contains("Wrist") || curr.childCount > 1) break;
                curr = curr.GetChild(0);
            }
            // Ensure wrist is added
            if (!bones.Contains(curr)) bones.Add(curr);

            armBones = bones.ToArray();
            armCount = armBones.Length;
            armLengths = new float[armCount - 1];
            solverPositions = new Vector3[armCount];
            armTotalLen = 0;

            for (int i = 0; i < armCount - 1; i++)
            {
                armLengths[i] = Vector3.Distance(armBones[i].position, armBones[i + 1].position);
                armTotalLen += armLengths[i];
            }
        }

        private void CacheClaws()
        {
            if (palmCenter == null) return;

            var clawList = new List<ClawData>();
            Transform wrist = armBones[armCount - 1];

            foreach (Transform child in wrist)
            {
                if (child == palmCenter || child.name.Contains("Mesh")) continue;

                var segs = new List<Transform>();
                var t = child;
                while (t.childCount > 0)
                {
                    segs.Add(t);
                    t = t.GetChild(0);
                }
                segs.Add(t); // Tip

                var data = new ClawData
                {
                    root = child,
                    segments = segs.ToArray(),
                    bindPoses = new Quaternion[segs.Count],
                    localHinge = Vector3.right, // Logic detect axis tự động ở đây nếu cần
                    tip = segs[segs.Count - 1]
                };

                for (int i = 0; i < segs.Count; i++) data.bindPoses[i] = segs[i].localRotation;
                clawList.Add(data);
            }
            claws = clawList.ToArray();
        }

        private void SolveArmFABRIK()
        {
            for (int i = 0; i < armCount; i++) solverPositions[i] = armBones[i].position;

            Vector3 rootPos = armBones[0].position;
            Vector3 targetPos = target.position;
            float distToTarget = Vector3.Distance(rootPos, targetPos);

            if (distToTarget >= armTotalLen)
            {
                Vector3 dir = (targetPos - rootPos).normalized;
                for (int i = 0; i < armCount - 1; i++)
                    solverPositions[i + 1] = solverPositions[i] + dir * armLengths[i];
            }
            else
            {
                for (int iter = 0; iter < armIterations; iter++)
                {
                    // Backward
                    solverPositions[armCount - 1] = targetPos;
                    for (int i = armCount - 2; i >= 0; i--)
                    {
                        Vector3 dir = (solverPositions[i] - solverPositions[i + 1]).normalized;
                        solverPositions[i] = solverPositions[i + 1] + dir * armLengths[i];
                    }

                    // Forward
                    solverPositions[0] = rootPos;
                    for (int i = 1; i < armCount; i++)
                    {
                        Vector3 dir = (solverPositions[i] - solverPositions[i - 1]).normalized;
                        solverPositions[i] = solverPositions[i - 1] + dir * armLengths[i - 1];
                    }
                }
            }
        }

        private void ApplyArmToScene()
        {
            // Apply Rotation for Arm Segments
            for (int i = 0; i < armCount - 1; i++)
            {
                Vector3 dir = (solverPositions[i + 1] - solverPositions[i]).normalized;
                if (dir == Vector3.zero) continue;

                Vector3 upHint = (i == 0) ? armRoot.up : armBones[i - 1].up; // Continuity hint
                Quaternion targetRot = Quaternion.LookRotation(dir, upHint) * axisCorrection;

                // Joint Limit Logic (Applied via Rotation Clamp to prevent breaking position too much)
                if (i > 0)
                {
                    Quaternion parentRot = armBones[i - 1].rotation;
                    Quaternion localRot = Quaternion.Inverse(parentRot) * targetRot;
                    // Clamp logic can be added here if needed, but FABRIK usually handles visual well
                }

                armBones[i].rotation = Quaternion.Slerp(armBones[i].rotation, targetRot, 1f - armDamping);
            }

            // Handle Wrist
            Transform wrist = armBones[armCount - 1];
            wrist.position = solverPositions[armCount - 1]; // FABRIK dictates position

            // Wrist Rotation: Drill Spin + Alignment
            currentSpin += Time.deltaTime * wristSpinSpeed;
            Vector3 armDir = (solverPositions[armCount - 1] - solverPositions[armCount - 2]).normalized;
            Quaternion baseLook = Quaternion.LookRotation(armDir, wrist.parent.up) * axisCorrection;
            wrist.rotation = baseLook * Quaternion.AngleAxis(currentSpin, armForward);
        }

        private void SolveHandCCD()
        {
            if (claws == null) return;

            Vector3 grabTarget = palmCenter.position;

            for (int c = 0; c < claws.Length; c++)
            {
                ref ClawData claw = ref claws[c];

                // Return to bind pose if not grabbing
                if (grabStrength <= 0.01f)
                {
                    for (int i = 0; i < claw.segments.Length; i++)
                        claw.segments[i].localRotation = Quaternion.Slerp(claw.segments[i].localRotation, claw.bindPoses[i], Time.deltaTime * 10f);
                    continue;
                }

                // CCD Solver (Warm start - no reset)
                Transform tip = claw.tip;
                Vector3 desired = Vector3.Lerp(tip.position, grabTarget, grabStrength); // Soft goal

                for (int iter = 0; iter < clawIterations; iter++)
                {
                    if ((tip.position - desired).sqrMagnitude < 0.0001f) break;

                    for (int i = claw.segments.Length - 2; i >= 0; i--) // Skip tip itself
                    {
                        Transform bone = claw.segments[i];
                        Vector3 toEnd = tip.position - bone.position;
                        Vector3 toTarget = desired - bone.position;

                        // Project to Hinge Plane
                        Vector3 axis = bone.TransformDirection(claw.localHinge);
                        Vector3 fromDir = Vector3.ProjectOnPlane(toEnd, axis).normalized;
                        Vector3 toDir = Vector3.ProjectOnPlane(toTarget, axis).normalized;

                        float angle = Vector3.SignedAngle(fromDir, toDir, axis);

                        // Limit angle per step for stability
                        angle = Mathf.Clamp(angle, -jointLimitAngle, jointLimitAngle);

                        // Apply
                        bone.rotation = Quaternion.AngleAxis(angle, axis) * bone.rotation;
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (armBones == null || armBones.Length == 0) return;
            Gizmos.color = Color.green;
            for (int i = 0; i < armBones.Length - 1; i++)
                Gizmos.DrawLine(armBones[i].position, armBones[i + 1].position);

            if (palmCenter && claws != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(palmCenter.position, 0.05f * grabStrength);
            }
        }
    }
}