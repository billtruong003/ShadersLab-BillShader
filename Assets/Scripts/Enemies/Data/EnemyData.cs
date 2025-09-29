// Path: Assets/Scripts/Enemies/Data/EnemyData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "ED_", menuName = "Elemental Echoes/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Core Stats")]
    public float maxHealth = 100f;
    public float movementSpeed = 3.5f;
    public float angularSpeed = 120f;
    public float stoppingDistance = 1.5f;

    [Header("Targeting & Detection")]
    public float detectionRadius = 15f;
    public LayerMask playerLayer;

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackCooldown = 2f;
    public float attackRange = 2f;
    public float attackAnimDuration = 1.2f; // DỮ LIỆU MỚI: Thời gian animation tấn công

    [Header("Ranged Logic (If Applicable)")]
    public GameObject projectilePrefab;
    public float kitingDistance = 4f;

    [Header("Visuals & Animation")] // Nhóm lại cho gọn gàng
    public Vector3 visualRotationOffset = Vector3.zero; // DỮ LIỆU MỚI: Offset cho model
    public string animIdle = "Idle";
    public string animMove = "Run";
    public string animAttack = "Attack";
    public string animTakeDamage = "TakeDamage";
    public string animDie = "Die";
}