using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporStateMachine
{
    /// <summary>
    /// A bundle of a state together with the outgoing transitions and trigger transitions.
    /// It's useful, as you only need to do one Dictionary lookup for these three items.
    /// => Much better performance
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

            if (!TriggerToTransitions.TryGetValue(trigger, out List<Transition> transitionsOfTrigger))
            {
                transitionsOfTrigger = new List<Transition>();
                TriggerToTransitions.Add(trigger, transitionsOfTrigger);
            }
            transitionsOfTrigger.Add(transition);
        }
    }
}
