// File: GameInitializer.cs
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private GridSystemManager GridSystemManager;
    void Start()
    {
        GridSystemManager.Initialize();
    }
}