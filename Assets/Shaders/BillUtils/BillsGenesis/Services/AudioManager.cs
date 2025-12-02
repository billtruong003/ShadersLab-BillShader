using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using BillsGenesis.Core;

namespace BillsGenesis.Services
{
    public sealed class AudioManager : GenesisSingletonService<AudioManager>
    {
        [Header("Settings")]
        [Range(0f, 1f)] public float MasterVolume = 1f;
        [Range(0f, 1f)] public float MusicVolume = 1f;
        [Range(0f, 1f)] public float SfxVolume = 1f;

        private AudioSource _musicSource;
        private AudioSource _musicSourceSecondary; // For cross-fading
        private List<AudioSource> _sfxSources = new List<AudioSource>();
        private GameObject _sfxRoot;
        private bool _isMuted;

        public int ActiveSfxCount
        {
            get
            {
                int c = 0;
                foreach (var s in _sfxSources) if (s.isPlaying) c++;
                return c;
            }
        }

        public override Task InitializeAsync()
        {
            _sfxRoot = new GameObject("SFX_Pool");
            _sfxRoot.transform.SetParent(transform);

            _musicSource = CreateSource("Music_Primary", true);
            _musicSourceSecondary = CreateSource("Music_Secondary", true);

            for (int i = 0; i < 5; i++) AddSfxSource();

            return Task.CompletedTask;
        }

        public void PlayMusic(AudioClip clip, float fadeDuration = 1f, bool loop = true)
        {
            if (!clip) return;
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;

            StartCoroutine(CrossFadeMusic(clip, fadeDuration, loop));
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitchRandom = 0f)
        {
            if (!clip || _isMuted) return;

            var source = GetAvailableSfx();
            source.pitch = 1f + Random.Range(-pitchRandom, pitchRandom);
            source.volume = MasterVolume * SfxVolume * volumeScale;
            source.PlayOneShot(clip);
        }

        public void StopMusic(float fadeDuration = 0.5f)
        {
            StartCoroutine(FadeOut(_musicSource, fadeDuration));
            StartCoroutine(FadeOut(_musicSourceSecondary, fadeDuration));
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            UpdateMusicVolume();
        }

        public void ToggleMute(bool mute)
        {
            _isMuted = mute;
            _musicSource.mute = mute;
            _musicSourceSecondary.mute = mute;
            // SFX sources update on next play, or we can iterate to mute current ones
            foreach (var s in _sfxSources) s.mute = mute;
        }

        private AudioSource GetAvailableSfx()
        {
            foreach (var s in _sfxSources) if (!s.isPlaying) return s;
            return AddSfxSource();
        }

        private AudioSource AddSfxSource()
        {
            var s = _sfxRoot.AddComponent<AudioSource>();
            s.playOnAwake = false;
            _sfxSources.Add(s);
            return s;
        }

        private AudioSource CreateSource(string name, bool loop)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform);
            var s = obj.AddComponent<AudioSource>();
            s.loop = loop;
            s.playOnAwake = false;
            return s;
        }

        private void UpdateMusicVolume()
        {
            if (_musicSource.isPlaying) _musicSource.volume = MasterVolume * MusicVolume;
        }

        private IEnumerator CrossFadeMusic(AudioClip newClip, float duration, bool loop)
        {
            AudioSource active = _musicSource.isPlaying ? _musicSource : _musicSourceSecondary;
            AudioSource next = active == _musicSource ? _musicSourceSecondary : _musicSource;

            next.clip = newClip;
            next.loop = loop;
            next.volume = 0;
            next.Play();

            float t = 0;
            float startVol = active.volume;
            float targetVol = MasterVolume * MusicVolume;

            while (t < duration)
            {
                t += Time.deltaTime;
                float progress = t / duration;

                if (_isMuted)
                {
                    active.volume = 0;
                    next.volume = 0;
                }
                else
                {
                    active.volume = Mathf.Lerp(startVol, 0, progress);
                    next.volume = Mathf.Lerp(0, targetVol, progress);
                }
                yield return null;
            }

            active.Stop();
            active.volume = 0;
            next.volume = _isMuted ? 0 : targetVol;

            // Swap references so Primary is always the active one conceptually if needed, 
            // but here we just toggle logic.
        }

        private IEnumerator FadeOut(AudioSource source, float duration)
        {
            float start = source.volume;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                source.volume = Mathf.Lerp(start, 0, t / duration);
                yield return null;
            }
            source.Stop();
        }
    }
}