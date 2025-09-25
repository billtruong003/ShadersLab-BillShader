using UnityEngine;
using System.Collections.Generic;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();
    private Dictionary<int, int> prefabInstanceIdMap = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int prefabId = prefab.GetInstanceID();
        Queue<GameObject> objectQueue;

        if (!poolDictionary.TryGetValue(prefabId, out objectQueue))
        {
            objectQueue = new Queue<GameObject>();
            poolDictionary[prefabId] = objectQueue;
        }

        GameObject objectToSpawn;
        if (objectQueue.Count > 0)
        {
            objectToSpawn = objectQueue.Dequeue();
        }
        else
        {
            objectToSpawn = Instantiate(prefab);
            prefabInstanceIdMap[objectToSpawn.GetInstanceID()] = prefabId;
        }

        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        return objectToSpawn;
    }

    public void ReturnToPool(GameObject objectToReturn)
    {
        if (objectToReturn == null) return;

        int instanceId = objectToReturn.GetInstanceID();
        if (prefabInstanceIdMap.TryGetValue(instanceId, out int prefabId))
        {
            if (poolDictionary.TryGetValue(prefabId, out Queue<GameObject> objectQueue))
            {
                objectToReturn.SetActive(false);
                objectQueue.Enqueue(objectToReturn);
            }
            else
            {
                Destroy(objectToReturn);
            }
        }
        else
        {
            Destroy(objectToReturn);
        }
    }
}