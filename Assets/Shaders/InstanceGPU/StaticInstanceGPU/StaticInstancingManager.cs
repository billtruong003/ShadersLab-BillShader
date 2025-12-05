using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace OptimizeStatic
{
    [DefaultExecutionOrder(-100)]
    public class StaticInstancingManager : MonoBehaviour
    {
        [System.Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct CompressedInstanceData
        {
            public Vector3 position;
            public Vector3 scale;
            public Vector4 rotation;
            public Vector2 lodRange;
        }

        [System.Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct BoundsData
        {
            public Vector3 center;
            public Vector3 extents;
        }

        [System.Serializable]
        public class StaticBatch
        {
            [ReadOnly] public string id;
            [Required] public Mesh mesh;
            [Required] public Material material;
            [HideInInspector] public CompressedInstanceData[] cpuData;
            [HideInInspector] public BoundsData[] cpuBounds;
            [SerializeField, HideInInspector] public List<MeshRenderer> originalRenderers = new List<MeshRenderer>();

            public ComputeBuffer sourceBuffer;
            public ComputeBuffer boundsBuffer;

            public ComputeBuffer visibleIndexBuffer;
            public ComputeBuffer visibleArgsBuffer;

            public ComputeBuffer shadowIndexBuffer;
            public ComputeBuffer shadowArgsBuffer;

            public MaterialPropertyBlock props;
            public int count;
            public Bounds globalBounds;

            public void Initialize()
            {
                count = cpuData.Length;
                if (count == 0) return;

                sourceBuffer = new ComputeBuffer(count, Marshal.SizeOf<CompressedInstanceData>());
                sourceBuffer.SetData(cpuData);

                boundsBuffer = new ComputeBuffer(count, Marshal.SizeOf<BoundsData>());
                boundsBuffer.SetData(cpuBounds);

                visibleIndexBuffer = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.Append);
                shadowIndexBuffer = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.Append);

                visibleArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
                shadowArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

                uint[] args = new uint[5] { (uint)mesh.GetIndexCount(0), 0, (uint)mesh.GetIndexStart(0), (uint)mesh.GetBaseVertex(0), 0 };
                visibleArgsBuffer.SetData(args);
                shadowArgsBuffer.SetData(args);

                props = new MaterialPropertyBlock();
                props.SetBuffer(ShaderIDs.SourceData, sourceBuffer);

                Vector3 min = Vector3.one * float.MaxValue;
                Vector3 max = Vector3.one * float.MinValue;
                for (int i = 0; i < cpuBounds.Length; i++)
                {
                    Vector3 c = cpuBounds[i].center;
                    Vector3 e = cpuBounds[i].extents;
                    min = Vector3.Min(min, c - e);
                    max = Vector3.Max(max, c + e);
                }
                globalBounds = new Bounds((min + max) * 0.5f, max - min);
            }

            public void Release()
            {
                sourceBuffer?.Release();
                boundsBuffer?.Release();
                visibleIndexBuffer?.Release();
                shadowIndexBuffer?.Release();
                visibleArgsBuffer?.Release();
                shadowArgsBuffer?.Release();
            }
        }

        [Title("Core")]
        [SerializeField, Required] private ComputeShader _cullingShader;

        [Title("Optimization")]
        [SerializeField, Range(0f, 0.1f)] private float _cullingInterval = 0.03f;
        [SerializeField] private float _moveThreshold = 0.1f;
        [SerializeField] private float _angleThreshold = 1.0f;

        [Title("Settings")]
        [SerializeField] private float _cullDistance = 500f;
        [SerializeField] private float _lodBias = 1.0f;
        [SerializeField] private int _minInstancesPerBatch = 32;
        [SerializeField] private LayerMask _staticLayer = 1;
        [SerializeField, InfoBox("Add shaders here to force them to be baked.")]
        private List<Shader> _allowedShaders = new List<Shader>();

        [Title("Data")]
        [SerializeField, ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "id")]
        private List<StaticBatch> _batches = new List<StaticBatch>();

        private Camera _mainCamera;
        private Transform _camTransform;
        private int _kernelID;
        private readonly Plane[] _cameraPlanes = new Plane[6];
        private readonly Vector4[] _frustumPlanesV4 = new Vector4[6];
        private bool _isInitialized;

        private Vector3 _lastPos;
        private Quaternion _lastRot;
        private float _lastCullTime;

        private static class ShaderIDs
        {
            public static readonly int SourceData = Shader.PropertyToID("_SourceData");
            public static readonly int SourceBounds = Shader.PropertyToID("_SourceBounds");
            public static readonly int VisibleIndices = Shader.PropertyToID("_VisibleIndices");
            public static readonly int ShadowIndices = Shader.PropertyToID("_ShadowIndices");
            public static readonly int CameraPlanes = Shader.PropertyToID("_CameraPlanes");
            public static readonly int CameraPosition = Shader.PropertyToID("_CameraPosition");
            public static readonly int MaxDistanceSq = Shader.PropertyToID("_MaxDistanceSq");
            public static readonly int ShadowDistanceSq = Shader.PropertyToID("_ShadowDistanceSq");
            public static readonly int Count = Shader.PropertyToID("_Count");
        }

        [Button]
        private void ForceHighLodForAll()
        {
            foreach (var batch in _batches)
            {
                for (int i = 0; i < batch.cpuData.Length; i++)
                {
                    batch.cpuData[i].lodRange.y = 1000000f * 1000000f; // gần như vô hạn
                }
                if (batch.sourceBuffer != null)
                {
                    batch.sourceBuffer.SetData(batch.cpuData);
                }
            }
        }

        [Button(ButtonSizes.Large), GUIColor(0f, 1f, 1f)]
        private void BakeStaticGeometry()
        {
            ClearBake();
            var grouping = new Dictionary<int, List<BakeData>>();
            var allowedShaderSet = new HashSet<Shader>(_allowedShaders);
            var processedRenderers = new HashSet<Renderer>();

            var lodGroups = FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
            foreach (var lodGroup in lodGroups)
            {
                if (((1 << lodGroup.gameObject.layer) & _staticLayer) == 0) continue;
                if (!lodGroup.enabled) continue;

                var lods = lodGroup.GetLODs();
                float size = lodGroup.size > 0 ? lodGroup.size : 1f;
                float previousMaxDist = 0f;

                for (int i = 0; i < lods.Length; i++)
                {
                    var lod = lods[i];
                    float transitionHeight = lod.screenRelativeTransitionHeight;
                    float maxDist = (transitionHeight > 0) ? (size / transitionHeight) * _lodBias : _cullDistance;
                    if (i == lods.Length - 1) maxDist = _cullDistance;

                    float minDistSq = previousMaxDist * previousMaxDist;
                    float maxDistSq = maxDist * maxDist;

                    foreach (var r in lod.renderers)
                    {
                        if (r == null || !(r is MeshRenderer mr)) continue;
                        if (!ShouldBake(mr, allowedShaderSet)) continue;
                        AddToGrouping(grouping, mr, minDistSq, maxDistSq);
                        processedRenderers.Add(mr);
                    }
                    previousMaxDist = maxDist;
                }
            }

            var renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                if (processedRenderers.Contains(r)) continue;
                if (((1 << r.gameObject.layer) & _staticLayer) == 0) continue;
                if (!ShouldBake(r, allowedShaderSet)) continue;
                AddToGrouping(grouping, r, 0, _cullDistance * _cullDistance);
            }

            foreach (var kvp in grouping)
            {
                var list = kvp.Value;
                if (list.Count == 0) continue;

                if (list.Count < _minInstancesPerBatch)
                {
                    foreach (var item in list)
                    {
                        item.renderer.enabled = true;
                        item.renderer.forceRenderingOff = false;
                    }
                    continue;
                }

                var first = list[0].renderer;
                var mesh = first.GetComponent<MeshFilter>().sharedMesh;
                var batch = new StaticBatch
                {
                    id = $"{first.name}_Count_{list.Count}",
                    mesh = mesh,
                    material = first.sharedMaterial,
                    cpuData = new CompressedInstanceData[list.Count],
                    cpuBounds = new BoundsData[list.Count],
                    originalRenderers = new List<MeshRenderer>(list.Count)
                };

                for (int i = 0; i < list.Count; i++)
                {
                    var data = list[i];
                    var t = data.renderer.transform;
                    batch.originalRenderers.Add(data.renderer);
                    batch.cpuData[i] = new CompressedInstanceData
                    {
                        position = t.position,
                        rotation = new Vector4(t.rotation.x, t.rotation.y, t.rotation.z, t.rotation.w),
                        scale = t.lossyScale,
                        lodRange = new Vector2(data.minDistSq, data.maxDistSq)
                    };
                    var b = data.renderer.bounds;
                    batch.cpuBounds[i] = new BoundsData
                    {
                        center = b.center,
                        extents = b.extents
                    };
                    data.renderer.enabled = false;
                }
                _batches.Add(batch);
            }

            _batches.Sort((a, b) =>
            {
                int matA = a.material != null ? a.material.GetInstanceID() : 0;
                int matB = b.material != null ? b.material.GetInstanceID() : 0;
                return matA.CompareTo(matB);
            });

            _isInitialized = false;
            InitializeGPU();
        }

        private struct BakeData
        {
            public MeshRenderer renderer;
            public float minDistSq;
            public float maxDistSq;
        }

        private bool ShouldBake(MeshRenderer r, HashSet<Shader> allowed)
        {
            if (!r.enabled) return false;
            var filter = r.GetComponent<MeshFilter>();
            if (!filter || !filter.sharedMesh || !r.sharedMaterial) return false;
            Shader s = r.sharedMaterial.shader;
            bool isAllowed = allowed.Contains(s);
            bool hasTag = r.sharedMaterial.GetTag("StaticInstancing", false, "") == "True";
            return isAllowed || hasTag;
        }

        private void AddToGrouping(Dictionary<int, List<BakeData>> grouping, MeshRenderer r, float minSq, float maxSq)
        {
            int key = r.GetComponent<MeshFilter>().sharedMesh.GetInstanceID() ^ r.sharedMaterial.GetInstanceID();
            if (!grouping.ContainsKey(key)) grouping[key] = new List<BakeData>();
            grouping[key].Add(new BakeData
            {
                renderer = r,
                minDistSq = minSq,
                maxDistSq = maxSq
            });
        }

        [Button(ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
        private void ClearBake()
        {
            if (_batches != null)
            {
                foreach (var batch in _batches)
                {
                    if (batch.originalRenderers != null)
                    {
                        foreach (var r in batch.originalRenderers)
                        {
                            if (r) r.enabled = true;
                        }
                    }
                    batch.Release();
                }
                _batches.Clear();
            }
            _isInitialized = false;
        }

        [Button(ButtonSizes.Medium), GUIColor(1f, 0.8f, 0.3f)]
        private void CleanLOD_RemoveLODGroupComponent()
        {
            LODGroup[] allLodGroups = FindObjectsByType<LODGroup>(FindObjectsSortMode.None);

            int count = 0;
            foreach (LODGroup lodGroup in allLodGroups)
            {
                if (((1 << lodGroup.gameObject.layer) & _staticLayer.value) == 0)
                    continue;

                // Force enable renderers của LOD0 trước khi xóa (đề phòng)
                LOD[] lods = lodGroup.GetLODs();
                if (lods.Length > 0)
                {
                    foreach (Renderer r in lods[0].renderers)
                        if (r) r.enabled = true;
                }

                DestroyImmediate(lodGroup);
                count++;
            }

            Debug.Log($"[StaticInstancingManager] Removed {count} LODGroup components – now always full detail.");
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera) _camTransform = _mainCamera.transform;
            InitializeGPU();
            PerformCulling();
        }

        private void InitializeGPU()
        {
            if (_isInitialized || _batches.Count == 0) return;
            _kernelID = _cullingShader.FindKernel("CSMain");
            foreach (var batch in _batches) batch.Initialize();
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized) return;
            if (!_mainCamera) { _mainCamera = Camera.main; if (_mainCamera) _camTransform = _mainCamera.transform; else return; }

            if (ShouldUpdateCulling())
            {
                PerformCulling();
            }

            PerformDrawing();
        }

        private bool ShouldUpdateCulling()
        {
            if (Time.time - _lastCullTime < _cullingInterval) return false;

            Vector3 currentPos = _camTransform.position;
            Quaternion currentRot = _camTransform.rotation;

            bool isDirty = Vector3.Distance(currentPos, _lastPos) > _moveThreshold ||
                           Quaternion.Angle(currentRot, _lastRot) > _angleThreshold;

            return isDirty;
        }

        private void PerformCulling()
        {
            UpdateFrustumPlanes();
            float shadowDist = _cullDistance * 0.5f;
            float maxDist = Mathf.Max(_cullDistance, shadowDist);

            _cullingShader.SetVectorArray(ShaderIDs.CameraPlanes, _frustumPlanesV4);
            _cullingShader.SetVector(ShaderIDs.CameraPosition, _camTransform.position);
            _cullingShader.SetFloat(ShaderIDs.MaxDistanceSq, maxDist * maxDist);
            _cullingShader.SetFloat(ShaderIDs.ShadowDistanceSq, shadowDist * shadowDist);

            for (int i = 0; i < _batches.Count; i++)
            {
                var batch = _batches[i];
                if (batch.count == 0) continue;

                batch.visibleIndexBuffer.SetCounterValue(0);
                batch.shadowIndexBuffer.SetCounterValue(0);

                _cullingShader.SetInt(ShaderIDs.Count, batch.count);
                _cullingShader.SetBuffer(_kernelID, ShaderIDs.SourceBounds, batch.boundsBuffer);
                _cullingShader.SetBuffer(_kernelID, ShaderIDs.SourceData, batch.sourceBuffer);
                _cullingShader.SetBuffer(_kernelID, ShaderIDs.VisibleIndices, batch.visibleIndexBuffer);
                _cullingShader.SetBuffer(_kernelID, ShaderIDs.ShadowIndices, batch.shadowIndexBuffer);

                int threadGroups = Mathf.CeilToInt(batch.count / 64f);
                _cullingShader.Dispatch(_kernelID, threadGroups, 1, 1);

                ComputeBuffer.CopyCount(batch.visibleIndexBuffer, batch.visibleArgsBuffer, 4);
                ComputeBuffer.CopyCount(batch.shadowIndexBuffer, batch.shadowArgsBuffer, 4);
            }

            _lastPos = _camTransform.position;
            _lastRot = _camTransform.rotation;
            _lastCullTime = Time.time;
        }

        private void PerformDrawing()
        {
            for (int i = 0; i < _batches.Count; i++)
            {
                var batch = _batches[i];
                if (batch.count == 0) continue;

                batch.props.SetBuffer(ShaderIDs.VisibleIndices, batch.visibleIndexBuffer);
                Graphics.DrawMeshInstancedIndirect(
                    batch.mesh,
                    0,
                    batch.material,
                    batch.globalBounds,
                    batch.visibleArgsBuffer,
                    0,
                    batch.props,
                    ShadowCastingMode.TwoSided,
                    true
                );

                batch.props.SetBuffer(ShaderIDs.VisibleIndices, batch.shadowIndexBuffer);
                Graphics.DrawMeshInstancedIndirect(
                    batch.mesh,
                    0,
                    batch.material,
                    batch.globalBounds,
                    batch.shadowArgsBuffer,
                    0,
                    batch.props,
                    ShadowCastingMode.ShadowsOnly,
                    true
                );
            }
        }

        private void UpdateFrustumPlanes()
        {
            if (XRSettings.enabled)
            {
                Matrix4x4 leftProj = _mainCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                Matrix4x4 leftView = _mainCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
                Matrix4x4 rightProj = _mainCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
                Matrix4x4 rightView = _mainCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);

                Plane[] leftPlanes = GeometryUtility.CalculateFrustumPlanes(leftProj * leftView);
                Plane[] rightPlanes = GeometryUtility.CalculateFrustumPlanes(rightProj * rightView);

                for (int i = 0; i < 6; i++)
                {
                    Vector3 lNormal = leftPlanes[i].normal;
                    Vector3 rNormal = rightPlanes[i].normal;

                    if (Vector3.Dot(lNormal, rNormal) > 0)
                    {
                        if (leftPlanes[i].distance > rightPlanes[i].distance)
                            _cameraPlanes[i] = leftPlanes[i];
                        else
                            _cameraPlanes[i] = rightPlanes[i];
                    }
                    else
                    {
                        _cameraPlanes[i] = leftPlanes[i];
                    }
                    var n = _cameraPlanes[i].normal;
                    _frustumPlanesV4[i] = new Vector4(n.x, n.y, n.z, _cameraPlanes[i].distance);
                }
            }
            else
            {
                GeometryUtility.CalculateFrustumPlanes(_mainCamera, _cameraPlanes);
                for (int i = 0; i < 6; i++)
                {
                    var n = _cameraPlanes[i].normal;
                    _frustumPlanesV4[i] = new Vector4(n.x, n.y, n.z, _cameraPlanes[i].distance);
                }
            }
        }

        private void OnDestroy() => ReleaseBuffers();

        private void ReleaseBuffers()
        {
            if (_batches == null) return;
            foreach (var batch in _batches) batch.Release();
            _isInitialized = false;
        }
    }
}