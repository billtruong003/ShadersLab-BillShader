using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace BillsGenesis.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InjectAttribute : Attribute { }

    public interface IGenesisService
    {
        Task InitializeAsync();
        void OnAppReady();
        void OnUpdate();
        void Dispose();
    }

    public abstract class BaseService : MonoBehaviour, IGenesisService
    {
        public virtual Task InitializeAsync() => Task.CompletedTask;
        public virtual void OnAppReady() { }
        public virtual void OnUpdate() { }
        public virtual void Dispose() { }
    }

    public abstract class GenesisSingletonService<T> : BaseService where T : GenesisSingletonService<T>
    {
        public static T Instance { get; private set; }

        public override Task InitializeAsync()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return Task.CompletedTask;
            }
            Instance = (T)this;
            DontDestroyOnLoad(gameObject);
            return base.InitializeAsync();
        }

        public override void Dispose()
        {
            if (Instance == this) Instance = null;
            base.Dispose();
        }
    }

    public static class Genesis
    {
        private static readonly Dictionary<Type, IGenesisService> _services = new Dictionary<Type, IGenesisService>();
        private static readonly List<IGenesisService> _updateList = new List<IGenesisService>();

        public static void Register<T>(T service) where T : class, IGenesisService
        {
            var type = typeof(T);
            if (_services.ContainsKey(type)) _services.Remove(type);
            _services[type] = service;
            _updateList.Add(service);
        }

        public static T Get<T>() where T : class, IGenesisService
        {
            if (_services.TryGetValue(typeof(T), out var service)) return service as T;
            return null;
        }

        public static void InjectDependencies(object target)
        {
            var fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            for (int i = 0; i < fields.Length; i++)
            {
                if (Attribute.IsDefined(fields[i], typeof(InjectAttribute)))
                {
                    var type = fields[i].FieldType;
                    if (_services.TryGetValue(type, out var service))
                    {
                        fields[i].SetValue(target, service);
                    }
                }
            }
        }

        public static void UpdateServices()
        {
            Profiler.BeginSample("Genesis.Update");
            for (int i = 0; i < _updateList.Count; i++) _updateList[i].OnUpdate();
            Profiler.EndSample();
        }

        public static void Clear()
        {
            foreach (var s in _services.Values) s.Dispose();
            _services.Clear();
            _updateList.Clear();
        }
    }

    public interface IGenesisEvent { }

    public static class EventBus
    {
        private static readonly Dictionary<Type, List<object>> _subscribers = new Dictionary<Type, List<object>>();

        public static void Subscribe<T>(Action<T> callback) where T : struct, IGenesisEvent
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<object>();
                _subscribers[type] = list;
            }
            if (!list.Contains(callback)) list.Add(callback);
        }

        public static void Unsubscribe<T>(Action<T> callback) where T : struct, IGenesisEvent
        {
            if (_subscribers.TryGetValue(typeof(T), out var list)) list.Remove(callback);
        }

        public static void Raise<T>(T eventData) where T : struct, IGenesisEvent
        {
            if (_subscribers.TryGetValue(typeof(T), out var list))
            {
                for (int i = list.Count - 1; i >= 0; i--) ((Action<T>)list[i]).Invoke(eventData);
            }
        }
    }
}