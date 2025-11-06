// AssetOrganizationUtility.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class AssetOrganizationUtility : EditorWindow
{
    private string destinationPath = "Assets/";
    private string assetPrefix = "";
    private string assetSuffix = "";
    private string newAssetName = "NewMaterial";
    private Texture2D selectedTextureForMaterial;

    private Vector2 scrollPosition;

    [MenuItem("Tools/Asset Organization Utility")]
    public static void ShowWindow()
    {
        GetWindow<AssetOrganizationUtility>("Asset Organizer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Asset Organization Utility", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Chọn các tài sản trong cửa sổ Project và sử dụng các chức năng bên dưới.", MessageType.Info);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawSectionSeparator();
        DrawExtractionSection();
        DrawSectionSeparator();
        DrawRenamingSection();
        DrawSectionSeparator();
        DrawCreationSection();
        DrawSectionSeparator();
        DrawCleaningSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawExtractionSection()
    {
        EditorGUILayout.LabelField("FBX Extraction", EditorStyles.boldLabel);
        DrawDestinationFolderSelector();

        if (GUILayout.Button("Extract Animations From Selected FBX"))
        {
            ExtractAnimationsFromSelectedFBX();
        }

        if (GUILayout.Button("Extract Meshes From Selected FBX"))
        {
            ExtractMeshesFromSelectedFBX();
        }
    }

    private void DrawRenamingSection()
    {
        EditorGUILayout.LabelField("Batch Renaming", EditorStyles.boldLabel);
        assetPrefix = EditorGUILayout.TextField("Prefix", assetPrefix);
        assetSuffix = EditorGUILayout.TextField("Suffix", assetSuffix);

        if (GUILayout.Button("Rename Selected Assets"))
        {
            RenameSelectedAssets();
        }
    }

    private void DrawCreationSection()
    {
        EditorGUILayout.LabelField("Quick Asset Creation", EditorStyles.boldLabel);
        DrawDestinationFolderSelector();

        selectedTextureForMaterial = (Texture2D)EditorGUILayout.ObjectField(
            "Texture for Material",
            selectedTextureForMaterial,
            typeof(Texture2D),
            false
        );
        newAssetName = EditorGUILayout.TextField("New Material Name", newAssetName);

        if (GUILayout.Button("Create Material From Texture"))
        {
            CreateMaterialFromSelectedTexture();
        }
    }

    private void DrawCleaningSection()
    {
        EditorGUILayout.LabelField("Project Cleaning", EditorStyles.boldLabel);
        if (GUILayout.Button("Delete Empty Folders In Project"))
        {
            if (EditorUtility.DisplayDialog(
                "Confirm Deletion",
                "Bạn có chắc chắn muốn xóa tất cả các thư mục rỗng trong project không? Hành động này không thể hoàn tác.",
                "Yes, Delete", "Cancel"))
            {
                DeleteEmptyFolders();
            }
        }
    }

    private void DrawDestinationFolderSelector()
    {
        EditorGUILayout.LabelField("Destination Folder", EditorStyles.miniBoldLabel);
        if (GUILayout.Button(destinationPath, EditorStyles.popup))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Destination Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                destinationPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
            }
        }
    }

    private void ExtractAnimationsFromSelectedFBX()
    {
        if (IsDestinationPathInvalid()) return;

        var selectedFbxGUIDs = Selection.assetGUIDs.Where(guid =>
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return Path.GetExtension(path).ToLower() == ".fbx";
        });

        if (!selectedFbxGUIDs.Any())
        {
            ShowNotification("Please select FBX files to extract animations from.");
            return;
        }

        int extractedCount = 0;
        foreach (string guid in selectedFbxGUIDs)
        {
            string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var animations = allAssets.OfType<AnimationClip>();

            foreach (AnimationClip clip in animations)
            {
                // Ignore internal rig animations
                if (clip.name.StartsWith("__preview__")) continue;

                AnimationClip newClip = new AnimationClip();
                EditorUtility.CopySerialized(clip, newClip);

                string newPath = Path.Combine(destinationPath, $"{clip.name}.anim");
                newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

                AssetDatabase.CreateAsset(newClip, newPath);
                extractedCount++;
            }
        }

        FinalizeAssetOperation($"Successfully extracted {extractedCount} animation clips.");
    }

    private void ExtractMeshesFromSelectedFBX()
    {
        if (IsDestinationPathInvalid()) return;

        var selectedFbxGUIDs = Selection.assetGUIDs.Where(guid =>
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return Path.GetExtension(path).ToLower() == ".fbx";
        });

        if (!selectedFbxGUIDs.Any())
        {
            ShowNotification("Please select FBX files to extract meshes from.");
            return;
        }

        int extractedCount = 0;
        foreach (string guid in selectedFbxGUIDs)
        {
            string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var meshes = allAssets.OfType<Mesh>();

            foreach (Mesh mesh in meshes)
            {
                if (mesh == null || string.IsNullOrEmpty(mesh.name)) continue;

                Mesh newMesh = Object.Instantiate(mesh); // Create a clone

                string newPath = Path.Combine(destinationPath, $"{mesh.name}.asset");
                newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

                AssetDatabase.CreateAsset(newMesh, newPath);
                extractedCount++;
            }
        }

        FinalizeAssetOperation($"Successfully extracted {extractedCount} meshes.");
    }

    private void RenameSelectedAssets()
    {
        var selectedObjects = Selection.objects;
        if (selectedObjects.Length == 0)
        {
            ShowNotification("Please select assets to rename.");
            return;
        }

        AssetDatabase.StartAssetEditing();
        foreach (var obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            string originalName = Path.GetFileNameWithoutExtension(path);
            string newName = $"{assetPrefix}{originalName}{assetSuffix}";
            AssetDatabase.RenameAsset(path, newName);
        }
        AssetDatabase.StopAssetEditing();

        ShowNotification($"Renamed {selectedObjects.Length} assets.");
    }

    private void CreateMaterialFromSelectedTexture()
    {
        if (IsDestinationPathInvalid()) return;
        if (selectedTextureForMaterial == null)
        {
            ShowNotification("Please select a texture to create a material from.");
            return;
        }
        if (string.IsNullOrWhiteSpace(newAssetName))
        {
            ShowNotification("Please provide a name for the new material.");
            return;
        }

        Material newMaterial = new Material(Shader.Find("Standard"));
        newMaterial.mainTexture = selectedTextureForMaterial;

        string newPath = Path.Combine(destinationPath, $"{newAssetName}.mat");
        newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

        AssetDatabase.CreateAsset(newMaterial, newPath);
        EditorGUIUtility.PingObject(newMaterial);
        ShowNotification($"Created material at {newPath}");
    }

    private void DeleteEmptyFolders()
    {
        var allFolders = Directory.GetDirectories(Application.dataPath, "*", SearchOption.AllDirectories);
        var emptyFolders = allFolders.Where(path => !Directory.EnumerateFileSystemEntries(path).Any()).ToList();

        if (!emptyFolders.Any())
        {
            ShowNotification("No empty folders found.");
            return;
        }

        int deletedCount = 0;
        foreach (var folder in emptyFolders)
        {
            // We need to convert back to a relative path for AssetDatabase
            string relativePath = "Assets" + folder.Substring(Application.dataPath.Length);
            if (AssetDatabase.DeleteAsset(relativePath))
            {
                deletedCount++;
            }
        }

        FinalizeAssetOperation($"Successfully deleted {deletedCount} empty folders.");
    }

    private bool IsDestinationPathInvalid()
    {
        if (string.IsNullOrEmpty(destinationPath) || !AssetDatabase.IsValidFolder(destinationPath))
        {
            ShowNotification("Please select a valid destination folder.");
            return true;
        }
        return false;
    }

    private void FinalizeAssetOperation(string message)
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ShowNotification(message);
        Repaint();
    }

    private void ShowNotification(string message)
    {
        ShowNotification(new GUIContent(message));
    }

    private void DrawSectionSeparator()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(10);
    }
}