using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public class LiveDebuggerWindow : EditorWindow
{
    #region Data Structures
    private class WatchedNode
    {
        public WeakReference TargetRef { get; }
        public MemberInfo MemberInfo { get; }
        public string DisplayName { get; }
        public int Depth { get; }

        public bool IsExpanded { get; set; }
        public List<WatchedNode> Children { get; private set; } = new List<WatchedNode>();

        private object _lastValue;
        private bool _hasValueChanged;

        private const int MaxExpansionDepth = 8;

        public WatchedNode(object target, MemberInfo memberInfo, string displayName, int depth)
        {
            TargetRef = new WeakReference(target);
            MemberInfo = memberInfo;
            DisplayName = displayName;
            Depth = depth;
        }

        public bool IsTargetAlive() => TargetRef.IsAlive && TargetRef.Target != null;

        public object GetValue()
        {
            if (!IsTargetAlive()) return null;

            var target = TargetRef.Target;
            return MemberInfo switch
            {
                FieldInfo field => field.GetValue(target),
                PropertyInfo property => property.CanRead ? property.GetValue(target) : null,
                _ => null
            };
        }

        public void UpdateValue()
        {
            if (!Application.isPlaying) return;

            var currentValue = GetValue();
            if (!Equals(currentValue, _lastValue))
            {
                _hasValueChanged = true;
                _lastValue = currentValue;
            }
            else
            {
                _hasValueChanged = false;
            }

            foreach (var child in Children)
            {
                child.UpdateValue();
            }
        }

        public bool HasValueChanged() => _hasValueChanged;

        public void Expand()
        {
            if (Depth >= MaxExpansionDepth) return;

            IsExpanded = true;
            Children.Clear();
            var value = GetValue();
            if (value == null) return;

            var type = value.GetType();
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum) return;

            var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.MemberType == MemberTypes.Field || (m.MemberType == MemberTypes.Property && ((PropertyInfo)m).GetIndexParameters().Length == 0))
                .OrderBy(m => m.Name);

            foreach (var member in members)
            {
                Children.Add(new WatchedNode(value, member, member.Name, Depth + 1));
            }
        }

        public void Collapse()
        {
            IsExpanded = false;
            Children.Clear();
        }
    }

    private enum DisplayMode { Search, Browse }
    #endregion

    #region Private Fields
    private DisplayMode _currentMode = DisplayMode.Search;
    private string _searchQuery = "";
    private List<WatchedNode> _watchedRoots = new List<WatchedNode>();
    private Vector2 _scrollPosition;

    // Browse mode fields
    private int _browseSelectedTypeIndex = 0;
    private string[] _sceneComponentTypeNames = new string[0];
    private Type[] _sceneComponentTypes = new Type[0];
    private List<Component> _instancesOfSelectedType = new List<Component>();
    #endregion

    #region Unity Methods
    [MenuItem("Window/Analysis/Live Debugger Pro")]
    public static void ShowWindow()
    {
        GetWindow<LiveDebuggerWindow>("Live Debugger Pro");
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnEditorUpdate()
    {
        if (Application.isPlaying)
        {
            foreach (var root in _watchedRoots)
            {
                root.UpdateValue();
            }
            Repaint();
        }
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            // Clear values but keep watch list for next play session
            ClearAllValues(_watchedRoots);
            Repaint();
        }
    }

    private void OnGUI()
    {
        DrawToolbar();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        switch (_currentMode)
        {
            case DisplayMode.Search:
                DrawSearchUI();
                break;
            case DisplayMode.Browse:
                DrawBrowseUI();
                break;
        }

        EditorGUILayout.Separator();
        DrawWatchedItems();

        EditorGUILayout.EndScrollView();
    }
    #endregion

    #region UI Drawing
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _currentMode = (DisplayMode)GUILayout.Toolbar((int)_currentMode, new[] { "Search by Name", "Browse Scene" }, EditorStyles.toolbarButton);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(new GUIContent("Clear All", EditorGUIUtility.IconContent("d_TreeEditor.Trash").image), EditorStyles.toolbarButton))
        {
            ClearAllWatched();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSearchUI()
    {
        EditorGUILayout.BeginHorizontal();
        _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField);
        if (GUILayout.Button("Watch by Name", GUILayout.Width(120)))
        {
            FindAndWatchByName(_searchQuery);
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawBrowseUI()
    {
        if (GUILayout.Button("Refresh Scene Types"))
        {
            RefreshSceneComponentTypes();
        }

        if (_sceneComponentTypeNames.Length == 0)
        {
            EditorGUILayout.HelpBox("Click 'Refresh Scene Types' to populate.", MessageType.Info);
            return;
        }

        int newIndex = EditorGUILayout.Popup("Component Type", _browseSelectedTypeIndex, _sceneComponentTypeNames);
        if (newIndex != _browseSelectedTypeIndex)
        {
            _browseSelectedTypeIndex = newIndex;
            FindInstancesOfSelectedType();
        }

        if (_instancesOfSelectedType.Any())
        {
            EditorGUILayout.LabelField("Select an object to watch:", EditorStyles.boldLabel);
            foreach (var instance in _instancesOfSelectedType)
            {
                if (instance == null) continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(instance.gameObject, typeof(GameObject), true);
                if (GUILayout.Button(new GUIContent("Watch", "Add this object to the watch list"), GUILayout.Width(60)))
                {
                    AddWatchedComponent(instance);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    private void DrawWatchedItems()
    {
        if (_watchedRoots.Count == 0)
        {
            EditorGUILayout.HelpBox("Use 'Search' or 'Browse' mode to add items to the watch list.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Watch List", EditorStyles.boldLabel);

        // Use RemoveAll for safe removal while iterating
        _watchedRoots.RemoveAll(root => !root.IsTargetAlive());

        foreach (var root in _watchedRoots)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawNode(root);
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawNode(WatchedNode node)
    {
        if (!node.IsTargetAlive()) return;

        EditorGUILayout.BeginHorizontal();

        // Indentation
        GUILayout.Space(node.Depth * 16);

        // Expander
        bool canExpand = CanExpand(node.GetValue());
        if (canExpand)
        {
            bool isCurrentlyExpanded = node.IsExpanded;
            bool newExpandedState = EditorGUILayout.Foldout(isCurrentlyExpanded, "", true);
            if (newExpandedState != isCurrentlyExpanded)
            {
                if (newExpandedState) node.Expand();
                else node.Collapse();
            }
        }
        else
        {
            // Placeholder for alignment
            GUILayout.Space(16);
        }

        // Display Name & Value
        string valueString = GetValueAsString(node.GetValue());
        var style = new GUIStyle(EditorStyles.label) { richText = true };
        if (node.HasValueChanged())
        {
            style.normal.textColor = Color.yellow;
        }

        string displayText = $"<b>{node.DisplayName}:</b> {valueString}";
        EditorGUILayout.LabelField(new GUIContent(displayText), style);

        // 'Select' button for top-level GameObjects
        if (node.Depth == 0 && node.TargetRef.Target is Component comp)
        {
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeGameObject = comp.gameObject;
            }
        }

        EditorGUILayout.EndHorizontal();

        if (node.IsExpanded)
        {
            foreach (var child in node.Children)
            {
                DrawNode(child);
            }
        }
    }
    #endregion

    #region Core Logic
    private void FindAndWatchByName(string memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName)) return;
        ClearAllWatched();

        var components = FindObjectsByType<Component>(FindObjectsSortMode.None);
        foreach (var component in components)
        {
            var members = component.GetType().GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => string.Equals(m.Name, memberName, StringComparison.OrdinalIgnoreCase) &&
                            (m.MemberType == MemberTypes.Field || (m.MemberType == MemberTypes.Property && ((PropertyInfo)m).GetIndexParameters().Length == 0)));

            foreach (var member in members)
            {
                string displayName = $"{component.gameObject.name} -> {component.GetType().Name}";
                _watchedRoots.Add(new WatchedNode(component, member, displayName, 0));
            }
        }
    }

    private void AddWatchedComponent(Component component)
    {
        if (component == null) return;

        // Prevent duplicates
        if (_watchedRoots.Any(r => ReferenceEquals(r.TargetRef.Target, component))) return;

        var type = component.GetType();
        _watchedRoots.Add(new WatchedNode(component, type.GetProperty("enabled"), component.name, 0));
    }

    private void RefreshSceneComponentTypes()
    {
        _sceneComponentTypes = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Select(c => c.GetType())
            .Distinct()
            .OrderBy(t => t.Name)
            .ToArray();
        _sceneComponentTypeNames = _sceneComponentTypes.Select(t => t.Name).ToArray();
        _browseSelectedTypeIndex = 0;
        FindInstancesOfSelectedType();
    }

    private void FindInstancesOfSelectedType()
    {
        _instancesOfSelectedType.Clear();
        if (_sceneComponentTypes.Length > 0)
        {
            var selectedType = _sceneComponentTypes[_browseSelectedTypeIndex];
            _instancesOfSelectedType = FindObjectsByType(selectedType, FindObjectsSortMode.None).Cast<Component>().ToList();
        }
    }

    private void ClearAllWatched()
    {
        _watchedRoots.Clear();
        GUI.FocusControl(null);
    }

    private void ClearAllValues(List<WatchedNode> nodes)
    {
        foreach (var node in nodes)
        {
            node.UpdateValue(); // This will reset the value changed flag
            if (node.Children.Any())
            {
                ClearAllValues(node.Children);
            }
        }
    }
    #endregion

    #region Helpers
    private string GetValueAsString(object value)
    {
        if (value == null) return "<color=grey>null</color>";

        var type = value.GetType();
        if (type.IsPrimitive || type == typeof(string) || type.IsEnum) return value.ToString();
        if (value is Vector3 v3) return v3.ToString("F2");
        if (value is Vector2 v2) return v2.ToString("F2");
        if (value is Quaternion q) return q.eulerAngles.ToString("F1");
        if (value is Color c) return $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";

        if (value is System.Collections.ICollection collection)
        {
            return $"{type.Name} (Count: {collection.Count})";
        }

        return type.Name;
    }

    private bool CanExpand(object value)
    {
        if (value == null) return false;
        var type = value.GetType();
        return !type.IsPrimitive && type != typeof(string) && !type.IsEnum;
    }
    #endregion
}