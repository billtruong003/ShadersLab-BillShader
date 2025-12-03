using UnityEngine;
using System.Collections.Generic;
using BillsGenesis.Core;
using BillsGenesis.Services;

public struct LevelLoadedSignal : ISignal
{
    public LevelContext Context;
}

public class LevelContext : MonoBehaviour
{
    [Header("Spawn Configuration")]
    public Transform PlayerSpawnPoint;
    public List<Transform> NPCSpawnPoints;

    [Header("NPC Waves")]
    public List<NPCProfile> NPCsToSpawn;

    private void Start()
    {
        Genesis.Get<SignalHub>().Fire(new LevelLoadedSignal { Context = this });
    }

    private void OnDrawGizmos()
    {
        if (PlayerSpawnPoint)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(PlayerSpawnPoint.position, 0.5f);
            Gizmos.DrawLine(PlayerSpawnPoint.position, PlayerSpawnPoint.position + PlayerSpawnPoint.forward);
        }

        Gizmos.color = Color.yellow;
        if (NPCSpawnPoints != null)
        {
            foreach (var p in NPCSpawnPoints)
            {
                if (p)
                {
                    Gizmos.DrawWireSphere(p.position, 0.5f);
                    Gizmos.DrawLine(p.position, p.position + p.forward * 0.5f);
                }
            }
        }
    }
}