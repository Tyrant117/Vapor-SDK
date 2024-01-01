using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vapor.Tweening
{
    public abstract class BaseUIAnimation
    {
        public InterfaceAnimationHandle Handle { get; set; }
        public int Index { get; set; }
        public float Duration => _duration;
        public int Iterations => _iterations;
        public bool IsPlaying => _isPlaying;
        public bool IsLast => SequentialAnimations.Count == 0;

        protected float _duration;
        protected int _iterations;
        protected bool _isPlaying;
        protected bool _isPaused;
        protected bool _isCompleting;
        protected bool _firstEntry;
        protected Timer _timer;
        protected int _iterationsLeft;
        protected Easing.Function _easingFunction;

        public event Action Started;
        protected void OnStarted() { Started?.Invoke(); }
        public event Action Stopped;
        protected void OnStopped() { Stopped?.Invoke(); }

        internal readonly List<BaseUIAnimation> ParallelAnimations = new();
        internal readonly List<BaseUIAnimation> SequentialAnimations = new();

        public BaseUIAnimation(float duration, int iterations, Easing.Ease ease)
        {
            _duration = duration;
            _iterations = iterations;
            _firstEntry = true;
            _timer = new Timer();
            _easingFunction = Easing.GetEasingFunction(ease);
        }

        public void AddParallel(BaseUIAnimation anim)
        {
            anim.Handle = Handle;
            ParallelAnimations.Add(anim);
            Handle.AddAnimation(anim);
        }

        public void AddSequence(BaseUIAnimation anim)
        {
            anim.Handle = Handle;
            SequentialAnimations.Add(anim);
            Handle.AddAnimation(anim);
        }

        public virtual void Play()
        {
            _isPlaying = true;
            _timer.Reset();
            _iterationsLeft = _iterations;
            if (_firstEntry)
            {
                CacheInitialData();
                _firstEntry = false;
            }
            CacheVelocity();
            OnReset();
            Started?.Invoke();
            foreach (var anim in ParallelAnimations)
            {
                anim.TryPlay();
            }
        }

        public virtual void TryPlay()
        {
            if (!IsPlaying)
            {
                Play();
            }
        }

        protected abstract void CacheInitialData();
        public virtual bool OnUpdate()
        {
            if (_timer.Elapsed <= _duration)
            {
                if (_isCompleting)
                {
                    _isCompleting = false;
                    OnSetFinalState();
                    return false;
                }

                if (_isPaused)
                {
                    _timer.Pause(Time.deltaTime);
                }
                else
                {
                    UpdateTargetState(_timer.Elapsed / _duration);
                }
                return true;
            }
            else
            {
                _iterationsLeft--;
                if (_iterationsLeft == 0)
                {
                    return false;
                }
                else
                {
                    OnReset();
                    _timer.Reset();
                    return true;
                }
            }
        }
        protected abstract void CacheVelocity();
        protected abstract void UpdateTargetState(double fraction);
        protected abstract void OnSetFinalState();
        public virtual void Reset()
        {
            if (!_firstEntry)
            {
                OnReset();
            }
        }
        protected abstract void OnReset();
        public virtual void Stop(bool silent = false)
        {
            OnSetFinalState();
            _isPlaying = false;
            _isCompleting = false;
            _isPaused = false;
            if (!silent)
            {
                Stopped?.Invoke();
                foreach (var anim in SequentialAnimations)
                {
                    anim.TryPlay();
                }
            }
        }

        public virtual void TriggerNext()
        {
            OnSetFinalState();
            _isPlaying = false;
            _isCompleting = false;
            _isPaused = false;
            foreach (var anim in SequentialAnimations)
            {
                anim.TryPlay();
            }
        }

        public virtual void Pause()
        {
            if (_isPlaying)
            {
                _isPaused = true;
            }
        }

        public virtual void Resume()
        {
            _isPaused = false;
        }

        public virtual void Complete()
        {
            if (_isPlaying)
            {
                _isCompleting = true;
            }
        }
    }
}
