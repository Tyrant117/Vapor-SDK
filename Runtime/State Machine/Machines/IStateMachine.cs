using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporStateMachine
{
    public interface IStateMachine
    {
        bool IsRoot { get; }

        /// <summary>
        /// Tells the state machine that, if there is a state transition pending,
        /// now is the time to perform it.
        /// </summary>
        void StateCanExit(Transition transition = null);

        void RequestStateChange(int name, bool force = false);
        void RequestStateChange(Transition transition, bool force = false);
        void RequestStateChange(int layer, int name, bool force = false);
        void RequestStateChange(int layer, Transition transition, bool force = false);
        void AttachLogger(StateLogger logger);
        void AttachSubLayerLogger(StateLogger.LayerLog logger);
    }
}
