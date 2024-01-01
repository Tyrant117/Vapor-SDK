using UnityEngine;

namespace VaporStateMachine
{
    public class CoroutineStateCompleteTransition : Transition
    {
        protected CoroutineState _watchingState;

        public CoroutineStateCompleteTransition(string from, string to, int desire, CoroutineState state) : base(from, to, desire)
        {
            _watchingState = state;
            Condition = CoroutineComplete;
        }

        public CoroutineStateCompleteTransition(State from, State to, int desire, CoroutineState state) : base(from, to, desire)
        {
            _watchingState = state;
            Condition = CoroutineComplete;
        }

        protected CoroutineStateCompleteTransition(int from, int to, int desire, bool inverse, CoroutineState state) : base(from, to, desire, inverse)
        {
            _watchingState = state;
            Condition = CoroutineComplete;
        }

        private bool CoroutineComplete(Transition t)
        {
            //Debug.Log($"CoComp: {_watchingState.CoroutineIsComplete}");
            return _watchingState.CoroutineIsComplete;
        }
    }
}
