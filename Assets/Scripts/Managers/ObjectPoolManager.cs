// Path: Assets/Scripts/Managers/ObjectPoolManager.cs

using UnityEngine;
using System.Collections.Generic;

public interface IPoolableObject
{
    void OnObjectSpawn();
    void OnObjectReturn();
}

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    private readonly Dictionary<int, Queue<GameObject>> _poolDictionary = new Dictionary<int, Queue<GameObject>>();
    private readonly Dictionary<int, int> _prefabInstanceIdMap = new Dictionary<int, int>();
    private readonly Dictionary<int, Transform> _poolParents = new Dictionary<int, Transform>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int prefabId = prefab.GetInstanceID();
        Transform parent = GetOrCreatePoolParent(prefabId, prefab.name);

        if (!_poolDictionary.TryGetValue(prefabId, out Queue<GameObject> objectQueue))
        {
            objectQueue = new Queue<GameObject>();
            _poolDictionary[prefabId] = objectQueue;
        }

        GameObject objectToSpawn;
        if (objectQueue.Count > 0)
        {
            objectToSpawn = objectQueue.Dequeue();
        }
        else
        {
            objectToSpawn = Instantiate(prefab);
            _prefabInstanceIdMap[objectToSpawn.GetInstanceID()] = prefabId;
        }

        objectToSpawn.transform.SetParent(null);
        objectToSpawn.transform.SetPositionAndRotation(position, rotation);
        objectToSpawn.SetActive(true);

        var poolable = objectToSpawn.GetComponent<IPoolableObject>();
        poolable?.OnObjectSpawn();

        return objectToSpawn;
    }

    public void ReturnToPool(GameObject objectToReturn)
    {
        if (objectToReturn == null) return;

        var poolable = objectToReturn.GetComponent<IPoolableObject>();
        poolable?.OnObjectReturn();

        int instanceId = objectToReturn.GetInstanceID();
        if (_prefabInstanceIdMap.TryGetValue(instanceId, out int prefabId))
        {
            if (_poolDictionary.TryGetValue(prefabId, out Queue<GameObject> objectQueue))
            {
                objectToReturn.SetActive(false);
                Transform parent = GetOrCreatePoolParent(prefabId, "UnknownPool");
                objectToReturn.transform.SetParent(parent);
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

    private Transform GetOrCreatePoolParent(int prefabId, string prefabName)
    {
        if (!_poolParents.TryGetValue(prefabId, out Transform parent))
        {
            var parentObject = new GameObject($"{prefabName}_Pool");
            parent = parentObject.transform;
            parent.SetParent(this.transform);
            _poolParents[prefabId] = parent;
        }
        return parent;
    }
}