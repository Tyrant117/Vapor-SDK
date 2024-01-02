using System;

namespace VaporStateMachine
{
    /// <summary>
    /// A transition that waits until the coroutine running in a <see cref="CoroutineState"/> is complete then returns true.
    /// </summary>
    public class CoroutineStateCompleteTransition : Transition
    {
        protected readonly CoroutineState WatchingState;

        public CoroutineStateCompleteTransition(string from, string to, int desire, CoroutineState state) : base(from, to, desire)
        {
            WatchingState = state;
            Condition = CoroutineComplete;
        }

        public CoroutineStateCompleteTransition(State from, State to, int desire, CoroutineState state) : base(from, to, desire)
        {
            WatchingState = state;
            Condition = CoroutineComplete;
        }
        
        protected CoroutineStateCompleteTransition(int from, int to, int desire, Func<Transition, bool> condition, CoroutineState state) : base(from, to, desire, true, condition)
        {
            WatchingState = state;
        }

        private bool CoroutineComplete(Transition t)
        {
            return WatchingState.CoroutineIsComplete;
        }

        public override Transition Reverse()
        {
            return new CoroutineStateCompleteTransition(From, To, Desire, CoroutineComplete, WatchingState);
        }
    }
}
