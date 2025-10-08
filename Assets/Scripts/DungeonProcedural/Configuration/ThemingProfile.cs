using UnityEngine;

namespace ProceduralDungeon.Configuration
{
    [CreateAssetMenu(fileName = "ThemingProfile", menuName = "Procedural Dungeon/Theming Profile")]
    public class ThemingProfile : ScriptableObject
    {
        [Header("Architectural Prefabs")]
        public GameObject floorPrefab;
        public GameObject wallPrefab;
        public GameObject doorPrefab;
        public GameObject staircasePrefab;

        // You can expand this with props, lights, etc.
    }
}