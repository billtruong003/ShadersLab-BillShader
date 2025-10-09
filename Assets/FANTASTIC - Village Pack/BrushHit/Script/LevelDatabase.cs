using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "ShadersLab/Level Database")]
public class LevelDatabase : ScriptableObject
{
    public List<LevelData> allLevels;
}