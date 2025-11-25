using UnityEngine;
using UnityEditor;
using System.IO;

public class ToonRampEditor : EditorWindow
{
    private Gradient gradient = new Gradient();
    private Material targetMaterial;
    private string propertyName = "_ColorRamp";
    private int width = 256;
    private string fileName = "NewToonRamp";

    private Texture2D previewTexture;
    private bool autoUpdate = true;

    [MenuItem("Tools/Toon Ramp Editor")]
    public static void ShowWindow()
    {
        GetWindow<ToonRampEditor>("Ramp Editor");
    }

    private void OnEnable()
    {
        InitTexture();
    }

    private void OnDisable()
    {
        if (previewTexture != null)
        {
            DestroyImmediate(previewTexture);
        }
    }

    private void InitTexture()
    {
        if (previewTexture == null || previewTexture.width != width)
        {
            previewTexture = new Texture2D(width, 1, TextureFormat.ARGB32, false);
            previewTexture.wrapMode = TextureWrapMode.Clamp;
            previewTexture.filterMode = FilterMode.Bilinear;
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Realtime Ramp Generator", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        targetMaterial = (Material)EditorGUILayout.ObjectField("Target Material", targetMaterial, typeof(Material), false);
        propertyName = EditorGUILayout.TextField("Shader Property Name", propertyName);

        GUILayout.Space(10);

        gradient = EditorGUILayout.GradientField("Gradient", gradient);
        width = EditorGUILayout.IntSlider("Resolution", width, 64, 512);
        autoUpdate = EditorGUILayout.Toggle("Realtime Preview", autoUpdate);

        if (EditorGUI.EndChangeCheck())
        {
            if (autoUpdate)
            {
                UpdatePreview();
            }
        }

        GUILayout.Space(20);
        GUILayout.Label("Export Settings", EditorStyles.boldLabel);
        fileName = EditorGUILayout.TextField("Asset Name", fileName);

        if (GUILayout.Button("Save to Asset & Assign", GUILayout.Height(30)))
        {
            SaveAndAssign();
        }

        if (previewTexture != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("Preview:", EditorStyles.label);
            GUI.DrawTexture(GUILayoutUtility.GetRect(position.width - 10, 30), previewTexture);
        }
    }

    private void UpdatePreview()
    {
        InitTexture();

        for (int i = 0; i < width; i++)
        {
            float t = (float)i / (width - 1);
            previewTexture.SetPixel(i, 0, gradient.Evaluate(t));
        }
        previewTexture.Apply();

        if (targetMaterial != null)
        {
            if (targetMaterial.HasProperty(propertyName))
            {
                targetMaterial.SetTexture(propertyName, previewTexture);
            }
        }
    }

    private void SaveAndAssign()
    {
        UpdatePreview();

        byte[] bytes = previewTexture.EncodeToPNG();
        string path = $"Assets/{fileName}.png";

        path = AssetDatabase.GenerateUniqueAssetPath(path);
        File.WriteAllBytes(path, bytes);

        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.sRGBTexture = true;
            importer.textureType = TextureImporterType.Default;
            importer.SaveAndReimport();
        }

        Texture2D savedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (targetMaterial != null && savedTexture != null)
        {
            Undo.RecordObject(targetMaterial, "Assign Toon Ramp");
            targetMaterial.SetTexture(propertyName, savedTexture);
            EditorUtility.SetDirty(targetMaterial);
            Debug.Log($"Saved and Assigned: {path}");
        }
    }
}