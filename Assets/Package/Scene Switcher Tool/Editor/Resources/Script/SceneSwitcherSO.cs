using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;

[CreateAssetMenu(fileName = "SceneSwitcher", menuName = "Tools/Scene Switcher")]
public class SceneSwitcherSO : ScriptableObject
{
    public List<SceneAsset> bookmarkedScenes = new List<SceneAsset>();
}

[CustomEditor(typeof(SceneSwitcherSO))]
public class SceneSwitcherEditor : Editor
{
    private string searchQuery = "";
    private Vector2 bookmarkedScenesScrollPosition;
    private SceneAsset selectedScene;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SceneSwitcherSO sceneSwitcher = (SceneSwitcherSO)target;

        EditorGUILayout.HelpBox("This asset stores and manages bookmarked scenes.", MessageType.Info);

        GUILayout.Label("Search Bookmarked Scenes", EditorStyles.boldLabel);
        searchQuery = EditorGUILayout.TextField(searchQuery);

        GUILayout.Space(10);
        DrawBookmarkedScenes(sceneSwitcher);

        GUILayout.Space(10);
        DrawDragAndDropArea(sceneSwitcher);

        GUILayout.Space(10);
        DrawButtons(sceneSwitcher);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBookmarkedScenes(SceneSwitcherSO sceneSwitcher)
    {
        GUILayout.Label("Bookmarked Scenes", EditorStyles.boldLabel);

        bookmarkedScenesScrollPosition = GUILayout.BeginScrollView(bookmarkedScenesScrollPosition, GUILayout.Height(200));

        IEnumerable<SceneAsset> filteredScenes = sceneSwitcher.bookmarkedScenes
            .Where(scene => scene != null && AssetDatabase.GetAssetPath(scene).ToLower().Contains(searchQuery.ToLower()));

        foreach (SceneAsset sceneAsset in filteredScenes)
        {
            if (sceneAsset == null) continue;

            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            EditorGUILayout.BeginHorizontal();

            GUIStyle buttonStyle = new GUIStyle("Button");
            if (sceneAsset == selectedScene)
            {
                buttonStyle.normal.textColor = Color.yellow;
                buttonStyle.fontStyle = FontStyle.Bold;
            }

            if (GUILayout.Button(sceneName, buttonStyle))
            {
                selectedScene = sceneAsset;
            }

            EditorGUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    private void DrawDragAndDropArea(SceneSwitcherSO sceneSwitcher)
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
        GUI.Label(dropArea, "Drag Scene Assets or GameObjects (with scenes) here to bookmark", dragAndDropStyle);

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

                SerializedProperty bookmarkedScenesProperty = serializedObject.FindProperty("bookmarkedScenes");

                foreach (Object draggedObject in DragAndDrop.objectReferences)
                {
                    SceneAsset sceneAsset = null;

                    if (draggedObject is SceneAsset)
                    {
                        sceneAsset = (SceneAsset)draggedObject;
                    }
                    else if (draggedObject is GameObject go)
                    {
                        Scene scene = go.scene;
                        if (scene.IsValid() && !string.IsNullOrEmpty(scene.path))
                        {
                            sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                        }
                    }

                    if (sceneAsset != null)
                    {
                        if (!IsSceneAlreadyBookmarked(sceneAsset, bookmarkedScenesProperty))
                        {
                            AddSceneToBookmarks(sceneAsset, bookmarkedScenesProperty);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Dragged object is not a SceneAsset or GameObject with a valid scene.");
                    }
                }
                Event.current.Use();
            }
        }

        GUILayout.EndVertical();
    }

    private bool IsSceneAlreadyBookmarked(SceneAsset sceneAsset, SerializedProperty bookmarkedScenesProperty)
    {
        for (int i = 0; i < bookmarkedScenesProperty.arraySize; ++i)
        {
            if (bookmarkedScenesProperty.GetArrayElementAtIndex(i).objectReferenceValue == sceneAsset)
            {
                return true;
            }
        }
        return false;
    }

    private void AddSceneToBookmarks(SceneAsset sceneAsset, SerializedProperty bookmarkedScenesProperty)
    {
        int index = bookmarkedScenesProperty.arraySize;
        bookmarkedScenesProperty.InsertArrayElementAtIndex(index);
        SerializedProperty element = bookmarkedScenesProperty.GetArrayElementAtIndex(index);
        element.objectReferenceValue = sceneAsset;
    }

    private void DrawButtons(SceneSwitcherSO sceneSwitcher)
    {
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = selectedScene != null;

        if (GUILayout.Button(new GUIContent("Load", EditorGUIUtility.IconContent("d_Refresh").image), GUILayout.Height(30)))
        {
            if (selectedScene != null)
            {
                string scenePath = AssetDatabase.GetAssetPath(selectedScene);
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        if (GUILayout.Button(new GUIContent("Additive", EditorGUIUtility.IconContent("d_Toolbar Plus More").image), GUILayout.Height(30)))
        {
            if (selectedScene != null)
            {
                string scenePath = AssetDatabase.GetAssetPath(selectedScene);
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }
        }

        EditorGUILayout.EndHorizontal();

        GUI.enabled = sceneSwitcher.bookmarkedScenes.Contains(selectedScene) && selectedScene != null;
        if (GUILayout.Button(new GUIContent("Remove Bookmark", EditorGUIUtility.IconContent("Toolbar Minus").image), GUILayout.Height(30)))
        {
            RemoveBookmark(sceneSwitcher, selectedScene);
        }
        GUI.enabled = true;

        if (GUILayout.Button(new GUIContent("Remove All Bookmarks", EditorGUIUtility.IconContent("d_CacheServerDisconnected@2x").image), GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "Clear Bookmarks",
                "Are you sure you want to clear all bookmarked scenes?",
                "Yes",
                "Cancel"))
            {
                SerializedProperty bookmarkedScenesProperty = serializedObject.FindProperty("bookmarkedScenes");
                bookmarkedScenesProperty.ClearArray();
                selectedScene = null;
            }
        }
    }

    private void RemoveBookmark(SceneSwitcherSO sceneSwitcher, SceneAsset sceneToRemove)
    {
        SerializedProperty bookmarkedScenesProperty = serializedObject.FindProperty("bookmarkedScenes");
        for (int i = 0; i < bookmarkedScenesProperty.arraySize; ++i)
        {
            if (bookmarkedScenesProperty.GetArrayElementAtIndex(i).objectReferenceValue == sceneToRemove)
            {
                Debug.Log("Deleted: " + bookmarkedScenesProperty.GetArrayElementAtIndex(i).objectReferenceValue.name);
                bookmarkedScenesProperty.DeleteArrayElementAtIndex(i); // required to fix bug in Unity
                selectedScene = null;
                return;
            }
        }
    }
}

[InitializeOnLoad]
public class SceneSwitcherToolbar
{
    static SceneSwitcherToolbar()
    {
        ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
    }

    static void OnToolbarGUI()
    {

        if (GUILayout.Button(new GUIContent("Scenes Switcher", "Scene Switcher Menu")))
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Edit Bookmarks"), false, OpenSceneSwitcherAsset);
            menu.AddSeparator("");

            SceneSwitcherSO sceneSwitcher = LoadSceneSwitcherAsset();

            if (sceneSwitcher != null)
            {
                foreach (SceneAsset sceneAsset in sceneSwitcher.bookmarkedScenes)
                {
                    if (sceneAsset != null)
                    {
                        string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                        menu.AddItem(new GUIContent(sceneName), false, OpenScene, sceneAsset);
                    }
                }
            }
            else
            {
                menu.AddItem(new GUIContent("No Scene Switcher Asset Found!"), false, () => { Debug.LogWarning("No Scene Switcher ScriptableObject found.  Create one via Assets/Create/Tools/Scene Switcher."); });
            }

            menu.ShowAsContext();
        }

        GUILayout.FlexibleSpace();

    }

    static void OpenScene(object sceneAssetObj)
    {
        SceneAsset sceneAsset = sceneAssetObj as SceneAsset;
        if (sceneAsset != null)
        {
            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            EditorSceneManager.OpenScene(scenePath);
        }
        else
        {
            Debug.LogError("Invalid SceneAsset passed to OpenScene.");
        }
    }

    static SceneSwitcherSO LoadSceneSwitcherAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:SceneSwitcherSO");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            SceneSwitcherSO sceneSwitcher = AssetDatabase.LoadAssetAtPath<SceneSwitcherSO>(path);
            return sceneSwitcher;
        }
        else
        {
            return null;
        }
    }


    static void OpenSceneSwitcherAsset()
    {
        SceneSwitcherSO sceneSwitcher = LoadSceneSwitcherAsset();

        if (sceneSwitcher != null)
        {
            Selection.activeObject = sceneSwitcher;
            EditorGUIUtility.PingObject(sceneSwitcher);
        }
        else
        {
            Debug.LogWarning("No Scene Switcher ScriptableObject found. Create one via Assets/Create/Tools/Scene Switcher.");
        }
    }
}