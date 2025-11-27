using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace CleanCode.EnvironmentTools
{
    public class FoliageInstanceRenderer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private ComputeShader _cullingCompute;
        [SerializeField] private Transform _targetRoot;
        [SerializeField] private bool _rebuildOnStart = true;
        [SerializeField] private float _cullingDistance = 150f;

        [StructLayout(LayoutKind.Sequential)]
        private struct IndirectData
        {
            public Matrix4x4 objectToWorld;
            public Matrix4x4 worldToObject;
        }

        private class RenderBatch
        {
            public Mesh Mesh;
            public Material Material;
            public List<IndirectData> CPUData = new List<IndirectData>();
            public ComputeBuffer AllDataBuffer;
            public ComputeBuffer VisibleDataBuffer;
            public ComputeBuffer ArgsBuffer;
            public MaterialPropertyBlock Props;
            public float BoundsSize;
            public int Capacity;
            public bool IsDirty;
            public int MaskPassIndex = -1;
        }

        private Dictionary<int, RenderBatch> _batchMap = new Dictionary<int, RenderBatch>();
        private List<RenderBatch> _activeBatches = new List<RenderBatch>();
        private ComputeBuffer _trashBuffer;

        private readonly Plane[] _cameraPlanes = new Plane[6];
        private readonly Vector4[] _planeNormals = new Vector4[6];
        private readonly uint[] _argsData = new uint[5] { 0, 0, 0, 0, 0 };

        private static readonly int ID_CameraPosition = Shader.PropertyToID("_CameraPosition");
        private static readonly int ID_FrustumPlanes = Shader.PropertyToID("_FrustumPlanes");
        private static readonly int ID_CullingBound = Shader.PropertyToID("_CullingBound");
        private static readonly int ID_InstanceCount = Shader.PropertyToID("_InstanceCount");
        private static readonly int ID_LODDistances = Shader.PropertyToID("_LODDistances");
        private static readonly int ID_AllInstances = Shader.PropertyToID("_AllInstances");
        private static readonly int ID_LOD0_Instances = Shader.PropertyToID("_LOD0_Instances");
        private static readonly int ID_LOD1_Instances = Shader.PropertyToID("_LOD1_Instances");
        private static readonly int ID_LOD2_Instances = Shader.PropertyToID("_LOD2_Instances");
        private static readonly int ID_IndirectInstanceData = Shader.PropertyToID("_IndirectInstanceData");

        private const int GROUP_SIZE = 64;

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            OutlineFeature.OnRenderFoliageMask += OnRenderMask;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            OutlineFeature.OnRenderFoliageMask -= OnRenderMask;
            ReleaseResources();
        }

        private void Start()
        {
            if (_rebuildOnStart) Rebuild();
        }

        public void RegisterRenderer(MeshRenderer renderer)
        {
            if (renderer == null) return;
            MeshFilter mf = renderer.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;

            int key = Hash(mf.sharedMesh, renderer.sharedMaterial);
            if (!_batchMap.TryGetValue(key, out RenderBatch batch))
            {
                batch = CreateNewBatch(mf.sharedMesh, renderer.sharedMaterial);
                _batchMap.Add(key, batch);
                _activeBatches.Add(batch);
            }

            batch.CPUData.Add(new IndirectData
            {
                objectToWorld = renderer.transform.localToWorldMatrix,
                worldToObject = renderer.transform.worldToLocalMatrix
            });
            batch.IsDirty = true;
            renderer.enabled = false;
        }

        public void RemoveRenderer(MeshRenderer renderer)
        {
            if (renderer == null) return;
            MeshFilter mf = renderer.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;

            int key = Hash(mf.sharedMesh, renderer.sharedMaterial);
            if (_batchMap.TryGetValue(key, out RenderBatch batch))
            {
                Matrix4x4 matrix = renderer.transform.localToWorldMatrix;
                int index = batch.CPUData.FindIndex(x => x.objectToWorld == matrix);
                if (index != -1)
                {
                    int lastIndex = batch.CPUData.Count - 1;
                    batch.CPUData[index] = batch.CPUData[lastIndex];
                    batch.CPUData.RemoveAt(lastIndex);
                    batch.IsDirty = true;
                    if (batch.CPUData.Count == 0)
                    {
                        ReleaseBatch(batch);
                        _batchMap.Remove(key);
                        _activeBatches.Remove(batch);
                    }
                }
            }
            renderer.enabled = true;
        }

        [ContextMenu("Rebuild Batches")]
        public void Rebuild()
        {
            ReleaseResources();
            if (_targetRoot == null) _targetRoot = transform;
            if (_cullingCompute == null) return;

            MeshRenderer[] renderers = _targetRoot.GetComponentsInChildren<MeshRenderer>(true);
            _batchMap.EnsureCapacity(renderers.Length / 10);
            foreach (var r in renderers) RegisterRenderer(r);
            SyncBuffersToGPU();
        }

        private void LateUpdate()
        {
            SyncBuffersToGPU();
        }

        private void SyncBuffersToGPU()
        {
            foreach (var batch in _activeBatches)
            {
                if (batch.IsDirty && batch.CPUData.Count > 0)
                {
                    EnsureBufferCapacity(batch, batch.CPUData.Count);
                    batch.AllDataBuffer.SetData(batch.CPUData);
                    batch.IsDirty = false;
                }
            }
            if ((_trashBuffer == null || !_trashBuffer.IsValid()) && _activeBatches.Count > 0)
            {
                _trashBuffer = new ComputeBuffer(1, Marshal.SizeOf<IndirectData>(), ComputeBufferType.Append);
            }
        }

        private void EnsureBufferCapacity(RenderBatch batch, int requiredCount)
        {
            if (batch.AllDataBuffer == null || batch.Capacity < requiredCount)
            {
                int newCapacity = Mathf.NextPowerOfTwo(Mathf.Max(requiredCount, 64));
                if (batch.AllDataBuffer != null) batch.AllDataBuffer.Release();
                if (batch.VisibleDataBuffer != null) batch.VisibleDataBuffer.Release();

                batch.AllDataBuffer = new ComputeBuffer(newCapacity, Marshal.SizeOf<IndirectData>());
                batch.VisibleDataBuffer = new ComputeBuffer(newCapacity, Marshal.SizeOf<IndirectData>(), ComputeBufferType.Append);
                batch.Capacity = newCapacity;
            }
        }

        private RenderBatch CreateNewBatch(Mesh mesh, Material mat)
        {
            RenderBatch batch = new RenderBatch
            {
                Mesh = mesh,
                Material = mat,
                Props = new MaterialPropertyBlock(),
                BoundsSize = mesh.bounds.extents.magnitude,
                Capacity = 0,
                IsDirty = true,
                MaskPassIndex = mat.FindPass("SelectionMask")
            };

            batch.ArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            _argsData[0] = (uint)mesh.GetIndexCount(0);
            _argsData[1] = 0;
            _argsData[2] = (uint)mesh.GetIndexStart(0);
            _argsData[3] = (uint)mesh.GetBaseVertex(0);
            batch.ArgsBuffer.SetData(_argsData);
            return batch;
        }

        private int Hash(Mesh mesh, Material mat)
        {
            int h1 = mesh.GetInstanceID();
            int h2 = mat.GetInstanceID();
            return ((h1 << 5) + h1) ^ h2;
        }

        // Callback from OutlineFeature to draw into Mask
        private void OnRenderMask(RasterCommandBuffer cmd, LayerMask mask)
        {
            if (((1 << gameObject.layer) & mask) == 0) return;

            foreach (var batch in _activeBatches)
            {
                if (batch.MaskPassIndex != -1 && batch.VisibleDataBuffer != null)
                {
                    batch.Props.SetBuffer(ID_IndirectInstanceData, batch.VisibleDataBuffer);
                    cmd.DrawMeshInstancedIndirect(batch.Mesh, 0, batch.Material, batch.MaskPassIndex, batch.ArgsBuffer, 0, batch.Props);
                }
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (_activeBatches.Count == 0 || _cullingCompute == null) return;
#if UNITY_EDITOR
            if (cam.cameraType == CameraType.Preview) return;
#endif
            GeometryUtility.CalculateFrustumPlanes(cam, _cameraPlanes);
            for (int i = 0; i < 6; i++)
            {
                Vector3 n = _cameraPlanes[i].normal;
                _planeNormals[i] = new Vector4(n.x, n.y, n.z, _cameraPlanes[i].distance);
            }

            int kernel = _cullingCompute.FindKernel("CSMain");
            _cullingCompute.SetVector(ID_CameraPosition, cam.transform.position);
            _cullingCompute.SetVectorArray(ID_FrustumPlanes, _planeNormals);
            _cullingCompute.SetVector(ID_LODDistances, new Vector4(_cullingDistance, _cullingDistance, _cullingDistance, 0));

            if (_trashBuffer == null || !_trashBuffer.IsValid())
                _trashBuffer = new ComputeBuffer(1, Marshal.SizeOf<IndirectData>(), ComputeBufferType.Append);

            foreach (var batch in _activeBatches)
            {
                int count = batch.CPUData.Count;
                if (count == 0) continue;

                batch.VisibleDataBuffer.SetCounterValue(0);
                _trashBuffer.SetCounterValue(0);

                _cullingCompute.SetInt(ID_InstanceCount, count);
                _cullingCompute.SetFloat(ID_CullingBound, batch.BoundsSize);
                _cullingCompute.SetBuffer(kernel, ID_AllInstances, batch.AllDataBuffer);
                _cullingCompute.SetBuffer(kernel, ID_LOD0_Instances, batch.VisibleDataBuffer);
                _cullingCompute.SetBuffer(kernel, ID_LOD1_Instances, _trashBuffer);
                _cullingCompute.SetBuffer(kernel, ID_LOD2_Instances, _trashBuffer);

                int groups = Mathf.CeilToInt(count / (float)GROUP_SIZE);
                _cullingCompute.Dispatch(kernel, groups, 1, 1);
                ComputeBuffer.CopyCount(batch.VisibleDataBuffer, batch.ArgsBuffer, 4);
                batch.Props.SetBuffer(ID_IndirectInstanceData, batch.VisibleDataBuffer);

                Graphics.DrawMeshInstancedIndirect(
                    batch.Mesh, 0, batch.Material,
                    new Bounds(Vector3.zero, Vector3.one * 10000f),
                    batch.ArgsBuffer, 0, batch.Props,
                    ShadowCastingMode.On, true, gameObject.layer, cam
                );
            }
        }

        private void ReleaseBatch(RenderBatch batch)
        {
            if (batch.AllDataBuffer != null) batch.AllDataBuffer.Release();
            if (batch.VisibleDataBuffer != null) batch.VisibleDataBuffer.Release();
            if (batch.ArgsBuffer != null) batch.ArgsBuffer.Release();
        }

        private void ReleaseResources()
        {
            if (_trashBuffer != null) { _trashBuffer.Release(); _trashBuffer = null; }
            foreach (var batch in _activeBatches) ReleaseBatch(batch);
            _activeBatches.Clear();
            _batchMap.Clear();
        }
    }
}