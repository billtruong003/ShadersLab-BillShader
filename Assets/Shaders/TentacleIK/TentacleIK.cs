using UnityEngine;
using System.Collections.Generic;
using System;

namespace Game.Mechanics
{
    public class MechanicalTentacleFinal : MonoBehaviour
    {
        [System.Serializable]
        public class ClawCluster
        {
            public Transform rootJoint;
            public Transform[] segments;
            [HideInInspector] public Quaternion[] initialRotations;
            public Vector3 localHingeAxis = Vector3.right;

            public Transform Tip
            {
                get
                {
                    if (segments == null || segments.Length == 0) return null;
                    Transform last = segments[segments.Length - 1];
                    return last.childCount > 0 ? last.GetChild(0) : last;
                }
            }
        }

        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private Transform palmCenter;
        [SerializeField] private Transform armRoot;
        [SerializeField] private Transform handWrist;

        [Header("Arm Settings")]
        [SerializeField] private Transform[] armBones;
        [SerializeField] private int armSolverIterations = 20;
        [SerializeField] private float maxArmBendAnglePerJoint = 60f;
        [Range(0f, 1f)][SerializeField] private float armRotationDamping = 0.05f;

        [Header("Claw Settings")]
        [SerializeField] private ClawCluster[] claws;
        [SerializeField] private int clawSolverIterations = 15;
        [Range(0f, 1f)][SerializeField] private float grabStrength = 0f;
        [SerializeField] private float maxClawAnglePerJoint = 90f;
        [SerializeField] private float wristSpinSpeed = 180f;

        [Header("Axis Configuration")]
        [SerializeField] private Vector3 armForwardAxis = Vector3.up;
        [SerializeField] private Vector3 armUpAxis = Vector3.forward;

        private Vector3[] armPositions;
        private float[] armBoneLengths;
        private float totalArmLength;
        private int armBoneCount;
        private Quaternion armAxisCorrection;

        private void Awake()
        {
            InitializeArm();
            InitializeClaws();
            armAxisCorrection = Quaternion.Inverse(Quaternion.LookRotation(armForwardAxis, armUpAxis));
        }

        private void LateUpdate()
        {
            if (armBoneCount < 2 || target == null) return;

            SolveArmFABRIK();
            ApplyArmRotations();
            ConstrainArmJointLimits();
            SolveClawCCD();
        }

        private void InitializeArm()
        {
            if (armBones == null || armBones.Length == 0) return;

            armBoneCount = armBones.Length;
            armPositions = new Vector3[armBoneCount];
            armBoneLengths = new float[armBoneCount - 1];
            totalArmLength = 0f;

            for (int i = 0; i < armBoneCount; i++)
            {
                armPositions[i] = armBones[i].position;
                if (i < armBoneCount - 1)
                {
                    armBoneLengths[i] = Vector3.Distance(armBones[i].position, armBones[i + 1].position);
                    totalArmLength += armBoneLengths[i];
                }
            }
        }

        private void InitializeClaws()
        {
            if (claws == null) return;

            foreach (var claw in claws)
            {
                if (claw.segments == null) continue;
                claw.initialRotations = new Quaternion[claw.segments.Length];
                for (int i = 0; i < claw.segments.Length; i++)
                {
                    if (claw.segments[i])
                        claw.initialRotations[i] = claw.segments[i].localRotation;
                }

                if (palmCenter != null && claw.segments.Length > 0)
                {
                    Transform seg = claw.segments[0];
                    Quaternion originalRot = seg.localRotation;
                    Transform tip = claw.Tip;

                    seg.localRotation = originalRot * Quaternion.AngleAxis(20f, Vector3.right);
                    float distPos = Vector3.SqrMagnitude(tip.position - palmCenter.position);

                    seg.localRotation = originalRot * Quaternion.AngleAxis(-20f, Vector3.right);
                    float distNeg = Vector3.SqrMagnitude(tip.position - palmCenter.position);

                    seg.localRotation = originalRot;
                    // claw.localHingeAxis = distNeg < distPos ? Vector3.left : Vector3.right;
                }
            }
        }

        private void SolveArmFABRIK()
        {
            for (int i = 0; i < armBoneCount; i++) armPositions[i] = armBones[i].position;

            Vector3 rootPos = armBones[0].position;
            Vector3 targetPos = target.position;

            if ((targetPos - rootPos).sqrMagnitude >= totalArmLength * totalArmLength)
            {
                Vector3 dir = (targetPos - rootPos).normalized;
                for (int i = 0; i < armBoneCount - 1; i++)
                    armPositions[i + 1] = armPositions[i] + dir * armBoneLengths[i];
            }
            else
            {
                for (int iter = 0; iter < armSolverIterations; iter++)
                {
                    armPositions[armBoneCount - 1] = targetPos;
                    for (int i = armBoneCount - 2; i >= 0; i--)
                    {
                        Vector3 dir = (armPositions[i] - armPositions[i + 1]).normalized;
                        armPositions[i] = armPositions[i + 1] + dir * armBoneLengths[i];
                    }

                    armPositions[0] = rootPos;
                    for (int i = 1; i < armBoneCount; i++)
                    {
                        Vector3 dir = (armPositions[i] - armPositions[i - 1]).normalized;
                        armPositions[i] = armPositions[i - 1] + dir * armBoneLengths[i - 1];
                    }
                }
            }
        }

        private void ApplyArmRotations()
        {
            for (int i = 0; i < armBoneCount - 1; i++)
            {
                Vector3 dir = (armPositions[i + 1] - armPositions[i]).normalized;
                if (dir.sqrMagnitude < 0.0001f) continue;

                Vector3 up = (i == 0) ? armBones[i].up : armBones[i].parent.TransformDirection(armUpAxis);
                Quaternion targetRot = Quaternion.LookRotation(dir, up) * armAxisCorrection;

                armBones[i].rotation = armRotationDamping > 0.001f
                    ? Quaternion.Slerp(armBones[i].rotation, targetRot, 1f - armRotationDamping)
                    : targetRot;
            }

            if (handWrist != null && armBoneCount >= 2)
            {
                handWrist.position = armPositions[armBoneCount - 1];
                Vector3 lastSegmentDir = (armPositions[armBoneCount - 1] - armPositions[armBoneCount - 2]).normalized;
                Vector3 wristUp = handWrist.parent.TransformDirection(armUpAxis);

                Quaternion baseLook = Quaternion.LookRotation(lastSegmentDir, wristUp) * armAxisCorrection;
                Quaternion drillSpin = Quaternion.AngleAxis(Time.time * wristSpinSpeed, armForwardAxis);

                handWrist.rotation = baseLook * drillSpin;
            }
        }

        private void ConstrainArmJointLimits()
        {
            for (int i = 1; i < armBoneCount; i++)
            {
                Transform bone = armBones[i];
                Transform parentBone = armBones[i - 1];

                Vector3 parentDir = parentBone.TransformDirection(armForwardAxis);
                Vector3 childDir = bone.TransformDirection(armForwardAxis);

                float angle = Vector3.Angle(parentDir, childDir);

                if (angle > maxArmBendAnglePerJoint)
                {
                    float excess = angle - maxArmBendAnglePerJoint;
                    Vector3 cross = Vector3.Cross(parentDir, childDir);

                    if (cross.sqrMagnitude < 0.0001f) cross = parentBone.right;

                    Quaternion correction = Quaternion.AngleAxis(-excess, cross.normalized);
                    bone.rotation = correction * bone.rotation;

                    for (int j = i + 1; j < armBoneCount; j++)
                    {
                        armBones[j].rotation = correction * armBones[j].rotation;
                    }
                }
            }
        }

        private void SolveClawCCD()
        {
            if (claws == null || claws.Length == 0 || palmCenter == null) return;

            foreach (var claw in claws)
            {
                for (int i = 0; i < claw.segments.Length; i++)
                    claw.segments[i].localRotation = claw.initialRotations[i];

                if (grabStrength < 0.001f) continue;

                Transform tip = claw.Tip;
                Vector3 desiredWorld = Vector3.Lerp(tip.position, palmCenter.position, grabStrength);

                for (int it = 0; it < clawSolverIterations; it++)
                {
                    if ((tip.position - desiredWorld).sqrMagnitude < 0.0001f) break;

                    for (int j = claw.segments.Length - 1; j >= 0; j--)
                    {
                        Transform bone = claw.segments[j];

                        Vector3 toTip = tip.position - bone.position;
                        Vector3 toDesired = desiredWorld - bone.position;

                        Vector3 hingeAxisWorld = bone.TransformDirection(claw.localHingeAxis);

                        Vector3 fromDir = Vector3.ProjectOnPlane(toTip, hingeAxisWorld).normalized;
                        Vector3 toDir = Vector3.ProjectOnPlane(toDesired, hingeAxisWorld).normalized;

                        if (fromDir.sqrMagnitude < 0.001f || toDir.sqrMagnitude < 0.001f) continue;

                        float signedAngle = Vector3.SignedAngle(fromDir, toDir, hingeAxisWorld);
                        signedAngle = Mathf.Clamp(signedAngle, -10f, maxClawAnglePerJoint);

                        if (Mathf.Abs(signedAngle) < 0.1f) continue;

                        bone.rotation = Quaternion.AngleAxis(signedAngle, hingeAxisWorld) * bone.rotation;
                    }
                }
            }
        }

        [ContextMenu("Auto Setup")]
        public void AutoSetup()
        {
            if (!armRoot) armRoot = transform;

            List<Transform> bones = new List<Transform>();
            Transform current = armRoot;
            handWrist = null;

            while (current.childCount > 0)
            {
                bones.Add(current);
                if (current.childCount > 1)
                {
                    handWrist = current;
                    break;
                }
                current = current.GetChild(0);
                handWrist = current;
            }
            if (handWrist && !bones.Contains(handWrist)) bones.Add(handWrist);
            armBones = bones.ToArray();

            if (handWrist)
            {
                if (!palmCenter)
                    palmCenter = handWrist.Find("PalmCenter") ?? handWrist.Find("Center");

                List<ClawCluster> clawList = new List<ClawCluster>();
                foreach (Transform child in handWrist)
                {
                    if (child == palmCenter) continue;

                    List<Transform> segments = new List<Transform>();
                    GetChildrenRecursive(child, segments);

                    if (segments.Count > 0)
                    {
                        clawList.Add(new ClawCluster
                        {
                            rootJoint = child,
                            segments = segments.ToArray()
                        });
                    }
                }
                claws = clawList.ToArray();
            }
        }

        private void GetChildrenRecursive(Transform t, List<Transform> list)
        {
            list.Add(t);
            if (t.childCount > 0) GetChildrenRecursive(t.GetChild(0), list);
        }

        private void OnDrawGizmos()
        {
            if (palmCenter)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(palmCenter.position, 0.05f);

                if (claws != null && grabStrength > 0.01f)
                {
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
                    foreach (var c in claws)
                    {
                        if (c.Tip == null) continue;
                        Vector3 desired = Vector3.Lerp(c.Tip.position, palmCenter.position, grabStrength);
                        Gizmos.DrawLine(c.Tip.position, desired);
                    }
                }
            }
        }
    }
}