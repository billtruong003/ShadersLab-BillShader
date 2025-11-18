using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shmackle.Utils.CoroutinesTimer;

namespace Nebulanook.Player
{
    public enum PlayerFXID
    {
        None,
        DashChargeStart,
        DashChargeLoop,
        DashChargeEnd,
        DashExecute,
        DashTrail,
        DashImpact,
        SprintTrail,
        FootstepDust,
        KnockbackHit
    }

    [Serializable]
    public class FXEntry
    {
        public PlayerFXID id;
        public GameObject fxObject;
        public bool autoDisable;
        [Sirenix.OdinInspector.ShowIf("autoDisable")] public float autoDisableDelay;

        [NonSerialized] public ParticleSystem particleSystem;
    }

    public class PlayerFXController : MonoBehaviour
    {
        [SerializeField] private List<FXEntry> fxEntries = new List<FXEntry>();

        private Dictionary<PlayerFXID, FXEntry> fxMap;
        private readonly Dictionary<PlayerFXID, Coroutine> activeDisableCoroutines = new Dictionary<PlayerFXID, Coroutine>();

        public static PlayerFXController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeFXMap();
        }

        private void InitializeFXMap()
        {
            fxMap = new Dictionary<PlayerFXID, FXEntry>(fxEntries.Count);

            foreach (var entry in fxEntries)
            {
                if (entry.id == PlayerFXID.None || entry.fxObject == null || fxMap.ContainsKey(entry.id)) continue;

                entry.fxObject.SetActive(false);
                entry.particleSystem = entry.fxObject.GetComponent<ParticleSystem>();
                fxMap[entry.id] = entry;
            }
        }

        // ... (Các hàm Play, Stop, SetActive, etc. không thay đổi) ...

        public void Play(PlayerFXID id)
        {
            if (id == PlayerFXID.None || !fxMap.TryGetValue(id, out FXEntry entry)) return;
            entry.fxObject.SetActive(true);
            if (entry.particleSystem != null)
            {
                entry.particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                entry.particleSystem.Play(true);
            }
        }

        public void Stop(PlayerFXID id)
        {
            if (id == PlayerFXID.None || !fxMap.TryGetValue(id, out FXEntry entry)) return;
            if (entry.particleSystem != null)
                entry.particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            else
                entry.fxObject.SetActive(false);
        }

        public void SetActive(PlayerFXID id, bool active)
        {
            if (id != PlayerFXID.None && fxMap.TryGetValue(id, out FXEntry entry))
            {
                entry.fxObject.SetActive(active);
                if (!active) StopDisableCoroutine(id);
            }
        }

        public void PlayOneShot(PlayerFXID id, float customDelay = -1f)
        {
            if (id == PlayerFXID.None || !fxMap.TryGetValue(id, out FXEntry entry)) return;
            Play(id);
            if (!entry.autoDisable) return;
            float delay = customDelay >= 0f ? customDelay : entry.autoDisableDelay;
            StartDisableCoroutine(id, delay);
        }

        public void StopAndDeactivateAfterDelay(PlayerFXID id, float delay = -1f)
        {
            if (id == PlayerFXID.None || !fxMap.TryGetValue(id, out FXEntry entry)) return;
            Stop(id);
            float effectiveDelay = delay >= 0f ? delay : entry.autoDisableDelay;
            StartDisableCoroutine(id, effectiveDelay);
        }

        private void StartDisableCoroutine(PlayerFXID id, float delay)
        {
            StopDisableCoroutine(id);
            if (delay <= 0f)
            {
                SetActive(id, false);
                return;
            }
            Coroutine newCoroutine = StartCoroutine(DisableAfterDelay(id, delay));
            activeDisableCoroutines[id] = newCoroutine;
        }

        private void StopDisableCoroutine(PlayerFXID id)
        {
            if (activeDisableCoroutines.TryGetValue(id, out Coroutine runningCoroutine) && runningCoroutine != null)
            {
                StopCoroutine(runningCoroutine);
                activeDisableCoroutines.Remove(id);
            }
        }

        private IEnumerator DisableAfterDelay(PlayerFXID id, float delay)
        {
            // Đã sử dụng CoroutineTimeUtils một cách chính xác
            yield return CoroutineTimeUtils.GetWaitForSeconds(delay);
            activeDisableCoroutines.Remove(id);
            SetActive(id, false);
        }

        public void ClearAll()
        {
            StopAllCoroutines();
            activeDisableCoroutines.Clear();
            foreach (var entry in fxMap.Values)
            {
                if (entry.particleSystem != null) entry.particleSystem.Clear();
                entry.fxObject.SetActive(false);
            }
        }

        // ... (Các hàm Getters và String Overloads không thay đổi) ...

        public GameObject GetFXGameObject(PlayerFXID id) => fxMap.TryGetValue(id, out var entry) ? entry.fxObject : null;
        public ParticleSystem GetFXParticleSystem(PlayerFXID id) => fxMap.TryGetValue(id, out var entry) ? entry.particleSystem : null;
        public void PlayFX(string fxIdString) => IfParse(fxIdString, id => Play(id));
        public void StopFX(string fxIdString) => IfParse(fxIdString, id => Stop(id));
        public void SetFXActive(string fxIdString, bool active) => IfParse(fxIdString, id => SetActive(id, active));
        public void PlayOneShot(string fxIdString, float customDelay = -1f) => IfParse(fxIdString, id => PlayOneShot(id, customDelay));
        public void StopAndDeactivateAfterDelay(string fxIdString, float delay = -1f) => IfParse(fxIdString, id => StopAndDeactivateAfterDelay(id, delay));
        private void IfParse(string fxIdString, Action<PlayerFXID> action)
        {
            if (Enum.TryParse(fxIdString, out PlayerFXID id))
                action(id);
        }

        private void OnValidate()
        {
            if (fxEntries == null) return;
            var seenIds = new HashSet<PlayerFXID>();
            fxEntries.RemoveAll(entry =>
            {
                if (entry.id == PlayerFXID.None) return false;
                return !seenIds.Add(entry.id);
            });
        }
    }
}