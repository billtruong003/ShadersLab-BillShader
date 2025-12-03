using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using BillsGenesis.Core;
using BillsGenesis.Services;
using BillsGenesis.Tools;

namespace BillsGenesis.UI
{
    public class LoadingUIService : GenesisSingletonService<LoadingUIService>
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private SmartSliceConfigurator _sliceConfigurator;
        [SerializeField] private TextMeshProUGUI _txtPercent;
        [SerializeField] private TextMeshProUGUI _txtLog;
        [SerializeField] private float _fadeDuration = 0.3f;

        private SceneManagerService _sceneManager;
        private bool _isClosing;

        public override Task InitializeAsync()
        {
            _sceneManager = Genesis.Get<SceneManagerService>();

            if (_sceneManager != null)
            {
                _sceneManager.OnLoadingStateChange += HandleStateChange;
                _sceneManager.OnProgressChange += UpdateVisuals;
            }

            if (_canvasGroup)
            {
                _canvasGroup.alpha = 0;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.gameObject.SetActive(false);
            }

            if (_sliceConfigurator)
            {
                _sliceConfigurator.SetProgress(0f);
            }

            return Task.CompletedTask;
        }

        private void HandleStateChange(bool isLoading)
        {
            if (isLoading)
            {
                _isClosing = false;
                if (_canvasGroup) _canvasGroup.gameObject.SetActive(true);
                Fade(1f);
            }
            else
            {
                _isClosing = true;
                Fade(0f);
            }
        }

        private void UpdateVisuals(float progress, string log)
        {
            if (_isClosing) return;

            if (_sliceConfigurator) _sliceConfigurator.SetProgress(progress);
            if (_txtPercent) _txtPercent.text = $"{Mathf.RoundToInt(progress * 100)}%";
            if (_txtLog) _txtLog.text = log;
        }

        private async void Fade(float targetAlpha)
        {
            if (!_canvasGroup) return;

            _canvasGroup.blocksRaycasts = targetAlpha > 0.5f;
            float start = _canvasGroup.alpha;
            float t = 0;

            while (t < _fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t / _fadeDuration);
                await Task.Yield();
            }

            _canvasGroup.alpha = targetAlpha;
            if (targetAlpha <= 0.01f) _canvasGroup.gameObject.SetActive(false);
        }

        public override void Dispose()
        {
            if (_sceneManager != null)
            {
                _sceneManager.OnLoadingStateChange -= HandleStateChange;
                _sceneManager.OnProgressChange -= UpdateVisuals;
            }
            base.Dispose();
        }
    }
}