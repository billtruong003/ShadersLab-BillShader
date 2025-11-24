using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct LightingKeyframe
{
    [Range(0f, 24f)] public float timeOfDay;
    public float intensity;
    public Color lightColor;
    [Range(0f, 1f)] public float shadowStrength;
}

[Serializable]
public class CelestialBody
{
    public string name;
    public Light bodyLight;
    public List<LightingKeyframe> keyframes = new List<LightingKeyframe>();
    public Vector3 rotationCorrection;

    public void Evaluate(float currentTimeHours, Vector3 pivotPosition)
    {
        if (bodyLight == null) return;

        UpdateLightingData(currentTimeHours);
        UpdateTransformAlignment(pivotPosition);
    }

    private void UpdateLightingData(float time)
    {
        if (keyframes == null || keyframes.Count == 0) return;

        GetSurroundingKeyframes(time, out LightingKeyframe from, out LightingKeyframe to, out float t);

        bodyLight.color = Color.Lerp(from.lightColor, to.lightColor, t);
        bodyLight.intensity = Mathf.Lerp(from.intensity, to.intensity, t);
        bodyLight.shadowStrength = Mathf.Lerp(from.shadowStrength, to.shadowStrength, t);

        bool shouldEnable = bodyLight.intensity > 0.001f;
        if (bodyLight.enabled != shouldEnable) bodyLight.enabled = shouldEnable;
    }

    private void UpdateTransformAlignment(Vector3 targetPosition)
    {
        if (bodyLight.transform.parent == null) return;

        Vector3 directionToPivot = targetPosition - bodyLight.transform.position;

        if (directionToPivot != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToPivot);
            bodyLight.transform.rotation = lookRotation * Quaternion.Euler(rotationCorrection);
        }
    }

    private void GetSurroundingKeyframes(float time, out LightingKeyframe from, out LightingKeyframe to, out float t)
    {
        int count = keyframes.Count;
        if (count == 1)
        {
            from = to = keyframes[0];
            t = 0f;
            return;
        }

        keyframes.Sort((a, b) => a.timeOfDay.CompareTo(b.timeOfDay));

        int fromIndex = -1;
        for (int i = 0; i < count; i++)
        {
            if (keyframes[i].timeOfDay <= time) fromIndex = i;
        }

        if (fromIndex == -1)
        {
            from = keyframes[count - 1];
            to = keyframes[0];
            float duration = (24f - from.timeOfDay) + to.timeOfDay;
            float elapsed = (24f - from.timeOfDay) + time;
            t = Mathf.Clamp01(elapsed / duration);
        }
        else if (fromIndex == count - 1)
        {
            from = keyframes[fromIndex];
            to = keyframes[0];
            float duration = (24f - from.timeOfDay) + to.timeOfDay;
            float elapsed = time - from.timeOfDay;
            t = Mathf.Clamp01(elapsed / duration);
        }
        else
        {
            from = keyframes[fromIndex];
            to = keyframes[fromIndex + 1];
            float duration = to.timeOfDay - from.timeOfDay;
            float elapsed = time - from.timeOfDay;
            t = Mathf.Clamp01(elapsed / duration);
        }
    }
}

[ExecuteAlways]
public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance { get; private set; }

    [Header("Time Settings")]
    [SerializeField, Range(0f, 24f)] private float currentTime = 6f;
    [SerializeField] private float dayDurationSeconds = 120f;
    [SerializeField] private bool isPaused = false;
    [SerializeField] private bool debugMode = false;

    [Header("Orbit Settings")]
    [SerializeField] private Transform orbitPivot;
    [SerializeField] private Vector3 orbitAxis = new Vector3(1f, 0f, 0f);

    [Header("Celestial Bodies")]
    [SerializeField] private List<CelestialBody> celestialBodies = new List<CelestialBody>();

    private void Awake()
    {
        if (Application.isPlaying)
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (Application.isPlaying && !isPaused && !debugMode)
        {
            AdvanceTime();
        }
        ProcessDayNightCycle();
    }

    private void OnValidate()
    {
        ProcessDayNightCycle();
    }

    private void AdvanceTime()
    {
        if (dayDurationSeconds <= 0f) return;
        float timeIncrement = (Time.deltaTime / dayDurationSeconds) * 24f;
        currentTime = (currentTime + timeIncrement) % 24f;
    }

    private void ProcessDayNightCycle()
    {
        if (orbitPivot == null) return;

        UpdateOrbitRotation();
        UpdateCelestialBodies();
    }

    private void UpdateOrbitRotation()
    {
        float normalizedTime = currentTime / 24f;
        float angle = normalizedTime * 360f;
        orbitPivot.localRotation = Quaternion.Euler(orbitAxis * angle);
    }

    private void UpdateCelestialBodies()
    {
        Vector3 targetPivotPos = orbitPivot.position;
        foreach (var body in celestialBodies)
        {
            body.Evaluate(currentTime, targetPivotPos);
        }
    }

    public void SetTime(float hour)
    {
        currentTime = Mathf.Repeat(hour, 24f);
        ProcessDayNightCycle();
    }

    public string GetDigitalClock()
    {
        TimeSpan ts = TimeSpan.FromHours(currentTime);
        return $"{ts.Hours:D2}:{ts.Minutes:D2}";
    }
}