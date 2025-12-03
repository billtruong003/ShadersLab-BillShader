using UnityEngine;
using BillsGenesis.Core;
using BillsGenesis.Services;
using Nebulanook.Player; // Namespace chứa PlayerInteraction

public class GameplayDirector : MonoBehaviour
{
    [Inject] private PoolManager _pool;
    [Inject] private SignalHub _signal;
    [Inject] private LoggerService _logger;

    [Header("Player Settings")]
    [SerializeField] private GameObject _playerPrefab;

    private void Start()
    {
        Genesis.InjectDependencies(this);
        _signal.Subscribe<LevelLoadedSignal>(OnLevelLoaded);
    }

    private void OnLevelLoaded(LevelLoadedSignal signal)
    {
        var levelData = signal.Context;
        SpawnPlayer(levelData);
        SpawnNPCs(levelData);
    }

    private void SpawnPlayer(LevelContext data)
    {
        if (!_playerPrefab || !data.PlayerSpawnPoint) return;

        var player = _pool.Spawn(_playerPrefab, data.PlayerSpawnPoint.position, data.PlayerSpawnPoint.rotation);

        // Setup Camera follow player
        if (Nebulanook.Core.IsometricCameraController.Instance != null)
        {
            var field = typeof(Nebulanook.Core.IsometricCameraController).GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(Nebulanook.Core.IsometricCameraController.Instance, player.transform);
        }
    }

    private void SpawnNPCs(LevelContext data)
    {
        if (data.NPCsToSpawn == null || data.NPCSpawnPoints == null) return;

        int pointCount = data.NPCSpawnPoints.Count;
        if (pointCount == 0) return;

        for (int i = 0; i < data.NPCsToSpawn.Count; i++)
        {
            var profile = data.NPCsToSpawn[i];
            var spawnTransform = data.NPCSpawnPoints[i % pointCount];

            if (profile.Prefab == null) continue;

            var npcObj = _pool.Spawn(profile.Prefab, spawnTransform.position, spawnTransform.rotation);

            var controller = npcObj.GetComponent<NPCController>();
            if (controller != null)
            {
                controller.Initialize(profile);
            }
        }
    }

    private void OnDestroy()
    {
        if (Genesis.Get<SignalHub>() != null)
            Genesis.Get<SignalHub>().Unsubscribe<LevelLoadedSignal>(OnLevelLoaded);
    }
}