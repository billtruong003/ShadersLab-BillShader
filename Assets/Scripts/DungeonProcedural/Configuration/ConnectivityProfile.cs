using UnityEngine;

namespace ProceduralDungeon.Configuration
{
    [CreateAssetMenu(fileName = "ConnectivityProfile", menuName = "Procedural Dungeon/Connectivity Profile")]
    public class ConnectivityProfile : ScriptableObject
    {
        [Range(0f, 1f)]
        [Tooltip("The probability of adding an edge from the Delaunay graph back into the MST to create cycles.")]
        public float cycleProbability = 0.125f;
    }
}