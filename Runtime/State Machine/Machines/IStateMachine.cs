namespace VaporStateMachine
{
    public interface IStateMachine
    {
        /// <summary>
        /// True if this state machine is the root of the tree.
        /// </summary>
        bool IsRoot { get; }

        /// <summary>
        /// Tells the state machine that, if there is a state transition pending,
        /// now is the time to perform it.
        /// </summary>
        void StateCanExit(Transition transition = null);

        /// <summary>
        /// Request a transition to the named state.
        /// </summary>
        /// <param name="name">The name of the state to transition to</param>
        /// <param name="force">If true, the transition will be forced through</param>
        void RequestStateChange(int name, bool force = false);
        /// <summary>
        /// Request a transition to the next state via transition.
        /// </summary>
        /// <param name="transition">The transition to use for the request</param>
        /// <param name="force">If true, the transition will be forced through</param>
        void RequestStateChange(Transition transition, bool force = false);
        /// <summary>
        /// Request a transition to the named state on a specific layer
        /// </summary>
        /// <param name="layer">The layer of the state</param>
        /// <param name="name">The name of the state to transition to</param>
        /// <param name="force">If true, the transition will be forced through</param>
        void RequestStateChange(int layer, int name, bool force = false);
        /// <summary>
        /// Request a transition to the next state via transition on a specific layer
        /// </summary>
        /// <param name="layer">The layer of the state</param>
        /// <param name="transition">The name of the state to transition to</param>
        /// <param name="force">If true, the transition will be forced through</param>
        void RequestStateChange(int layer, Transition transition, bool force = false);
        /// <summary>
        /// Attaches a logger to debug the states.
        /// </summary>
        /// <param name="logger">The logger to attach</param>
        void AttachLogger(StateLogger logger);
        /// <summary>
        /// Recursively attach loggers down the state tree.
        /// </summary>
        /// <param name="logger">The layer logger to attach</param>
        void AttachSubLayerLogger(StateLogger.LayerLog logger);
    }
}
