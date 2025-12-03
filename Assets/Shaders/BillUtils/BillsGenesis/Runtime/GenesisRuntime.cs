using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using BillsGenesis.Core;
using BillsGenesis.Services;
using BillsGenesis.UI;
using BillsGenesis.Data;

namespace BillsGenesis.Runtime
{
    [DefaultExecutionOrder(-9999)]
    public class GenesisBootstrapper : MonoBehaviour
    {
        public static string DevTargetScene;

        private async void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            await BootAsync();
        }

        private async Task BootAsync()
        {
            var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IGenesisService).IsAssignableFrom(t)
                            && t.IsClass
                            && !t.IsAbstract
                            && typeof(MonoBehaviour).IsAssignableFrom(t));

            var servicesToInit = new List<(IGenesisService service, int priority)>();
            var existingServices = GetComponentsInChildren<IGenesisService>(true);

            foreach (var s in existingServices)
            {
                Genesis.Register(s);
                servicesToInit.Add((s, 0));
            }

            foreach (var type in serviceTypes)
            {
                if (Genesis.Get(type.BaseType == typeof(MonoBehaviour) ? type : type) != null) continue;

                var attr = type.GetCustomAttribute<ServiceConfigAttribute>();
                bool autoRegister = attr == null || attr.AutoRegister;
                int priority = attr?.InitPriority ?? 0;

                if (autoRegister)
                {
                    var go = new GameObject(type.Name);
                    go.transform.SetParent(transform);
                    var service = (IGenesisService)go.AddComponent(type);
                    Genesis.Register(service);
                    servicesToInit.Add((service, priority));
                }
            }

            var orderedServices = servicesToInit.OrderBy(x => x.priority).ToList();

            foreach (var item in orderedServices)
            {
                await item.service.InitializeAsync();
            }

            Genesis.NotifyAppReady();
            Debug.Log("[Genesis] System Boot Complete.");

            if (!string.IsNullOrEmpty(DevTargetScene))
            {
                await Genesis.Get<SceneManagerService>().LoadSceneAsync(DevTargetScene, 0.5f);
                DevTargetScene = null;
            }
            else
            {
                var manifest = Resources.Load<GenesisManifest>("GenesisManifest");
                if (manifest && !string.IsNullOrEmpty(manifest.InitialGroupId))
                {
                    Genesis.Get<SceneManagerService>().LoadSceneGroup(manifest.InitialGroupId);
                }
                else
                {
                    Debug.LogWarning("[Genesis] No Initial Group ID set in Manifest!");
                }
            }
        }

        private void Update() => Genesis.UpdateServices();
        private void OnDestroy() => Genesis.Clear();
    }
}