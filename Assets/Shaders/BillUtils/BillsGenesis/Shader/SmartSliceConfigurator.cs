using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace BillsGenesis.Tools
{
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SmartSliceConfigurator : MonoBehaviour
    {
        private const string SHADER_NAME = "BillsGenesis/UI/SmartSliceLoading";

        private static readonly int PropRect = Shader.PropertyToID("_Rect");
        private static readonly int PropProgress = Shader.PropertyToID("_Progress");

        [Title("Runtime Control")]
        [SerializeField, Range(0f, 1f), OnValueChanged(nameof(UpdateVisuals))]
        private float _progress = 1f;

        [Title("References")]
        [SerializeField, ReadOnly] private Image _targetImage;
        [SerializeField, ReadOnly] private RectTransform _rectTransform;

        private Material _instancedMaterial;
        private bool _isInitialized;

        private void OnEnable()
        {
            Initialize();
            UpdateVisuals();
        }

        private void OnDisable()
        {
            if (_instancedMaterial && Application.isPlaying)
            {
                Destroy(_instancedMaterial);
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!_isInitialized) Initialize();
            UpdateVisuals();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) UpdateVisuals();
        }

        private void Initialize()
        {
            if (!_targetImage) _targetImage = GetComponent<Image>();
            if (!_rectTransform) _rectTransform = GetComponent<RectTransform>();

            if (_targetImage && _targetImage.material)
            {
                if (Application.isPlaying)
                {
                    if (_instancedMaterial == null || _targetImage.material != _instancedMaterial)
                    {
                        _instancedMaterial = new Material(_targetImage.material);
                        _instancedMaterial.name = $"{_targetImage.material.name}_Instance";
                        _targetImage.material = _instancedMaterial;
                    }
                }
                else
                {
                    _instancedMaterial = _targetImage.material;
                }
            }

            _isInitialized = true;
        }

        [Button(ButtonSizes.Large, Icon = SdfIconType.Magic), GUIColor(0.3f, 0.85f, 0.6f)]
        public void AutoSetup()
        {
            if (!_targetImage) _targetImage = GetComponent<Image>();

            Shader shader = Shader.Find(SHADER_NAME);
            if (!shader)
            {
                Debug.LogError($"[SmartSlice] Shader '{SHADER_NAME}' not found!");
                return;
            }

            if (_targetImage.material == null || _targetImage.material.shader != shader)
            {
                Material newMat = new Material(shader);
                newMat.name = "SmartSlice_Material";
                _targetImage.material = newMat;
            }

            Initialize();
            UpdateVisuals();
        }

        public void SetProgress(float value)
        {
            if (Mathf.Abs(_progress - value) < 0.001f) return;
            _progress = value;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (!_isInitialized || !_instancedMaterial || !_rectTransform) return;

            Rect r = _rectTransform.rect;

            _instancedMaterial.SetVector(PropRect, new Vector4(r.x, r.y, r.width, r.height));
            _instancedMaterial.SetFloat(PropProgress, Mathf.Clamp01(_progress));
        }
    }
}