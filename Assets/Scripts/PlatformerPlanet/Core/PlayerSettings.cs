using UnityEngine;
using Sirenix.OdinInspector;

namespace PlatformerPlanet
{
    [CreateAssetMenu(fileName = "PlatformerPlayerSettings", menuName = "PlatformerPlanet/Player Settings")]
    public class PlayerSettings : ScriptableObject
    {
        [Title("Movement")]
        public float MoveSpeed = 8f;
        [Range(0f, 1f)] public float MovementSmoothTime = 0.05f;

        [Title("Jumping & Gravity")]
        public float JumpHeight = 2.2f;
        public float Gravity = -30f;
        [Tooltip("Gives a small window of time to jump even after leaving a ledge.")]
        public float CoyoteTime = 0.15f;

        [Title("Air Control")]
        public float AirControlSpeed = 6f;
        [Range(0f, 1f)] public float AirControlSmoothTime = 0.1f;

        [Title("Ground Check")]
        public LayerMask GroundLayer;
        public float GroundCheckDistance = 0.1f;
        public Vector3 GroundCheckOffset = Vector3.zero;

        [Title("Wall Check")]
        public LayerMask WallLayer;
        public float WallCheckDistance = 0.5f;

        [Title("Push & Pull")]
        public float PushSpeed = 4f;
        public LayerMask PushableLayer;

        [Title("Swimming")]
        public float SwimSpeed = 5f;
        public float SwimBuoyancy = 15f;
        [Range(0f, 1f)] public float SwimDamping = 0.1f;

        [Title("Flying")]
        public float FlySpeed = 10f;
        [Range(0f, 1f)] public float FlyDamping = 0.08f;

        [Title("Abilities")]
        public float TeleportDistance = 10f;

        [Title("Cover System")]
        [Tooltip("Tốc độ di chuyển tối đa mà người chơi vẫn được coi là đứng yên để vào trạng thái nấp.")]
        public float StillThreshold = 0.1f;
    }
}