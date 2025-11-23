using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class CelestialBody
{
    public string bodyName;
    public Transform bodyTransform;
    public Light bodyLight;
    public Gradient lightColor;
    public AnimationCurve intensityCurve;
    public AnimationCurve shadowStrengthCurve;
    public Vector3 rotationOffset;

    public void UpdateBody(float timePercent)
    {
        if (bodyTransform == null || bodyLight == null) return;

        float intensity = intensityCurve.Evaluate(timePercent);
        bodyLight.intensity = intensity;

        bodyLight.color = lightColor.Evaluate(timePercent);
        bodyLight.shadowStrength = shadowStrengthCurve.Evaluate(timePercent);

        if (intensity <= 0.01f)
        {
            if (bodyLight.enabled) bodyLight.enabled = false;
        }
        else
        {
            if (!bodyLight.enabled) bodyLight.enabled = true;
        }

        bodyTransform.localRotation = Quaternion.Euler(rotationOffset);
    }
}

public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance { get; private set; }

    [Header("Time Settings")]
    [SerializeField, Range(0f, 1f)] private float timeOfDay = 0.25f;
    [SerializeField] private float dayDurationInSeconds = 120f;
    [SerializeField] private bool isPaused = false;

    [Header("Orbit Settings")]
    [SerializeField] private Transform orbitPivot;
    [SerializeField] private Vector3 orbitAxis = new Vector3(1, 0, 0);

    [Header("Celestial Bodies")]
    [SerializeField] private List<CelestialBody> celestialBodies = new List<CelestialBody>();

    public float CurrentTimeNormalized => timeOfDay;
    public float DayDuration => dayDurationInSeconds;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!isPaused)
        {
            AdvanceTime();
        }

        UpdateOrbit();
        UpdateCelestialBodies();
    }

    private void AdvanceTime()
    {
        timeOfDay += Time.deltaTime / dayDurationInSeconds;
        if (timeOfDay >= 1f) timeOfDay = 0f;
    }

    private void UpdateOrbit()
    {
        if (orbitPivot != null)
        {
            float angle = timeOfDay * 360f;
            orbitPivot.localRotation = Quaternion.Euler(orbitAxis * angle);
        }
    }

    private void UpdateCelestialBodies()
    {
        foreach (var body in celestialBodies)
        {
            body.UpdateBody(timeOfDay);
        }
    }

    public void SetTime(float normalizedTime)
    {
        timeOfDay = Mathf.Clamp01(normalizedTime);
    }

    public void SetDuration(float durationInSeconds)
    {
        if (durationInSeconds > 0)
            dayDurationInSeconds = durationInSeconds;
    }

    public void Pause(bool pause)
    {
        isPaused = pause;
    }

    public string GetDigitalClock()
    {
        float totalHours = timeOfDay * 24f;
        int hours = (int)totalHours;
        int minutes = (int)((totalHours - hours) * 60f);
        return $"{hours:D2}:{minutes:D2}";
    }

    public Vector2 GetTimeVector()
    {
        float totalHours = timeOfDay * 24f;
        int hours = (int)totalHours;
        int minutes = (int)((totalHours - hours) * 60f);
        return new Vector2(hours, minutes);
    }
}