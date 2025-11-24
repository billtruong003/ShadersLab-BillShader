using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
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

        // Brush Settings
        private float _brushRadius = 3.0f;
        private int _density = 5;

        // Filters
        private LayerMask _layerMask = ~0;
        private float _maxSlopeAngle = 45f;

        // Alignment & Transforms
        private AlignmentMode _alignmentMode = AlignmentMode.AlignToSurface;
        private bool _randomizeRotationY = true;
        private bool _lockRotationX = false;
        private bool _lockRotationZ = false;
        private Vector3 _randomRotationMin = Vector3.zero;
        private Vector3 _randomRotationMax = Vector3.zero;
        private Vector2 _scaleRange = new Vector2(0.8f, 1.2f);
        private Vector3 _positionOffset = Vector3.zero;

        // Internal State
        private bool _isActive = false;
        private Vector2 _scrollPosition;
        private double _lastEraseTime;

        [MenuItem("Tools/CleanCode/Environment Placer")]
        public static void ShowWindow()
        {
            EnvironmentPlacerWindow window = GetWindow<EnvironmentPlacerWindow>("Env Placer");
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

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
            {
                _density = EditorGUILayout.IntSlider("Density / Flow", _density, 1, 50);
            }

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
            EditorGUILayout.LabelField("Lock Axis (Fix Vertical):", GUILayout.Width(150));
            _lockRotationX = EditorGUILayout.ToggleLeft("Lock X", _lockRotationX, GUILayout.Width(70));
            _lockRotationZ = EditorGUILayout.ToggleLeft("Lock Z", _lockRotationZ, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Randomization", EditorStyles.miniBoldLabel);
            _randomizeRotationY = EditorGUILayout.Toggle("Randomize Yaw (Y)", _randomizeRotationY);

            Vector3 rotMin = _randomRotationMin;
            Vector3 rotMax = _randomRotationMax;
            EditorGUILayout.BeginHorizontal();
            rotMin = EditorGUILayout.Vector3Field("Min Rot Offset", rotMin);
            rotMax = EditorGUILayout.Vector3Field("Max Rot Offset", rotMax);
            EditorGUILayout.EndHorizontal();
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

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition)) return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject is GameObject go && !_prefabs.Contains(go))
                            {
                                _prefabs.Add(go);
                            }
                        }
                    }
                    break;
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
            EditorGUILayout.HelpBox(
                "[ ] : Adjust Brush Size\n" +
                "Shift : Hold to Erase (Paint Mode)\n" +
                "Ctrl + Scroll : Adjust Density",
                MessageType.Info);
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
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _layerMask))
            {
                DrawBrushVisuals(hit.point, hit.normal);

                if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0 && !e.alt)
                {
                    ExecuteTool(hit);
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
                if (e.keyCode == KeyCode.Escape)
                {
                    _isActive = false;
                    Repaint();
                }
                if (e.keyCode == KeyCode.RightBracket)
                {
                    _brushRadius += 0.5f;
                    Repaint();
                }
                if (e.keyCode == KeyCode.LeftBracket)
                {
                    _brushRadius = Mathf.Max(0.1f, _brushRadius - 0.5f);
                    Repaint();
                }
            }

            if (e.type == EventType.ScrollWheel && e.control)
            {
                _density -= (int)e.delta.y;
                _density = Mathf.Clamp(_density, 1, 50);
                e.Use();
                Repaint();
            }
        }

        private void DrawBrushVisuals(Vector3 point, Vector3 normal)
        {
            Color brushColor = (_currentMode == ToolMode.Erase || Event.current.shift) ? Color.red : Color.green;
            brushColor.a = 0.5f;
            Handles.color = brushColor;

            Handles.DrawWireDisc(point, normal, _brushRadius);

            Color solidColor = brushColor;
            solidColor.a = 0.1f;
            Handles.DrawSolidDisc(point, normal, _brushRadius);

            Handles.ArrowHandleCap(0, point, Quaternion.LookRotation(normal), 1f, EventType.Repaint);
        }

        private void ExecuteTool(RaycastHit hit)
        {
            bool isErasing = _currentMode == ToolMode.Erase || (_currentMode == ToolMode.Paint && Event.current.shift);

            if (isErasing)
            {
                PerformErase(hit.point);
            }
            else if (_currentMode == ToolMode.Paint)
            {
                PerformPaint(hit);
            }
            else if (_currentMode == ToolMode.Single && Event.current.type == EventType.MouseDown)
            {
                SpawnSingle(hit);
            }
        }

        private void PerformPaint(RaycastHit centerHit)
        {
            if (_prefabs.Count == 0) return;

            for (int i = 0; i < _density; i++)
            {
                Vector2 randomPoint = UnityEngine.Random.insideUnitCircle * _brushRadius;
                Vector3 origin = centerHit.point + new Vector3(randomPoint.x, 0, randomPoint.y) + Vector3.up * 20f;

                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 50f, _layerMask))
                {
                    float distanceToCenter = Vector3.Distance(new Vector3(hit.point.x, 0, hit.point.z), new Vector3(centerHit.point.x, 0, centerHit.point.z));
                    if (distanceToCenter <= _brushRadius)
                    {
                        SpawnObject(hit);
                    }
                }
            }
        }

        private void SpawnSingle(RaycastHit hit)
        {
            if (_prefabs.Count == 0) return;
            SpawnObject(hit);
        }

        private void SpawnObject(RaycastHit hit)
        {
            if (Vector3.Angle(Vector3.up, hit.normal) > _maxSlopeAngle) return;

            GameObject prefab = _prefabs[UnityEngine.Random.Range(0, _prefabs.Count)];
            if (prefab == null) return;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Place Environment Object");

            // 1. Position
            Vector3 finalPosition = hit.point + _positionOffset;
            instance.transform.position = finalPosition;

            // 2. Base Alignment
            Quaternion orientation = Quaternion.identity;
            if (_alignmentMode == AlignmentMode.AlignToSurface)
            {
                orientation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            else
            {
                orientation = Quaternion.identity; // World Up
            }

            // 3. Random Y Rotation
            if (_randomizeRotationY)
            {
                orientation *= Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0);
            }

            // 4. Random Offset Rotation (Tilt)
            orientation *= Quaternion.Euler(
                UnityEngine.Random.Range(_randomRotationMin.x, _randomRotationMax.x),
                UnityEngine.Random.Range(_randomRotationMin.y, _randomRotationMax.y),
                UnityEngine.Random.Range(_randomRotationMin.z, _randomRotationMax.z)
            );

            // 5. Apply Lock Axis (Fix Alignment)
            // This ensures grass stands straight up even if aligned to slope originally, 
            // or simply prevents unwanted tilting.
            Vector3 finalEuler = orientation.eulerAngles;
            if (_lockRotationX) finalEuler.x = 0;
            if (_lockRotationZ) finalEuler.z = 0;

            instance.transform.rotation = Quaternion.Euler(finalEuler);

            // 6. Scale
            float scale = UnityEngine.Random.Range(_scaleRange.x, _scaleRange.y);
            instance.transform.localScale = Vector3.one * scale;

            // 7. Parenting
            if (_parentContainer == null)
            {
                GameObject autoParent = GameObject.Find("Environment_Container");
                if (autoParent == null)
                {
                    autoParent = new GameObject("Environment_Container");
                    Undo.RegisterCreatedObjectUndo(autoParent, "Create Container");
                }
                _parentContainer = autoParent.transform;
            }

            instance.transform.SetParent(_parentContainer);
        }

        private void PerformErase(Vector3 center)
        {
            if (EditorApplication.timeSinceStartup - _lastEraseTime < 0.1f) return;
            _lastEraseTime = EditorApplication.timeSinceStartup;

            Collider[] colliders = Physics.OverlapSphere(center, _brushRadius);

            foreach (Collider col in colliders)
            {
                if (col.transform.parent == _parentContainer || IsInPrefabList(col.gameObject))
                {
                    Undo.DestroyObjectImmediate(col.gameObject);
                }
            }
        }

        private bool IsInPrefabList(GameObject obj)
        {
            if (PrefabUtility.IsPartOfAnyPrefab(obj))
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                return _prefabs.Contains(source);
            }
            return false;
        }
    }
}