using System;
using System.Collections.Generic;
using UnityEngine;
using BillsGenesis.Core;

namespace BillsGenesis.Services
{
    public class GenesisTimer
    {
        public int Id { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsPaused { get; private set; }

        private float _duration;
        private float _elapsed;
        private bool _isLoop;
        private bool _useUnscaledTime;
        private Action _onComplete;
        private Action<float> _onUpdate;
        private GameObject _boundTarget;
        private TimerManager _manager;

        public void Setup(int id, float duration, Action onComplete, TimerManager manager)
        {
            Id = id;
            _duration = duration;
            _onComplete = onComplete;
            _manager = manager;
            _elapsed = 0;
            IsActive = true;
            IsPaused = false;
            _isLoop = false;
            _useUnscaledTime = false;
            _onUpdate = null;
            _boundTarget = null;
        }

        public void Tick()
        {
            if (!IsActive || IsPaused) return;

            if (_boundTarget != null && _boundTarget == null) // Object destroyed
            {
                Stop();
                return;
            }

            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _elapsed += dt;

            _onUpdate?.Invoke(_elapsed / _duration);

            if (_elapsed >= _duration)
            {
                _onComplete?.Invoke();
                if (_isLoop) _elapsed = 0;
                else Stop();
            }
        }

        public void Stop()
        {
            if (!IsActive) return;
            IsActive = false;
            _manager.Recycle(this);
        }

        public GenesisTimer SetLoop(bool loop)
        {
            _isLoop = loop;
            return this;
        }

        public GenesisTimer SetUnscaled(bool unscaled)
        {
            _useUnscaledTime = unscaled;
            return this;
        }

        public GenesisTimer SetUpdateCallback(Action<float> onUpdate)
        {
            _onUpdate = onUpdate;
            return this;
        }

        public GenesisTimer BindTo(GameObject target)
        {
            _boundTarget = target;
            return this;
        }

        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;
    }

    public sealed class TimerManager : GenesisSingletonService<TimerManager>
    {
        private readonly List<GenesisTimer> _activeTimers = new List<GenesisTimer>();
        private readonly Stack<GenesisTimer> _pool = new Stack<GenesisTimer>();
        private readonly List<GenesisTimer> _tempList = new List<GenesisTimer>();
        private int _idCounter;

        public int ActiveTimersCount => _activeTimers.Count; // FIX ADDED HERE

        public override void OnUpdate()
        {
            if (_activeTimers.Count == 0) return;

            _tempList.Clear();
            _tempList.AddRange(_activeTimers);

            for (int i = 0; i < _tempList.Count; i++) _tempList[i].Tick();
        }

        public GenesisTimer Register(float duration, Action onComplete)
        {
            GenesisTimer timer = _pool.Count > 0 ? _pool.Pop() : new GenesisTimer();
            timer.Setup(++_idCounter, duration, onComplete, this);
            _activeTimers.Add(timer);
            return timer;
        }

        public void Cancel(GenesisTimer timer)
        {
            if (timer != null && timer.IsActive) timer.Stop();
        }

        public void Recycle(GenesisTimer timer)
        {
            if (_activeTimers.Contains(timer))
            {
                _activeTimers.Remove(timer);
                _pool.Push(timer);
            }
        }

        public void CancelAll()
        {
            foreach (var t in _activeTimers) _pool.Push(t);
            _activeTimers.Clear();
        }

        public void DoAfter(float delay, Action action) => Register(delay, action);
        public void DoNextFrame(Action action) => Register(0f, action);
        public void DoEvery(float interval, Action action, GameObject boundObject = null) => Register(interval, action).SetLoop(true).BindTo(boundObject);
    }
}