using System.Collections.Generic;

namespace VaporStateMachine
{
    /// <summary>
    /// A bundle of a state together with the outgoing transitions and trigger transitions.
    /// </summary>
    internal class StateBundle
    {
        // By default, these fields are all null and only get a value when you need them
        // => Lazy evaluation => Memory efficient, when you only need a subset of features
        public State State;
        public List<Transition> Transitions;
        public Dictionary<int, List<Transition>> TriggerToTransitions;

        public void AddTransition(Transition t)
        {
            Transitions ??= new List<Transition>();
            Transitions.Add(t);
        }

        public void AddTriggerTransition(int trigger, Transition transition)
        {
            TriggerToTransitions ??= new Dictionary<int, List<Transition>>();

            if (!TriggerToTransitions.TryGetValue(trigger, out var transitionsOfTrigger))
            {
                transitionsOfTrigger = new List<Transition>();
                TriggerToTransitions.Add(trigger, transitionsOfTrigger);
            }
            transitionsOfTrigger.Add(transition);
        }
    }
}
