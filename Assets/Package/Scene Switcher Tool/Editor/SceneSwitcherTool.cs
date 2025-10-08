// Save this as SceneSwitcherTool.cs
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;

public class SceneSwitcherToolWindow : EditorWindow
{
    private List<SceneAsset> bookmarkedScenes = new List<SceneAsset>();
    private List<EditorBuildSettingsScene> scenesInBuild = new List<EditorBuildSettingsScene>();

    // UI Elements
    private VisualElement root;
    private VisualElement mainContainer;
    private VisualElement listContainer;
    private ListView sceneListView;
    private DropdownField modeDropdown;
    private TextField searchField;
    private Button loadButton;
    private Button additiveButton;
    private Button pingInFolderButton;
    private Button addCurrentBookmarkButton;
    private Button removeBookmarkButton;
    private Button removeAllBookmarksButton;
    private VisualElement emptyListHint;

    private int selectedMode = 0;
    private const string BookmarkedScenesKey = "BookmarkedScenes";
    private const string DragHoverClassName = "drag-hover-active";

    // --- RESPONSIVE LAYOUT ---
    private const float MinWidthForHorizontalLayout = 480f;
    private bool isHorizontalLayout = true;

    [MenuItem("Tools/Bill Utils/Scene Switcher Tool Pro")]
    public static void ShowWindow()
    {
        SceneSwitcherToolWindow wnd = GetWindow<SceneSwitcherToolWindow>();
        wnd.titleContent = new GUIContent("Scene Switcher");
        wnd.minSize = new Vector2(280, 300);
    }

    private void OnEnable()
    {
        bookmarkedScenes = LoadBookmarkedScenesFromPrefs();
        scenesInBuild = EditorBuildSettings.scenes.ToList();
    }

    public void CreateGUI()
    {
        root = rootVisualElement;

        MonoScript thisScript = MonoScript.FromScriptableObject(this);
        string scriptPath = AssetDatabase.GetAssetPath(thisScript);
        string scriptFolder = Path.GetDirectoryName(scriptPath);
        string uxmlPath = Path.Combine(scriptFolder, "SceneSwitcherTool.uxml");
        string ussPath = Path.Combine(scriptFolder, "SceneSwitcherTool.uss");

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
        if (visualTree == null)
        {
            root.Add(new Label("Could not load SceneSwitcherTool.uxml. Make sure it's in the same folder as the script."));
            return;
        }
        visualTree.CloneTree(root);

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
        if (styleSheet != null) root.styleSheets.Add(styleSheet);
        else Debug.LogWarning("Could not load SceneSwitcherTool.uss stylesheet.");

        // Query UI elements
        mainContainer = root.Q<VisualElement>("main-container");
        listContainer = root.Q<VisualElement>("list-container");
        sceneListView = root.Q<ListView>("scene-list");
        modeDropdown = root.Q<DropdownField>("mode-dropdown");
        searchField = root.Q<TextField>("search-field");
        loadButton = root.Q<Button>("load-button");
        additiveButton = root.Q<Button>("additive-button");
        pingInFolderButton = root.Q<Button>("ping-in-folder-button");
        addCurrentBookmarkButton = root.Q<Button>("add-current-bookmark-button");
        removeBookmarkButton = root.Q<Button>("remove-bookmark-button");
        removeAllBookmarksButton = root.Q<Button>("remove-all-bookmarks-button");
        emptyListHint = root.Q<VisualElement>("empty-list-hint");

        SetupIcons(root);
        SetupSceneListView();
        SetupModeDropdown();
        RegisterCallbacks();

        root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);

        RefreshSceneList();
        UpdateButtonsState();
    }

    private void OnRootGeometryChanged(GeometryChangedEvent evt)
    {
        bool newIsHorizontal = evt.newRect.width > MinWidthForHorizontalLayout;
        if (newIsHorizontal != isHorizontalLayout)
        {
            isHorizontalLayout = newIsHorizontal;
            UpdateLayoutClasses();
        }
    }

    private void UpdateLayoutClasses()
    {
        if (mainContainer == null) return;
        mainContainer.EnableInClassList("wide-layout", isHorizontalLayout);
        mainContainer.EnableInClassList("narrow-layout", !isHorizontalLayout);
    }

    private void SetupIcons(VisualElement root)
    {
        root.Q<Image>("search-icon").image = EditorGUIUtility.IconContent("d_Search Icon").image;
        root.Q<Image>("load-icon").image = EditorGUIUtility.IconContent("d_PlayButton").image;
        root.Q<Image>("additive-icon").image = EditorGUIUtility.IconContent("d_Toolbar Plus").image;
        root.Q<Image>("ping-icon").image = EditorGUIUtility.IconContent("d_FolderOpened Icon").image;
        root.Q<Image>("add-current-icon").image = EditorGUIUtility.IconContent("d_Toolbar Plus More").image;
        root.Q<Image>("remove-selected-icon").image = EditorGUIUtility.IconContent("d_Toolbar Minus").image;
        root.Q<Image>("remove-all-icon").image = EditorGUIUtility.IconContent("d_TreeEditor.Trash").image;
    }

    private void RegisterCallbacks()
    {
        searchField.RegisterValueChangedCallback(evt => RefreshSceneList());
        loadButton.clicked += () => LoadSelectedScene(OpenSceneMode.Single);
        additiveButton.clicked += () => LoadSelectedScene(OpenSceneMode.Additive);
        pingInFolderButton.clicked += PingSelectedSceneInFolder;
        addCurrentBookmarkButton.clicked += AddCurrentSceneToBookmarks;
        removeBookmarkButton.clicked += RemoveSelectedBookmark;
        removeAllBookmarksButton.clicked += RemoveAllBookmarkedScenes;

        // Register drag events on BOTH the container and the list view for robust dropping
        listContainer.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
        listContainer.RegisterCallback<DragPerformEvent>(OnDragPerform);
        listContainer.RegisterCallback<DragLeaveEvent>(OnDragLeave);

        sceneListView.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
        sceneListView.RegisterCallback<DragPerformEvent>(OnDragPerform);
        sceneListView.RegisterCallback<DragLeaveEvent>(OnDragLeave);
    }

    private void OnDragUpdate(DragUpdatedEvent evt)
    {
        if (selectedMode != 0)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            return;
        }

        bool validDrag = DragAndDrop.objectReferences.Any(obj =>
            obj is SceneAsset ||
            (obj is GameObject go && go.scene.IsValid() && !string.IsNullOrEmpty(go.scene.path)));

        if (validDrag)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            listContainer.AddToClassList(DragHoverClassName);
            evt.StopPropagation();
        }
        else
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
        }
    }

    private void OnDragPerform(DragPerformEvent evt)
    {
        listContainer.RemoveFromClassList(DragHoverClassName);
        if (selectedMode != 0) return;

        DragAndDrop.AcceptDrag();
        bool scenesAdded = false;

        foreach (Object draggedObject in DragAndDrop.objectReferences)
        {
            SceneAsset sceneToAdd = null;
            if (draggedObject is SceneAsset sceneAsset) sceneToAdd = sceneAsset;
            else if (draggedObject is GameObject go && go.scene.IsValid() && !string.IsNullOrEmpty(go.scene.path))
            {
                sceneToAdd = AssetDatabase.LoadAssetAtPath<SceneAsset>(go.scene.path);
            }
            if (sceneToAdd != null && AddSceneToBookmarksInternal(sceneToAdd)) scenesAdded = true;
        }

        if (scenesAdded)
        {
            SaveBookmarkedScenes();
            RefreshSceneList();
        }
        evt.StopPropagation();
    }

    private void OnDragLeave(DragLeaveEvent evt)
    {
        listContainer.RemoveFromClassList(DragHoverClassName);
    }

    private void SetupModeDropdown()
    {
        modeDropdown.choices = new List<string> { "Bookmarks", "Scenes In Build" };
        modeDropdown.index = 0;
        modeDropdown.RegisterValueChangedCallback(evt =>
        {
            selectedMode = modeDropdown.index;
            sceneListView.selectedIndex = -1;
            listContainer.RemoveFromClassList(DragHoverClassName);
            RefreshSceneList();
            UpdateButtonsState();
        });
    }

    private void SetupSceneListView()
    {
        sceneListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        sceneListView.selectionType = SelectionType.Single;
        sceneListView.makeItem = () =>
        {
            var itemRoot = new VisualElement();
            itemRoot.AddToClassList("list-item-container");
            var icon = new Image { name = "list-item-icon", image = EditorGUIUtility.IconContent("d_UnityEditor.GameView").image };
            var label = new Label { name = "list-item-label" };
            itemRoot.Add(icon);
            itemRoot.Add(label);
            return itemRoot;
        };
        sceneListView.bindItem = (element, i) =>
        {
            var label = element.Q<Label>("list-item-label");
            string sceneName = "", scenePath = "";
            if (selectedMode == 0)
            {
                var scene = (SceneAsset)sceneListView.itemsSource[i];
                sceneName = scene.name;
                scenePath = AssetDatabase.GetAssetPath(scene);
            }
            else
            {
                var scene = (EditorBuildSettingsScene)sceneListView.itemsSource[i];
                scenePath = scene.path;
                sceneName = Path.GetFileNameWithoutExtension(scenePath);
            }
            label.text = sceneName;
            label.tooltip = scenePath;
        };
        sceneListView.selectionChanged += (selection) => UpdateButtonsState();
        sceneListView.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.clickCount == 2 && evt.button == 0) LoadSelectedScene(OpenSceneMode.Single);
        });
    }

    private void RefreshSceneList()
    {
        bookmarkedScenes = bookmarkedScenes.Where(s => s != null).ToList();
        System.Collections.IList itemsSource;
        if (selectedMode == 0) itemsSource = bookmarkedScenes.Where(s => IsSceneVisible(s.name)).ToList();
        else
        {
            scenesInBuild = EditorBuildSettings.scenes.ToList();
            itemsSource = scenesInBuild.Where(s => IsSceneVisible(Path.GetFileNameWithoutExtension(s.path))).ToList();
        }
        sceneListView.itemsSource = itemsSource;
        sceneListView.Rebuild();
        bool showHint = selectedMode == 0 && itemsSource.Count == 0;
        emptyListHint.style.display = showHint ? DisplayStyle.Flex : DisplayStyle.None;
        sceneListView.style.display = showHint ? DisplayStyle.None : DisplayStyle.Flex;
        UpdateButtonsState();
    }

    private void LoadSelectedScene(OpenSceneMode mode)
    {
        if (sceneListView.selectedIndex < 0 || sceneListView.itemsSource == null) return;
        string scenePath = "";
        if (selectedMode == 0)
        {
            var scene = (SceneAsset)sceneListView.itemsSource[sceneListView.selectedIndex];
            scenePath = AssetDatabase.GetAssetPath(scene);
        }
        else
        {
            var scene = (EditorBuildSettingsScene)sceneListView.itemsSource[sceneListView.selectedIndex];
            scenePath = scene.path;
        }
        if (!string.IsNullOrEmpty(scenePath) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath, mode);
        }
    }

    private void PingSelectedSceneInFolder()
    {
        if (sceneListView.selectedIndex < 0 || sceneListView.itemsSource == null) return;
        Object sceneAsset = null;
        if (selectedMode == 0) sceneAsset = (SceneAsset)sceneListView.itemsSource[sceneListView.selectedIndex];
        else
        {
            var scene = (EditorBuildSettingsScene)sceneListView.itemsSource[sceneListView.selectedIndex];
            sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
        }
        if (sceneAsset != null) EditorGUIUtility.PingObject(sceneAsset);
    }

    private bool IsSceneVisible(string sceneName) => string.IsNullOrEmpty(searchField.value) || sceneName.ToLower().Contains(searchField.value.ToLower());

    private void UpdateButtonsState()
    {
        bool isSceneSelected = sceneListView.selectedIndex >= 0;
        bool isBookmarkMode = selectedMode == 0;
        loadButton.SetEnabled(isSceneSelected);
        additiveButton.SetEnabled(isSceneSelected);
        pingInFolderButton.SetEnabled(isSceneSelected);
        addCurrentBookmarkButton.SetEnabled(isBookmarkMode);
        removeBookmarkButton.SetEnabled(isSceneSelected && isBookmarkMode);
        removeAllBookmarksButton.SetEnabled(isBookmarkMode && bookmarkedScenes.Count > 0);
    }

    private void AddCurrentSceneToBookmarks()
    {
        string currentScenePath = EditorSceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(currentScenePath))
        {
            EditorUtility.DisplayDialog("No Scene", "The current scene has not been saved yet. Please save the scene first.", "OK");
            return;
        }
        AddSceneToBookmarks(AssetDatabase.LoadAssetAtPath<SceneAsset>(currentScenePath));
    }

    private void AddSceneToBookmarks(SceneAsset sceneAsset)
    {
        if (AddSceneToBookmarksInternal(sceneAsset))
        {
            SaveBookmarkedScenes();
            RefreshSceneList();
        }
    }

    private bool AddSceneToBookmarksInternal(SceneAsset sceneAsset)
    {
        if (sceneAsset != null && !bookmarkedScenes.Contains(sceneAsset))
        {
            bookmarkedScenes.Add(sceneAsset);
            return true;
        }
        return false;
    }

    private void RemoveSelectedBookmark()
    {
        if (selectedMode != 0 || sceneListView.selectedIndex < 0 || sceneListView.itemsSource == null) return;
        SceneAsset sceneToRemove = (SceneAsset)sceneListView.itemsSource[sceneListView.selectedIndex];
        if (sceneToRemove != null)
        {
            bookmarkedScenes.Remove(sceneToRemove);
            SaveBookmarkedScenes();
            sceneListView.selectedIndex = -1;
            RefreshSceneList();
        }
    }

    private void RemoveAllBookmarkedScenes()
    {
        if (EditorUtility.DisplayDialog("Remove All Bookmarks?", "Are you sure you want to remove all bookmarked scenes?", "Yes", "No"))
        {
            bookmarkedScenes.Clear();
            SaveBookmarkedScenes();
            RefreshSceneList();
        }
    }

    private void SaveBookmarkedScenes()
    {
        bookmarkedScenes = bookmarkedScenes.Where(s => s != null).Distinct().ToList();
        var guids = bookmarkedScenes.Select(asset => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset))).ToList();
        EditorPrefs.SetString(BookmarkedScenesKey, string.Join(";", guids));
    }

    private List<SceneAsset> LoadBookmarkedScenesFromPrefs()
    {
        List<SceneAsset> scenes = new List<SceneAsset>();
        if (EditorPrefs.HasKey(BookmarkedScenesKey))
        {
            string data = EditorPrefs.GetString(BookmarkedScenesKey);
            if (!string.IsNullOrEmpty(data))
            {
                string[] guids = data.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                        if (sceneAsset != null) scenes.Add(sceneAsset);
                    }
                }
            }
        }
        return scenes.Distinct().ToList();
    }
}