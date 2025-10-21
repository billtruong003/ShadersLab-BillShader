using UnityEditor;
using System.IO;

public class ShaderTemplateProcessor : AssetModificationProcessor
{
    private const string shaderNamePlaceholder = "#{SHADER_NAME}#";
    private const string assetsPrefix = "Assets/";
    private const string shadersRootFolder = "Shaders/";

    public static void OnWillCreateAsset(string path)
    {
        string assetPath = path.Replace(".meta", "");
        if (!assetPath.EndsWith(".shader"))
        {
            return;
        }

        string fileContent = File.ReadAllText(assetPath);
        if (!fileContent.Contains(shaderNamePlaceholder))
        {
            return;
        }

        string internalShaderPath = GetInternalShaderPath(assetPath);

        fileContent = fileContent.Replace(shaderNamePlaceholder, internalShaderPath);
        File.WriteAllText(assetPath, fileContent);
        AssetDatabase.Refresh();
    }

    private static string GetInternalShaderPath(string fullAssetPath)
    {
        // Loại bỏ ".shader" extension
        string pathWithoutExtension = Path.ChangeExtension(fullAssetPath, null);

        // Loại bỏ "Assets/" ở đầu
        if (pathWithoutExtension.StartsWith(assetsPrefix))
        {
            pathWithoutExtension = pathWithoutExtension.Substring(assetsPrefix.Length);
        }

        // Tùy chọn: Loại bỏ một thư mục gốc chung như "Shaders/" để tên gọn hơn
        int shadersRootIndex = pathWithoutExtension.IndexOf(shadersRootFolder);
        if (shadersRootIndex != -1)
        {
            pathWithoutExtension = pathWithoutExtension.Substring(shadersRootIndex + shadersRootFolder.Length);
        }

        return pathWithoutExtension;
    }
}