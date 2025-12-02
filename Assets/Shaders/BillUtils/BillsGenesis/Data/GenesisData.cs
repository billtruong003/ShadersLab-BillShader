#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace BillsGenesis.Data
{
    [CreateAssetMenu(fileName = "GenesisManifest", menuName = "BillsGenesis/Manifest")]
    public sealed class GenesisManifest : SerializedScriptableObject
    {
        [Title("System Configuration")]
        // FIX: Expanded -> DefaultExpandedState
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true)]
        [LabelText("Global Services")]
        public List<GameObject> SystemPrefabs;

        [Title("Scene Configuration")]
        [Searchable]
        [TableList(ShowIndexLabels = true, DrawScrollView = true, MaxScrollViewHeight = 400)]
        public List<SceneGroup> SceneGroups;

        [ValueDropdown("GetGroupIds")]
        [BoxGroup("Defaults")]
        public string InitialGroupId;

        public SceneGroup GetGroup(string id) => SceneGroups.Find(x => x.GroupId == id);

#if UNITY_EDITOR
        private IEnumerable<string> GetGroupIds()
        {
            return SceneGroups != null ? SceneGroups.Select(x => x.GroupId) : Enumerable.Empty<string>();
        }
#endif
    }

    // ... Phần dưới giữ nguyên
    [Serializable]
    public class SceneGroup
    {
        [TableColumnWidth(150, Resizable = false)]
        [Required]
        public string GroupId;

        [TableColumnWidth(200)]
        [ValueDropdown("GetAllScenesInBuild")]
        [ValidateInput("ValidateSceneInBuild", "Scene not in Build Settings!")]
        public string ActiveScene;

        [ValueDropdown("GetAllScenesInBuild")]
        public List<string> AdditiveScenes;

#if UNITY_EDITOR
        private static IEnumerable<string> GetAllScenesInBuild()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => System.IO.Path.GetFileNameWithoutExtension(scene.path));
        }

        private bool ValidateSceneInBuild(string sceneName)
        {
            return GetAllScenesInBuild().Contains(sceneName);
        }
#endif
    }
}