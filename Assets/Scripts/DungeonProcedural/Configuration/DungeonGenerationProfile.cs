using UnityEngine;

namespace ProceduralDungeon.Configuration
{
    [CreateAssetMenu(fileName = "DungeonGenerationProfile", menuName = "Procedural Dungeon/Dungeon Generation Profile")]
    public class DungeonGenerationProfile : ScriptableObject
    {
        public RoomLayoutProfile roomLayout;
        public ConnectivityProfile connectivity;
        public ThemingProfile theme;
    }
}