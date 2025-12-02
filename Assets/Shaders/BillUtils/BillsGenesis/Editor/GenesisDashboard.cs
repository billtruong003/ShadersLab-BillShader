#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;
using BillsGenesis.Data;
using BillsGenesis.Services;
using BillsGenesis.Core;
using System.Collections.Generic;

namespace BillsGenesis.EditorTools
{
    public class GenesisDashboard : OdinMenuEditorWindow
    {
        [MenuItem("Tools/BillsGenesis/Dashboard")]
        private static void OpenWindow()
        {
            var window = GetWindow<GenesisDashboard>();
            window.titleContent = new GUIContent("Genesis Hub");
            var main = GUIHelper.GetEditorWindowRect();
            var w = 1000;
            var h = 700;
            window.position = new Rect(main.x + (main.width - w) / 2, main.y + (main.height - h) / 2, w, h);
            window.Show();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            tree.Selection.SupportsMultiSelect = false;
            tree.Config.DrawSearchToolbar = true;
            tree.DefaultMenuStyle.IconSize = 24.00f;
            tree.Config.DefaultMenuStyle.Height = 32;

            tree.Add("Home", new GenesisHomeInfo());

            if (Application.isPlaying)
            {
                tree.Add("Runtime Monitor", new RuntimeMonitorPage(), SdfIconType.Activity);
            }

            tree.Add("Documentation", new GenesisDocs(), SdfIconType.Book);
            tree.Add("Documentation/Cheat Sheet", new GenesisCheatSheet(), SdfIconType.CodeSlash);

            var manifestGUIDs = AssetDatabase.FindAssets("t:GenesisManifest");
            if (manifestGUIDs.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(manifestGUIDs[0]);
                var manifest = AssetDatabase.LoadAssetAtPath<GenesisManifest>(path);
                tree.Add("Configuration", manifest, SdfIconType.Sliders);
            }
            else
            {
                tree.Add("Configuration", new CreateManifestPage(this), SdfIconType.GearFill);
            }

            return tree;
        }

        protected override void OnBeginDrawEditors()
        {
            SirenixEditorGUI.BeginHorizontalToolbar();
            GUILayout.Label("BillsGenesis Ecosystem V2.1", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Force Refresh", EditorStyles.toolbarButton)) ForceMenuTreeRebuild();
            SirenixEditorGUI.EndHorizontalToolbar();
        }

        // =================================================================================================
        // PAGE: RUNTIME MONITOR
        // =================================================================================================
        public class RuntimeMonitorPage
        {
            [Title("System Resources")]
            [HorizontalGroup("Stats", 0.5f)]
            [VerticalGroup("Stats/Left")]
            [ShowInInspector, ProgressBar(0, 120, r: 0, g: 1, b: 0), HideLabel]
            public float FPS => 1.0f / Time.smoothDeltaTime;

            [VerticalGroup("Stats/Right")]
            [ShowInInspector, DisplayAsString, LabelText("Memory Used")]
            public string TotalMemory => $"{UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576} MB";

            [Title("Service Status")]

            [BoxGroup("Timer Manager"), ShowInInspector, HideLabel, DisplayAsString]
            public string TimerStatus
            {
                get
                {
                    var tm = Genesis.Get<TimerManager>();
                    return tm ? $"Active Timers: {tm.ActiveTimersCount}" : "Service Not Found";
                }
            }

            [BoxGroup("Audio Manager"), ShowInInspector, HideLabel, DisplayAsString]
            public string AudioStatus
            {
                get
                {
                    var am = Genesis.Get<AudioManager>();
                    if (!am) return "Service Not Found";
                    return $"Volume (M/B/S): {am.MasterVolume:0.0}/{am.MusicVolume:0.0}/{am.SfxVolume:0.0} | Active SFX: {am.ActiveSfxCount}";
                }
            }

            [BoxGroup("Pool Manager")]
            [ShowInInspector, HideLabel]
            [DictionaryDrawerSettings(IsReadOnly = true, DisplayMode = DictionaryDisplayOptions.ExpandedFoldout, KeyLabel = "Prefab", ValueLabel = "Status")]
            public Dictionary<string, string> PoolInfo
            {
                get
                {
                    var pm = Genesis.Get<PoolManager>();
                    return pm ? pm.GetDebugInfo() : new Dictionary<string, string> { { "Status", "Offline" } };
                }
            }

            [Button(ButtonSizes.Large, Icon = SdfIconType.Trash), PropertySpace(20)]
            public void ForceGC()
            {
                System.GC.Collect();
                Resources.UnloadUnusedAssets();
            }
        }

        // =================================================================================================
        // PAGE: CHEAT SHEET (CODE SNIPPETS)
        // =================================================================================================
        public class GenesisCheatSheet
        {
            [Title("Quick Copy & Paste")]

            [TabGroup("Storage")]
            [HideLabel, TextArea(3, 10), ReadOnly]
            public string StorageCode =
@"// Save Data
Genesis.Get<StorageManager>().SetInt(""Highscore"", 100);
Genesis.Get<StorageManager>().SaveJson(""player_data"", myObject, encrypt: true);
Genesis.Get<StorageManager>().SaveList(""inventory"", myList);

// Load Data
int score = Genesis.Get<StorageManager>().GetInt(""Highscore"", 0);
var data = Genesis.Get<StorageManager>().LoadJson<PlayerData>(""player_data"");
";

            [TabGroup("Pool & VFX")]
            [HideLabel, TextArea(3, 10), ReadOnly]
            public string PoolCode =
@"// Spawning
GameObject enemy = Genesis.Get<PoolManager>().Spawn(enemyPrefab, position, rotation);
Genesis.Get<VFXManager>().Play(explosionVfx, position);
Genesis.Get<VFXManager>().PlayAttached(auraVfx, playerTransform, offset: Vector3.up);

// Despawning
Genesis.Get<PoolManager>().Despawn(enemy); // Returns to pool
Genesis.Get<PoolManager>().Despawn(enemy, 2.5f); // Delayed
";

            [TabGroup("Timer")]
            [HideLabel, TextArea(3, 10), ReadOnly]
            public string TimerCode =
@"// Simple Delay
Genesis.Get<TimerManager>().DoAfter(2f, () => Debug.Log(""Done""));

// Loop
Genesis.Get<TimerManager>().DoEvery(1f, () => CheckStatus(), boundObject: this.gameObject);

// Advanced
Genesis.Get<TimerManager>().Register(5f, OnComplete)
    .SetUpdateCallback(p => progressBar.fillAmount = p)
    .SetUnscaled(true);
";

            [TabGroup("Audio")]
            [HideLabel, TextArea(3, 10), ReadOnly]
            public string AudioCode =
@"// Play
Genesis.Get<AudioManager>().PlayMusic(bgmClip, fadeDuration: 1.5f);
Genesis.Get<AudioManager>().PlaySfx(jumpClip, pitchRandom: 0.1f);

// Control
Genesis.Get<AudioManager>().SetMasterVolume(0.5f);
Genesis.Get<AudioManager>().ToggleMute(true);
";
        }

        // =================================================================================================
        // PAGE: DOCUMENTATION
        // =================================================================================================
        public class GenesisDocs
        {
            [Title("Architecture")]
            [InfoBox("Genesis sử dụng Singleton Service Pattern. Mọi Manager đều kế thừa GenesisSingletonService<T>.", InfoMessageType.Info)]

            [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true, DefaultExpandedState = true)]
            [LabelText("Core Principles")]
            public string[] Principles = new string[]
            {
                "Clean Code: Không comment rác, đặt tên biến chuẩn.",
                "Zero Garbage: PoolManager và TimerManager được tối ưu để không sinh GC runtime.",
                "Dependency Injection: Dùng Genesis.Get<T>() hoặc [Inject] field.",
                "Async First: Các tác vụ nặng (Load Scene, IO) đều dùng Task/Async."
            };
        }

        // =================================================================================================
        // PAGE: HOME
        // =================================================================================================
        public class GenesisHomeInfo
        {
            [Title("Control Center", TitleAlignment = TitleAlignments.Centered)]
            [HorizontalGroup("H1", 0.7f), VerticalGroup("H1/Left")]
            [InfoBox("BillsGenesis Ready", InfoMessageType.Info, Icon = SdfIconType.CheckCircleFill)]
            [DisplayAsString] public string CurrentMode => Application.isPlaying ? "RUNTIME" : "EDITOR";

            [VerticalGroup("H1/Right")]
            [Button(ButtonSizes.Large, Name = "Play Bootstrap", Icon = SdfIconType.PlayFill), GUIColor(0.4f, 0.8f, 0.4f)]
            public void PlayBootstrap()
            {
                if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
                var scenes = EditorBuildSettings.scenes;
                if (scenes.Length > 0 && scenes[0].path.Contains("_Bootstrap"))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenes[0].path);
                    EditorApplication.isPlaying = true;
                }
            }

            [Title("Maintenance")]
            [HorizontalGroup("Actions")]
            [Button(ButtonSizes.Medium, Icon = SdfIconType.Trash)]
            public void ClearPlayerPrefs() => PlayerPrefs.DeleteAll();

            [Button(ButtonSizes.Medium, Icon = SdfIconType.FileCode)]
            public void OpenBuildSettings() => GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
        }

        public class CreateManifestPage
        {
            private GenesisDashboard _win;
            public CreateManifestPage(GenesisDashboard w) => _win = w;
            [Button(ButtonSizes.Gigantic, Icon = SdfIconType.CloudUpload)]
            public void CreateManifest()
            {
                var asset = ScriptableObject.CreateInstance<GenesisManifest>();
                if (!AssetDatabase.IsValidFolder("Assets/BillsGenesis/Resources"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/BillsGenesis")) AssetDatabase.CreateFolder("Assets", "BillsGenesis");
                    AssetDatabase.CreateFolder("Assets/BillsGenesis", "Resources");
                }
                AssetDatabase.CreateAsset(asset, "Assets/BillsGenesis/Resources/GenesisManifest.asset");
                AssetDatabase.SaveAssets();
                _win.ForceMenuTreeRebuild();
            }
        }
    }
}
#endif