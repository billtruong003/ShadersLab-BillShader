using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using BillsGenesis.Core;
using BillsGenesis.Data;

namespace BillsGenesis.Services
{
    public class LoggerService : BaseService
    {
        private StringBuilder _buffer = new StringBuilder();
        private string _path;
        private int _count = 0;

        public override Task InitializeAsync()
        {
            _path = Path.Combine(Application.persistentDataPath, "app_log.txt");
            File.WriteAllText(_path, $"Session: {DateTime.Now}\n");
            return Task.CompletedTask;
        }

        public void Log(string msg) => Write("[INF]", msg);
        public void Error(string msg) => Write("[ERR]", msg);

        private void Write(string prefix, string msg)
        {
            string line = $"{DateTime.Now:HH:mm:ss} {prefix} {msg}";
            Debug.Log(line);
            _buffer.AppendLine(line);
            _count++;
            if (_count >= 10) Flush();
        }

        private void Flush()
        {
            if (_buffer.Length == 0) return;
            File.AppendAllText(_path, _buffer.ToString());
            _buffer.Clear();
            _count = 0;
        }

        public override void Dispose() => Flush();
    }

    public class ObjectPoolService : BaseService
    {
        private readonly Dictionary<int, Queue<GameObject>> _pools = new Dictionary<int, Queue<GameObject>>();
        private Transform _root;

        public override Task InitializeAsync()
        {
            _root = new GameObject("PoolRoot").transform;
            DontDestroyOnLoad(_root);
            return Task.CompletedTask;
        }

        public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            int key = prefab.GetInstanceID();
            if (!_pools.ContainsKey(key)) _pools[key] = new Queue<GameObject>();

            GameObject obj = _pools[key].Count > 0 ? _pools[key].Dequeue() : Instantiate(prefab, _root);
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.SetActive(true);
            return obj;
        }

        public void Return(GameObject obj, GameObject prefab)
        {
            obj.SetActive(false);
            obj.transform.SetParent(_root);
            _pools[prefab.GetInstanceID()].Enqueue(obj);
        }
    }

    public class SoundService : BaseService
    {
        private List<AudioSource> _sources = new List<AudioSource>();
        private GameObject _root;

        public override Task InitializeAsync()
        {
            _root = new GameObject("AudioRoot");
            DontDestroyOnLoad(_root);
            for (int i = 0; i < 5; i++) AddSource();
            return Task.CompletedTask;
        }

        public void PlaySfx(AudioClip clip, float vol = 1f)
        {
            if (!clip) return;
            var src = GetSource();
            src.volume = vol;
            src.PlayOneShot(clip);
        }

        private AudioSource GetSource()
        {
            for (int i = 0; i < _sources.Count; i++) if (!_sources[i].isPlaying) return _sources[i];
            return AddSource();
        }

        private AudioSource AddSource()
        {
            var s = _root.AddComponent<AudioSource>();
            _sources.Add(s);
            return s;
        }
    }

    public class SceneManagerService : BaseService
    {
        public event Action<float> OnProgress;
        private GenesisManifest _manifest;
        private CanvasGroup _loadingUI;

        public void Setup(GenesisManifest manifest, CanvasGroup ui)
        {
            _manifest = manifest;
            _loadingUI = ui;
        }

        public async Task LoadGroupAsync(string id)
        {
            var group = _manifest.GetGroup(id);
            if (group == null) return;

            await Fade(true);
            await UnloadAll();
            await LoadInternal(group);
            await Clean();
            await Fade(false);
        }

        public async Task LoadSceneDirectAsync(string sceneName)
        {
            await Fade(true);
            await UnloadAll();
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!op.isDone) await Task.Yield();
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            await Clean();
            await Fade(false);
        }

        private async Task LoadInternal(SceneGroup group)
        {
            var ops = new List<AsyncOperation>();
            ops.Add(SceneManager.LoadSceneAsync(group.ActiveScene, LoadSceneMode.Additive));
            foreach (var s in group.AdditiveScenes) ops.Add(SceneManager.LoadSceneAsync(s, LoadSceneMode.Additive));

            foreach (var op in ops) op.allowSceneActivation = false;

            while (!IsDone(ops))
            {
                float p = 0;
                foreach (var op in ops) p += op.progress;
                OnProgress?.Invoke(p / ops.Count);
                if (p / ops.Count >= 0.89f) foreach (var op in ops) op.allowSceneActivation = true;
                await Task.Yield();
            }
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(group.ActiveScene));
        }

        private async Task UnloadAll()
        {
            var count = SceneManager.sceneCount;
            var ops = new List<AsyncOperation>();
            for (int i = 0; i < count; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.name != "_Bootstrap") ops.Add(SceneManager.UnloadSceneAsync(s));
            }
            foreach (var op in ops) while (!op.isDone) await Task.Yield();
        }

        private bool IsDone(List<AsyncOperation> ops)
        {
            foreach (var op in ops) if (!op.isDone) return false;
            return true;
        }

        private async Task Clean()
        {
            await Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        private async Task Fade(bool show)
        {
            if (!_loadingUI) return;
            float t = 0;
            float start = _loadingUI.alpha;
            float end = show ? 1f : 0f;
            _loadingUI.blocksRaycasts = show;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                _loadingUI.alpha = Mathf.Lerp(start, end, t / 0.25f);
                await Task.Yield();
            }
            _loadingUI.alpha = end;
        }
    }
}