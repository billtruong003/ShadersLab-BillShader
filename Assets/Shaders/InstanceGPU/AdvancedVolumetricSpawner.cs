using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[AddComponentMenu("Systems/Volumetric Spawner")]
public class VolumetricSpawner : MonoBehaviour
{
    public enum ShapeType { Box, Sphere, Cylinder, Mesh }
    public enum RotationMode { Random, LookAtCenter, AlignToSurface }
    public enum SpawnState { Idle, Previewing }

    [System.Serializable]
    public class SpawnConfig
    {
        [InfoBox("Drop Prefabs, Models (FBX), or Scene Objects here.")]
        [ListDrawerSettings(ShowFoldout = true)]
        [PreviewField(45, ObjectFieldAlignment.Left)]
        public List<GameObject> spawnObjects = new List<GameObject>();

        [MinValue(1)] public int count = 10;
        public int seed = 1234;
    }

    [System.Serializable]
    public class TransformConfig
    {
        [ToggleGroup("EnableRotation", "Random Rotation")] public bool EnableRotation;
        [ToggleGroup("EnableRotation")][EnumToggleButtons] public RotationMode rotationMode;

        [ToggleGroup("EnableScale", "Random Scale")] public bool EnableScale;
        [ToggleGroup("EnableScale")][MinMaxSlider(0.1f, 5f, true)] public Vector2 scaleRange = Vector2.one;

        [ToggleGroup("SnapToSurface", "Snap To Surface")] public bool SnapToSurface;
        [ToggleGroup("SnapToSurface")] public LayerMask surfaceLayer;
        [ToggleGroup("SnapToSurface")] public float raycastHeight = 100f;
    }

    [System.Serializable]
    public class ValidationConfig
    {
        public bool preventOverlap;
        [ShowIf("preventOverlap"), MinValue(0.01f)] public float radius = 1f;
        [ShowIf("preventOverlap")] public LayerMask overlapLayer;
        [ShowIf("preventOverlap"), MinValue(1)] public int maxAttempts = 50;
    }

    [Title("1. Shape Definition")]
    [EnumToggleButtons, HideLabel]
    public ShapeType shapeType = ShapeType.Box;

    [HideLabel, ShowIf("@shapeType == ShapeType.Box")]
    public BoxVolume boxSettings = new BoxVolume();

    [HideLabel, ShowIf("@shapeType == ShapeType.Sphere")]
    public SphereVolume sphereSettings = new SphereVolume();

    [HideLabel, ShowIf("@shapeType == ShapeType.Cylinder")]
    public CylinderVolume cylinderSettings = new CylinderVolume();

    [HideLabel, ShowIf("@shapeType == ShapeType.Mesh")]
    public MeshVolume meshSettings = new MeshVolume();

    [Title("2. Configuration")]
    [HideLabel, TabGroup("Settings", "Spawning")] public SpawnConfig spawnSettings = new SpawnConfig();
    [HideLabel, TabGroup("Settings", "Transform")] public TransformConfig transformSettings = new TransformConfig();
    [HideLabel, TabGroup("Settings", "Validation")] public ValidationConfig validationSettings = new ValidationConfig();

    [InfoBox("Align To Surface requires Snap To Surface to be enabled.", InfoMessageType.Warning, "@transformSettings.EnableRotation && transformSettings.rotationMode == RotationMode.AlignToSurface && !transformSettings.SnapToSurface")]
    [Title("3. Actions")]
    [ShowInInspector, ReadOnly, ProgressBar(0, 100, r: 0.1f, g: 0.8f, b: 0.1f), LabelWidth(100)]
    private float successRate;

    private List<Vector3> _previewPoints = new List<Vector3>();
    private List<Quaternion> _previewRotations = new List<Quaternion>();
    private SpawnState _currentState = SpawnState.Idle;

    private IVolumeShape CurrentVolumeShape => shapeType switch
    {
        ShapeType.Box => boxSettings,
        ShapeType.Sphere => sphereSettings,
        ShapeType.Cylinder => cylinderSettings,
        ShapeType.Mesh => meshSettings,
        _ => boxSettings
    };

    [Button(ButtonSizes.Large), GUIColor(0.2f, 0.6f, 1f), ButtonGroup("MainActions")]
    public void Preview()
    {
        CalculatePoints();
        _currentState = SpawnState.Previewing;
#if UNITY_EDITOR
        SceneView.RepaintAll();
#endif
    }

    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f), ButtonGroup("MainActions")]
    public void Spawn()
    {
        if (_currentState != SpawnState.Previewing) CalculatePoints();
        ExecuteSpawn();
        _currentState = SpawnState.Idle;
    }

    [Button(ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f), ButtonGroup("MainActions")]
    public void Clear()
    {
        var children = new List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);

        foreach (var child in children)
        {
            if (Application.isEditor && !Application.isPlaying)
                DestroyImmediate(child);
            else
                Destroy(child);
        }

        _previewPoints.Clear();
        _previewRotations.Clear();
        _currentState = SpawnState.Idle;
        successRate = 0;
    }

    private void CalculatePoints()
    {
        Random.InitState(spawnSettings.seed);
        _previewPoints.Clear();
        _previewRotations.Clear();

        IVolumeShape shape = CurrentVolumeShape;
        shape.Prepare(transform);

        int successfulPoints = 0;
        int target = spawnSettings.count;

        for (int i = 0; i < target; i++)
        {
            if (TryGetValidPoint(shape, out Vector3 p, out Quaternion r))
            {
                _previewPoints.Add(p);
                _previewRotations.Add(r);
                successfulPoints++;
            }
        }

        successRate = target > 0 ? (float)successfulPoints / target * 100f : 0f;
    }

    private bool TryGetValidPoint(IVolumeShape shape, out Vector3 pos, out Quaternion rot)
    {
        pos = Vector3.zero;
        rot = Quaternion.identity;

        int attempts = (transformSettings.SnapToSurface || validationSettings.preventOverlap) ? validationSettings.maxAttempts : 1;

        for (int k = 0; k < attempts; k++)
        {
            Vector3 candidate = shape.GetRandomPoint(transform);

            if (transformSettings.SnapToSurface)
            {
                Vector3 rayOrigin = candidate + Vector3.up * transformSettings.raycastHeight;
                if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, transformSettings.raycastHeight * 2f, transformSettings.surfaceLayer))
                    continue;

                candidate = hit.point;

                if (transformSettings.EnableRotation && transformSettings.rotationMode == RotationMode.AlignToSurface)
                    rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }

            if (validationSettings.preventOverlap)
            {
                if (Physics.CheckSphere(candidate, validationSettings.radius, validationSettings.overlapLayer))
                    continue;
            }

            if (transformSettings.EnableRotation && transformSettings.rotationMode != RotationMode.AlignToSurface)
            {
                rot = transformSettings.rotationMode == RotationMode.LookAtCenter
                    ? Quaternion.LookRotation(transform.position - candidate)
                    : Random.rotation;
            }
            else if (!transformSettings.EnableRotation)
            {
                rot = Quaternion.identity;
            }

            pos = candidate;
            return true;
        }

        return false;
    }

    private void ExecuteSpawn()
    {
        if (spawnSettings.spawnObjects == null || spawnSettings.spawnObjects.Count == 0) return;

        for (int i = 0; i < _previewPoints.Count; i++)
        {
            GameObject sourceObj = spawnSettings.spawnObjects[Random.Range(0, spawnSettings.spawnObjects.Count)];
            if (sourceObj == null) continue;

            Vector3 pos = _previewPoints[i];
            Quaternion rot = _previewRotations[i];
            GameObject instance;

#if UNITY_EDITOR
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(sourceObj);
            bool isPrefab = prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant;

            if (isPrefab)
                instance = (GameObject)PrefabUtility.InstantiatePrefab(sourceObj, transform);
            else
            {
                instance = Instantiate(sourceObj, transform);
                instance.name = sourceObj.name;
            }
            Undo.RegisterCreatedObjectUndo(instance, "Volumetric Spawn");
#else
            instance = Instantiate(sourceObj, transform);
#endif
            instance.transform.position = pos;
            instance.transform.rotation = rot;

            if (transformSettings.EnableScale)
            {
                float s = Random.Range(transformSettings.scaleRange.x, transformSettings.scaleRange.y);
                instance.transform.localScale = Vector3.one * s;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        var shape = CurrentVolumeShape;
        if (shape == null) return;

        shape.DrawGizmos(transform, new Color(0, 1, 1, 0.15f));

        if (_currentState == SpawnState.Previewing)
        {
            Gizmos.color = Color.yellow;
            float size = validationSettings.preventOverlap ? validationSettings.radius * 2f : 0.15f;
            foreach (var p in _previewPoints)
                Gizmos.DrawSphere(p, size);
        }
    }
}

public interface IVolumeShape
{
    void Prepare(Transform root);
    Vector3 GetRandomPoint(Transform root);
    void DrawGizmos(Transform root, Color color);
}

[System.Serializable]
public class BoxVolume : IVolumeShape
{
    [BoxGroup("Shape Settings")] public Vector3 size = new Vector3(10, 5, 10);

    public void Prepare(Transform root) { }

    public Vector3 GetRandomPoint(Transform root) =>
        root.TransformPoint(Vector3.Scale(new Vector3(Random.value - 0.5f, Random.value - 0.5f, Random.value - 0.5f), size));

    public void DrawGizmos(Transform root, Color color)
    {
        Gizmos.color = color;
        Gizmos.matrix = root.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}

[System.Serializable]
public class SphereVolume : IVolumeShape
{
    [BoxGroup("Shape Settings")] public float radius = 5f;

    public void Prepare(Transform root) { }

    public Vector3 GetRandomPoint(Transform root) => root.TransformPoint(Random.insideUnitSphere * radius);

    public void DrawGizmos(Transform root, Color color)
    {
        Gizmos.color = color;
        Gizmos.matrix = root.localToWorldMatrix;
        Gizmos.DrawWireSphere(Vector3.zero, radius);
    }
}

[System.Serializable]
public class CylinderVolume : IVolumeShape
{
    [BoxGroup("Shape Settings")] public float radius = 5f;
    [BoxGroup("Shape Settings")] public float height = 10f;

    public void Prepare(Transform root) { }

    public Vector3 GetRandomPoint(Transform root)
    {
        Vector2 circle = Random.insideUnitCircle * radius;
        float y = Random.Range(-height * 0.5f, height * 0.5f);
        return root.TransformPoint(new Vector3(circle.x, y, circle.y));
    }

    public void DrawGizmos(Transform root, Color color)
    {
        Gizmos.color = color;
        Gizmos.matrix = root.localToWorldMatrix;
        float h = height * 0.5f;
        Vector3 top = Vector3.up * h;
        Vector3 bottom = Vector3.down * h;

        int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            float nextAngle = (i + 1) / (float)segments * Mathf.PI * 2f;

            float sinA = Mathf.Sin(angle) * radius;
            float cosA = Mathf.Cos(angle) * radius;
            float sinB = Mathf.Sin(nextAngle) * radius;
            float cosB = Mathf.Cos(nextAngle) * radius;

            Vector3 p1 = top + new Vector3(sinA, 0, cosA);
            Vector3 p2 = top + new Vector3(sinB, 0, cosB);
            Vector3 p3 = bottom + new Vector3(sinA, 0, cosA);
            Vector3 p4 = bottom + new Vector3(sinB, 0, cosB);

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p1, p3);
        }
    }
}

[System.Serializable]
public class MeshVolume : IVolumeShape
{
    [Required, BoxGroup("Shape Settings")] public Mesh sourceMesh;
    [BoxGroup("Shape Settings")] public Vector3 scale = Vector3.one;

    [InfoBox("Points are uniformly distributed inside the mesh volume (triangle-area weighted).", InfoMessageType.Info)]

    private Mesh _cachedMesh;
    private Vector3[] _cachedVertices;
    private int[] _cachedTriangles;
    private float[] _cumulativeAreas;
    private float _totalArea;

    public void Prepare(Transform root)
    {
        if (sourceMesh == null) return;

        if (_cachedMesh != sourceMesh || _cumulativeAreas == null || _cumulativeAreas.Length == 0)
        {
            _cachedMesh = sourceMesh;
            _cachedVertices = sourceMesh.vertices;
            _cachedTriangles = sourceMesh.triangles;

            if (_cachedTriangles.Length == 0) return;

            int triCount = _cachedTriangles.Length / 3;
            _cumulativeAreas = new float[triCount];
            _totalArea = 0f;

            for (int i = 0; i < triCount; i++)
            {
                int idx = i * 3;
                Vector3 a = _cachedVertices[_cachedTriangles[idx]];
                Vector3 b = _cachedVertices[_cachedTriangles[idx + 1]];
                Vector3 c = _cachedVertices[_cachedTriangles[idx + 2]];

                float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                _totalArea += area;
                _cumulativeAreas[i] = _totalArea;
            }
        }
    }

    public Vector3 GetRandomPoint(Transform root)
    {
        if (sourceMesh == null) return root.position;

        if (_cumulativeAreas == null || _cumulativeAreas.Length == 0)
        {
            Bounds b = sourceMesh.bounds;
            Vector3 local = new Vector3(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y),
                Random.Range(b.min.z, b.max.z)
            );
            return root.TransformPoint(Vector3.Scale(local, scale));
        }

        float pick = Random.Range(0f, _totalArea);
        int triIndex = System.Array.BinarySearch(_cumulativeAreas, pick);
        if (triIndex < 0) triIndex = ~triIndex;
        if (triIndex >= _cumulativeAreas.Length) triIndex = _cumulativeAreas.Length - 1;

        float r1 = Random.value;
        float r2 = Random.value;

        if (r1 + r2 > 1f)
        {
            r1 = 1f - r1;
            r2 = 1f - r2;
        }

        float r3 = 1f - r1 - r2;

        int idx = triIndex * 3;
        Vector3 A = _cachedVertices[_cachedTriangles[idx]];
        Vector3 B = _cachedVertices[_cachedTriangles[idx + 1]];
        Vector3 C = _cachedVertices[_cachedTriangles[idx + 2]];

        Vector3 localPoint = A * r3 + B * r1 + C * r2;
        return root.TransformPoint(Vector3.Scale(localPoint, scale));
    }

    public void DrawGizmos(Transform root, Color color)
    {
        if (sourceMesh == null) return;
        Gizmos.color = color;
        Gizmos.matrix = root.localToWorldMatrix * Matrix4x4.Scale(scale);
        Gizmos.DrawWireMesh(sourceMesh);
    }
}