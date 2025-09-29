// Path: Assets/Scripts/FX/CharacterFXController.cs

using UnityEngine;
using ModularTopDown.Locomotion;
using Sirenix.OdinInspector;

[RequireComponent(typeof(AudioSource))]
public class CharacterFXController : MonoBehaviour
{
    [Title("Core Dependencies")]
    [InlineEditor]
    [Required, SerializeField] private CharacterFXProfile _profile;
    [Required, SerializeField] private AfterImageController _afterImageController;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        LocomotionState.OnFXRequest += HandleFXRequest;
    }

    private void OnDisable()
    {
        LocomotionState.OnFXRequest -= HandleFXRequest;
    }

    private void HandleFXRequest(CharacterFXProfile.FXType type, Vector3 position)
    {
        if (_profile.TryGetEntry(type, out var entry))
        {
            PlayParticleEffect(entry.ParticlePrefab, position);
            PlaySoundEffect(entry.SoundClip, entry.SoundVolume);
        }

        if (type == CharacterFXProfile.FXType.Dash)
        {
            TriggerDashAfterImage();
        }
    }

    private void PlayParticleEffect(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        ObjectPoolManager.Instance.Spawn(prefab, position, Quaternion.identity);
    }

    private void PlaySoundEffect(AudioClip clip, float volume)
    {
        if (clip == null) return;
        _audioSource.PlayOneShot(clip, volume);
    }

    private void TriggerDashAfterImage()
    {
        _afterImageController.Trigger();
    }
}