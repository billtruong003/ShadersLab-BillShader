using UnityEngine;
using UnityEditor;
using System.IO;

public static class ShaderTemplateMenu
{
    private const string dataAssetSearchString = "t:ShaderTemplateData";
    private const string menuRoot = "Assets/Create/Shader/";

    [MenuItem(menuRoot, true)]
    private static bool ValidateShowWindow()
    {
        return LoadTemplateData() != null;
    }

    [MenuItem(menuRoot, false, 81)]
    private static void ShowWindow()
    {
        var templateData = LoadTemplateData();
        if (templateData == null) return;

        var menu = new GenericMenu();
        foreach (var template in templateData.templates)
        {
            menu.AddItem(new GUIContent(template.menuName), false, CreateShaderAsset, template);
        }
        menu.ShowAsContext();
    }

    private static ShaderTemplateData LoadTemplateData()
    {
        string[] guids = AssetDatabase.FindAssets(dataAssetSearchString);
        if (guids.Length == 0)
        {
            Debug.LogWarning("Không tìm thấy tệp 'ShaderTemplateRegistry'. Vui lòng tạo một tệp thông qua Assets -> Create -> Rendering -> URP Shader Template Registry.");
            return null;
        }
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<ShaderTemplateData>(path);
    }

    private static void CreateShaderAsset(object templateObject)
    {
        var template = (ShaderTemplate)templateObject;

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path) || Path.GetExtension(path) != "")
        {
            path = "Assets";
        }

        string fileName = $"{template.defaultFileName}.shader";
        string fullPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, fileName));
        string fileContent = template.templateFile.text;

        ProjectWindowUtil.CreateAssetWithContent(fullPath, fileContent);
    }
}