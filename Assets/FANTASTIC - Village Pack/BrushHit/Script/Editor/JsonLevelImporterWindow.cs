using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;

public class JsonLevelImporterWindow : EditorWindow
{
    private TextAsset jsonFileToImport;
    private string importStatusMessage;
    private MessageType statusMessageType = MessageType.Info;

    [System.Serializable]
    private class RowData
    {
        public int[] row;
    }

    [System.Serializable]
    private class JsonLevelData
    {
        public string levelName;
        public RowData[] grid;
    }

    [System.Serializable]
    private class JsonLevelCollection
    {
        public JsonLevelData[] levels;
    }

    [MenuItem("ShadersLab/JSON Level Importer")]
    public static void ShowWindow()
    {
        GetWindow<JsonLevelImporterWindow>("JSON Level Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("JSON to LevelData Batch Converter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Kéo file JSON chứa danh sách các level vào đây. Công cụ sẽ tạo ra nhiều file LevelData tương ứng.", MessageType.Info);

        jsonFileToImport = (TextAsset)EditorGUILayout.ObjectField("JSON Levels File", jsonFileToImport, typeof(TextAsset), false);

        if (GUILayout.Button("Import JSON and Create Multiple LevelData Assets"))
        {
            ProcessJsonBatchImport();
        }

        if (!string.IsNullOrEmpty(importStatusMessage))
        {
            EditorGUILayout.HelpBox(importStatusMessage, statusMessageType);
        }
    }

    private void ProcessJsonBatchImport()
    {
        if (jsonFileToImport == null)
        {
            SetStatus("Lỗi: Vui lòng chọn một file JSON để import.", MessageType.Error);
            return;
        }

        string outputDirectory = EditorUtility.OpenFolderPanel("Chọn thư mục để lưu các LevelData", "Assets/", "");
        if (string.IsNullOrEmpty(outputDirectory))
        {
            SetStatus("Hủy bỏ thao tác. Vui lòng chọn một thư mục hợp lệ.", MessageType.Warning);
            return;
        }

        try
        {
            JsonLevelCollection levelCollection = ReadAndParseJsonCollection(jsonFileToImport);
            int importedCount = 0;
            var importedFiles = new StringBuilder();
            importedFiles.AppendLine("Các file đã được tạo thành công:");

            foreach (var levelJson in levelCollection.levels)
            {
                ValidateLevelJsonData(levelJson);
                LevelData newLevelData = CreateLevelDataFromGrid(levelJson.grid);
                string assetPath = SaveLevelDataAsset(newLevelData, levelJson.levelName, outputDirectory);
                importedFiles.AppendLine(assetPath);
                importedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SetStatus($"Hoàn tất! Đã import {importedCount} level.\n{importedFiles}", MessageType.Info);
        }
        catch (Exception ex)
        {
            SetStatus($"Lỗi xử lý file JSON: {ex.Message}", MessageType.Error);
        }
    }

    private JsonLevelCollection ReadAndParseJsonCollection(TextAsset jsonAsset)
    {
        var collection = JsonUtility.FromJson<JsonLevelCollection>(jsonAsset.text);
        if (collection == null || collection.levels == null)
        {
            throw new ArgumentException("File JSON không hợp lệ hoặc không chứa mảng 'levels'.");
        }
        return collection;
    }

    private void ValidateLevelJsonData(JsonLevelData levelData)
    {
        const int expectedDimension = 10;
        if (string.IsNullOrEmpty(levelData.levelName))
        {
            throw new ArgumentException("Một level trong file JSON không có 'levelName'.");
        }

        if (levelData.grid == null || levelData.grid.Length != expectedDimension)
        {
            throw new ArgumentException($"Lỗi grid của '{levelData.levelName}'. Chiều cao phải là {expectedDimension}, hiện tại là {levelData.grid.Length}.");
        }

        for (int i = 0; i < levelData.grid.Length; i++)
        {
            if (levelData.grid[i].row == null || levelData.grid[i].row.Length != expectedDimension)
            {
                throw new ArgumentException($"Lỗi grid của '{levelData.levelName}'. Chiều rộng của hàng {i} phải là {expectedDimension}.");
            }
        }
    }

    private LevelData CreateLevelDataFromGrid(RowData[] gridData)
    {
        LevelData levelData = CreateInstance<LevelData>();
        levelData.gridWidth = 10;
        levelData.gridHeight = 10;
        levelData.cellSize = 10;
        levelData.cells = new System.Collections.Generic.List<GridCell>();

        for (int y = 0; y < levelData.gridHeight; y++)
        {
            for (int x = 0; x < levelData.gridWidth; x++)
            {
                var newCell = new GridCell(x, y);
                int jsonValue = gridData[y].row[x];
                newCell.objectType = MapIntToObjectType(jsonValue);
                levelData.cells.Add(newCell);
            }
        }
        return levelData;
    }

    private ObjectType MapIntToObjectType(int typeValue)
    {
        switch (typeValue)
        {
            case 0: return ObjectType.Ground;
            case 1: return ObjectType.Collectible;
            case 2: return ObjectType.Obstacle;
            case 3: return ObjectType.DangerZone;
            default:
                throw new ArgumentOutOfRangeException($"Giá trị không hợp lệ trong grid JSON: {typeValue}. Chỉ chấp nhận 0, 1, 2, 3.");
        }
    }

    private string SaveLevelDataAsset(LevelData levelData, string levelName, string fullDirectoryPath)
    {
        string relativePath = ConvertFullPathToProjectRelative(fullDirectoryPath);
        string assetPath = Path.Combine(relativePath, $"LevelData_{levelName}.asset");
        AssetDatabase.CreateAsset(levelData, assetPath);
        return assetPath;
    }

    private string ConvertFullPathToProjectRelative(string fullPath)
    {
        if (fullPath.StartsWith(Application.dataPath))
        {
            return "Assets" + fullPath.Substring(Application.dataPath.Length);
        }
        throw new ArgumentException("Thư mục được chọn phải nằm trong thư mục 'Assets' của dự án.");
    }

    private void SetStatus(string message, MessageType type)
    {
        importStatusMessage = message;
        statusMessageType = type;
    }
}