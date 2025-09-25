using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[System.Serializable]
public enum LayoutShape
{
    Grid,
    Circle,
    Cylinder,
    Triangle,
    Pyramid,
    Trapezoid,
    Sphere,
    Spiral,
    Wave
}

public class ObjectPlacer3D : MonoBehaviour
{
    [Title("Core Settings", bold: true)]
    [Required("Please assign a Prefab to be placed.")]
    [AssetsOnly]
    public GameObject PrefabToPlace;

    [SceneObjectsOnly]
    public Transform ObjectsParent;

    [Title("Layout Configuration", bold: true)]
    [EnumToggleButtons]
    public LayoutShape Shape;

    private List<GameObject> spawnedObjects = new List<GameObject>();

    // Grid Parameters
    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Grid)]
    public Vector3Int GridSize = new Vector3Int(5, 1, 5);

    // Circle & Cylinder Parameters
    [BoxGroup("Shape Parameters")]
    [ShowIf("@this.Shape == LayoutShape.Circle || this.Shape == LayoutShape.Cylinder")]
    [MinValue(1)]
    public int ObjectCount = 12;

    [BoxGroup("Shape Parameters")]
    [ShowIf("@this.Shape == LayoutShape.Circle || this.Shape == LayoutShape.Cylinder")]
    [MinValue(0f)]
    public float Radius = 5f;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Cylinder)]
    [MinValue(1)]
    public int Layers = 5;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Cylinder)]
    public float LayerHeight = 1f;

    // Triangle Parameters
    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Triangle)]
    [MinValue(1)]
    public int BaseWidth = 7;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Triangle)]
    public bool IsHollowTriangle = false;

    // Pyramid Parameters
    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Pyramid)]
    [MinValue(2)]
    public int BaseSize = 5;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Pyramid)]
    [MinValue(1)]
    public int Height = 5;

    // Trapezoid Parameters
    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Trapezoid)]
    [MinValue(3)]
    public int BottomBase = 7;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Trapezoid)]
    [MinValue(1)]
    public int TopBase = 3;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Trapezoid)]
    [MinValue(2)]
    public int TrapezoidHeight = 4;

    // Sphere Parameters
    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Sphere)]
    [MinValue(1f)]
    public float SphereRadius = 5f;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Sphere)]
    [Range(3, 100)]
    public int LatitudeDivisions = 12;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Sphere)]
    [Range(3, 100)]
    public int LongitudeDivisions = 24;

    // Spiral Parameters
    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Spiral)]
    [MinValue(1)]
    public int SpiralObjectCount = 50;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Spiral)]
    public float StartRadius = 1f;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Spiral)]
    public float EndRadius = 10f;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Spiral)]
    public float SpiralHeight = 5f;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Spiral)]
    public int Rotations = 3;

    // Wave Parameters
    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Wave)]
    public Vector2Int WaveGridSize = new Vector2Int(20, 20);

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Wave)]
    public float Amplitude = 1f;

    [BoxGroup("Shape Parameters")]
    [ShowIf("Shape", LayoutShape.Wave)]
    public float Frequency = 0.5f;

    // Common Parameters
    [Title("Shared Parameters")]
    [BoxGroup("Shape Parameters")]
    public Vector3 Spacing = Vector3.one;

    [Title("Actions", Bold = true)]
    [Button(ButtonSizes.Large, Name = "Generate Layout")]
    [GUIColor(0.4f, 0.8f, 1f)]
    private void GenerateLayout()
    {
        ClearGeneratedObjects();
        EnsureParentExists();

        switch (Shape)
        {
            case LayoutShape.Grid:
                GenerateGridLayout();
                break;
            case LayoutShape.Circle:
                GenerateCircularLayout();
                break;
            case LayoutShape.Cylinder:
                GenerateCylindricalLayout();
                break;
            case LayoutShape.Triangle:
                GenerateTriangleLayout();
                break;
            case LayoutShape.Pyramid:
                GeneratePyramidLayout();
                break;
            case LayoutShape.Trapezoid:
                GenerateTrapezoidLayout();
                break;
            case LayoutShape.Sphere:
                GenerateSphericalLayout();
                break;
            case LayoutShape.Spiral:
                GenerateSpiralLayout();
                break;
            case LayoutShape.Wave:
                GenerateWaveLayout();
                break;
        }
    }

    [Button(ButtonSizes.Large)]
    [GUIColor(1f, 0.5f, 0.5f)]
    private void ClearGeneratedObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        spawnedObjects.Clear();
    }

    private void GenerateGridLayout()
    {
        Vector3 offset = new Vector3(
            -(GridSize.x - 1) * Spacing.x * 0.5f,
            0,
            -(GridSize.z - 1) * Spacing.z * 0.5f
        );

        for (int y = 0; y < GridSize.y; y++)
        {
            for (int z = 0; z < GridSize.z; z++)
            {
                for (int x = 0; x < GridSize.x; x++)
                {
                    Vector3 position = new Vector3(x * Spacing.x, y * Spacing.y, z * Spacing.z) + offset;
                    InstantiateObject(position);
                }
            }
        }
    }

    private void GenerateCircularLayout()
    {
        for (int i = 0; i < ObjectCount; i++)
        {
            float angle = i * (2 * Mathf.PI / ObjectCount);
            float x = Mathf.Cos(angle) * Radius;
            float z = Mathf.Sin(angle) * Radius;
            InstantiateObject(new Vector3(x, 0, z));
        }
    }

    private void GenerateCylindricalLayout()
    {
        for (int y = 0; y < Layers; y++)
        {
            for (int i = 0; i < ObjectCount; i++)
            {
                float angle = i * (2 * Mathf.PI / ObjectCount);
                float x = Mathf.Cos(angle) * Radius;
                float z = Mathf.Sin(angle) * Radius;
                InstantiateObject(new Vector3(x, y * LayerHeight, z));
            }
        }
    }

    private void GenerateTriangleLayout()
    {
        for (int y = 0; y < BaseWidth; y++)
        {
            int rowWidth = BaseWidth - y;
            for (int x = 0; x < rowWidth; x++)
            {
                if (IsHollowTriangle && y > 0 && y < BaseWidth - 1 && x > 0 && x < rowWidth - 1)
                {
                    continue;
                }
                float xPos = (x - (rowWidth - 1) * 0.5f) * Spacing.x;
                float zPos = y * Spacing.z;
                InstantiateObject(new Vector3(xPos, 0, zPos));
            }
        }
    }

    private void GeneratePyramidLayout()
    {
        for (int y = 0; y < Height; y++)
        {
            float ratio = (float)(Height - y) / Height;
            int layerSize = Mathf.Max(1, Mathf.CeilToInt(BaseSize * ratio));

            float offset = (layerSize - 1) * 0.5f;

            for (int x = 0; x < layerSize; x++)
            {
                for (int z = 0; z < layerSize; z++)
                {
                    if (y < Height - 1 && x > 0 && x < layerSize - 1 && z > 0 && z < layerSize - 1)
                    {
                        continue;
                    }

                    Vector3 position = new Vector3(
                        (x - offset) * Spacing.x,
                        y * Spacing.y,
                        (z - offset) * Spacing.z
                    );
                    InstantiateObject(position);
                }
            }
        }
    }

    private void GenerateTrapezoidLayout()
    {
        for (int y = 0; y < TrapezoidHeight; y++)
        {
            float t = (float)y / (TrapezoidHeight - 1);
            int rowWidth = (int)Mathf.Lerp(BottomBase, TopBase, t);

            float offset = (rowWidth - 1) * 0.5f;

            for (int x = 0; x < rowWidth; x++)
            {
                Vector3 position = new Vector3(
                    (x - offset) * Spacing.x,
                    y * Spacing.y,
                    0
                );
                InstantiateObject(position);
            }
        }
    }

    private void GenerateSphericalLayout()
    {
        for (int i = 0; i <= LatitudeDivisions; i++)
        {
            float latitudeAngle = Mathf.PI * i / LatitudeDivisions;
            float sinLat = Mathf.Sin(latitudeAngle);
            float cosLat = Mathf.Cos(latitudeAngle);

            int currentLongitudeDivisions = (i == 0 || i == LatitudeDivisions) ? 1 : LongitudeDivisions;

            for (int j = 0; j < currentLongitudeDivisions; j++)
            {
                float longitudeAngle = 2 * Mathf.PI * j / currentLongitudeDivisions;
                float sinLon = Mathf.Sin(longitudeAngle);
                float cosLon = Mathf.Cos(longitudeAngle);

                float x = SphereRadius * sinLat * cosLon;
                float y = SphereRadius * cosLat;
                float z = SphereRadius * sinLat * sinLon;

                InstantiateObject(new Vector3(x, y, z));
            }
        }
    }

    private void GenerateSpiralLayout()
    {
        float totalAngle = Rotations * 2 * Mathf.PI;

        for (int i = 0; i < SpiralObjectCount; i++)
        {
            float progress = (float)i / (SpiralObjectCount - 1);

            float currentRadius = Mathf.Lerp(StartRadius, EndRadius, progress);
            float currentAngle = progress * totalAngle;
            float currentHeight = progress * SpiralHeight;

            float x = Mathf.Cos(currentAngle) * currentRadius;
            float z = Mathf.Sin(currentAngle) * currentRadius;

            InstantiateObject(new Vector3(x, currentHeight, z));
        }
    }

    private void GenerateWaveLayout()
    {
        Vector3 offset = new Vector3(
            -(WaveGridSize.x - 1) * Spacing.x * 0.5f,
            0,
            -(WaveGridSize.y - 1) * Spacing.z * 0.5f
        );

        for (int z = 0; z < WaveGridSize.y; z++)
        {
            for (int x = 0; x < WaveGridSize.x; x++)
            {
                float xPos = x * Spacing.x + offset.x;
                float zPos = z * Spacing.z + offset.z;
                float yPos = Amplitude * Mathf.Sin(Frequency * (xPos + zPos));

                InstantiateObject(new Vector3(xPos, yPos, zPos));
            }
        }
    }

    private void EnsureParentExists()
    {
        if (ObjectsParent == null)
        {
            GameObject parentObject = new GameObject($"{this.gameObject.name}_GeneratedObjects");
            parentObject.transform.SetParent(this.transform);
            ObjectsParent = parentObject.transform;
        }
    }

    private void InstantiateObject(Vector3 position)
    {
        if (PrefabToPlace == null) return;
        Vector3 finalPosition = ObjectsParent.position + position;
        GameObject newObj = Instantiate(PrefabToPlace, finalPosition, Quaternion.identity, ObjectsParent);
        spawnedObjects.Add(newObj);
    }
}