using UnityEngine;

namespace DungeonRush
{
    [CreateAssetMenu(fileName = "NewPlayerStats", menuName = "DungeonRush/Player Stats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("Movement")]
        [Min(0)] public float walkSpeed = 5f;
        [Min(0)] public float runSpeed = 8f;
        [Tooltip("Lực đẩy để nhân vật đạt được tốc độ mong muốn. Giá trị càng cao, gia tốc càng nhanh.")]
        [Min(0)] public float moveForce = 50f;
        [Min(0)] public float rotationSpeed = 15f;

        [Header("Dashing")]
        [Min(0)] public float dashForce = 500f; // Sử dụng Force thay vì Speed
        [Min(0)] public float dashDuration = 0.15f;
        [Min(0)] public float dashCooldown = 1f;
    }
}