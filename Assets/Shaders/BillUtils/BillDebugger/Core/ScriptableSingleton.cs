using UnityEngine;
using System.Linq;

public abstract class ScriptableSingleton<T> : ScriptableObject where T : ScriptableObject
{
    private static T _instance = null;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                T[] assets = Resources.LoadAll<T>("");
                _instance = assets.FirstOrDefault();
                if (_instance == null)
                {
                    Debug.LogError($"Could not find ScriptableObject of type {typeof(T).Name}. Please create one via Assets/Create menu.");
                }
            }
            return _instance;
        }
    }
}