using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace OptimizeGrass
{
    [DefaultExecutionOrder(-100)]
    public class BakedGrassManager : MonoBehaviour
    {
        [System.Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct GrassInstance
        {
            public float3 position;
            public float rotY;
            public float2 scale;
            public uint colorSeed;
            public float padding;
        }

        [System.Serializable]
        public class BakedBatch
        {
            [ReadOnly] public string name;
            [Required] public Mesh mesh;
            [Required] public Material material;
            [ReadOnly] public float boundRadius;
            [HideInInspector] public List<GrassInstance> data = new List<GrassInstance>();
            [HideInInspector] public int count;

            public ComputeBuffer sourceBuffer;
            public ComputeBuffer visibleBuffer;
            public ComputeBuffer argsBuffer;
            public MaterialPropertyBlock props;
            public int occlusionPassIndex = -1;

            public void Initialize()
            {
                count = data.Count;
                if (count == 0) return;

                sourceBuffer = new ComputeBuffer(count, 32);
                sourceBuffer.SetData(data);

                visibleBuffer = new ComputeBuffer(count, 32, ComputeBufferType.Append);

                argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
                uint[] args = new uint[5] { (uint)mesh.GetIndexCount(0), 0, (uint)mesh.GetIndexStart(0), (uint)mesh.GetBaseVertex(0), 0 };
                argsBuffer.SetData(args);

                props = new MaterialPropertyBlock();
                props.SetBuffer(ShaderIDs.VisibleBuffer, visibleBuffer);

                occlusionPassIndex = material.FindPass("OcclusionMask");
            }

            public void Release()
            {
                sourceBuffer?.Release();
                visibleBuffer?.Release();
                argsBuffer?.Release();
                sourceBuffer = null;
            }
        }

        [Title("Settings")]
        [SerializeField, Required] private ComputeShader _cullingShader;
        [SerializeField] private float _cullDistance = 150f;
        [SerializeField] private float _boundsPadding = 1.0f;

        [Title("Outline Integration")]
        [SerializeField] private LayerMask _occlusionLayer; // Set this to the layer Grass is on

        [Title("Baked Data")]
        [SerializeField, ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "name")]
        private List<BakedBatch> _batches = new List<BakedBatch>();

        private Camera _mainCamera;
        private int _kernelID;
        private readonly Plane[] _cameraPlanes = new Plane[6];
        private readonly Vector4[] _frustumPlanesV4 = new Vector4[6];
        private bool _isInitialized;

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
            ReleaseAllBuffers();
            _batches.Clear();
            var dict = new Dictionary<int, BakedBatch>();
            var renderers = GetComponentsInChildren<MeshRenderer>(true);

            foreach (var r in renderers)
            {
                if (r.gameObject == gameObject) continue;
                var filter = r.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh) continue;

                int key = filter.sharedMesh.GetInstanceID() ^ r.sharedMaterial.GetInstanceID();
                if (!dict.TryGetValue(key, out var batch))
                {
                    batch = new BakedBatch
                    {
                        name = r.gameObject.name,
                        mesh = filter.sharedMesh,
                        material = r.sharedMaterial,
                        boundRadius = filter.sharedMesh.bounds.extents.magnitude
                    };
                    dict.Add(key, batch);
                    _batches.Add(batch);
                }

                Transform t = r.transform;
                batch.data.Add(new GrassInstance
                {
                    position = t.position,
                    rotY = t.eulerAngles.y,
                    scale = new float2(t.lossyScale.x, t.lossyScale.y),
                    colorSeed = (uint)UnityEngine.Random.Range(0, 10000),
                    padding = 0
                });
                r.gameObject.SetActive(false);
            }

            _isInitialized = false;
            InitializeGPU();
        }

        [Button(ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
        private void ClearDataAndShowChildren()
        {
            var renderers = GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers) r.gameObject.SetActive(true);
            _batches.Clear();
            ReleaseAllBuffers();
        }

        private void Start() => InitializeGPU();

        private void OnEnable()
        {
            OutlineFeature.OnRenderFoliageMask += RenderOcclusionMask;
        }

        private void OnDisable()
        {
            OutlineFeature.OnRenderFoliageMask -= RenderOcclusionMask;
        }

        private void InitializeGPU()
        {
            if (_isInitialized) return;
            _mainCamera = Camera.main;
            if (_cullingShader != null) _kernelID = _cullingShader.FindKernel("CSMain");

            foreach (var batch in _batches) batch.Initialize();
            _isInitialized = true;
        }

        private void RenderOcclusionMask(RasterCommandBuffer cmd, LayerMask maskLayer)
        {
            if (!_isInitialized || _batches.Count == 0) return;
            if ((_occlusionLayer.value & maskLayer.value) == 0) return;

            for (int i = 0; i < _batches.Count; i++)
            {
                var batch = _batches[i];
                if (batch.occlusionPassIndex != -1 && batch.visibleBuffer != null)
                {
                    cmd.DrawMeshInstancedIndirect(batch.mesh, 0, batch.material, batch.occlusionPassIndex, batch.argsBuffer, 0, batch.props);
                }
            }
        }

        private void Update()
        {
            if (!_isInitialized || _batches.Count == 0) return;
            if (!_mainCamera) { _mainCamera = Camera.main; if (!_mainCamera) return; }

            GeometryUtility.CalculateFrustumPlanes(_mainCamera, _cameraPlanes);
            for (int i = 0; i < 6; i++)
            {
                var n = _cameraPlanes[i].normal;
                _frustumPlanesV4[i] = new Vector4(n.x, n.y, n.z, _cameraPlanes[i].distance);
            }

            _cullingShader.SetVectorArray(ShaderIDs.CameraPlanes, _frustumPlanesV4);
            _cullingShader.SetVector(ShaderIDs.CameraPosition, _mainCamera.transform.position);
            _cullingShader.SetFloat(ShaderIDs.MaxDistanceSq, _cullDistance * _cullDistance);

            var bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);

            for (int i = 0; i < _batches.Count; i++)
            {
                var batch = _batches[i];
                if (batch.sourceBuffer == null) continue;

                batch.visibleBuffer.SetCounterValue(0);

                _cullingShader.SetInt(ShaderIDs.Count, batch.count);
                _cullingShader.SetFloat(ShaderIDs.BoundRadius, batch.boundRadius + _boundsPadding);
                _cullingShader.SetBuffer(_kernelID, ShaderIDs.SourceBuffer, batch.sourceBuffer);
                _cullingShader.SetBuffer(_kernelID, ShaderIDs.VisibleBuffer, batch.visibleBuffer);

                _cullingShader.Dispatch(_kernelID, Mathf.CeilToInt(batch.count / 256f), 1, 1);
                ComputeBuffer.CopyCount(batch.visibleBuffer, batch.argsBuffer, 4);

                Graphics.DrawMeshInstancedIndirect(batch.mesh, 0, batch.material, bounds, batch.argsBuffer, 0, batch.props);
            }
        }

        private void OnDestroy() => ReleaseAllBuffers();

        private void ReleaseAllBuffers()
        {
            if (_batches == null) return;
            foreach (var batch in _batches) batch.Release();
            _isInitialized = false;
        }
    }
}