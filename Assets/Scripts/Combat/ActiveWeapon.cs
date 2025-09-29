// Path: Assets/Scripts/Combat/ActiveWeapon.cs
using UnityEngine;

public abstract class ActiveWeapon : MonoBehaviour
{
    protected WeaponData weaponData;
    protected float cooldownTimer;
    protected Transform idleAnchor;

    public virtual void Initialize(WeaponData data)
    {
        this.weaponData = data;
        cooldownTimer = 0;
    }

    // Cho phép PlayerCombat gán điểm neo
    public virtual void SetIdleTarget(Transform anchor)
    {
        idleAnchor = anchor;
    }

    // Để PlayerCombat kiểm tra vũ khí hiện tại
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