using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrushHit
{
    [Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.7f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop = false;
    }

    [HideMonoScript]
    public class AudioManager : SerializedMonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Title("Sound Library")]
        [SerializeField] private List<Sound> sounds;

        private readonly Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSources();
            }
        }

        private void InitializeAudioSources()
        {
            foreach (Sound s in sounds)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.clip = s.clip;
                source.volume = s.volume;
                source.pitch = s.pitch;
                source.loop = s.loop;
                source.playOnAwake = false;
                audioSources[s.name] = source;
            }
        }

        public void PlaySound(string soundName)
        {
            if (audioSources.TryGetValue(soundName, out AudioSource source))
            {
                source.Play();
            }
            else
            {
                Debug.LogWarning($"Sound with name '{soundName}' not found.");
            }
        }

        public void StopSound(string soundName)
        {
            if (audioSources.TryGetValue(soundName, out AudioSource source))
            {
                source.Stop();
            }
        }
    }
}