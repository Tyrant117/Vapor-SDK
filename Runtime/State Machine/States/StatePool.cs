using System;
using UnityEngine.Pool;

namespace VaporStateMachine
{
    public class StatePool<T> where T : State
    {
        private readonly ObjectPool<T> _pool;
        private readonly string _name;
        private readonly bool _canExitInstantly;
        private readonly bool _isStateMachine;

        private readonly Action<State> Entered;
        private readonly Action<State> Updated;
        private readonly Action<State> Exited;

        public StatePool(string name, bool canExitInstantly = false)
        {
            _name = name;
            _canExitInstantly = canExitInstantly;
            _isStateMachine = true;

            _pool = new ObjectPool<T>(OnCreateState, OnGetState, OnReleaseState, OnDestroyState);
        }

        public StatePool(string name, bool canExitInstantly, Action<State> entered = null, Action<State> updated = null, Action<State> exited = null)
        {
            _name = name;
            _canExitInstantly = canExitInstantly;
            Entered = entered;
            Updated = updated;
            Exited = exited;

            _pool = new ObjectPool<T>(OnCreateState, OnGetState, OnReleaseState, OnDestroyState);
        }

        public T Get() => _pool.Get();

        private T OnCreateState()
        {
            return _isStateMachine
                ? (T)Activator.CreateInstance(typeof(T), _name, _canExitInstantly)
                : (T)Activator.CreateInstance(typeof(T), _name, _canExitInstantly, Entered, Updated, Exited);
        }

        private void OnGetState(T fsm)
        {
            fsm.RemoveFromPool();
        }

        private void OnReleaseState(T fsm)
        {
            fsm.OnReturnedToPool();
        }

        private void OnDestroyState(T fsm)
        {

        }

        public void Release(T pooledObject)
        {
            _pool.Release(pooledObject);
        }
    }
}
