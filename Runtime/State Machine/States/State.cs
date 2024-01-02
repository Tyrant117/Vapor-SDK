using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor;

namespace VaporStateMachine
{
    public class State
    {
        private static int? _emptyState;
        public static int EmptyState
        {
            get
            {
                _emptyState ??= "".GetKeyHashCode();
                return _emptyState.Value;
            }
        }

        /// <summary>
        /// The name of the state
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The ID of the state
        /// </summary>
        public int ID { get; }
        /// <summary>
        /// True if the state can exit immediately when it is transitioned to.
        /// </summary>
        public bool CanExitInstantly { get; }

        /// <summary>
        /// The state machine this state belongs to.
        /// </summary>
        public IStateMachine StateMachine { get; set; }
        /// <summary>
        /// The internal timer that tracks the since last entry to the state
        /// </summary>
        public Timer Timer { get; }

        /// <summary>
        /// True if the state is playing.
        /// </summary>
        public bool IsPlaying { get; protected set; }

        protected Action<State> Entered;
        protected Action<State> Updated;
        protected Action<State, Transition> Exited;
        protected Dictionary<int, Delegate> ActionsByEventMap = new();

        public State(string name, bool canExitInstantly)
        {
            Name = name;
            ID = name.GetKeyHashCode();
            CanExitInstantly = canExitInstantly;

            Timer = new Timer();
        }

        public void InitEvents(Action<State> entered = null, Action<State> updated = null, Action<State, Transition> exited = null)
        {
            Entered = entered;
            Updated = updated;
            Exited = exited;
        }

        public virtual void Init()
        {

        }

        public virtual void OnEnter()
        {
            IsPlaying = true;
            Timer.Reset();

            Entered?.Invoke(this);
        }

        public virtual void OnUpdate()
        {
            Updated?.Invoke(this);
        }

        public virtual void OnExit(Transition transition)
        {
            IsPlaying = false;
            Exited?.Invoke(this, transition);
        }

        public virtual void OnExitRequest(Transition transition = null)
        {
            StateMachine.StateCanExit(transition);
        }

        #region - Actions -

        protected void AddGenericAction(int actionID, Delegate action)
        {
            ActionsByEventMap ??= new Dictionary<int, Delegate>();
            ActionsByEventMap[actionID] = action;
        }

        /// <summary>
        /// Adds an action that can be called with OnAction(). Actions are like the builtin events
        /// OnEnter / OnLogic / ... but are defined by the user.
        /// </summary>
        /// <param name="actionID">ID of the action</param>
        /// <param name="action">Function that should be called when the action is run</param>
        /// <returns>Itself</returns>
        public State AddAction(int actionID, Action action)
        {
            AddGenericAction(actionID, action);
            return this;
        }

        /// <summary>
        /// Adds an action that can be called with OnAction{T}(). This overload allows you to
        /// run a function that takes one data parameter.
        /// Actions are like the builtin events OnEnter / OnLogic / ... but are defined by the user.
        /// </summary>
        /// <param name="actionID">ID of the action</param>
        /// <param name="action">Function that should be called when the action is run</param>
        /// <typeparam name="TData">Data type of the parameter of the function</typeparam>
        /// <returns>Itself</returns>
        public State AddAction<TData>(int actionID, Action<TData> action)
        {
            AddGenericAction(actionID, action);
            return this;
        }

        protected TTarget TryGetAndCastAction<TTarget>(int actionID) where TTarget : Delegate
        {
            if (!ActionsByEventMap.TryGetValue(actionID, out var action))
            {
                return null;
            }

            if (action is TTarget target)
            {
                return target;
            }

            Debug.LogError(StateMachineExceptions.ActionTypeMismatch(typeof(TTarget), action));
            return null;
        }

        /// <summary>
        /// Runs an action with the given name.
        /// If the action is not defined / hasn't been added, nothing will happen.
        /// </summary>
        /// <param name="actionID">Name of the action</param>
        public void OnAction(int actionID) => TryGetAndCastAction<Action>(actionID)?.Invoke();

        /// <summary>
        /// Runs an action with a given name and lets you pass in one parameter to the action function.
        /// If the action is not defined / hasn't been added, nothing will happen.
        /// </summary>
        /// <param name="actionID">Name of the action</param>
        /// <param name="data">Data to pass as the first parameter to the action</param>
        /// <typeparam name="TData">Type of the data parameter</typeparam>
        public void OnAction<TData>(int actionID, TData data) => TryGetAndCastAction<Action<TData>>(actionID)?.Invoke(data);

        #endregion

        #region - Pooling -

        public virtual void RemoveFromPool()
        {

        }

        public virtual void OnReturnedToPool()
        {
            // Clear StateMachine
            StateMachine = null;
            // Clear Events
            Entered = null;
            Updated = null;
            Exited = null;
            // Clear Actions
            ActionsByEventMap.Clear();
        }

        #endregion
    }
}
