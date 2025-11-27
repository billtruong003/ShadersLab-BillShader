using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using Unity.Mathematics;

namespace OptimizeGrass
{
    public class GrassGPUController : MonoBehaviour
    {
        [System.Serializable]
        public class GrassTypeGroup
        {
            [HideInInspector] public string name;
            [Required] public Mesh mesh;
            [Required] public Material material;
            [ReadOnly] public int count;
            [ReadOnly] public float boundRadius;

            // Runtime Buffers
            public ComputeBuffer sourceBuffer;
            public ComputeBuffer visibleBuffer;
            public ComputeBuffer argsBuffer;

            // CPU Data (Cleared after bake)
            [HideInInspector] public List<GrassInstance> instances = new List<GrassInstance>();
        }

        [Title("Settings")]
        [SerializeField, Required] private ComputeShader _cullingShader;
        [SerializeField] private float _cullDistance = 150f;
        [SerializeField] private float _boundsPadding = 0.5f;

        [Title("Data")]
        [SerializeField, ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "name")]
        private List<GrassTypeGroup> _groups = new List<GrassTypeGroup>();

        private Camera _mainCamera;
        private int _kernelID;
        private readonly Plane[] _cameraPlanes = new Plane[6];
        private readonly Vector4[] _frustumPlanesV4 = new Vector4[6];
        private readonly uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };

        private static class ShaderIDs
        {
            public static readonly int SourceBuffer = Shader.PropertyToID("_SourceBuffer");
            public static readonly int VisibleBuffer = Shader.PropertyToID("_VisibleBuffer");
            public static readonly int CameraPlanes = Shader.PropertyToID("_CameraPlanes");
            public static readonly int CameraPosition = Shader.PropertyToID("_CameraPosition");
            public static readonly int MaxDistanceSq = Shader.PropertyToID("_MaxDistanceSq");
            public static readonly int Count = Shader.PropertyToID("_Count");
            public static readonly int BoundRadius = Shader.PropertyToID("_BoundRadius");
        }

        [Button(ButtonSizes.Large), GUIColor(0.2f, 1f, 0.4f)]
        private void BakeFromChildren()
        {
            ReleaseBuffers();
            _groups.Clear();
            var dict = new Dictionary<int, GrassTypeGroup>();
            var renderers = GetComponentsInChildren<MeshRenderer>(true);

            foreach (var r in renderers)
            {
                if (r.gameObject == this.gameObject) continue;
                var filter = r.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh) continue;

                int key = filter.sharedMesh.GetInstanceID() ^ r.sharedMaterial.GetInstanceID();
                if (!dict.TryGetValue(key, out var group))
                {
                    group = new GrassTypeGroup
                    {
                        name = r.gameObject.name,
                        mesh = filter.sharedMesh,
                        material = r.sharedMaterial,
                        boundRadius = filter.sharedMesh.bounds.extents.magnitude
                    };
                    dict.Add(key, group);
                    _groups.Add(group);
                }

                Transform t = r.transform;
                // Pack Data: Position (float3) + RotationY (float) + Scale (float2) + ColorSeed (uint)
                var instance = new GrassInstance
                {
                    position = t.position,
                    rotY = t.eulerAngles.y,
                    scale = new float2(t.lossyScale.x, t.lossyScale.y),
                    colorSeed = (uint)UnityEngine.Random.Range(0, 10000),
                    padding = 0
                };
                group.instances.Add(instance);
                group.count++;

                r.gameObject.SetActive(false);
            }

            // Upload immediately to ensure data persists if we save scene? 
            // Better to re-initialize on Start, but for Editor logic we keep List.
            Debug.Log($"Baked {renderers.Length} objects into {_groups.Count} GPU batches.");
        }

        [Button(ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
        private void ClearAndEnableChildren()
        {
            var renderers = GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers) r.gameObject.SetActive(true);
            _groups.Clear();
            ReleaseBuffers();
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            _kernelID = _cullingShader.FindKernel("CSMain");
            InitializeGPU();
        }

        private void InitializeGPU()
        {
            foreach (var group in _groups)
            {
                if (group.count == 0) continue;

                group.sourceBuffer = new ComputeBuffer(group.count, 32); // 32 bytes stride
                group.sourceBuffer.SetData(group.instances);

                group.visibleBuffer = new ComputeBuffer(group.count, 32, ComputeBufferType.Append);
                group.argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

                _args[0] = (uint)group.mesh.GetIndexCount(0);
                _args[2] = (uint)group.mesh.GetIndexStart(0);
                _args[3] = (uint)group.mesh.GetBaseVertex(0);
                group.argsBuffer.SetData(_args);

                // Set material buffer once if possible, or per frame
                group.material.SetBuffer(ShaderIDs.VisibleBuffer, group.visibleBuffer);
            }
        }

        private void Update()
        {
            if (_groups.Count == 0 || !_mainCamera) return;

            // Prepare Global Data
            GeometryUtility.CalculateFrustumPlanes(_mainCamera, _cameraPlanes);
            for (int i = 0; i < 6; i++)
            {
                var n = _cameraPlanes[i].normal;
                _frustumPlanesV4[i] = new Vector4(n.x, n.y, n.z, _cameraPlanes[i].distance);
            }
            _cullingShader.SetVectorArray(ShaderIDs.CameraPlanes, _frustumPlanesV4);
            _cullingShader.SetVector(ShaderIDs.CameraPosition, _mainCamera.transform.position);
            _cullingShader.SetFloat(ShaderIDs.MaxDistanceSq, _cullDistance * _cullDistance);

            foreach (var group in _groups)
            {
                if (group.sourceBuffer == null) continue;

                group.visibleBuffer.SetCounterValue(0);

                _cullingShader.SetInt(ShaderIDs.Count, group.count);
                _cullingShader.SetFloat(ShaderIDs.BoundRadius, group.boundRadius + _boundsPadding);
                _cullingShader.SetBuffer(_kernelID, ShaderIDs.SourceBuffer, group.sourceBuffer);
                _cullingShader.SetBuffer(_kernelID, ShaderIDs.VisibleBuffer, group.visibleBuffer);

                int threadGroups = Mathf.CeilToInt(group.count / 256f);
                _cullingShader.Dispatch(_kernelID, threadGroups, 1, 1);

                ComputeBuffer.CopyCount(group.visibleBuffer, group.argsBuffer, 4);

                Graphics.DrawMeshInstancedIndirect(group.mesh, 0, group.material,
                    new Bounds(Vector3.zero, Vector3.one * 10000), group.argsBuffer);
            }
        }

        private void OnDestroy() => ReleaseBuffers();

        private void ReleaseBuffers()
        {
            foreach (var group in _groups)
            {
                group.sourceBuffer?.Release();
                group.visibleBuffer?.Release();
                group.argsBuffer?.Release();
            }
        }
    }
}