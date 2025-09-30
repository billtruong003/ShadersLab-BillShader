// Path: Assets/Scripts/Combat/ActiveWeapon.cs
using UnityEngine;

public abstract class ActiveWeapon : MonoBehaviour
{
    protected WeaponData weaponData;
    protected float cooldownTimer;

    // Hai loại điểm neo mà vũ khí có thể sử dụng
    protected Transform idleAnchor;
    protected Transform centerAnchor;

    public virtual void Initialize(WeaponData data)
    {
        this.weaponData = data;
        cooldownTimer = 0;
    }

    // Phương thức mới, rõ ràng hơn để gán các điểm neo
    public virtual void SetAnchors(Transform idle, Transform center)
    {
        this.idleAnchor = idle;
        this.centerAnchor = center;
    }

    public WeaponData GetWeaponData()
    {
        return weaponData;
    }

    protected virtual void Update()
    {
        cooldownTimer -= Time.deltaTime;
    }

    public bool IsReady()
    {
        return cooldownTimer <= 0;
    }

    public void Attack()
    {
        if (!IsReady()) return;
        PerformAttack();
        cooldownTimer = weaponData.cooldown;
    }

    protected abstract void PerformAttack();
}