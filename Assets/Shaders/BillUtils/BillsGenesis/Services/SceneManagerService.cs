using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using BillsGenesis.Core;
using BillsGenesis.Data;

namespace BillsGenesis.Services
{
    public class SceneManagerService : GenesisSingletonService<SceneManagerService>
    {
        public event Action<float, string> OnProgressChange;
        public event Action<bool> OnLoadingStateChange;

        private GenesisManifest _manifest;

        public override Task InitializeAsync()
        {
            _manifest = Resources.Load<GenesisManifest>("GenesisManifest");
            return Task.CompletedTask;
        }

        public async void LoadSceneGroup(string groupId, float minDuration = 1.0f)
        {
            if (!_manifest) return;
            var group = _manifest.GetGroup(groupId);
            if (group == null) return;

            await LoadGroupInternal(group, minDuration);
        }

        public async Task LoadSceneAsync(string sceneName, float minDuration = 0.5f)
        {
            OnLoadingStateChange?.Invoke(true);
            OnProgressChange?.Invoke(0f, "Initializing...");

            await CleanMemory();

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            await HandleOperation(op, minDuration);

            OnLoadingStateChange?.Invoke(false);
        }

        public async Task ReloadCurrentScene(float minDuration = 0.5f)
        {
            string current = SceneManager.GetActiveScene().name;
            await LoadSceneAsync(current, minDuration);
        }

        public async Task UnloadSceneAsync(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                await SceneManager.UnloadSceneAsync(sceneName);
                await Resources.UnloadUnusedAssets();
            }
        }

        private async Task LoadGroupInternal(SceneGroup group, float minDuration)
        {
            OnLoadingStateChange?.Invoke(true);
            OnProgressChange?.Invoke(0f, $"Loading Group: {group.GroupId}");

            await CleanMemory();

            float startTime = Time.realtimeSinceStartup;
            var operations = new List<AsyncOperation>();

            var mainOp = SceneManager.LoadSceneAsync(group.ActiveScene, LoadSceneMode.Single);
            operations.Add(mainOp);

            foreach (var scene in group.AdditiveScenes)
            {
                var addOp = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
                operations.Add(addOp);
            }

            while (!IsAllDone(operations))
            {
                float totalProgress = GetTotalProgress(operations);
                float timePassed = Time.realtimeSinceStartup - startTime;
                float displayProgress = Mathf.Min(totalProgress, timePassed / minDuration);

                OnProgressChange?.Invoke(displayProgress, $"Loading... {(int)(displayProgress * 100)}%");
                await Task.Yield();
            }

            while (Time.realtimeSinceStartup - startTime < minDuration)
            {
                OnProgressChange?.Invoke(1f, "Finalizing...");
                await Task.Yield();
            }

            OnProgressChange?.Invoke(1f, "Completed");
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(group.ActiveScene));

            await Task.Delay(200);
            OnLoadingStateChange?.Invoke(false);
        }

        private bool IsAllDone(List<AsyncOperation> ops)
        {
            foreach (var op in ops) if (!op.isDone) return false;
            return true;
        }

        private float GetTotalProgress(List<AsyncOperation> ops)
        {
            float total = 0;
            foreach (var op in ops) total += op.progress;
            return total / ops.Count;
        }

        private async Task HandleOperation(AsyncOperation op, float minDuration)
        {
            op.allowSceneActivation = false;
            float startTime = Time.realtimeSinceStartup;

            while (!op.isDone)
            {
                float progress = Mathf.Clamp01(op.progress / 0.9f);
                float timePassed = Time.realtimeSinceStartup - startTime;
                float displayProgress = Mathf.Min(progress, timePassed / minDuration);

                OnProgressChange?.Invoke(displayProgress, "Loading...");

                if (op.progress >= 0.9f && timePassed >= minDuration)
                {
                    op.allowSceneActivation = true;
                }
                await Task.Yield();
            }
        }

        private async Task CleanMemory()
        {
            Genesis.Get<PoolManager>()?.DespawnAll();
            Genesis.Get<TimerManager>()?.CancelAll();
            Genesis.Get<VFXManager>()?.ClearDurationCache();

            OnProgressChange?.Invoke(0f, "Cleaning Memory...");
            await Resources.UnloadUnusedAssets();
            GC.Collect();
        }
    }
}