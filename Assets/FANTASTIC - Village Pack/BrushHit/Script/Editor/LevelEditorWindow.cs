using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public class LevelEditorWindow : EditorWindow
{
    private LevelData currentLevelData;
    private Vector2 scrollPosition;
    private readonly Dictionary<ObjectType, Color> typeColors = new Dictionary<ObjectType, Color>();

    private static readonly ObjectType[] cycleOrder = {
        ObjectType.Ground,
        ObjectType.Collectible,
        ObjectType.Obstacle,
        ObjectType.DangerZone
    };

    [MenuItem("ShadersLab/Ultimate Grid Editor")]
    public static void ShowWindow() => GetWindow<LevelEditorWindow>("Ultimate Grid Editor");

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        InitializeColors();
    }

    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void InitializeColors()
    {
        // Màu sắc được chọn lại để rõ ràng hơn
        typeColors[ObjectType.Ground] = new Color(0.7f, 0.7f, 0.7f);       // Wireframe Gray
        typeColors[ObjectType.Collectible] = new Color(0.2f, 0.7f, 1f);   // Solid Cyan
        typeColors[ObjectType.Obstacle] = new Color(1f, 0.6f, 0.2f);    // Solid Orange
        typeColors[ObjectType.DangerZone] = new Color(1f, 0.3f, 0.3f);   // Solid Red
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        GUILayout.Label("Ultimate Grid Editor", EditorStyles.boldLabel);

        currentLevelData = (LevelData)EditorGUILayout.ObjectField("Current Level Data", currentLevelData, typeof(LevelData), false);

        if (GUILayout.Button("Create New Grid Level Data")) CreateNewLevelDataAsset();
        if (currentLevelData == null)
        {
            EditorGUILayout.HelpBox("Please assign or create a Level Data asset.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.Space(10);
        DrawLevelConfiguration();
        DrawInstructions();
        DrawColorKeyLegend(); // Thêm bảng chú thích màu sắc
        EditorGUILayout.EndScrollView();
    }

    private void DrawLevelConfiguration()
    {
        EditorGUI.BeginChangeCheck();
        int newWidth = EditorGUILayout.IntField("Grid Width", currentLevelData.gridWidth);
        int newHeight = EditorGUILayout.IntField("Grid Height", currentLevelData.gridHeight);
        float newCellSize = EditorGUILayout.FloatField("Cell Size", currentLevelData.cellSize);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(currentLevelData, "Resize Grid");
            currentLevelData.gridWidth = Mathf.Max(1, newWidth);
            currentLevelData.gridHeight = Mathf.Max(1, newHeight);
            currentLevelData.cellSize = Mathf.Max(0.1f, newCellSize);
            currentLevelData.InitializeOrResizeGrid();
            EditorUtility.SetDirty(currentLevelData);
            SceneView.RepaintAll();
        }
    }

    private void DrawInstructions()
    {
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Instructions (Scene View)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "• Left Click: Cycle (Ground -> Collectible -> ...)\n" +
            "• Right Click: Erase to Void (create a hole)\n" +
            "• Drag Mouse: Paint multiple cells", MessageType.None);
    }

    // --- MỚI: Bảng chú thích màu sắc ---
    private void DrawColorKeyLegend()
    {
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Color Legend", EditorStyles.boldLabel);

        // Vẽ chú thích cho từng loại đối tượng có màu
        foreach (var pair in typeColors.OrderBy(p => (int)p.Key))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // Vô hiệu hóa để người dùng không thay đổi màu được
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ColorField(GUIContent.none, pair.Value, false, false, false, GUILayout.Width(50));
                EditorGUI.EndDisabledGroup();

                string description = pair.Key == ObjectType.Ground ? "Ground (Wireframe)" : $"{pair.Key} (Solid)";
                EditorGUILayout.LabelField(description);
            }
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (currentLevelData == null) return;

        DrawAllCellVisuals();
        HandleMouseEvents(sceneView);
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
    }

    // --- CẢI TIẾN LỚN: Logic vẽ được làm lại hoàn toàn ---
    private void DrawAllCellVisuals()
    {
        foreach (var cell in currentLevelData.cells)
        {
            Vector3 worldPos = GridToWorldPosition(cell.gridPosition);
            Vector3[] cellVertices = GetCellVertices(worldPos);

            // Sử dụng switch để logic rõ ràng, mỗi loại ô chỉ có một cách vẽ duy nhất
            switch (cell.objectType)
            {
                case ObjectType.Void:
                    // Không làm gì cả, ô này hoàn toàn trống
                    break;

                case ObjectType.Ground:
                    // Vẽ một hình vuông rỗng (chỉ có viền)
                    Handles.color = typeColors[ObjectType.Ground];
                    Handles.DrawPolyLine(cellVertices[0], cellVertices[1], cellVertices[2], cellVertices[3], cellVertices[0]);
                    break;

                default:
                    // Vẽ một hình vuông được tô đầy cho các đối tượng khác (Collectible, Obstacle, ...)
                    Handles.color = typeColors[cell.objectType];
                    Handles.DrawSolidRectangleWithOutline(cellVertices, Handles.color, Color.clear);
                    break;
            }
        }
        // Vẫn vẽ lưới chung lên trên để dễ nhìn
        DrawGridLines();
    }

    private void DrawGridLines()
    {
        Handles.color = new Color(1, 1, 1, 0.1f); // Làm cho lưới mờ hơn
        float width = currentLevelData.gridWidth * currentLevelData.cellSize;
        float height = currentLevelData.gridHeight * currentLevelData.cellSize;
        Vector3 startPoint = -GetGridCenterOffset();
        for (int i = 0; i <= currentLevelData.gridHeight; i++)
            Handles.DrawLine(startPoint + new Vector3(0, 0, i * currentLevelData.cellSize), startPoint + new Vector3(width, 0, i * currentLevelData.cellSize));
        for (int i = 0; i <= currentLevelData.gridWidth; i++)
            Handles.DrawLine(startPoint + new Vector3(i * currentLevelData.cellSize, 0, 0), startPoint + new Vector3(i * currentLevelData.cellSize, 0, height));
    }

    private Vector3[] GetCellVertices(Vector3 cellCenter)
    {
        float halfSize = currentLevelData.cellSize / 2f;
        return new Vector3[] {
            cellCenter + new Vector3(-halfSize, 0, -halfSize),
            cellCenter + new Vector3(halfSize, 0, -halfSize),
            cellCenter + new Vector3(halfSize, 0, halfSize),
            cellCenter + new Vector3(-halfSize, 0, halfSize)
        };
    }

    private void HandleMouseEvents(SceneView sceneView)
    {
        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float enter))
        {
            Vector2Int gridPos = WorldToGridPosition(ray.GetPoint(enter));
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && (e.button == 0 || e.button == 1))
            {
                e.Use();
                if (e.button == 0) CycleCellType(gridPos);
                else if (e.button == 1) SetCellType(gridPos, ObjectType.Void);
            }
        }
    }

    // Logic click được làm lại để đảm bảo tính đúng đắn
    private void CycleCellType(Vector2Int gridPos)
    {
        GridCell cell = currentLevelData.GetCell(gridPos.x, gridPos.y);
        if (cell == null) return;

        ObjectType nextType;
        // Nếu ô hiện tại là Void, click trái sẽ luôn biến nó thành Ground
        if (cell.objectType == ObjectType.Void)
        {
            nextType = ObjectType.Ground;
        }
        else
        {
            // Tìm vị trí hiện tại trong chu trình và lấy vị trí tiếp theo
            int currentIndex = Array.IndexOf(cycleOrder, cell.objectType);
            int nextIndex = (currentIndex + 1) % cycleOrder.Length;
            nextType = cycleOrder[nextIndex];
        }

        SetCellType(gridPos, nextType);
    }

    private void SetCellType(Vector2Int gridPos, ObjectType newType)
    {
        GridCell cell = currentLevelData.GetCell(gridPos.x, gridPos.y);
        if (cell == null || cell.objectType == newType) return;

        Undo.RecordObject(currentLevelData, $"Set Cell to {newType}");
        cell.objectType = newType;
        EditorUtility.SetDirty(currentLevelData);
    }

    // Các hàm helper và tạo asset không đổi
    private Vector3 GetGridCenterOffset() => new Vector3(currentLevelData.gridWidth * currentLevelData.cellSize / 2f, 0, currentLevelData.gridHeight * currentLevelData.cellSize / 2f);
    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 localPos = worldPos + GetGridCenterOffset();
        return new Vector2Int(Mathf.FloorToInt(localPos.x / currentLevelData.cellSize), Mathf.FloorToInt(localPos.z / currentLevelData.cellSize));
    }
    private Vector3 GridToWorldPosition(Vector2Int gridPos) => new Vector3((gridPos.x * currentLevelData.cellSize) + (currentLevelData.cellSize / 2f), 0, (gridPos.y * currentLevelData.cellSize) + (currentLevelData.cellSize / 2f)) - GetGridCenterOffset();
    private void CreateNewLevelDataAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save New Grid Level", "NewGridLevelData.asset", "asset", "");
        if (string.IsNullOrEmpty(path)) return;
        LevelData newAsset = CreateInstance<LevelData>();
        AssetDatabase.CreateAsset(newAsset, path);
        AssetDatabase.SaveAssets();
        currentLevelData = newAsset;
        Selection.activeObject = newAsset;
    }
}