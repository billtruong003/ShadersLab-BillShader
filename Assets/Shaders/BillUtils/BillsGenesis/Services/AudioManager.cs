using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using Sirenix.OdinInspector;
using BillsGenesis.Core;

namespace BillsGenesis.Services
{
    public sealed class AudioManager : GenesisSingletonService<AudioManager>
    {
        [Title("Configuration")]
        [SerializeField, Required] private AudioMixer _audioMixer;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;

        [Title("Settings")]
        [Range(0f, 1f), OnValueChanged("OnVolumeChanged")]
        public float MasterVolume = 1f;
        [Range(0f, 1f), OnValueChanged("OnVolumeChanged")]
        public float MusicVolume = 1f;
        [Range(0f, 1f), OnValueChanged("OnVolumeChanged")]
        public float SfxVolume = 1f;

        private AudioSource _musicSourcePrimary;
        private AudioSource _musicSourceSecondary;
        private bool _isMuted;
        private bool _isPaused;

        private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
        private readonly List<AudioSource> _activeSfx = new List<AudioSource>();
        private GameObject _poolRoot;

        [ShowInInspector, ReadOnly, BoxGroup("Status")]
        public int PoolSize => _sfxPool.Count;
        [ShowInInspector, ReadOnly, BoxGroup("Status")]
        public int ActiveVoices => _activeSfx.Count;

        public override Task InitializeAsync()
        {
            _poolRoot = new GameObject("Audio_Pool_Root");
            _poolRoot.transform.SetParent(transform);

            _musicSourcePrimary = CreateSource("Music_Primary", true, _musicGroup);
            _musicSourceSecondary = CreateSource("Music_Secondary", true, _musicGroup);

            for (int i = 0; i < 10; i++) _sfxPool.Enqueue(CreateSource("SFX_Pooled", false, _sfxGroup));

            var storage = Genesis.Get<StorageManager>();
            if (storage != null)
            {
                MasterVolume = storage.GetFloat("Audio_Master", 1f);
                MusicVolume = storage.GetFloat("Audio_Music", 1f);
                SfxVolume = storage.GetFloat("Audio_SFX", 1f);
                _isMuted = storage.GetBool("Audio_Mute", false);
            }

            ApplyVolume();
            return Task.CompletedTask;
        }

        #region Public API

        public void PlayMusic(AudioClip clip, float fadeDuration = 1f, bool loop = true)
        {
            if (!clip) return;
            if (_musicSourcePrimary.clip == clip && _musicSourcePrimary.isPlaying) return;

            StopAllCoroutines();
            StartCoroutine(CrossFadeRoutine(clip, fadeDuration, loop));
        }

        public void StopMusic(float fadeDuration = 0.5f)
        {
            StopAllCoroutines();
            StartCoroutine(FadeOutRoutine(_musicSourcePrimary, fadeDuration));
            StartCoroutine(FadeOutRoutine(_musicSourceSecondary, fadeDuration));
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitchRandom = 0f)
        {
            PlaySfx(clip, Vector3.zero, volumeScale, pitchRandom, false);
        }

        public void PlaySfx3D(AudioClip clip, Vector3 position, float volumeScale = 1f, float pitchRandom = 0f)
        {
            PlaySfx(clip, position, volumeScale, pitchRandom, true);
        }

        public void StopAllSfx()
        {
            for (int i = _activeSfx.Count - 1; i >= 0; i--)
            {
                ReturnSfxSource(_activeSfx[i]);
            }
        }

        public void PauseAll(bool pause)
        {
            if (_isPaused == pause) return;
            _isPaused = pause;

            if (pause)
            {
                if (_musicSourcePrimary.isPlaying) _musicSourcePrimary.Pause();
                if (_musicSourceSecondary.isPlaying) _musicSourceSecondary.Pause();
                foreach (var sfx in _activeSfx) if (sfx.isPlaying) sfx.Pause();
            }
            else
            {
                if (!_isMuted)
                {
                    _musicSourcePrimary.UnPause();
                    _musicSourceSecondary.UnPause();
                    foreach (var sfx in _activeSfx) sfx.UnPause();
                }
            }
        }

        public void ToggleMute(bool mute)
        {
            _isMuted = mute;
            ApplyVolume();
            Genesis.Get<StorageManager>()?.SetBool("Audio_Mute", mute);
        }

        public void SaveSettings()
        {
            var storage = Genesis.Get<StorageManager>();
            if (storage != null)
            {
                storage.SetFloat("Audio_Master", MasterVolume);
                storage.SetFloat("Audio_Music", MusicVolume);
                storage.SetFloat("Audio_SFX", SfxVolume);
                storage.SavePrefs();
            }
        }

        #endregion

        #region Internal Logic

        private void PlaySfx(AudioClip clip, Vector3 pos, float vol, float pitchVar, bool is3D)
        {
            if (!clip || _isMuted || _isPaused) return;

            AudioSource source = GetSfxSource();

            source.transform.position = is3D ? pos : Vector3.zero;
            source.spatialBlend = is3D ? 1f : 0f;
            source.clip = clip;
            source.volume = vol;
            source.pitch = 1f + UnityEngine.Random.Range(-pitchVar, pitchVar);
            source.Play();

            _activeSfx.Add(source);

            Genesis.Get<TimerManager>().Post(clip.length + 0.1f, () => ReturnSfxSource(source));
        }

        private AudioSource GetSfxSource()
        {
            AudioSource s;
            if (_sfxPool.Count > 0)
            {
                s = _sfxPool.Dequeue();
                s.gameObject.SetActive(true);
            }
            else
            {
                s = CreateSource("SFX_Pooled_Extra", false, _sfxGroup);
            }
            return s;
        }

        private void ReturnSfxSource(AudioSource source)
        {
            if (!_activeSfx.Contains(source)) return;

            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
            source.transform.SetParent(_poolRoot.transform);

            _activeSfx.Remove(source);
            _sfxPool.Enqueue(source);
        }

        private AudioSource CreateSource(string name, bool loop, AudioMixerGroup group)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_poolRoot.transform);
            var s = obj.AddComponent<AudioSource>();
            s.loop = loop;
            s.playOnAwake = false;
            s.outputAudioMixerGroup = group;
            return s;
        }

        private void OnVolumeChanged() => ApplyVolume();

        private void ApplyVolume()
        {
            if (_audioMixer)
            {
                SetMixerVol("MasterVol", _isMuted ? 0 : MasterVolume);
                SetMixerVol("MusicVol", MusicVolume);
                SetMixerVol("SFXVol", SfxVolume);
            }
            else
            {
                _musicSourcePrimary.volume = _isMuted ? 0 : MasterVolume * MusicVolume;
                _musicSourceSecondary.volume = _isMuted ? 0 : MasterVolume * MusicVolume;
            }
        }

        private void SetMixerVol(string param, float normalizedVol)
        {
            float db = normalizedVol <= 0.001f ? -80f : Mathf.Log10(normalizedVol) * 20f;
            _audioMixer.SetFloat(param, db);
        }

        private IEnumerator CrossFadeRoutine(AudioClip newClip, float duration, bool loop)
        {
            var active = _musicSourcePrimary.isPlaying ? _musicSourcePrimary : _musicSourceSecondary;
            var next = active == _musicSourcePrimary ? _musicSourceSecondary : _musicSourcePrimary;

            next.clip = newClip;
            next.loop = loop;
            next.volume = 0;
            if (!_isPaused) next.Play();

            float t = 0;
            float targetVol = _isMuted ? 0 : (MasterVolume * MusicVolume);

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = t / duration;
                active.volume = Mathf.Lerp(targetVol, 0, p);
                next.volume = Mathf.Lerp(0, targetVol, p);
                yield return null;
            }

            active.Stop();
            active.volume = 0;
            next.volume = targetVol;
        }

        private IEnumerator FadeOutRoutine(AudioSource source, float duration)
        {
            float start = source.volume;
            float t = 0;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(start, 0, t / duration);
                yield return null;
            }
            source.Stop();
        }

        #endregion
    }
}