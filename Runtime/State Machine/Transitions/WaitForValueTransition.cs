using System;

namespace VaporStateMachine
{
    public class WaitForValueTransition<T> : Transition
    {
        protected T _watch;
        protected Func<T, bool> _waitFor;

        public WaitForValueTransition(string from, string to, int desire, T watch, Func<T, bool> waitFor) : base(from, to, desire)
        {
            _watch = watch;
            _waitFor = waitFor;
            Condition = CoroutineComplete;
        }

        public WaitForValueTransition(State from, State to, int desire, T watch, Func<T, bool> waitFor) : base(from, to, desire)
        {
            _watch = watch;
            _waitFor = waitFor;
            Condition = CoroutineComplete;
        }

        protected WaitForValueTransition(int from, int to, int desire, bool inverse, T watch, Func<T, bool> waitFor) : base(from, to, desire, inverse)
        {
            _watch = watch;
            _waitFor = waitFor;
            Condition = CoroutineComplete;
        }

        private bool CoroutineComplete(Transition t)
        {
            return _waitFor.Invoke(_watch);
        }
    }
}
