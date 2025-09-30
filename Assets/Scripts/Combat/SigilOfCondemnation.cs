// Path: Assets/Scripts/Combat/Projectiles/SigilOfCondemnation.cs
using UnityEngine;
using System;

public class SigilOfCondemnation : MonoBehaviour, IPoolableObject
{
    public Transform Target { get; private set; }
    private Action<SigilOfCondemnation> _onDetonateCallback;
    private float _timer;

    public void Initialize(Transform target, float delay, Action<SigilOfCondemnation> onDetonate)
    {
        Target = target;
        _timer = delay;
        _onDetonateCallback = onDetonate;
    }

    private void Update()
    {
        if (Target == null || !Target.gameObject.activeInHierarchy)
        {
            ReturnToPool();
            return;
        }

        transform.position = Target.position;

        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            _onDetonateCallback?.Invoke(this);
            ReturnToPool();
        }
    }

    public void OnObjectSpawn() { }
    public void OnObjectReturn()
    {
        Target = null;
        _onDetonateCallback = null;
    }

    private void ReturnToPool()
    {
        ObjectPoolManager.Instance.ReturnToPool(gameObject);
    }
}