using UnityEngine;

[CreateAssetMenu(fileName = "FX_NewEffect", menuName = "My Indie Game/Visual Effects/Pooled FX Player")]
public sealed class PooledFXPlayer : ScriptableObject
{
    [Header("Particle Effect")]
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private bool attachToTransform = false;

    [Header("Sound Effect")]
    [SerializeField] private AudioClip audioClip;
    [SerializeField][Range(0f, 1f)] private float volume = 1.0f;

    public void Play(Transform spawnTransform)
    {
        if (spawnTransform == null) return;

        PlayParticles(spawnTransform);
        PlaySound(spawnTransform.position);
    }

    public void Play(Vector3 position)
    {
        PlayParticles(position, Quaternion.identity);
        PlaySound(position);
    }

    private void PlayParticles(Transform spawnTransform)
    {
        if (particlePrefab == null) return;

        GameObject spawnedVFX = ObjectPoolManager.Instance.Spawn(particlePrefab, spawnTransform.position, spawnTransform.rotation);

        if (attachToTransform)
        {
            spawnedVFX.transform.SetParent(spawnTransform, true);
        }
    }

    private void PlayParticles(Vector3 position, Quaternion rotation)
    {
        if (particlePrefab == null) return;

        ObjectPoolManager.Instance.Spawn(particlePrefab, position, rotation);
    }

    private void PlaySound(Vector3 position)
    {
        if (audioClip == null) return;

        AudioSource.PlayClipAtPoint(audioClip, position, volume);
    }
}