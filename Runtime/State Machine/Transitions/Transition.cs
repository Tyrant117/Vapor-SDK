using System;

namespace VaporStateMachine
{
    public class Transition
    {
        public int From;
        public int To;
        public int Desire;

        public IStateMachine StateMachine;

        public Func<Transition, bool> Condition;
        public Action Exited;

        private readonly bool _inverse;

        /// <summary>
		/// Initialises a new instance of the TransitionBase class
		/// </summary>
		/// <param name="from">The name / identifier of the active state</param>
		/// <param name="to">The name / identifier of the next state</param>
        /// <param name="condition">A function that returns true if the state machine should transition to the <b>to</b> state</param>
        public Transition(string from, string to, int desire, Func<Transition, bool> condition = null)
        {
            From = from.GetHashCode();
            To = to.GetHashCode();
            Desire = desire;
            Condition = condition;
            _inverse = false;
        }

        public Transition(State from, State to, int desire, Func<Transition, bool> condition = null)
        {
            From = from.ID;
            To = to.ID;
            Desire = desire;
            Condition = condition;
            _inverse = false;
        }

        protected Transition(int from, int to, int desire, bool inverse, Func<Transition, bool> condition = null)
        {
            From = from;
            To = to;
            Desire = desire;
            _inverse = inverse;
            Condition = condition;
        }
        

        /// <summary>
		/// Called to initialise the transition, after values like mono and fsm have been set
		/// </summary>
		public virtual void Init()
        {

        }

        /// <summary>
		/// Called when the state machine enters the "from" state
		/// </summary>
		public virtual void OnEnter()
        {

        }

        /// <summary>
        /// Called when the state machine exits with this transition.
        /// </summary>
        public virtual void OnExit()
        {
            Exited?.Invoke();
        }

        /// <summary>
		/// Called to determin whether the state machine should transition to the <c>to</c> state
		/// </summary>
		/// <returns>True if the state machine should change states / transition</returns>
		public virtual bool ShouldTransition()
        {
            return _inverse ? Condition == null || !Condition(this) : Condition == null || Condition(this);
        }

        public Transition Reverse()
        {
            return new Transition(To, From, Desire, true, Condition);
        }
    }
}
