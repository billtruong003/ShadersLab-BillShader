// Path: Assets/Scripts/Combat/Projectiles/EnemyProjectile.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : MonoBehaviour, IPoolableObject
{
    private Rigidbody rb;
    private float damage;
    private float lifetime = 5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(float projDamage, float speed, Vector3 direction)
    {
        damage = projDamage;
        rb.linearVelocity = direction.normalized * speed;
    }

    public void OnObjectSpawn()
    {
        Invoke(nameof(ReturnToPool), lifetime);
    }

    public void OnObjectReturn()
    {
        CancelInvoke();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerHealth>() != null)
        {
            other.GetComponent<PlayerHealth>().TakeDamage(damage);
            ReturnToPool();
        }
        // Có thể thêm logic va chạm với môi trường
        else if (other.gameObject.layer != gameObject.layer) // Tránh va vào kẻ địch khác
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        ObjectPoolManager.Instance.ReturnToPool(gameObject);
    }
}