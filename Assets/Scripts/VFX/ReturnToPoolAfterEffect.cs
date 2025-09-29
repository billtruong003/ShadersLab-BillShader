using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public sealed class ReturnToPoolAfterEffect : MonoBehaviour
{
    private ParticleSystem _particleSystem;

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        StartCoroutine(CheckIfAlive());
    }

    private IEnumerator CheckIfAlive()
    {
        yield return new WaitForSeconds(0.5f); // Initial delay to ensure the system has started
        while (_particleSystem.IsAlive(true))
        {
            yield return new WaitForSeconds(0.5f);
        }
        ObjectPoolManager.Instance.ReturnToPool(gameObject);
    }
}