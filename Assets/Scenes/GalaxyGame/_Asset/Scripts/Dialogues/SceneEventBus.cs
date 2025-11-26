using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class SceneEventBus
{
    // Dictionary lưu trữ các sự kiện đang active trong Scene
    private static Dictionary<string, UnityEvent> events = new Dictionary<string, UnityEvent>();

    public static void Register(string key, UnityEvent unityEvent)
    {
        if (!events.ContainsKey(key)) events.Add(key, unityEvent);
        else events[key] = unityEvent;
    }

    public static void Unregister(string key)
    {
        if (events.ContainsKey(key)) events.Remove(key);
    }

    public static void Trigger(string key)
    {
        if (events.TryGetValue(key, out var unityEvent))
        {
            unityEvent?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[SceneEventBus] Không tìm thấy event nào có key: {key}");
        }
    }

    // Helper cho Odin Dropdown
    public static IEnumerable<string> GetAllKeys() => events.Keys;
}