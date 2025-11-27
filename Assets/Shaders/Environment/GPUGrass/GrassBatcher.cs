using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;

namespace CleanCode.Grass
{
    public class GrassBatcher : MonoBehaviour
    {
        #region Structures
        [System.Serializable]
        public struct SerializedMatrix
        {
            public Vector4 c0, c1, c2, c3;
            public Matrix4x4 ToMatrix() => new Matrix4x4(c0, c1, c2, c3);
            public static SerializedMatrix FromMatrix(Matrix4x4 m) => new SerializedMatrix { c0 = m.GetColumn(0), c1 = m.GetColumn(1), c2 = m.GetColumn(2), c3 = m.GetColumn(3) };
        }

        [System.Serializable]
        public class BatchData
        {
            [ReadOnly] public Mesh mesh;
            [ReadOnly] public Material material;
            [ReadOnly] public float boundRadius;
            [HideInInspector] public List<SerializedMatrix> instances = new List<SerializedMatrix>();

            // Runtime Buffers
            public ComputeBuffer allInstancesBuffer;
            public ComputeBuffer visibleInstancesBuffer;
            public ComputeBuffer argsBuffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IndirectData
        {
            public Matrix4x4 objectToWorld;
        }
        #endregion

        [Title("GPU Settings")]
        [SerializeField, Required] private ComputeShader _cullingShader;
        [SerializeField] private float _cullDistance = 150f;
        [SerializeField] private float _boundsPadding = 1.0f;

        [Title("Data")]
        [SerializeField, ReadOnly, ListDrawerSettings(Expanded = true)]
        private List<BatchData> _batches = new List<BatchData>();

        private Camera _mainCam;
        private int _kernelID;
        private readonly Plane[] _planes = new Plane[6];
        private readonly Vector4[] _planeNormals = new Vector4[6];
        private readonly uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };

        private static readonly int ID_AllInstances = Shader.PropertyToID("_AllInstances");
        private static readonly int ID_VisibleInstances = Shader.PropertyToID("_VisibleInstances");
        private static readonly int ID_CameraPlanes = Shader.PropertyToID("_CameraPlanes");
        private static readonly int ID_CameraPosition = Shader.PropertyToID("_CameraPosition");
        private static readonly int ID_MaxDistance = Shader.PropertyToID("_MaxDistance");
        private static readonly int ID_InstanceCount = Shader.PropertyToID("_InstanceCount");
        private static readonly int ID_BoundRadius = Shader.PropertyToID("_BoundRadius");
        private static readonly int ID_VisibleInstanceData = Shader.PropertyToID("_VisibleInstanceData");

        [Button(ButtonSizes.Large), GUIColor(0.4f, 1f, 0.4f)]
        private void BakeAndDisableChildren()
        {
            _batches.Clear();
            var renderers = GetComponentsInChildren<MeshRenderer>(true);
            var grouped = new Dictionary<int, BatchData>();

            foreach (var r in renderers)
            {
                if (r.gameObject == this.gameObject) continue;
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                int key = CombineHash(mf.sharedMesh.GetInstanceID(), r.sharedMaterial.GetInstanceID());

                if (!grouped.TryGetValue(key, out BatchData batch))
                {
                    batch = new BatchData
                    {
                        mesh = mf.sharedMesh,
                        material = r.sharedMaterial,
                        boundRadius = mf.sharedMesh.bounds.extents.magnitude
                    };
                    grouped.Add(key, batch);
                    _batches.Add(batch);
                }

                batch.instances.Add(SerializedMatrix.FromMatrix(r.transform.localToWorldMatrix));
                r.gameObject.SetActive(false);
            }

            Debug.Log($"Baked {renderers.Length} objects into {_batches.Count} batches.");
        }

        [Button(ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.5f)]
        private void EnableChildren()
        {
            var renderers = GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers) r.gameObject.SetActive(true);
            _batches.Clear();
        }

        private void Start()
        {
            _mainCam = Camera.main;
            _kernelID = _cullingShader.FindKernel("CSMain");
            InitializeBuffers();
        }

        private void InitializeBuffers()
        {
            foreach (var batch in _batches)
            {
                int count = batch.instances.Count;
                if (count == 0) continue;

                batch.allInstancesBuffer = new ComputeBuffer(count, Marshal.SizeOf<IndirectData>());

                var data = new IndirectData[count];
                for (int i = 0; i < count; i++) data[i].objectToWorld = batch.instances[i].ToMatrix();
                batch.allInstancesBuffer.SetData(data);

                batch.visibleInstancesBuffer = new ComputeBuffer(count, Marshal.SizeOf<IndirectData>(), ComputeBufferType.Append);
                batch.argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

                _args[0] = (uint)batch.mesh.GetIndexCount(0);
                _args[2] = (uint)batch.mesh.GetIndexStart(0);
                _args[3] = (uint)batch.mesh.GetBaseVertex(0);
                batch.argsBuffer.SetData(_args);
            }
        }

        private void Update()
        {
            if (_batches.Count == 0 || _mainCam == null) return;

            GeometryUtility.CalculateFrustumPlanes(_mainCam, _planes);
            for (int i = 0; i < 6; i++)
            {
                Vector3 n = _planes[i].normal;
                _planeNormals[i] = new Vector4(n.x, n.y, n.z, _planes[i].distance);
            }

            _cullingShader.SetVectorArray(ID_CameraPlanes, _planeNormals);
            _cullingShader.SetVector(ID_CameraPosition, _mainCam.transform.position);
            _cullingShader.SetFloat(ID_MaxDistance, _cullDistance);

            foreach (var batch in _batches)
            {
                if (batch.allInstancesBuffer == null) continue;

                batch.visibleInstancesBuffer.SetCounterValue(0);
                _cullingShader.SetInt(ID_InstanceCount, batch.instances.Count);
                _cullingShader.SetFloat(ID_BoundRadius, batch.boundRadius + _boundsPadding);
                _cullingShader.SetBuffer(_kernelID, ID_AllInstances, batch.allInstancesBuffer);
                _cullingShader.SetBuffer(_kernelID, ID_VisibleInstances, batch.visibleInstancesBuffer);

                int groups = Mathf.CeilToInt(batch.instances.Count / 64f);
                _cullingShader.Dispatch(_kernelID, groups, 1, 1);

                ComputeBuffer.CopyCount(batch.visibleInstancesBuffer, batch.argsBuffer, 4);
                batch.material.SetBuffer(ID_VisibleInstanceData, batch.visibleInstancesBuffer);

                Graphics.DrawMeshInstancedIndirect(batch.mesh, 0, batch.material,
                    new Bounds(Vector3.zero, Vector3.one * 100000), batch.argsBuffer);
            }
        }

        private void OnDestroy()
        {
            foreach (var batch in _batches)
            {
                batch.allInstancesBuffer?.Release();
                batch.visibleInstancesBuffer?.Release();
                batch.argsBuffer?.Release();
            }
        }

        private int CombineHash(int h1, int h2) => ((h1 << 5) + h1) ^ h2;
    }
}