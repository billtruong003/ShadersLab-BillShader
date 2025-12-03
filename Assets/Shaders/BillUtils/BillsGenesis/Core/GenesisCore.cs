using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace BillsGenesis.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InjectAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceConfigAttribute : Attribute
    {
        public bool AutoRegister { get; set; } = true;
        public int InitPriority { get; set; } = 0;
    }

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
        private static readonly Dictionary<Type, FieldInfo[]> _cachedInjectables = new Dictionary<Type, FieldInfo[]>();

        public static void Register<T>(T service) where T : class, IGenesisService
        {
            var type = service.GetType();
            if (_services.ContainsKey(type)) _services.Remove(type);
            _services[type] = service;

            if (service is BaseService && !_updateList.Contains(service))
                _updateList.Add(service);
        }

        public static T Get<T>() where T : class, IGenesisService
        {
            return _services.TryGetValue(typeof(T), out var s) ? s as T : null;
        }

        public static IGenesisService Get(Type type)
        {
            return _services.TryGetValue(type, out var s) ? s : null;
        }

        public static void InjectDependencies(object target)
        {
            var type = target.GetType();
            if (!_cachedInjectables.TryGetValue(type, out var fields))
            {
                var allFields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var list = new List<FieldInfo>();
                for (int i = 0; i < allFields.Length; i++)
                {
                    if (Attribute.IsDefined(allFields[i], typeof(InjectAttribute))) list.Add(allFields[i]);
                }
                fields = list.ToArray();
                _cachedInjectables[type] = fields;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                var fieldType = fields[i].FieldType;
                if (_services.TryGetValue(fieldType, out var service)) fields[i].SetValue(target, service);
            }
        }

        public static void NotifyAppReady()
        {
            foreach (var s in _services.Values) s.OnAppReady();
        }

        public static void UpdateServices()
        {
            for (int i = 0; i < _updateList.Count; i++) _updateList[i].OnUpdate();
        }

        public static void Clear()
        {
            foreach (var s in _services.Values) s.Dispose();
            _services.Clear();
            _updateList.Clear();
            _cachedInjectables.Clear();
        }
    }
}