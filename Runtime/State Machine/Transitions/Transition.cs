using System;
using Vapor;

namespace VaporStateMachine
{
    public class Transition
    {
        public int From { get; }
        public int To { get; }
        public int Desire { get; }

        public IStateMachine StateMachine { get; set; }

        protected Func<Transition, bool> Condition;
        public Action Exited;

        private readonly bool _inverse;

        /// <summary>
        /// Initialises a new instance of the TransitionBase class
        /// </summary>
        /// <param name="from">The name / identifier of the active state</param>
        /// <param name="to">The name / identifier of the next state</param>
        /// <param name="desire">The desire value of this transition. Higher is more desirable</param>
        /// <param name="condition">A function that returns true if the state machine should transition to the <b>to</b> state</param>
        public Transition(string from, string to, int desire, Func<Transition, bool> condition)
        {
            From = from.GetKeyHashCode();
            To = to.GetKeyHashCode();
            Desire = desire;
            Condition = condition;
            _inverse = false;
        }

        public Transition(State from, State to, int desire, Func<Transition, bool> condition)
        {
            From = from.ID;
            To = to.ID;
            Desire = desire;
            Condition = condition;
            _inverse = false;
        }
        
        protected Transition(string from, string to, int desire)
        {
            From = from.GetKeyHashCode();
            To = to.GetKeyHashCode();
            Desire = desire;
            _inverse = false;
        }

        protected Transition(State from, State to, int desire)
        {
            From = from.ID;
            To = to.ID;
            Desire = desire;
            _inverse = false;
        }

        protected Transition(int from, int to, int desire, bool inverse, Func<Transition, bool> condition)
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
		/// Called to determine whether the state machine should transition to the <c>To</c> state
		/// </summary>
		/// <returns>True if the state machine should change states / transition</returns>
		public virtual bool ShouldTransition()
        {
            return _inverse ? !Condition(this) : Condition(this);
        }

        public virtual Transition Reverse()
        {
            return new Transition(To, From, Desire, true, Condition);
        }
    }
}
