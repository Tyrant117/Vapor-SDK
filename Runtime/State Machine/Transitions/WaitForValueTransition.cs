using System;

namespace VaporStateMachine
{
    /// <summary>
    /// Waits until a value is met then triggers the transition.
    /// </summary>
    /// <typeparam name="T">The type of the value to check against</typeparam>
    public class WaitForValueTransition<T> : Transition
    {
        protected readonly T Watch;
        protected readonly Func<T, bool> WaitFor;

        public WaitForValueTransition(string from, string to, int desire, T watch, Func<T, bool> waitFor) : base(from, to, desire)
        {
            Watch = watch;
            WaitFor = waitFor;
            Condition = WaitForComplete;
        }

        public WaitForValueTransition(State from, State to, int desire, T watch, Func<T, bool> waitFor) : base(from, to, desire)
        {
            Watch = watch;
            WaitFor = waitFor;
            Condition = WaitForComplete;
        }

        protected WaitForValueTransition(int from, int to, int desire, T watch, Func<T, bool> waitFor, Func<Transition, bool> condition) : base(from, to, desire, true, condition)
        {
            Watch = watch;
            WaitFor = waitFor;
        }

        private bool WaitForComplete(Transition t)
        {
            return WaitFor.Invoke(Watch);
        }

        public override Transition Reverse()
        {
            return new WaitForValueTransition<T>(From, To, Desire, Watch, WaitFor, WaitForComplete);
        }
    }
}
