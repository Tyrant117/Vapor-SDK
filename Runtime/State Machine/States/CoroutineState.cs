using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporStateMachine
{
    public class CoroutineState : State
    {
        protected Func<CoroutineState, IEnumerator> OnCoroutineUpdated;
        protected MonoBehaviour _runner;
        protected Coroutine _routine;
        protected bool _exitAfterCoroutine;
        protected int _iterations;

        public bool CoroutineIsComplete { get; private set; }
        protected int _iterationCount;

        public CoroutineState(MonoBehaviour runner, string name, bool canExitInstantly, Action<State> entered = null, Func<CoroutineState, IEnumerator> updated = null, Action<State, Transition> exited = null) : base(name, canExitInstantly, entered, null, exited)
        {
            _runner = runner;
            OnCoroutineUpdated = updated;
        }
        public CoroutineState(MonoBehaviour runner, string name, bool canExitInstantly, bool exitAfterCoroutine, int interations, Action<State> entered = null, Func<CoroutineState, IEnumerator> updated = null, Action<State, Transition> exited = null) 
            : base(name, canExitInstantly, entered, null, exited)
        {
            _runner = runner;
            _exitAfterCoroutine = exitAfterCoroutine;
            _iterations = interations;
            OnCoroutineUpdated = updated;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _routine = null;
            CoroutineIsComplete = false;
            _iterationCount = 0;
        }

        public override void OnUpdate()
        {
            if (_routine == null && OnCoroutineUpdated != null)
            {
                _routine = _exitAfterCoroutine ? _runner.StartCoroutine(RunCoroutine()) : _runner.StartCoroutine(LoopCoroutine());
            }
        }

        private IEnumerator RunCoroutine()
        {
            IEnumerator routine = OnCoroutineUpdated(this);
            for (int i = 0; i < _iterations; i++)
            {
                // This checks if the routine needs at least one frame to execute.
                // If not, LoopCoroutine will wait 1 frame to avoid an infinite
                // loop which will crash Unity
                yield return routine.MoveNext() ? routine.Current : null;

                // Iterate from the onLogic coroutine until it is depleted
                while (routine.MoveNext())
                {
                    yield return routine.Current;
                }
                _iterationCount++;                
                if (_iterationCount < _iterations)
                {
                    // Restart the onLogic coroutine
                    routine = OnCoroutineUpdated(this);
                }
            }
            Debug.Log("Run Iterated");
            CoroutineIsComplete = true;
        }

        private IEnumerator LoopCoroutine()
        {
            IEnumerator routine = OnCoroutineUpdated(this);
            while (true)
            {

                // This checks if the routine needs at least one frame to execute.
                // If not, LoopCoroutine will wait 1 frame to avoid an infinite
                // loop which will crash Unity
                yield return routine.MoveNext() ? routine.Current : null;

                // Iterate from the onLogic coroutine until it is depleted
                while (routine.MoveNext())
                {
                    yield return routine.Current;
                }

                // Restart the onLogic coroutine
                routine = OnCoroutineUpdated(this);
            }
        }

        public override void OnExit(Transition transition)
        {
            if (_routine != null)
            {
                _runner.StopCoroutine(_routine);
                _routine = null;
            }
            CoroutineIsComplete = false;
            _iterationCount = 0;
            base.OnExit(transition);
        }
    }
}
