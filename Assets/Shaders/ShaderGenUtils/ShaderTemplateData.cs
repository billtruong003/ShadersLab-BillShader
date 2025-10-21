using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ShaderTemplateRegistry", menuName = "Rendering/URP Shader Template Registry")]
public class ShaderTemplateData : ScriptableObject
{
    public List<ShaderTemplate> templates = new List<ShaderTemplate>();
}

[System.Serializable]
public class ShaderTemplate
{
    public string menuName;
    public TextAsset templateFile;
    public string defaultFileName;
}