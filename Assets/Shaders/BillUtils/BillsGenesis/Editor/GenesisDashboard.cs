#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BillsGenesis.Data;
using BillsGenesis.Services;
using BillsGenesis.Core;

namespace BillsGenesis.EditorTools
{
    public class GenesisDashboard : OdinMenuEditorWindow
    {
        private static readonly Color DarkBackground = new Color(0.12f, 0.12f, 0.14f);
        private static readonly Color SidebarColor = new Color(0.16f, 0.16f, 0.18f);
        private static readonly Color AccentColor = new Color(0.3f, 0.85f, 0.6f);
        private static readonly Color MutedText = new Color(0.6f, 0.6f, 0.65f);
        private static readonly Color LineColor = new Color(0.25f, 0.25f, 0.28f);

        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private Language _currentLanguage;

        private enum Language { EN, VI }

        [MenuItem("Tools/BillsGenesis/Dashboard %g")]
        private static void OpenWindow()
        {
            var w = GetWindow<GenesisDashboard>();
            w.titleContent = new GUIContent("Genesis Hub", EditorGUIUtility.FindTexture("d_UnityEditor.ConsoleWindow"));
            w.position = GUIHelper.GetEditorWindowRect().AlignCenter(1050, 680);
            w.Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _currentLanguage = (Language)EditorPrefs.GetInt("Genesis_Lang", (int)Language.EN);
        }

        protected override void OnBeginDrawEditors()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 22,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = AccentColor }
                };
                _subHeaderStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = MutedText }
                };
            }

            SirenixEditorGUI.DrawSolidRect(new Rect(0, 0, position.width, 60), SidebarColor);

            GUILayout.BeginHorizontal(GUILayout.Height(60));
            GUILayout.Space(20);

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label("BILLS GENESIS", _headerStyle);
            GUILayout.Label("Framework Version 2.5.1", _subHeaderStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            DrawLanguageToggle();

            GUILayout.Space(10);

            if (Application.isPlaying)
            {
                GUIHelper.PushColor(AccentColor);
                if (GUILayout.Button(new GUIContent(" RUNTIME ACTIVE", EditorGUIUtility.FindTexture("PlayButton On")), GUILayout.Height(30), GUILayout.Width(140))) { }
                GUIHelper.PopColor();
            }
            else
            {
                GUIHelper.PushColor(MutedText);
                if (GUILayout.Button(new GUIContent(" RELOAD", EditorGUIUtility.FindTexture("Refresh")), EditorStyles.miniButton, GUILayout.Height(24), GUILayout.Width(80)))
                {
                    ForceMenuTreeRebuild();
                }
                GUIHelper.PopColor();
            }

            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            SirenixEditorGUI.DrawSolidRect(new Rect(0, 59, position.width, 1), LineColor);
        }

        private void DrawLanguageToggle()
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(24));

            GUIHelper.PushColor(_currentLanguage == Language.EN ? AccentColor : Color.gray);
            if (GUILayout.Button("EN", EditorStyles.label, GUILayout.Width(25)))
            {
                SetLanguage(Language.EN);
            }
            GUIHelper.PopColor();

            GUILayout.Label("|", GUILayout.Width(10));

            GUIHelper.PushColor(_currentLanguage == Language.VI ? AccentColor : Color.gray);
            if (GUILayout.Button("VI", EditorStyles.label, GUILayout.Width(25)))
            {
                SetLanguage(Language.VI);
            }
            GUIHelper.PopColor();

            GUILayout.EndHorizontal();
        }

        private void SetLanguage(Language lang)
        {
            if (_currentLanguage == lang) return;
            _currentLanguage = lang;
            EditorPrefs.SetInt("Genesis_Lang", (int)lang);
            ForceMenuTreeRebuild();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(false);
            tree.Config.DrawSearchToolbar = true;
            tree.DefaultMenuStyle.IconSize = 20;
            tree.DefaultMenuStyle.Height = 35;
            tree.DefaultMenuStyle.IndentAmount = 15;
            tree.Config.DefaultMenuStyle.BorderPadding = 0;

            var customStyle = new OdinMenuStyle
            {
                Height = 35,
                IconSize = 20,
                SelectedColorDarkSkin = new Color(0.3f, 0.85f, 0.6f, 0.15f),
                AlignTriangleLeft = false,
                TriangleSize = 12f,
                Borders = false
            };

            tree.DefaultMenuStyle = customStyle;

            tree.Add("Dashboard", new HomeView(), SdfIconType.Speedometer);

            if (Application.isPlaying)
            {
                tree.Add("Live Monitor", new RuntimeMonitorView(), SdfIconType.Activity);
            }

            var manifest = AssetDatabase.LoadAssetAtPath<GenesisManifest>("Assets/BillsGenesis/Resources/GenesisManifest.asset");
            if (manifest) tree.Add("Configuration", manifest, SdfIconType.Sliders);
            else tree.Add("Configuration", new CreateManifestView(this), SdfIconType.ExclamationTriangleFill);

            var fullDocs = GenesisDocLoader.LoadDocs();
            if (fullDocs != null)
            {
                var content = _currentLanguage == Language.EN ? fullDocs.en : fullDocs.vi;

                tree.Add("Documentation", new DocMetaDataView(fullDocs.metadata), SdfIconType.InfoCircle);

                if (content.architecture != null)
                {
                    tree.Add("Documentation/Architecture", new DocArchView(content.architecture), SdfIconType.Diagram3);
                }

                if (content.modules != null)
                {
                    foreach (var mod in content.modules)
                    {
                        tree.Add($"Documentation/{mod.name}", new DocModuleView(mod), GetIcon(mod.icon));
                    }
                }
            }
            else
            {
                tree.Add("Documentation", new ErrorView("Documentation missing. Ensure 'genesis_docs.json' is in Resources."), SdfIconType.QuestionCircle);
            }

            return tree;
        }

        private SdfIconType GetIcon(string name)
        {
            if (System.Enum.TryParse(name, true, out SdfIconType icon)) return icon;
            return SdfIconType.Box;
        }

        public void Refresh() => ForceMenuTreeRebuild();

        public class HomeView
        {
            [Title("Quick Actions")]
            [HorizontalGroup("Actions", 0.5f, PaddingRight = 10)]
            [VerticalGroup("Actions/Left")]
            [Button(ButtonSizes.Large, Icon = SdfIconType.PlayFill, Name = "Boot Game"), GUIColor(0.3f, 0.85f, 0.6f)]
            public void PlayBootstrap()
            {
                if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
                var s = EditorBuildSettings.scenes.FirstOrDefault(x => x.path.Contains("_Bootstrap"));
                if (s != null && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(s.path);
                    EditorApplication.isPlaying = true;
                }
            }

            [VerticalGroup("Actions/Left")]
            [InfoBox("Starts from '_Bootstrap' scene.", InfoMessageType.None), ShowInInspector, HideLabel, DisplayAsString]
            public string BootInfo => "";

            [VerticalGroup("Actions/Right")]
            [Button(ButtonSizes.Large, Icon = SdfIconType.TrashFill, Name = "Wipe Data"), GUIColor(0.9f, 0.4f, 0.4f)]
            public void WipeData()
            {
                PlayerPrefs.DeleteAll();
                if (Directory.Exists(Application.persistentDataPath))
                {
                    var di = new DirectoryInfo(Application.persistentDataPath);
                    foreach (FileInfo file in di.GetFiles()) file.Delete();
                }
                Debug.Log("[Genesis] User Data Wiped.");
            }

            [VerticalGroup("Actions/Right")]
            [InfoBox("Clears PlayerPrefs & Persistent Files.", InfoMessageType.None), ShowInInspector, HideLabel, DisplayAsString]
            public string WipeInfo => "";

            [Title("System Status")]
            [HorizontalGroup("Status")]
            [BoxGroup("Status/Info"), ShowInInspector, HideLabel, DisplayAsString]
            public string ManifestState => AssetDatabase.LoadAssetAtPath<GenesisManifest>("Assets/BillsGenesis/Resources/GenesisManifest.asset") ? "✔ Manifest Linked" : "✘ Manifest Missing";

            [BoxGroup("Status/Info"), ShowInInspector, HideLabel, DisplayAsString]
            public string UnityVer => $"Unity {Application.unityVersion}";
        }

        public class RuntimeMonitorView
        {
            [Title("Performance")]
            [HorizontalGroup("Perf")]
            [BoxGroup("Perf/FPS"), ProgressBar(0, 144, 0.3f, 0.85f, 0.6f), ShowInInspector, HideLabel]
            public float FPS => 1.0f / Time.smoothDeltaTime;

            [BoxGroup("Perf/RAM"), ProgressBar(0, 2048, 0.3f, 0.5f, 0.9f, Segmented = true), ShowInInspector, HideLabel]
            public float MemoryMB => UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f;

            [Title("Active Services")]
            [TableList(IsReadOnly = true, AlwaysExpanded = true), ShowInInspector, HideLabel]
            public List<ServiceStatus> Services
            {
                get
                {
                    var list = new List<ServiceStatus>();
                    Add(list, "Audio", Genesis.Get<AudioManager>());
                    Add(list, "Pool", Genesis.Get<PoolManager>());
                    Add(list, "Signals", Genesis.Get<SignalHub>());
                    Add(list, "Storage", Genesis.Get<StorageManager>());
                    Add(list, "Scenes", Genesis.Get<SceneManagerService>());
                    Add(list, "Timers", Genesis.Get<TimerManager>());
                    Add(list, "VFX", Genesis.Get<VFXManager>());
                    return list;
                }
            }

            private void Add<T>(List<ServiceStatus> list, string name, T service) where T : class
            {
                list.Add(new ServiceStatus { Name = name, Status = service != null ? "Active" : "Offline" });
            }

            public struct ServiceStatus
            {
                [TableColumnWidth(150)] public string Name;
                [TableColumnWidth(100)][GUIColor("@Status == \"Active\" ? new Color(0.3f, 0.85f, 0.6f) : new Color(0.9f, 0.4f, 0.4f)")] public string Status;
            }
        }

        public class CreateManifestView
        {
            private GenesisDashboard _win;
            public CreateManifestView(GenesisDashboard win) => _win = win;

            [InfoBox("GenesisManifest is missing from Resources.", InfoMessageType.Error)]
            [Button(ButtonSizes.Gigantic, Icon = SdfIconType.CloudUploadFill), GUIColor(0.3f, 0.85f, 0.6f)]
            public void CreateManifest()
            {
                if (!AssetDatabase.IsValidFolder("Assets/BillsGenesis/Resources"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/BillsGenesis")) AssetDatabase.CreateFolder("Assets", "BillsGenesis");
                    AssetDatabase.CreateFolder("Assets/BillsGenesis", "Resources");
                }
                var asset = ScriptableObject.CreateInstance<GenesisManifest>();
                AssetDatabase.CreateAsset(asset, "Assets/BillsGenesis/Resources/GenesisManifest.asset");
                AssetDatabase.SaveAssets();
                _win.Refresh();
            }
        }

        public class DocMetaDataView
        {
            [Title("Framework Information", null, TitleAlignments.Centered)]
            [PropertySpace(20)]

            [BoxGroup("General Info", CenterLabel = true)]
            [LabelWidth(100)]
            [DisplayAsString, ShowInInspector, GUIColor(0.6f, 1f, 0.8f)]
            public string Framework;

            [BoxGroup("General Info")]
            [LabelWidth(100)]
            [DisplayAsString, ShowInInspector]
            public string Version;

            [BoxGroup("General Info")]
            [LabelWidth(100)]
            [DisplayAsString, ShowInInspector]
            public string Author;

            [BoxGroup("Build Details", CenterLabel = true)]
            [LabelWidth(100)]
            [DisplayAsString, ShowInInspector]
            public string BuildDate;

            [BoxGroup("Build Details")]
            [LabelWidth(100)]
            [DisplayAsString, ShowInInspector]
            public string Theme;

            public DocMetaDataView(DocMetadata m)
            {
                Framework = m.framework;
                Version = m.version;
                Author = m.author;
                BuildDate = m.build_date;
                Theme = m.theme;
            }
        }

        public class DocArchView
        {
            [Title("System Overview")]
            [HideLabel, DisplayAsString, ShowInInspector] public string Summary;

            [Title("Core Principles")]
            [ListDrawerSettings(IsReadOnly = true, ShowPaging = false)]
            [ShowInInspector] public List<string> Principles;

            [Title("Boot Lifecycle")]
            [ListDrawerSettings(IsReadOnly = true, ShowPaging = false, ShowIndexLabels = false)]
            [ShowInInspector] public List<string> BootSequence;

            public DocArchView(DocArchitecture arch)
            {
                if (arch?.overview != null)
                {
                    Summary = arch.overview.summary;
                    Principles = arch.overview.core_principles;
                }
                if (arch?.lifecycle != null)
                {
                    BootSequence = arch.lifecycle.boot_sequence;
                }
            }
        }

        public class DocModuleView
        {
            [Title("$Name", "$Desc", TitleAlignments.Split)]
            [HideLabel, DisplayAsString, ShowInInspector, HideInEditorMode] public string Name;
            [HideInInspector] public string Desc;

            [Space(10)]
            [LabelText("Best Practices"), ListDrawerSettings(IsReadOnly = true, ShowPaging = false, ShowFoldout = true)]
            [ShowInInspector, HideIf("@Practices == null || Practices.Count == 0")]
            public List<string> Practices;

            [Space(10)]
            [LabelText("Troubleshooting"), ListDrawerSettings(IsReadOnly = true, ShowPaging = false, ShowFoldout = true)]
            [ShowInInspector, HideIf("@Troubleshooting == null || Troubleshooting.Count == 0")]
            [GUIColor(1f, 0.6f, 0.6f)]
            public List<string> Troubleshooting;

            [Space(10)]
            [Title("API Reference")]
            [ListDrawerSettings(IsReadOnly = true, ShowPaging = false, DraggableItems = false)]
            [ShowInInspector] public List<DocApiMethod> Api;

            public DocModuleView(DocModule m)
            {
                Name = m.name;
                Desc = m.description;
                Practices = m.best_practices ?? new List<string>();
                Troubleshooting = m.troubleshooting ?? new List<string>();
                Api = m.api != null ? m.api.Select(x => new DocApiMethod(x)).ToList() : new List<DocApiMethod>();
            }
        }

        [HideReferenceObjectPicker]
        public class DocApiMethod
        {
            [DisplayAsString, HideLabel, GUIColor(0.5f, 0.8f, 1f), PropertyOrder(0)]
            [ShowInInspector] public string Signature;

            [DisplayAsString, HideLabel, PropertyOrder(1)]
            [ShowInInspector] public string Description;

            [TextArea(2, 6), HideLabel, ReadOnly, PropertyOrder(2)]
            [GUIColor(0.8f, 0.8f, 0.8f)]
            [ShowInInspector] public string Example;

            [HorizontalGroup("Btns")]
            [Button("Copy Code", ButtonSizes.Small), PropertyOrder(3)]
            public void Copy()
            {
                GUIUtility.systemCopyBuffer = Example;
                Debug.Log("[Genesis] Copied to clipboard.");
            }

            public DocApiMethod(DocApi api)
            {
                Signature = api.signature;
                Description = api.description;
                Example = api.example;
            }
        }

        public class ErrorView
        {
            [InfoBox("$Msg", InfoMessageType.Error)] public string Msg;
            public ErrorView(string m) => Msg = m;
        }

        public static class GenesisDocLoader
        {
            public static DocRoot LoadDocs()
            {
                var asset = Resources.Load<TextAsset>("genesis_docs");
                return asset ? JsonUtility.FromJson<DocRoot>(asset.text) : null;
            }
        }

        [System.Serializable]
        public class DocRoot
        {
            public DocMetadata metadata;
            public DocContent en;
            public DocContent vi;
        }

        [System.Serializable]
        public class DocContent
        {
            public DocArchitecture architecture;
            public List<DocModule> modules;
        }

        [System.Serializable]
        public class DocMetadata
        {
            public string framework;
            public string version;
            public string build_date;
            public string author;
            public string theme;
        }

        [System.Serializable]
        public class DocArchitecture
        {
            public DocOverview overview;
            public DocLifecycle lifecycle;
        }

        [System.Serializable]
        public class DocOverview
        {
            public string summary;
            public List<string> core_principles;
        }

        [System.Serializable]
        public class DocLifecycle
        {
            public List<string> boot_sequence;
        }

        [System.Serializable]
        public class DocModule
        {
            public string id;
            public string name;
            public string icon;
            public string description;
            public List<string> best_practices;
            public List<string> troubleshooting;
            public List<DocApi> api;
        }

        [System.Serializable]
        public class DocApi
        {
            public string signature;
            public string description;
            public string example;
        }
    }
}
#endif