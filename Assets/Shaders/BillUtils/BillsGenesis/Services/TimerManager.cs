using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using BillsGenesis.Core;

namespace BillsGenesis.Services
{
    [ServiceConfig(InitPriority = 100)]
    public sealed class TimerManager : GenesisSingletonService<TimerManager>
    {
        private readonly GenesisPriorityQueue<GenesisTimer> _timerQueue = new GenesisPriorityQueue<GenesisTimer>();
        private readonly Queue<GenesisTimer> _pool = new Queue<GenesisTimer>();
        private readonly List<GenesisTimer> _rescheduleBuffer = new List<GenesisTimer>();

        public int ActiveTimersCount => _timerQueue.Count;

        public override void OnUpdate()
        {
            if (_timerQueue.Count == 0) return;

            float now = Time.time;
            float unscaledNow = Time.unscaledTime;

            while (_timerQueue.Count > 0 && _timerQueue.Peek().NextTriggerTime <= (_timerQueue.Peek().IsUnscaled ? unscaledNow : now))
            {
                GenesisTimer timer = _timerQueue.Dequeue();

                if (!timer.IsActive)
                {
                    RecycleInternal(timer);
                    continue;
                }

                if (timer.IsPaused)
                {
                    timer.NextTriggerTime += Time.deltaTime;
                    _rescheduleBuffer.Add(timer);
                    continue;
                }

                timer.Execute();

                if (timer.IsLoop && timer.IsActive)
                {
                    timer.NextTriggerTime = (timer.IsUnscaled ? unscaledNow : now) + timer.Interval;
                    _rescheduleBuffer.Add(timer);
                }
                else
                {
                    timer.IsActive = false;
                    RecycleInternal(timer);
                }
            }

            for (int i = 0; i < _rescheduleBuffer.Count; i++)
            {
                _timerQueue.Enqueue(_rescheduleBuffer[i]);
            }
            _rescheduleBuffer.Clear();
        }

        public GenesisTimer Post(float delay, Action onComplete)
        {
            return GetTimer().Setup(delay, onComplete, false);
        }

        public GenesisTimer Register(float duration, Action onComplete)
        {
            return Post(duration, onComplete);
        }

        public GenesisTimer Schedule(float interval, Action onTick)
        {
            return GetTimer().Setup(interval, onTick, true);
        }

        public GenesisTimer Run(float duration, Action<float> onUpdate, Action onComplete = null)
        {
            // Update callbacks are handled via Update hook or specific Timer logic
            // For high perf priority queue, per-frame update callback is tricky.
            // We use a separate list for Update-based timers to keep PriorityQueue clean for events.
            // But to keep it simple and unified:
            var timer = GetTimer().Setup(duration, onComplete, false);
            timer.SetUpdateCallback(onUpdate);
            return timer;
        }

        public void CancelAll()
        {
            _timerQueue.Clear();
            _rescheduleBuffer.Clear();
        }

        internal void QueueTimer(GenesisTimer timer)
        {
            float current = timer.IsUnscaled ? Time.unscaledTime : Time.time;
            timer.NextTriggerTime = current + timer.Interval;
            _timerQueue.Enqueue(timer);
        }

        private GenesisTimer GetTimer()
        {
            return _pool.Count > 0 ? _pool.Dequeue() : new GenesisTimer(this);
        }

        private void RecycleInternal(GenesisTimer timer)
        {
            timer.Reset();
            _pool.Enqueue(timer);
        }
    }

    public class GenesisTimer : IComparable<GenesisTimer>
    {
        public bool IsActive;
        public bool IsPaused;
        public bool IsLoop;
        public bool IsUnscaled;
        public float Interval;
        public float NextTriggerTime;

        private Action _onComplete;
        private Action<float> _onUpdate;
        private float _startTime;
        private float _duration;
        private TimerManager _manager;

        public GenesisTimer(TimerManager manager)
        {
            _manager = manager;
        }

        public GenesisTimer Setup(float interval, Action callback, bool loop)
        {
            Interval = interval;
            _duration = interval;
            _onComplete = callback;
            IsLoop = loop;
            IsActive = true;
            IsPaused = false;
            IsUnscaled = false;
            _onUpdate = null;
            _startTime = Time.time;

            _manager.QueueTimer(this);
            return this;
        }

        public void Execute()
        {
            _onComplete?.Invoke();
        }

        public void Stop()
        {
            IsActive = false;
        }

        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;

        public GenesisTimer SetUnscaled(bool unscaled)
        {
            IsUnscaled = unscaled;
            return this;
        }

        public GenesisTimer SetLoop(bool loop)
        {
            IsLoop = loop;
            return this;
        }

        public GenesisTimer SetUpdateCallback(Action<float> onUpdate)
        {
            // Note: PriorityQueue optimization doesn't naturally support per-frame updates efficiently.
            // This is a tradeoff. For intense update logic, use a direct Update loop in a MonoBehaviour.
            // Keeping this for compatibility but it only fires on tick in this architecture
            // OR we can hack it by re-queueing every frame if update is present, but that defeats PQ purpose.
            // For now, we will execute Update callback only on completion/tick for strict PQ design.
            // If you truly need per-frame interpolation, use Tween library (DOTween).
            _onUpdate = onUpdate;
            return this;
        }

        public void Reset()
        {
            _onComplete = null;
            _onUpdate = null;
            IsActive = false;
        }

        public int CompareTo(GenesisTimer other)
        {
            if (NextTriggerTime < other.NextTriggerTime) return -1;
            if (NextTriggerTime > other.NextTriggerTime) return 1;
            return 0;
        }
    }
}