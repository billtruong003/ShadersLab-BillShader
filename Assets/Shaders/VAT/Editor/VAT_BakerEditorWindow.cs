using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class VAT_BakerEditorWindow : EditorWindow
{
    private struct RendererInfo
    {
        public Renderer renderer;
        public Mesh sharedMesh;
        public bool isSkinned;

        public bool IsValid() => renderer != null && sharedMesh != null;
    }

    private List<GameObject> _sourceObjects = new List<GameObject>();
    private Vector2 _scrollPosition;
    private const int FRAMERATE_OVERRIDE = 30;

    [MenuItem("Tools/BillTheDev/VAT Baker (Batch)")]
    public static void ShowWindow()
    {
        GetWindow<VAT_BakerEditorWindow>("VAT Baker (Batch)");
    }

    private void OnGUI()
    {
        GUILayout.Label("Vertex Animation Texture Baker (Batch Mode)", EditorStyles.boldLabel);
        DrawDragAndDropArea();
        DrawSourceObjectsList();

        using (new EditorGUI.DisabledScope(_sourceObjects.Count == 0))
        {
            if (GUILayout.Button("Bake All Selected Animations", GUILayout.Height(30)))
            {
                BakeAllSelectedObjects();
            }
        }
    }

    private void DrawDragAndDropArea()
    {
        var dropAreaRect = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        var style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Italic,
            normal = { textColor = Color.gray }
        };
        GUI.Box(dropAreaRect, "Drag & Drop GameObjects (Prefabs/Scene) Here", style);
        ProcessDragAndDropEvents(dropAreaRect);
    }

    private void ProcessDragAndDropEvents(Rect dropArea)
    {
        var currentEvent = Event.current;
        if (!dropArea.Contains(currentEvent.mousePosition)) return;

        switch (currentEvent.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    AddDraggedObjects();
                }
                currentEvent.Use();
                break;
        }
    }

    private void AddDraggedObjects()
    {
        var draggedObjects = DragAndDrop.objectReferences
            .OfType<GameObject>()
            .Where(go => !_sourceObjects.Contains(go));

        _sourceObjects.AddRange(draggedObjects);
    }

    private void DrawSourceObjectsList()
    {
        EditorGUILayout.LabelField("Objects to Bake:", EditorStyles.boldLabel);
        using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition, GUILayout.MinHeight(100), GUILayout.MaxHeight(300)))
        {
            _scrollPosition = scrollView.scrollPosition;
            for (int i = _sourceObjects.Count - 1; i >= 0; i--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _sourceObjects[i] = EditorGUILayout.ObjectField(_sourceObjects[i], typeof(GameObject), true) as GameObject;
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        _sourceObjects.RemoveAt(i);
                    }
                }
            }
        }

        if (_sourceObjects.Count > 0 && GUILayout.Button("Clear List"))
        {
            _sourceObjects.Clear();
        }
    }

    private void BakeAllSelectedObjects()
    {
        string outputPath = EditorUtility.OpenFolderPanel("Select Output Folder for Baked Assets", "Assets", "");
        if (string.IsNullOrEmpty(outputPath)) return;

        string projectRelativePath = "Assets" + outputPath.Substring(Application.dataPath.Length);

        int bakedCount = 0;
        try
        {
            for (int i = 0; i < _sourceObjects.Count; i++)
            {
                var sourceObject = _sourceObjects[i];
                if (sourceObject == null) continue;

                string progressBarTitle = $"VAT Baker ({i + 1}/{_sourceObjects.Count})";
                string progressBarInfo = $"Processing: {sourceObject.name}";
                EditorUtility.DisplayProgressBar(progressBarTitle, progressBarInfo, (float)i / _sourceObjects.Count);

                if (BakeSingleObject(sourceObject, projectRelativePath))
                {
                    bakedCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Bake Complete", $"Successfully baked {bakedCount} out of {_sourceObjects.Count} objects.\nAssets saved in: {projectRelativePath}", "OK");
            AssetDatabase.Refresh();
        }
    }

    private bool BakeSingleObject(GameObject sourceObject, string directory)
    {
        var (isValid, rendererInfo) = IsSourceValid(sourceObject);
        if (!isValid)
        {
            Debug.LogWarning($"Skipping '{sourceObject.name}': Source is not valid. Check for a Renderer (Skinned or Mesh), Animator, and valid clips.", sourceObject);
            return false;
        }

        var tempInstance = Instantiate(sourceObject);
        // Đặt lại transform của đối tượng gốc để việc bake vào local space được nhất quán
        tempInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        tempInstance.transform.localScale = Vector3.one;

        try
        {
            var animationClips = tempInstance.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips
                .Where(c => !c.legacy && c.length > 0).Distinct().ToArray();

            if (animationClips.Length == 0)
            {
                Debug.LogWarning($"Skipping '{sourceObject.name}': No valid animation clips found.", sourceObject);
                return false;
            }

            var totalAnimationBounds = CalculateTotalLocalSpaceBounds(tempInstance, animationClips, rendererInfo);
            var (positionTexture, clipInfos) = BakeAnimationsToTexture(tempInstance, animationClips, rendererInfo, totalAnimationBounds);

            var bakedMesh = CreateMeshWithVertexIdUVs(rendererInfo.sharedMesh, totalAnimationBounds, sourceObject.name);
            var animationData = CreateAnimationDataAsset(bakedMesh, positionTexture, totalAnimationBounds, clipInfos, sourceObject.name);
            var material = CreateOptimizedMaterial(sourceObject.name);

            SaveAllAssets(directory, sourceObject.name, animationData, bakedMesh, positionTexture, material);
        }
        finally
        {
            if (tempInstance != null) DestroyImmediate(tempInstance);
        }
        return true;
    }

    private (bool, RendererInfo) IsSourceValid(GameObject sourceObject)
    {
        var animator = sourceObject.GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null) return (false, default);

        var skinnedRenderer = sourceObject.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedRenderer != null && skinnedRenderer.sharedMesh != null)
        {
            return (true, new RendererInfo { renderer = skinnedRenderer, sharedMesh = skinnedRenderer.sharedMesh, isSkinned = true });
        }

        var meshRenderer = sourceObject.GetComponentInChildren<MeshRenderer>();
        var meshFilter = sourceObject.GetComponentInChildren<MeshFilter>();
        if (meshRenderer != null && meshFilter != null && meshFilter.sharedMesh != null)
        {
            return (true, new RendererInfo { renderer = meshRenderer, sharedMesh = meshFilter.sharedMesh, isSkinned = false });
        }

        return (false, default);
    }

    private Bounds CalculateTotalLocalSpaceBounds(GameObject instance, AnimationClip[] clips, RendererInfo rendererInfo)
    {
        var totalBounds = new Bounds();
        var tempMesh = new Mesh();
        bool first = true;

        // Lấy renderer tương ứng từ trong instance, thay vì từ source object
        var instanceRenderer = instance.GetComponentInChildren<Renderer>();

        foreach (var clip in clips)
        {
            float timeStep = 1.0f / FRAMERATE_OVERRIDE;
            for (float time = 0; time <= clip.length; time += timeStep)
            {
                clip.SampleAnimation(instance, time);

                var vertices = new Vector3[rendererInfo.sharedMesh.vertexCount];
                if (rendererInfo.isSkinned)
                {
                    (instanceRenderer as SkinnedMeshRenderer).BakeMesh(tempMesh, true);
                    vertices = tempMesh.vertices;
                }
                else
                {
                    // Lấy ma trận biến đổi của renderer (có thể là con hoặc là chính instance)
                    var matrix = instanceRenderer.transform.localToWorldMatrix;
                    var sourceVertices = rendererInfo.sharedMesh.vertices;
                    for (int i = 0; i < sourceVertices.Length; i++)
                    {
                        vertices[i] = matrix.MultiplyPoint3x4(sourceVertices[i]);
                    }
                }

                var currentBounds = GeometryUtility.CalculateBounds(vertices, Matrix4x4.identity);
                if (first)
                {
                    totalBounds = currentBounds;
                    first = false;
                }
                else
                {
                    totalBounds.Encapsulate(currentBounds);
                }
            }
        }
        totalBounds.Expand(0.01f);
        DestroyImmediate(tempMesh);
        return totalBounds;
    }

    private (Texture2D, List<VAT_AnimationData.ClipInfo>) BakeAnimationsToTexture(GameObject instance, AnimationClip[] clips, RendererInfo rendererInfo, Bounds totalBounds)
    {
        var clipInfos = new List<VAT_AnimationData.ClipInfo>();
        var vertexCount = rendererInfo.sharedMesh.vertexCount;
        var tempMesh = new Mesh();
        var allFramesData = new List<Color[]>();
        int totalFrames = 0;

        var instanceRenderer = instance.GetComponentInChildren<Renderer>();

        foreach (var clip in clips)
        {
            int frameCount = Mathf.Max(2, Mathf.CeilToInt(clip.length * FRAMERATE_OVERRIDE));
            clipInfos.Add(new VAT_AnimationData.ClipInfo { name = clip.name, startFrame = totalFrames, frameCount = frameCount, duration = clip.length, wrapMode = clip.wrapMode });

            var frameVertices = new Vector3[vertexCount];
            for (int frame = 0; frame < frameCount; frame++)
            {
                float sampleTime = (frame / (float)(frameCount - 1)) * clip.length;
                clip.SampleAnimation(instance, sampleTime);

                if (rendererInfo.isSkinned)
                {
                    (instanceRenderer as SkinnedMeshRenderer).BakeMesh(tempMesh, true);
                    frameVertices = tempMesh.vertices;
                }
                else
                {
                    var matrix = instanceRenderer.transform.localToWorldMatrix;
                    var sourceVertices = rendererInfo.sharedMesh.vertices;
                    for (int i = 0; i < sourceVertices.Length; i++)
                    {
                        frameVertices[i] = matrix.MultiplyPoint3x4(sourceVertices[i]);
                    }
                }

                var frameColors = new Color[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    frameColors[i] = EncodeLocalPositionToColor(frameVertices[i], totalBounds);
                }
                allFramesData.Add(frameColors);
            }
            totalFrames += frameCount;
        }

        DestroyImmediate(tempMesh);

        var positionTexture = new Texture2D(vertexCount, totalFrames, TextureFormat.RGBAHalf, false)
        {
            name = $"{instance.name}_VAT_PositionTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < totalFrames; y++)
        {
            positionTexture.SetPixels(0, y, vertexCount, 1, allFramesData[y]);
        }
        positionTexture.Apply(false, true);
        return (positionTexture, clipInfos);
    }

    private Mesh CreateMeshWithVertexIdUVs(Mesh originalMesh, Bounds animationBounds, string baseName)
    {
        var newMesh = new Mesh
        {
            name = $"{baseName}_Mesh",
            vertices = originalMesh.vertices,
            normals = originalMesh.normals,
            tangents = originalMesh.tangents,
            uv = originalMesh.uv,
            triangles = originalMesh.triangles,
            bounds = animationBounds
        };

        int vertexCount = originalMesh.vertexCount;
        var vertexIdUVs = new Vector2[vertexCount];
        float invTexWidth = 1.0f / vertexCount;
        float halfTexel = 0.5f * invTexWidth;

        for (int i = 0; i < vertexCount; i++)
        {
            vertexIdUVs[i] = new Vector2(i * invTexWidth + halfTexel, 0);
        }

        newMesh.SetUVs(1, vertexIdUVs);
        newMesh.UploadMeshData(true);
        return newMesh;
    }

    private Color EncodeLocalPositionToColor(Vector3 localPosition, Bounds totalBounds)
    {
        float r = Mathf.InverseLerp(totalBounds.min.x, totalBounds.max.x, localPosition.x);
        float g = Mathf.InverseLerp(totalBounds.min.y, totalBounds.max.y, localPosition.y);
        float b = Mathf.InverseLerp(totalBounds.min.z, totalBounds.max.z, localPosition.z);
        return new Color(r, g, b, 1.0f);
    }

    private VAT_AnimationData CreateAnimationDataAsset(Mesh mesh, Texture2D tex, Bounds bounds, List<VAT_AnimationData.ClipInfo> infos, string baseName)
    {
        var dataAsset = CreateInstance<VAT_AnimationData>();
        dataAsset.name = $"{baseName}_Data";
        dataAsset.bakedMesh = mesh;
        dataAsset.positionTexture = tex;
        dataAsset.positionMinBounds = bounds.min;
        dataAsset.positionMaxBounds = bounds.max;
        dataAsset.animationClips = infos;
        return dataAsset;
    }

    private Material CreateOptimizedMaterial(string baseName)
    {
        var shader = Shader.Find("BillTheDev/VAT/Optimized_VAT");
        if (shader == null)
        {
            Debug.LogError("Shader 'BillTheDev/VAT/Optimized_VAT' not found. Ensure it is compiled and included in the build.");
            return null;
        }
        var material = new Material(shader) { name = $"{baseName}_Mat" };
        material.enableInstancing = true;
        return material;
    }

    private void SaveAllAssets(string dir, string name, VAT_AnimationData data, Mesh mesh, Texture2D tex, Material mat)
    {
        string dataPath = Path.Combine(dir, $"{name}_VAT_Data.asset");
        AssetDatabase.CreateAsset(data, dataPath);

        if (mesh != null) AssetDatabase.AddObjectToAsset(mesh, data);
        if (tex != null) AssetDatabase.AddObjectToAsset(tex, data);
        if (mat != null) AssetDatabase.AddObjectToAsset(mat, data);

        AssetDatabase.SaveAssets();
    }
}