using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;

namespace CleanCode.EnvironmentTools
{
    public class EnvironmentPlacerWindow : EditorWindow
    {
        private enum ToolMode { Paint, Erase, Single }
        private enum AlignmentMode { AlignToSurface, WorldUp }

        private ToolMode _currentMode = ToolMode.Paint;
        private List<GameObject> _prefabs = new List<GameObject>();
        private Transform _parentContainer;
        private const string DEFAULT_CONTAINER_NAME = "Environment_Container";

        private float _brushRadius = 3.0f;
        private int _density = 5;

        private LayerMask _layerMask = ~0;
        private float _maxSlopeAngle = 45f;

        private AlignmentMode _alignmentMode = AlignmentMode.AlignToSurface;
        private bool _randomizeRotationY = true;
        private bool _lockRotationX = false;
        private bool _lockRotationZ = false;
        private Vector3 _randomRotationMin = Vector3.zero;
        private Vector3 _randomRotationMax = Vector3.zero;
        private Vector2 _scaleRange = new Vector2(0.8f, 1.2f);
        private Vector3 _positionOffset = Vector3.zero;

        private bool _isActive = false;
        private Vector2 _scrollPosition;
        private double _lastEraseTime;

        [MenuItem("Tools/CleanCode/Environment Placer")]
        public static void ShowWindow()
        {
            GetWindow<EnvironmentPlacerWindow>("Env Placer").Show();
        }

        private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
        private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

        private void OnGUI()
        {
            DrawHeader();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawToolModeSelection();
            EditorGUILayout.Space();
            DrawBrushSettings();
            EditorGUILayout.Space();
            DrawTransformSettings();
            EditorGUILayout.Space();
            DrawFilters();
            EditorGUILayout.Space();
            DrawPrefabManagement();
            EditorGUILayout.Space();
            DrawShortcutsInfo();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            GUI.backgroundColor = _isActive ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button(_isActive ? "TOOL ACTIVE (ESC to Stop)" : "ACTIVATE TOOL", GUILayout.Height(35)))
            {
                _isActive = !_isActive;
                if (_isActive) FocusSceneView();
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawToolModeSelection()
        {
            EditorGUILayout.LabelField("Tool Mode", EditorStyles.boldLabel);
            _currentMode = (ToolMode)GUILayout.Toolbar((int)_currentMode, new string[] { "Paint Brush", "Eraser", "Single Place" });
        }

        private void DrawBrushSettings()
        {
            EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            _brushRadius = EditorGUILayout.Slider("Brush Radius", _brushRadius, 0.1f, 20f);
            if (_currentMode == ToolMode.Paint)
                _density = EditorGUILayout.IntSlider("Density", _density, 1, 50);
            EditorGUI.indentLevel--;
        }

        private void DrawTransformSettings()
        {
            if (_currentMode == ToolMode.Erase) return;

            EditorGUILayout.LabelField("Transform & Alignment", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            _parentContainer = (Transform)EditorGUILayout.ObjectField("Parent Container", _parentContainer, typeof(Transform), true);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Alignment", EditorStyles.miniBoldLabel);
            _alignmentMode = (AlignmentMode)EditorGUILayout.EnumPopup("Base Orientation", _alignmentMode);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Lock Axis:", GUILayout.Width(150));
            _lockRotationX = EditorGUILayout.ToggleLeft("Lock X", _lockRotationX, GUILayout.Width(70));
            _lockRotationZ = EditorGUILayout.ToggleLeft("Lock Z", _lockRotationZ, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            _randomizeRotationY = EditorGUILayout.Toggle("Randomize Yaw (Y)", _randomizeRotationY);

            Vector3 rotMin = EditorGUILayout.Vector3Field("Min Rot Offset", _randomRotationMin);
            Vector3 rotMax = EditorGUILayout.Vector3Field("Max Rot Offset", _randomRotationMax);
            _randomRotationMin = rotMin;
            _randomRotationMax = rotMax;

            float minScale = _scaleRange.x;
            float maxScale = _scaleRange.y;
            EditorGUILayout.MinMaxSlider("Scale Variation", ref minScale, ref maxScale, 0.1f, 5f);
            _scaleRange = new Vector2(minScale, maxScale);
            EditorGUILayout.LabelField($"Scale: {minScale:F2} - {maxScale:F2}");
            _positionOffset = EditorGUILayout.Vector3Field("Position Offset", _positionOffset);
            EditorGUI.indentLevel--;
        }

        private void DrawFilters()
        {
            EditorGUILayout.LabelField("Placement Filters", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            LayerMask tempMask = EditorGUILayout.MaskField("Hit Layer Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(_layerMask), InternalEditorUtility.layers);
            _layerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
            _maxSlopeAngle = EditorGUILayout.Slider("Max Slope Angle", _maxSlopeAngle, 0f, 90f);
            EditorGUI.indentLevel--;
        }

        private void DrawPrefabManagement()
        {
            EditorGUILayout.LabelField("Prefabs Palette", EditorStyles.boldLabel);
            DrawDragDropArea();
            DrawPrefabList();
        }

        private void DrawDragDropArea()
        {
            Event evt = Event.current;
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop Prefabs Here", EditorStyles.helpBox);

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (!dropArea.Contains(evt.mousePosition)) return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is GameObject go && !_prefabs.Contains(go))
                            _prefabs.Add(go);
                    }
                }
            }
        }

        private void DrawPrefabList()
        {
            if (_prefabs.Count == 0) return;
            if (GUILayout.Button("Clear List")) _prefabs.Clear();

            for (int i = 0; i < _prefabs.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _prefabs[i] = (GameObject)EditorGUILayout.ObjectField(_prefabs[i], typeof(GameObject), false);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    _prefabs.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawShortcutsInfo()
        {
            EditorGUILayout.HelpBox("[ ] : Size | Shift : Erase | Ctrl+Scroll : Density", MessageType.Info);
        }

        private void FocusSceneView()
        {
            if (SceneView.sceneViews.Count > 0) ((SceneView)SceneView.sceneViews[0]).Focus();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isActive) return;

            HandleInput();

            Event e = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            bool isEraser = _currentMode == ToolMode.Erase || (_currentMode == ToolMode.Paint && e.shift);
            LayerMask castMask = isEraser ? ~0 : _layerMask;

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, castMask))
            {
                DrawBrushVisuals(hit.point, hit.normal, isEraser);

                if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0 && !e.alt)
                {
                    ExecuteTool(hit, isEraser);
                    e.Use();
                }
            }

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag) sceneView.Repaint();
        }

        private void HandleInput()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape) { _isActive = false; Repaint(); }
                if (e.keyCode == KeyCode.RightBracket) { _brushRadius += 0.5f; Repaint(); }
                if (e.keyCode == KeyCode.LeftBracket) { _brushRadius = Mathf.Max(0.1f, _brushRadius - 0.5f); Repaint(); }
            }
            if (e.type == EventType.ScrollWheel && e.control)
            {
                _density = Mathf.Clamp(_density - (int)e.delta.y, 1, 50);
                e.Use(); Repaint();
            }
        }

        private void DrawBrushVisuals(Vector3 point, Vector3 normal, bool isEraser)
        {
            Handles.color = isEraser ? new Color(1, 0, 0, 0.5f) : new Color(0, 1, 0, 0.5f);
            Handles.DrawWireDisc(point, normal, _brushRadius);
            Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.1f);
            Handles.DrawSolidDisc(point, normal, _brushRadius);
            if (!isEraser) Handles.ArrowHandleCap(0, point, Quaternion.LookRotation(normal), 1f, EventType.Repaint);
        }

        private void ExecuteTool(RaycastHit hit, bool isEraser)
        {
            if (isEraser) PerformEraseSimple(hit.point);
            else if (_currentMode == ToolMode.Paint) PerformPaint(hit);
            else if (_currentMode == ToolMode.Single && Event.current.type == EventType.MouseDown) SpawnSingle(hit);
        }

        private Transform GetContainer()
        {
            if (_parentContainer != null) return _parentContainer;
            GameObject found = GameObject.Find(DEFAULT_CONTAINER_NAME);
            if (found == null)
            {
                found = new GameObject(DEFAULT_CONTAINER_NAME);
                Undo.RegisterCreatedObjectUndo(found, "Create Container");
            }
            _parentContainer = found.transform;
            return _parentContainer;
        }

        private void PerformEraseSimple(Vector3 brushCenter)
        {
            if (EditorApplication.timeSinceStartup - _lastEraseTime < 0.1f) return;
            _lastEraseTime = EditorApplication.timeSinceStartup;

            Transform container = GetContainer();
            if (container.childCount == 0) return;

            float radiusSq = _brushRadius * _brushRadius;
            List<GameObject> toDelete = new List<GameObject>();

            for (int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);

                if (Vector3.SqrMagnitude(child.position - brushCenter) <= radiusSq)
                {
                    if (IsMatchingActivePrefab(child.gameObject))
                    {
                        toDelete.Add(child.gameObject);
                    }
                }
            }

            foreach (GameObject obj in toDelete)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }

        private bool IsMatchingActivePrefab(GameObject instance)
        {
            if (_prefabs == null || _prefabs.Count == 0) return true;

            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            if (source == null) return false;

            return _prefabs.Contains(source);
        }

        private void PerformPaint(RaycastHit centerHit)
        {
            if (_prefabs.Count == 0) return;
            Transform container = GetContainer();

            for (int i = 0; i < _density; i++)
            {
                Vector2 rnd = UnityEngine.Random.insideUnitCircle * _brushRadius;
                Vector3 origin = centerHit.point + new Vector3(rnd.x, 0, rnd.y) + Vector3.up * 50f;

                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100f, _layerMask))
                {
                    if (Vector3.Distance(hit.point, centerHit.point) <= _brushRadius)
                        SpawnObject(hit, container);
                }
            }
        }

        private void SpawnSingle(RaycastHit hit)
        {
            if (_prefabs.Count == 0) return;
            SpawnObject(hit, GetContainer());
        }

        private void SpawnObject(RaycastHit hit, Transform container)
        {
            if (Vector3.Angle(Vector3.up, hit.normal) > _maxSlopeAngle) return;

            GameObject prefab = _prefabs[UnityEngine.Random.Range(0, _prefabs.Count)];
            if (prefab == null) return;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Place Object");

            instance.transform.position = hit.point + _positionOffset;

            Quaternion orient = (_alignmentMode == AlignmentMode.AlignToSurface)
                ? Quaternion.FromToRotation(Vector3.up, hit.normal)
                : Quaternion.identity;

            if (_randomizeRotationY) orient *= Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0);

            orient *= Quaternion.Euler(
                UnityEngine.Random.Range(_randomRotationMin.x, _randomRotationMax.x),
                UnityEngine.Random.Range(_randomRotationMin.y, _randomRotationMax.y),
                UnityEngine.Random.Range(_randomRotationMin.z, _randomRotationMax.z)
            );

            Vector3 euler = orient.eulerAngles;
            if (_lockRotationX) euler.x = 0;
            if (_lockRotationZ) euler.z = 0;
            instance.transform.rotation = Quaternion.Euler(euler);

            float scale = UnityEngine.Random.Range(_scaleRange.x, _scaleRange.y);
            instance.transform.localScale = Vector3.one * scale;

            instance.transform.SetParent(container);
        }
    }
}