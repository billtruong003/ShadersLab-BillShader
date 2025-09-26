using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

public class SceneSwitcherTool : EditorWindow
{
    private List<SceneAsset> bookmarkedScenes = new List<SceneAsset>();
    private string selectedScene = null;
    private string searchQuery = "";
    private Vector2 scenesInBuildScrollPosition;
    private Vector2 bookmarkedScenesScrollPosition;
    private double lastClickTime = 0;
    private const float doubleClickThreshold = 0.3f;
    private string lastClickedScene = null;
    private Texture2D profileImage;
    private int selectedBookmarkIndex = -1;
    private int selectedMode = 0; // 0: Bookmarks, 1: Scenes in Build

    private const string BookmarkedScenesKey = "BookmarkedScenes";
    private const string SceneSwitchDropdownName = "Scene Switcher"; // Name for the dropdown menu

    [MenuItem("Tools/Bill Utils/Scene Switcher Tool")]
    public static void ShowWindow()
    {
        GetWindow<SceneSwitcherTool>("Scene Switcher Tool");
    }

    [MenuItem("Tools/Bill Utils/Scene Switcher Tool", false, 1000)] // Add a menu item to the main menu
    public static void Init()
    {
        SceneSwitcherTool window = (SceneSwitcherTool)EditorWindow.GetWindow(typeof(SceneSwitcherTool), false, "Scene Switcher");
        window.Show();
    }

    private void OnEnable()
    {
        LoadBookmarkedScenes();
        profileImage = Resources.Load<Texture2D>("UI/ProfileImg");
    }

    private void OnGUI()
    {
        GUILayout.Label("Search Scenes", EditorStyles.boldLabel);
        searchQuery = EditorGUILayout.TextField(searchQuery);

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.MaxWidth(position.width / 2 - 10));


        // Dropdown for selecting mode
        string[] modeOptions = { "Bookmarks", "Scenes In Build" };
        selectedMode = EditorGUILayout.Popup("Select Mode:", selectedMode, modeOptions);


        if (selectedMode == 0)
        {
            DrawBookmarkedScenes();
        }
        else if (selectedMode == 1)
        {
            DrawScenesInBuild();
        }

        GUILayout.EndVertical();

        GUILayout.Space(10);

        GUILayout.BeginVertical("box", GUILayout.MaxWidth(position.width / 2 - 10), GUILayout.ExpandHeight(true));

        GUILayout.Label("Scene Options", EditorStyles.boldLabel);

        if (GUILayout.Button(new GUIContent("Load", EditorGUIUtility.IconContent("d_Refresh").image), GUILayout.Height(30)))
        {
            if (selectedMode == 1 && !string.IsNullOrEmpty(selectedScene))
            {
                Debug.Log($"Additive button clicked for scene: {selectedScene}");
                string scenePath = GetScenePathByName(selectedScene);
                EditorSceneManager.OpenScene(scenePath);
            }
            else if (selectedMode == 0 && selectedBookmarkIndex != -1)
            {
                string scenePath = AssetDatabase.GetAssetPath(bookmarkedScenes[selectedBookmarkIndex]);
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        if (GUILayout.Button(new GUIContent("Additive", EditorGUIUtility.IconContent("d_Toolbar Plus More").image), GUILayout.Height(30)))
        {
            if (selectedMode == 1 && !string.IsNullOrEmpty(selectedScene))
            {
                Debug.Log($"Additive button clicked for scene: {selectedScene}");
                string scenePath = GetScenePathByName(selectedScene);
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }
            else if (selectedMode == 0 && selectedBookmarkIndex != -1)
            {
                string scenePath = AssetDatabase.GetAssetPath(bookmarkedScenes[selectedBookmarkIndex]);
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("Bookmark Edit", EditorStyles.boldLabel);

        DrawDragAndDropArea();

        GUI.enabled = selectedBookmarkIndex != -1 && selectedMode == 0;
        if (GUILayout.Button(new GUIContent("Remove Bookmark", EditorGUIUtility.IconContent("Toolbar Minus").image), GUILayout.Height(30)))
        {
            RemoveBookmarkedScene(selectedBookmarkIndex);
        }
        GUI.enabled = true;

        if (GUILayout.Button(new GUIContent("Remove All Bookmarks", EditorGUIUtility.IconContent("d_CacheServerDisconnected@2x").image), GUILayout.Height(30)))
        {
            Debug.Log("Remove All Bookmarks button clicked");
            RemoveAllBookmarkedScenes();
        }
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        if (profileImage != null)
        {
            Rect windowRect = position;
            float imageWidth = profileImage.width;
            float imageHeight = profileImage.height;
            float x = windowRect.width - imageWidth - 20;
            float y = windowRect.height - imageHeight - 60;

            Color originalColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(x - 5, y - 5, imageWidth + 10, imageHeight + 10), Texture2D.whiteTexture);
            GUI.color = originalColor;

            GUI.DrawTexture(new Rect(x, y, imageWidth, imageHeight), profileImage);
        }

        GUILayout.Space(10);
    }

    private void DrawBookmarkedScenes()
    {
        GUILayout.Label("Bookmarked Scenes", EditorStyles.boldLabel);

        bookmarkedScenesScrollPosition = GUILayout.BeginScrollView(bookmarkedScenesScrollPosition, GUILayout.ExpandHeight(true));

        for (int i = 0; i < bookmarkedScenes.Count; i++)
        {
            SceneAsset sceneAsset = bookmarkedScenes[i];
            if (sceneAsset == null) continue;

            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (!string.IsNullOrEmpty(searchQuery) && !sceneName.ToLower().Contains(searchQuery.ToLower()))
            {
                continue;
            }

            EditorGUILayout.BeginHorizontal();
            GUIContent content = new GUIContent(sceneName, EditorGUIUtility.IconContent("d_UnityEditor.GameView").image);
            bool isSelected = (selectedBookmarkIndex == i);
            isSelected = GUILayout.Toggle(isSelected, content, "Button");

            if (isSelected)
            {
                selectedBookmarkIndex = i;
                selectedScene = null;
            }

            EditorGUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    private void DrawScenesInBuild()
    {
        GUILayout.Label("Scenes In Build", EditorStyles.boldLabel);

        scenesInBuildScrollPosition = GUILayout.BeginScrollView(scenesInBuildScrollPosition, GUILayout.ExpandHeight(true));

        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);

            if (!string.IsNullOrEmpty(searchQuery) && !sceneName.ToLower().Contains(searchQuery.ToLower()))
            {
                continue;
            }

            EditorGUILayout.BeginHorizontal();
            GUIContent content = new GUIContent(sceneName, EditorGUIUtility.IconContent("d_UnityEditor.GameView").image);
            bool isSelected = GUILayout.Toggle(selectedScene == sceneName, content, "Button");
            if (isSelected)
            {
                selectedScene = sceneName;
                selectedBookmarkIndex = -1;
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    private void DrawDragAndDropArea()
    {
        GUIStyle dragAndDropStyle = new GUIStyle("box");
        dragAndDropStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        dragAndDropStyle.alignment = TextAnchor.MiddleCenter;
        dragAndDropStyle.fontSize = 10;

        Texture2D backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, new Color(0.25f, 0.25f, 0.25f));
        backgroundTexture.Apply();
        dragAndDropStyle.normal.background = backgroundTexture;

        Texture2D borderTexture = new Texture2D(1, 1);
        borderTexture.SetPixel(0, 0, Color.white);
        borderTexture.Apply();

        dragAndDropStyle.border = new RectOffset(2, 2, 2, 2);
        dragAndDropStyle.padding = new RectOffset(5, 5, 5, 5);
        GUILayout.BeginVertical(dragAndDropStyle);

        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Label(dropArea, "Drag Object in Scene/Scene in Project Here to Bookmark", dragAndDropStyle);

        // HANDLE DRAG AND DROP:
        EventType eventType = Event.current.type;
        if (dropArea.Contains(Event.current.mousePosition))
        {
            if (eventType == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
            }
            else if (eventType == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (Object draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject is SceneAsset sceneAsset)
                    {
                        Debug.Log("Dragged object is a SceneAsset (from Project).");
                        AddSceneToBookmarks(sceneAsset);
                        continue;
                    }

                    if (draggedObject is GameObject go)
                    {
                        Scene scene = go.scene;

                        if (scene.IsValid() && scene.path != null && scene.path != "")
                        {
                            string scenePath = scene.path;
                            SceneAsset draggedSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

                            if (draggedSceneAsset != null)
                            {
                                Debug.Log("Bookmark Scene (from Hierarchy) - Success!");
                                AddSceneToBookmarks(draggedSceneAsset);
                            }
                            else
                            {
                                Debug.LogError("Dragged Scene (from Hierarchy) - Could not load SceneAsset.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Dragged GameObject is not part of a valid, saved scene.  Make sure your scene is saved.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Dragged object is of an unexpected type: {draggedObject.GetType()}");
                    }
                }
                Event.current.Use();
            }
        }

        GUILayout.EndVertical();
    }

    private void AddSceneToBookmarks(SceneAsset sceneAsset)
    {
        if (!bookmarkedScenes.Contains(sceneAsset))
        {
            bookmarkedScenes.Add(sceneAsset);
            SaveBookmarkedScenes();
        }
    }

    private string GetScenePathByName(string sceneName)
    {
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (System.IO.Path.GetFileNameWithoutExtension(scene.path) == sceneName)
            {
                return scene.path;
            }
        }
        return null;
    }

    private void HandleBookmarkButtonClick(string sceneName)
    {
        double currentTime = EditorApplication.timeSinceStartup;
        Debug.Log($"HandleBookmarkButtonClick - sceneName: {sceneName}, currentTime: {currentTime}");
        if (lastClickedScene == sceneName && (currentTime - lastClickTime) < doubleClickThreshold)
        {
            Debug.Log($"Double-click detected for scene: {sceneName}");
            string scenePath = GetScenePathByName(sceneName);
            EditorSceneManager.OpenScene(scenePath);
            lastClickedScene = null;
        }
        else
        {
            lastClickedScene = sceneName;
            lastClickTime = currentTime;
            Debug.Log($"Single click - setting lastClickedScene: {sceneName}, lastClickTime: {lastClickTime}");
        }
    }

    private void LoadBookmarkedScenes()
    {
        bookmarkedScenes.Clear();
        string[] sceneGUIDs = EditorPrefs.GetString(BookmarkedScenesKey, "").Split(';');
        foreach (string guid in sceneGUIDs)
        {
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (sceneAsset != null)
                {
                    bookmarkedScenes.Add(sceneAsset);
                }
            }
        }
    }

    private void SaveBookmarkedScenes()
    {
        List<string> sceneGUIDs = new List<string>();
        foreach (SceneAsset sceneAsset in bookmarkedScenes)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            sceneGUIDs.Add(guid);
        }
        EditorPrefs.SetString(BookmarkedScenesKey, string.Join(";", sceneGUIDs));
    }

    private void RemoveAllBookmarkedScenes()
    {
        Debug.Log("Removing all bookmarked scenes");
        bookmarkedScenes.Clear();
        EditorPrefs.DeleteKey(BookmarkedScenesKey);
        selectedBookmarkIndex = -1;
    }

    private void RemoveBookmarkedScene(int index)
    {
        if (index >= 0 && index < bookmarkedScenes.Count)
        {
            bookmarkedScenes.RemoveAt(index);
            SaveBookmarkedScenes();
            selectedBookmarkIndex = -1;
        }
        else
        {
            Debug.LogError("Invalid bookmark index to remove.");
        }
    }
}