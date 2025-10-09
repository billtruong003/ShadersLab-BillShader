using UnityEngine;
using Sirenix.OdinInspector;

[HideMonoScript]
public class GameDataPersistence : MonoBehaviour
{
    public static GameDataPersistence Instance { get; private set; }

    [ShowInInspector, ReadOnly]
    public LevelData LevelToLoad { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetLevelToLoad(LevelData levelData)
    {
        LevelToLoad = levelData;
    }
}