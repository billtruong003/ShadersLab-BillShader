// Path: Assets/Scripts/FX/CharacterFXProfile.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "FXProfile_New", menuName = "My Indie Game/Character FX Profile")]
public class CharacterFXProfile : SerializedScriptableObject
{
    [System.Serializable]
    public struct FXEntry
    {
        [AssetsOnly]
        public GameObject ParticlePrefab;
        public AudioClip SoundClip;
        [Range(0, 2)]
        public float SoundVolume;
    }

    public enum FXType
    {
        Jump,
        Land,
        Dash
    }

    [Title("Effect Definitions")]
    [DictionaryDrawerSettings(KeyLabel = "Action Type", ValueLabel = "Effect Details")]
    public Dictionary<FXType, FXEntry> Effects = new Dictionary<FXType, FXEntry>();

    public bool TryGetEntry(FXType type, out FXEntry entry)
    {
        return Effects.TryGetValue(type, out entry);
    }
}