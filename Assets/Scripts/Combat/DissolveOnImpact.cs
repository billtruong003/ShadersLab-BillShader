// Path: Assets/Scripts/VFX/DissolveOnImpact.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
public class DissolveOnImpact : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float dissolveDuration = 0.25f;
    [Tooltip("The name of the float property in the shader that controls the dissolve effect.")]
    [SerializeField] private string dissolveAmountProperty = "_DissolveAmount";

    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;
    private int _dissolvePropertyID;
    private Collider _collider;
    private Rigidbody _rigidbody;
    private Coroutine _dissolveCoroutine;
    private bool _isDissolving = false;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
        _dissolvePropertyID = Shader.PropertyToID(dissolveAmountProperty);
        TryGetComponent<Collider>(out _collider);
        TryGetComponent<Rigidbody>(out _rigidbody);
    }

    private void OnEnable()
    {
        ResetDissolve();
    }

    public void TriggerDissolve()
    {
        if (_isDissolving) return;

        if (_rigidbody) _rigidbody.linearVelocity = Vector3.zero;
        if (_collider) _collider.enabled = false;

        _dissolveCoroutine = StartCoroutine(DissolveRoutine());
    }

    private void ResetDissolve()
    {
        if (_dissolveCoroutine != null)
        {
            StopCoroutine(_dissolveCoroutine);
            _dissolveCoroutine = null;
        }

        _isDissolving = false;
        if (_collider) _collider.enabled = true;

        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(_dissolvePropertyID, 0f);
        _renderer.SetPropertyBlock(_propertyBlock);
    }

    private IEnumerator DissolveRoutine()
    {
        _isDissolving = true;
        float elapsedTime = 0f;

        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;
            float dissolveValue = Mathf.Clamp01(elapsedTime / dissolveDuration);

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(_dissolvePropertyID, dissolveValue);
            _renderer.SetPropertyBlock(_propertyBlock);

            yield return null;
        }

        ObjectPoolManager.Instance.ReturnToPool(gameObject);
    }
}