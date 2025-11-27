using System.Collections.Generic;
using UnityEngine;

public static class DialogueActorRegistry
{
    private static readonly Dictionary<CharacterProfile, Transform> actorMap = new Dictionary<CharacterProfile, Transform>();

    public static void Register(CharacterProfile profile, Transform speechPoint)
    {
        if (profile == null || speechPoint == null) return;
        if (!actorMap.ContainsKey(profile))
        {
            actorMap.Add(profile, speechPoint);
        }
        else
        {
            actorMap[profile] = speechPoint;
        }
    }

    public static void Unregister(CharacterProfile profile)
    {
        if (profile != null && actorMap.ContainsKey(profile))
        {
            actorMap.Remove(profile);
        }
    }

    public static Transform GetSpeechPoint(CharacterProfile profile)
    {
        if (profile != null && actorMap.TryGetValue(profile, out Transform point))
        {
            return point;
        }
        return null;
    }
}