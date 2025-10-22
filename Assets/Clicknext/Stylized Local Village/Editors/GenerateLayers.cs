using Clicknext.StylizedLocalVillage.Entities;
using UnityEditor;
using UnityEngine;

namespace Clicknext.Editor
{
#if (UNITY_EDITOR)
    [InitializeOnLoad]
#endif
    public class GenerateLayers
    {
#if (UNITY_EDITOR)
        private static readonly int maxLayers = 31;
        static GenerateLayers()
        {
            var serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            Generate(serializedObject);
            serializedObject.ApplyModifiedProperties();
        }

        static void Generate(SerializedObject serializedObject)
        {
            SerializedProperty layers = serializedObject.FindProperty("layers");
            if (layers == null || !layers.isArray)
                return;
            if (!PropertyExists(layers, 0, maxLayers, LayerType.Ground.ToString()))
            {
                SerializedProperty sp;
                for (int i = 8, j = maxLayers; i < j; i++)
                {
                    sp = layers.GetArrayElementAtIndex(i);
                    if (sp.stringValue == "")
                    {
                        var layerName = LayerType.Ground.ToString();
                        sp.stringValue = layerName;
                        Debug.Log("Layer: " + layerName + " has been added");
                        break;
                    }
                }
            }
        }

        static bool PropertyExists(SerializedProperty property, int start, int end, string value)
        {
            for (int i = start; i < end; i++)
            {
                SerializedProperty t = property.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(value))
                {
                    return true;
                }
            }
            return false;
        }
#endif
    }
}