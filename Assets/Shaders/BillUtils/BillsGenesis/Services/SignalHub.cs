using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using BillsGenesis.Core;

namespace BillsGenesis.Services
{
    public interface ISignal { }

    [ServiceConfig(InitPriority = 50)]
    public sealed class SignalHub : GenesisSingletonService<SignalHub>
    {
        private readonly Dictionary<int, Dictionary<Type, Delegate>> _channels = new Dictionary<int, Dictionary<Type, Delegate>>();
        private readonly Dictionary<int, Dictionary<Type, ISignal>> _stickySignals = new Dictionary<int, Dictionary<Type, ISignal>>();

        [ShowInInspector, ReadOnly, Title("Status")]
        public int TotalChannels => _channels.Count;

        [ShowInInspector, ReadOnly]
        public int TotalListeners
        {
            get
            {
                int count = 0;
                foreach (var chan in _channels.Values) count += chan.Count;
                return count;
            }
        }

        public IDisposable Subscribe<T>(Action<T> callback, int channelId = 0) where T : ISignal
        {
            if (!_channels.TryGetValue(channelId, out var signalMap))
            {
                signalMap = new Dictionary<Type, Delegate>();
                _channels[channelId] = signalMap;
            }

            var type = typeof(T);
            if (signalMap.TryGetValue(type, out var d))
            {
                signalMap[type] = Delegate.Combine(d, callback);
            }
            else
            {
                signalMap[type] = callback;
            }

            if (_stickySignals.TryGetValue(channelId, out var stickyMap) && stickyMap.TryGetValue(type, out var signal))
            {
                callback.Invoke((T)signal);
            }

            return new SignalSubscription<T>(this, callback, channelId);
        }

        public void Once<T>(Action<T> callback, int channelId = 0) where T : ISignal
        {
            Action<T> wrapper = null;
            wrapper = (signal) =>
            {
                callback(signal);
                Unsubscribe(wrapper, channelId);
            };
            Subscribe(wrapper, channelId);
        }

        public void Unsubscribe<T>(Action<T> callback, int channelId = 0) where T : ISignal
        {
            if (_channels.TryGetValue(channelId, out var signalMap))
            {
                var type = typeof(T);
                if (signalMap.TryGetValue(type, out var d))
                {
                    var current = Delegate.Remove(d, callback);
                    if (current != null) signalMap[type] = current;
                    else signalMap.Remove(type);
                }
            }
        }

        public void UnsubscribeAll(int channelId)
        {
            if (_channels.ContainsKey(channelId))
            {
                _channels[channelId].Clear();
            }
        }

        public void Fire<T>(T signal, int channelId = 0) where T : ISignal
        {
            var type = typeof(T);
            if (_channels.TryGetValue(channelId, out var signalMap) && signalMap.TryGetValue(type, out var d))
            {
                (d as Action<T>)?.Invoke(signal);
            }
        }

        public void Fire<T>(int channelId = 0) where T : ISignal, new()
        {
            Fire(new T(), channelId);
        }

        public void FireSticky<T>(T signal, int channelId = 0) where T : ISignal
        {
            if (!_stickySignals.TryGetValue(channelId, out var stickyMap))
            {
                stickyMap = new Dictionary<Type, ISignal>();
                _stickySignals[channelId] = stickyMap;
            }

            stickyMap[typeof(T)] = signal;
            Fire(signal, channelId);
        }

        public T GetSticky<T>(int channelId = 0) where T : ISignal
        {
            if (_stickySignals.TryGetValue(channelId, out var stickyMap) && stickyMap.TryGetValue(typeof(T), out var signal))
            {
                return (T)signal;
            }
            return default;
        }

        public bool TryGetSticky<T>(out T result, int channelId = 0) where T : ISignal
        {
            result = default;
            if (_stickySignals.TryGetValue(channelId, out var stickyMap) && stickyMap.TryGetValue(typeof(T), out var signal))
            {
                result = (T)signal;
                return true;
            }
            return false;
        }

        public void ClearSticky<T>(int channelId = 0) where T : ISignal
        {
            if (_stickySignals.TryGetValue(channelId, out var stickyMap))
            {
                stickyMap.Remove(typeof(T));
            }
        }

        public void ClearAllSticky(int channelId = 0)
        {
            if (_stickySignals.ContainsKey(channelId))
            {
                _stickySignals[channelId].Clear();
            }
        }

        public override void Dispose()
        {
            _channels.Clear();
            _stickySignals.Clear();
            base.Dispose();
        }

        private struct SignalSubscription<T> : IDisposable where T : ISignal
        {
            private readonly SignalHub _hub;
            private readonly Action<T> _callback;
            private readonly int _channel;

            public SignalSubscription(SignalHub hub, Action<T> callback, int channel)
            {
                _hub = hub;
                _callback = callback;
                _channel = channel;
            }

            public void Dispose()
            {
                _hub.Unsubscribe(_callback, _channel);
            }
        }
    }
}