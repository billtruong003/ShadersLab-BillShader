#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using BillsGenesis.Runtime;

namespace BillsGenesis.EditorTools
{
    public static class GenesisAutoBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            var active = SceneManager.GetActiveScene();
            if (active.name == "_Bootstrap") return;
            if (Object.FindAnyObjectByType<GenesisBootstrapper>()) return;

            GenesisBootstrapper.DevTargetScene = active.name;
            SceneManager.LoadScene("_Bootstrap");
        }
    }

    public class ManifestWizard : EditorWindow
    {
        [MenuItem("BillsGenesis/Manifest Wizard")]
        public static void Open() => GetWindow<ManifestWizard>("Genesis Setup");

        private void OnGUI()
        {
            if (GUILayout.Button("Create Basic Manifest"))
            {
                var asset = CreateInstance<BillsGenesis.Data.GenesisManifest>();
                AssetDatabase.CreateAsset(asset, "Assets/GenesisManifest.asset");
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(asset);
            }
        }
    }
}
#endif