// LiveDebuggerWindow.cs
// Đặt file này trong thư mục "Editor"

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEditor.Search;

public class LiveDebuggerWindow : EditorWindow
{
    private const string SessionDataPath = "ProjectSettings/LiveDebuggerSessions.json";

    private VisualTreeAsset inspectorTemplate;
    private ScrollView monitorContainer;
    private Button saveButton;
    private Button deleteButton;
    private TextField sessionNameField;
    private DropdownField sessionDropdown;

    private readonly Dictionary<GameObject, VisualElement> targetInspectors = new Dictionary<GameObject, VisualElement>();
    private readonly Dictionary<GameObject, MonoScriptCache> scriptCaches = new Dictionary<GameObject, MonoScriptCache>();
    private SavedSessionData savedSessionData;

    [MenuItem("Tools/Live Debugger (UI Toolkit)")]
    public static void ShowWindow()
    {
        var window = GetWindow<LiveDebuggerWindow>();
        window.titleContent = new GUIContent("Live Debugger");
    }

    public void CreateGUI()
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Shmackle/Scripts/Utils/Bill/Editor/LiveDebuggerWindow.uxml");
        visualTree.CloneTree(rootVisualElement);

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/_Shmackle/Scripts/Utils/Bill/Editor/LiveDebuggerWindow.uss");
        rootVisualElement.styleSheets.Add(styleSheet);

        inspectorTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Shmackle/Scripts/Utils/Bill/Editor/ObjectInspectorTemplate.uxml");

        InitializeUI();
        RegisterCallbacks();
        LoadSessionData();
        PopulateSessionDropdown();
    }

    private void OnEnable() => EditorApplication.update += UpdateLiveValues;
    private void OnDisable() => EditorApplication.update -= UpdateLiveValues;

    private void InitializeUI()
    {
        monitorContainer = rootVisualElement.Q<ScrollView>("monitor-container");
        saveButton = rootVisualElement.Q<Button>("save-button");
        deleteButton = rootVisualElement.Q<Button>("delete-button");
        sessionNameField = rootVisualElement.Q<TextField>("session-name-field");
        sessionDropdown = rootVisualElement.Q<DropdownField>("session-dropdown");

        var dropArea = rootVisualElement.Q<VisualElement>("drop-area");
        dropArea.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
        dropArea.RegisterCallback<DragPerformEvent>(OnDragPerform);
    }

    private void RegisterCallbacks()
    {
        saveButton.clicked += SaveCurrentSession;
        deleteButton.clicked += DeleteSelectedSession;
        sessionDropdown.RegisterValueChangedCallback(evt => LoadSession(evt.newValue));
        rootVisualElement.Q<Button>("clear-button").clicked += ClearAllTargets;
    }

    private void AddTarget(GameObject target)
    {
        if (target == null || targetInspectors.ContainsKey(target)) return;

        var inspectorInstance = inspectorTemplate.Instantiate();
        monitorContainer.Add(inspectorInstance);

        var objectField = inspectorInstance.Q<ObjectField>();
        objectField.value = target;

        var scriptCache = new MonoScriptCache(target);
        scriptCaches[target] = scriptCache;
        targetInspectors[target] = inspectorInstance;

        var scriptSelector = inspectorInstance.Q<DropdownField>("script-selector");
        scriptSelector.choices = scriptCache.GetScriptNames().ToList();
        scriptSelector.RegisterValueChangedCallback(evt => OnScriptSelected(target, scriptSelector.index));

        inspectorInstance.Q<Button>("remove-button").clicked += () => RemoveTarget(target);

        if (scriptCache.HasScripts())
        {
            scriptSelector.index = 0;
            OnScriptSelected(target, 0);
        }
    }

    private void OnScriptSelected(GameObject target, int scriptIndex)
    {
        var cache = scriptCaches[target];
        cache.SelectScript(scriptIndex);

        var inspector = targetInspectors[target];
        var fieldsContainer = inspector.Q<VisualElement>("fields-container");
        fieldsContainer.Clear();

        foreach (var field in cache.AvailableFields)
        {
            var toggle = new Toggle(field.Name) { name = field.Name };
            toggle.RegisterValueChangedCallback(evt => cache.SetFieldMonitorState(field.Name, evt.newValue));
            fieldsContainer.Add(toggle);
        }
    }

    private void RemoveTarget(GameObject target)
    {
        if (!targetInspectors.ContainsKey(target)) return;

        monitorContainer.Remove(targetInspectors[target]);
        targetInspectors.Remove(target);
        scriptCaches.Remove(target);
    }

    private void ClearAllTargets()
    {
        foreach (var target in targetInspectors.Keys.ToList())
        {
            RemoveTarget(target);
        }
    }

    private void UpdateLiveValues()
    {
        if (!Application.isPlaying) return;

        foreach (var pair in targetInspectors)
        {
            var target = pair.Key;
            var inspector = pair.Value;
            var cache = scriptCaches[target];
            var valuesContainer = inspector.Q<VisualElement>("values-container");
            valuesContainer.Clear();

            if (!cache.HasSelectedScript()) continue;

            foreach (var field in cache.GetMonitoredFields())
            {
                object value = field.GetValue(cache.GetSelectedScript());
                string displayValue = (value == null) ? "null" : value.ToString();
                var label = new Label($"{field.Name}: {displayValue}");
                valuesContainer.Add(label);
            }
        }
    }

    #region Drag and Drop
    private void OnDragUpdate(DragUpdatedEvent evt)
    {
        if (DragAndDrop.GetGenericData("DragSelection") is List<GameObject>)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }
    }

    private void OnDragPerform(DragPerformEvent evt)
    {
        var draggedObjects = DragAndDrop.objectReferences.OfType<GameObject>();
        foreach (var go in draggedObjects)
        {
            AddTarget(go);
        }
        DragAndDrop.AcceptDrag();
    }
    #endregion

    #region Session Management
    private void SaveCurrentSession()
    {
        string sessionName = sessionNameField.value;
        if (string.IsNullOrWhiteSpace(sessionName))
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng nhập tên cho session.", "OK");
            return;
        }

        var session = new DebuggerSession
        {
            sessionName = sessionName,
            monitoredObjects = targetInspectors.Keys.Select(go =>
            {
                var cache = scriptCaches[go];
                return new MonitoredObjectData
                {
                    gameObjectPath = GetGameObjectPath(go),
                    scriptName = cache.GetSelectedScriptName(),
                    monitoredFieldNames = cache.GetMonitoredFields().Select(f => f.Name).ToList()
                };
            }).ToList()
        };

        var existingSession = savedSessionData.sessions.FirstOrDefault(s => s.sessionName == sessionName);
        if (existingSession != null)
        {
            savedSessionData.sessions.Remove(existingSession);
        }
        savedSessionData.sessions.Add(session);

        File.WriteAllText(SessionDataPath, JsonUtility.ToJson(savedSessionData, true));
        PopulateSessionDropdown();
        sessionDropdown.value = sessionName;
    }

    private void LoadSession(string sessionName)
    {
        var session = savedSessionData.sessions.FirstOrDefault(s => s.sessionName == sessionName);
        if (session == null) return;

        sessionNameField.value = sessionName;
        ClearAllTargets();

        foreach (var data in session.monitoredObjects)
        {
            GameObject target = FindGameObjectByPath(data.gameObjectPath);
            if (target == null) continue;

            AddTarget(target);
            var cache = scriptCaches[target];
            int scriptIndex = Array.IndexOf(cache.GetScriptNames(), data.scriptName);

            if (scriptIndex != -1)
            {
                var inspector = targetInspectors[target];
                inspector.Q<DropdownField>("script-selector").index = scriptIndex;
                OnScriptSelected(target, scriptIndex);

                foreach (var fieldName in data.monitoredFieldNames)
                {
                    var toggle = inspector.Q<Toggle>(fieldName);
                    if (toggle != null)
                    {
                        toggle.value = true;
                    }
                }
            }
        }
    }

    private void DeleteSelectedSession()
    {
        string sessionName = sessionDropdown.value;
        if (string.IsNullOrWhiteSpace(sessionName)) return;

        var sessionToRemove = savedSessionData.sessions.FirstOrDefault(s => s.sessionName == sessionName);
        if (sessionToRemove != null)
        {
            savedSessionData.sessions.Remove(sessionToRemove);
            File.WriteAllText(SessionDataPath, JsonUtility.ToJson(savedSessionData, true));
            PopulateSessionDropdown();
            sessionNameField.value = "";
        }
    }

    private void LoadSessionData()
    {
        if (File.Exists(SessionDataPath))
        {
            string json = File.ReadAllText(SessionDataPath);
            savedSessionData = JsonUtility.FromJson<SavedSessionData>(json) ?? new SavedSessionData();
        }
        else
        {
            savedSessionData = new SavedSessionData();
        }
    }

    private void PopulateSessionDropdown()
    {
        sessionDropdown.choices = savedSessionData.sessions.Select(s => s.sessionName).ToList();
    }
    #endregion

    #region Utility
    private string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }

    private GameObject FindGameObjectByPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var rootObjects = scene.GetRootGameObjects();

        GameObject current = rootObjects.FirstOrDefault(go => go.name == parts[0]);
        if (current == null) return null;

        for (int i = 1; i < parts.Length; i++)
        {
            var child = current.transform.Find(parts[i]);
            if (child == null) return null;
            current = child.gameObject;
        }
        return current;
    }
    #endregion
}

#region Data Models & Cache
[Serializable]
public class MonitoredObjectData
{
    public string gameObjectPath;
    public string scriptName;
    public List<string> monitoredFieldNames = new List<string>();
}

[Serializable]
public class DebuggerSession
{
    public string sessionName;
    public List<MonitoredObjectData> monitoredObjects = new List<MonitoredObjectData>();
}

[Serializable]
public class SavedSessionData
{
    public List<DebuggerSession> sessions = new List<DebuggerSession>();
}

public class MonoScriptCache
{
    private readonly MonoBehaviour[] attachedScripts;
    private readonly string[] scriptNames;

    public FieldInfo[] AvailableFields { get; private set; } = Array.Empty<FieldInfo>();
    public int SelectedScriptIndex { get; private set; } = -1;

    private readonly Dictionary<string, bool> monitoredFieldsState = new Dictionary<string, bool>();

    public MonoScriptCache(GameObject target)
    {
        attachedScripts = target.GetComponents<MonoBehaviour>();
        scriptNames = attachedScripts.Select(s => s.GetType().Name).ToArray();
    }

    public bool HasScripts() => attachedScripts.Length > 0;
    public bool HasSelectedScript() => SelectedScriptIndex >= 0 && SelectedScriptIndex < attachedScripts.Length;
    public string[] GetScriptNames() => scriptNames;
    public MonoBehaviour GetSelectedScript() => HasSelectedScript() ? attachedScripts[SelectedScriptIndex] : null;
    public string GetSelectedScriptName() => HasSelectedScript() ? GetSelectedScript().GetType().Name : string.Empty;

    public void SelectScript(int index)
    {
        SelectedScriptIndex = index;
        monitoredFieldsState.Clear();

        if (HasSelectedScript())
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            AvailableFields = GetSelectedScript().GetType().GetFields(flags);
        }
        else
        {
            AvailableFields = Array.Empty<FieldInfo>();
        }
    }

    public void SetFieldMonitorState(string fieldName, bool state) => monitoredFieldsState[fieldName] = state;
    public IEnumerable<FieldInfo> GetMonitoredFields() => AvailableFields.Where(f => monitoredFieldsState.ContainsKey(f.Name) && monitoredFieldsState[f.Name]);
}
#endregion