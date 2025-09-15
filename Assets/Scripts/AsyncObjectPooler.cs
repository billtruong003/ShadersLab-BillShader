// File: AsyncObjectPooler.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AsyncObjectPooler : MonoBehaviour
{
    public static AsyncObjectPooler Instance { get; private set; }

    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, AsyncOperationHandle<GameObject>> loadHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public async Task<GameObject> GetObject(AssetReferenceGameObject assetReference)
    {
        string key = assetReference.AssetGUID;
        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary[key] = new Queue<GameObject>();
        }

        if (poolDictionary[key].Count > 0)
        {
            GameObject obj = poolDictionary[key].Dequeue();
            obj.SetActive(true);
            return obj;
        }

        return await CreateNewObject(assetReference);
    }

    public void ReturnObject(string key, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"Pool with key {key} doesn't exist.");
            Destroy(objectToReturn);
            return;
        }

        objectToReturn.SetActive(false);
        poolDictionary[key].Enqueue(objectToReturn);
    }

    private async Task<GameObject> CreateNewObject(AssetReferenceGameObject assetReference)
    {
        AsyncOperationHandle<GameObject> handle;
        string key = assetReference.AssetGUID;

        if (loadHandles.TryGetValue(key, out handle))
        {
            if (!handle.IsDone)
            {
                await handle.Task;
            }
        }
        else
        {
            handle = assetReference.LoadAssetAsync<GameObject>();
            loadHandles[key] = handle;
            await handle.Task;
        }

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            return Instantiate(handle.Result);
        }
        else
        {
            Debug.LogError($"Failed to load asset with key {key}");
            return null;
        }
    }
}