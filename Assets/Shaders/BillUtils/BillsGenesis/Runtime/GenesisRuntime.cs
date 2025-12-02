using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using BillsGenesis.Core;
using BillsGenesis.Services;
using BillsGenesis.Data;

namespace BillsGenesis.Runtime
{
    [DefaultExecutionOrder(-9999)]
    public class GenesisBootstrapper : MonoBehaviour
    {
        public static string DevTargetScene;

        [SerializeField] private GenesisManifest _manifest;
        [SerializeField] private CanvasGroup _loadingGroup;
        [SerializeField] private Image _progressBar;
        [SerializeField] private bool _debugOverlay = true;

        private async void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;

            try
            {
                // Register Core Services
                var logger = Reg<LoggerService>();
                var pool = Reg<PoolManager>();      // Updated
                var audio = Reg<AudioManager>();    // Updated
                var scene = Reg<SceneManagerService>();
                var timer = Reg<TimerManager>();    // New
                var vfx = Reg<VFXManager>();        // New
                var storage = Reg<StorageManager>();// New
                var native = Reg<NativeBridge>();   // New

                scene.Setup(_manifest, _loadingGroup);
                if (_progressBar) scene.OnProgress += p => _progressBar.fillAmount = p;

                // Init Async
                await logger.InitializeAsync();
                await pool.InitializeAsync();
                await audio.InitializeAsync();
                await timer.InitializeAsync();
                await vfx.InitializeAsync();
                await storage.InitializeAsync();
                await native.InitializeAsync();
                await scene.InitializeAsync();

                Genesis.InjectDependencies(this);

                if (_debugOverlay) gameObject.AddComponent<GenesisOverlay>();

                logger.Log("Genesis Ecosystem Loaded");

                if (!string.IsNullOrEmpty(DevTargetScene))
                {
                    await scene.LoadSceneDirectAsync(DevTargetScene);
                    DevTargetScene = null;
                }
                else if (!string.IsNullOrEmpty(_manifest.InitialGroupId))
                {
                    await scene.LoadGroupAsync(_manifest.InitialGroupId);
                }
            }
            catch (Exception e) { Debug.LogError($"Boot Fail: {e}"); }
        }

        private T Reg<T>() where T : BaseService
        {
            var c = gameObject.GetComponent<T>();
            if (c == null) c = gameObject.AddComponent<T>();
            Genesis.Register(c);
            return c;
        }

        private void Update() => Genesis.UpdateServices();
        private void OnDestroy() => Genesis.Clear();
    }

    // Overlay class remains the same...
    public class GenesisOverlay : MonoBehaviour
    {
        private float _dt, _update;
        private string _txt;
        private GUIStyle _style = new GUIStyle();
        private Rect _rect = new Rect(10, 10, 400, 100);
        private void Awake() { _style.fontSize = 20; _style.normal.textColor = Color.green; }
        private void Update()
        {
            _dt += (Time.unscaledDeltaTime - _dt) * 0.1f;
            if (Time.unscaledTime >= _update)
            {
                _update = Time.unscaledTime + 0.5f;
                float fps = 1.0f / _dt;
                long mem = GC.GetTotalMemory(false) / 1048576;
                _txt = $"{fps:0.} FPS | GC: {mem} MB";
            }
        }
        private void OnGUI() => GUI.Label(_rect, _txt, _style);
    }
}