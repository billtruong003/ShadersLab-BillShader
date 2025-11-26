using UnityEngine;
using System;
using System.Collections.Generic; // Quan trọng: Fix lỗi CS0308
using Sirenix.OdinInspector;

// Class gốc
[Serializable]
public abstract class DialogueAction
{
    public abstract void Execute();
}

// --- CÁC IMPLEMENTATION ---

[Serializable]
public class LogAction : DialogueAction
{
    [TextArea] public string Message;
    public override void Execute() => Debug.Log($"[Dialogue]: {Message}");
}

[Serializable]
public class PlaySoundAction : DialogueAction
{
    [Required] public AudioClip Clip;
    [Range(0, 1)] public float Volume = 1f;

    public override void Execute()
    {
        if (Clip != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(Clip, Camera.main.transform.position, Volume);
    }
}

[Serializable]
public class TriggerSceneEventAction : DialogueAction
{
    [ValueDropdown("GetAllEventKeys")]
    public string EventKey;

    public override void Execute()
    {
        SceneEventBus.Trigger(EventKey);
    }

    // Fix lỗi CS0308: Đảm bảo dùng IEnumerable của System.Collections.Generic
    private IEnumerable<string> GetAllEventKeys()
    {
        return SceneEventBus.GetAllKeys();
    }
}

[Serializable]
public class AddGoldAction : DialogueAction
{
    public int Amount;
    public override void Execute()
    {
        Debug.Log($"[Mockup] Player received {Amount} Gold.");
        // Inventory.Instance.AddGold(Amount);
    }
}