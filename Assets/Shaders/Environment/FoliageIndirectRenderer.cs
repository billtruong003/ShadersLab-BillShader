using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace CleanCode.EnvironmentTools
{
    public class FoliageManager : MonoBehaviour
    {
        // Global list to allow RenderPasses to find active managers
        public static readonly List<FoliageManager> ActiveManagers = new List<FoliageManager>();

        private struct IndirectData
        {
            public Matrix4x4 objectToWorld;
            public Matrix4x4 worldToObject;
        }

        private class FoliageTypeBatch
        {
            public string ID;
            public Mesh[] Meshes = new Mesh[3];
            public Material Material;
            public List<IndirectData> Instances = new List<IndirectData>();
            public ComputeBuffer AllInstancesBuffer;
            public ComputeBuffer[] ArgsBuffers = new ComputeBuffer[3];
            public ComputeBuffer[] VisibleBuffers = new ComputeBuffer[3];
            public MaterialPropertyBlock MatProps;
            public float BoundingRadius;
            public int PassIndex;
        }

        [Header("Settings")]
        public ComputeShader cullingCompute;
        public bool bakeOnStart = true;
        public float lod0Distance = 30f;
        public float lod1Distance = 60f;
        public float lod2Distance = 100f;

        private List<FoliageTypeBatch> _batches = new List<FoliageTypeBatch>();
        private Plane[] _cameraPlanes = new Plane[6];
        private Vector4[] _planeNormals = new Vector4[6];
        private ComputeBuffer _trashBuffer;

        private static readonly int ID_CameraPosition = Shader.PropertyToID("_CameraPosition");
        private static readonly int ID_LODDistances = Shader.PropertyToID("_LODDistances");
        private static readonly int ID_FrustumPlanes = Shader.PropertyToID("_FrustumPlanes");
        private static readonly int ID_InstanceCount = Shader.PropertyToID("_InstanceCount");
        private static readonly int ID_CullingBound = Shader.PropertyToID("_CullingBound");
        private static readonly int ID_AllInstances = Shader.PropertyToID("_AllInstances");
        private static readonly int ID_LOD0_Instances = Shader.PropertyToID("_LOD0_Instances");
        private static readonly int ID_LOD1_Instances = Shader.PropertyToID("_LOD1_Instances");
        private static readonly int ID_LOD2_Instances = Shader.PropertyToID("_LOD2_Instances");
        private static readonly int ID_IndirectInstanceData = Shader.PropertyToID("_IndirectInstanceData");

        private void OnEnable()
        {
            if (!ActiveManagers.Contains(this))
                ActiveManagers.Add(this);
        }

        private void Start()
        {
            if (bakeOnStart) Bake();
        }

        private void OnDisable()
        {
            ActiveManagers.Remove(this);
            Cleanup();
        }

        [ContextMenu("Scan & Bake All Children")]
        public void Bake()
        {
            Cleanup();
            _batches.Clear();
            _trashBuffer = new ComputeBuffer(1, Marshal.SizeOf<IndirectData>(), ComputeBufferType.Append);

            Dictionary<string, FoliageTypeBatch> batchMap = new Dictionary<string, FoliageTypeBatch>();

            // Process LODGroups
            var lodGroups = GetComponentsInChildren<LODGroup>();
            foreach (var group in lodGroups)
            {
                ProcessLODGroup(group, batchMap);
                group.gameObject.SetActive(false);
            }

            // Process Single MeshRenderers
            var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                if (r.gameObject.activeSelf)
                {
                    ProcessSingleRenderer(r, batchMap);
                    r.enabled = false;
                }
            }

            foreach (var batch in batchMap.Values)
            {
                if (batch.Instances.Count == 0) continue;
                SetupBatchBuffers(batch);
                _batches.Add(batch);
            }
        }

        private void ProcessLODGroup(LODGroup group, Dictionary<string, FoliageTypeBatch> map)
        {
            LOD[] lods = group.GetLODs();
            if (lods.Length == 0) return;

            Renderer r0 = lods[0].renderers.FirstOrDefault();
            if (r0 == null || !(r0 is MeshRenderer)) return;

            MeshFilter mf0 = r0.GetComponent<MeshFilter>();
            if (mf0 == null || mf0.sharedMesh == null) return;

            int meshID = mf0.sharedMesh.GetInstanceID();
            int matID = r0.sharedMaterial.GetInstanceID();
            string key = $"{meshID}_{matID}";

            if (!map.TryGetValue(key, out FoliageTypeBatch batch))
            {
                batch = new FoliageTypeBatch
                {
                    ID = r0.gameObject.name,
                    Material = r0.sharedMaterial,
                    MatProps = new MaterialPropertyBlock(),
                    BoundingRadius = mf0.sharedMesh.bounds.extents.magnitude,
                    PassIndex = r0.sharedMaterial.FindPass("FoliageForward") // Adjusted to match shader tags
                };
                if (batch.PassIndex == -1) batch.PassIndex = r0.sharedMaterial.FindPass("ForwardLit");

                for (int i = 0; i < 3; i++)
                {
                    if (i < lods.Length && lods[i].renderers.Length > 0)
                    {
                        var mr = lods[i].renderers[0].GetComponent<MeshFilter>();
                        if (mr != null) batch.Meshes[i] = mr.sharedMesh;
                    }
                }
                map.Add(key, batch);
            }

            batch.Instances.Add(new IndirectData
            {
                objectToWorld = group.transform.localToWorldMatrix,
                worldToObject = group.transform.worldToLocalMatrix
            });
        }

        private void ProcessSingleRenderer(MeshRenderer r, Dictionary<string, FoliageTypeBatch> map)
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;

            int meshID = mf.sharedMesh.GetInstanceID();
            int matID = r.sharedMaterial.GetInstanceID();
            string key = $"{meshID}_{matID}";

            if (!map.TryGetValue(key, out FoliageTypeBatch batch))
            {
                batch = new FoliageTypeBatch
                {
                    ID = r.gameObject.name,
                    Material = r.sharedMaterial,
                    MatProps = new MaterialPropertyBlock(),
                    BoundingRadius = mf.sharedMesh.bounds.extents.magnitude,
                    PassIndex = r.sharedMaterial.FindPass("FoliageForward")
                };
                if (batch.PassIndex == -1) batch.PassIndex = r.sharedMaterial.FindPass("ForwardLit");

                batch.Meshes[0] = mf.sharedMesh;
                map.Add(key, batch);
            }

            batch.Instances.Add(new IndirectData
            {
                objectToWorld = r.transform.localToWorldMatrix,
                worldToObject = r.transform.worldToLocalMatrix
            });
        }

        private void SetupBatchBuffers(FoliageTypeBatch batch)
        {
            int count = batch.Instances.Count;
            batch.AllInstancesBuffer = new ComputeBuffer(count, Marshal.SizeOf<IndirectData>());
            batch.AllInstancesBuffer.SetData(batch.Instances);

            for (int i = 0; i < 3; i++)
            {
                if (batch.Meshes[i] != null)
                {
                    batch.VisibleBuffers[i] = new ComputeBuffer(count, Marshal.SizeOf<IndirectData>(), ComputeBufferType.Append);
                    batch.ArgsBuffers[i] = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
                    uint[] args = new uint[5] { (uint)batch.Meshes[i].GetIndexCount(0), 0, (uint)batch.Meshes[i].GetIndexStart(0), (uint)batch.Meshes[i].GetBaseVertex(0), 0 };
                    batch.ArgsBuffers[i].SetData(args);
                }
            }
        }

        private void Update()
        {
            if (_batches.Count == 0 || cullingCompute == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            GeometryUtility.CalculateFrustumPlanes(cam, _cameraPlanes);
            for (int i = 0; i < 6; i++)
            {
                Vector3 normal = _cameraPlanes[i].normal;
                _planeNormals[i] = new Vector4(normal.x, normal.y, normal.z, _cameraPlanes[i].distance);
            }

            int kernel = cullingCompute.FindKernel("CSMain");
            cullingCompute.SetVector(ID_CameraPosition, cam.transform.position);
            cullingCompute.SetVector(ID_LODDistances, new Vector4(lod0Distance, lod1Distance, lod2Distance, 0));
            cullingCompute.SetVectorArray(ID_FrustumPlanes, _planeNormals);

            foreach (var batch in _batches)
            {
                PerformCulling(batch, kernel);
            }
        }

        private void PerformCulling(FoliageTypeBatch batch, int kernel)
        {
            for (int i = 0; i < 3; i++)
            {
                if (batch.VisibleBuffers[i] != null)
                    batch.VisibleBuffers[i].SetCounterValue(0);
            }

            cullingCompute.SetInt(ID_InstanceCount, batch.Instances.Count);
            cullingCompute.SetFloat(ID_CullingBound, batch.BoundingRadius);
            cullingCompute.SetBuffer(kernel, ID_AllInstances, batch.AllInstancesBuffer);

            _trashBuffer.SetCounterValue(0);
            cullingCompute.SetBuffer(kernel, ID_LOD0_Instances, batch.VisibleBuffers[0] ?? _trashBuffer);
            cullingCompute.SetBuffer(kernel, ID_LOD1_Instances, batch.VisibleBuffers[1] ?? _trashBuffer);
            cullingCompute.SetBuffer(kernel, ID_LOD2_Instances, batch.VisibleBuffers[2] ?? _trashBuffer);

            int threadGroups = Mathf.CeilToInt(batch.Instances.Count / 64f);
            cullingCompute.Dispatch(kernel, threadGroups, 1, 1);

            for (int i = 0; i < 3; i++)
            {
                if (batch.Meshes[i] != null && batch.VisibleBuffers[i] != null)
                {
                    ComputeBuffer.CopyCount(batch.VisibleBuffers[i], batch.ArgsBuffers[i], 4);
                }
            }
        }

        // Standard CommandBuffer support (for Built-in or older URP contexts if needed)
        public void RenderFoliage(CommandBuffer cmd)
        {
            foreach (var batch in _batches)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (batch.Meshes[i] != null && batch.VisibleBuffers[i] != null)
                    {
                        batch.MatProps.SetBuffer(ID_IndirectInstanceData, batch.VisibleBuffers[i]);
                        int pass = batch.PassIndex != -1 ? batch.PassIndex : 0;

                        cmd.DrawMeshInstancedIndirect(
                            batch.Meshes[i],
                            0,
                            batch.Material,
                            pass,
                            batch.ArgsBuffers[i],
                            0,
                            batch.MatProps
                        );
                    }
                }
            }
        }

        // RenderGraph RasterCommandBuffer support (Fixes CS1503)
        public void RenderFoliage(RasterCommandBuffer cmd)
        {
            foreach (var batch in _batches)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (batch.Meshes[i] != null && batch.VisibleBuffers[i] != null)
                    {
                        batch.MatProps.SetBuffer(ID_IndirectInstanceData, batch.VisibleBuffers[i]);
                        int pass = batch.PassIndex != -1 ? batch.PassIndex : 0;

                        cmd.DrawMeshInstancedIndirect(
                            batch.Meshes[i],
                            0,
                            batch.Material,
                            pass,
                            batch.ArgsBuffers[i],
                            0,
                            batch.MatProps
                        );
                    }
                }
            }
        }

        private void Cleanup()
        {
            if (_trashBuffer != null) { _trashBuffer.Release(); _trashBuffer = null; }
            foreach (var batch in _batches)
            {
                if (batch.AllInstancesBuffer != null) batch.AllInstancesBuffer.Release();
                for (int i = 0; i < 3; i++)
                {
                    if (batch.ArgsBuffers[i] != null) batch.ArgsBuffers[i].Release();
                    if (batch.VisibleBuffers[i] != null) batch.VisibleBuffers[i].Release();
                }
            }
            _batches.Clear();
        }
    }
}