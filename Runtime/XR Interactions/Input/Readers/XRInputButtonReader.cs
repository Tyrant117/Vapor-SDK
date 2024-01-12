namespace VaporXR
{
    /// <summary>
    /// Interface which allows for callers to read the button's state from an input source.
    /// </summary>
    public interface IXRInputButtonReader : IXRInputValueReader<float>
    {
        /// <summary>
        /// Read whether the button is currently performed, which typically means whether the button is being pressed.
        /// This is typically true for multiple frames.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if the button is performed. Otherwise, returns <see langword="false"/>.</returns>
        /// <remarks>
        /// For input actions, this depends directly on the interaction(s) driving the action (including the
        /// default interaction if no specific interaction has been added to the action or binding).
        /// </remarks>
        bool ReadIsPerformed();

        /// <summary>
        /// Read whether the button performed this frame, which typically means whether the button started being pressed during this frame.
        /// This is typically only true for one single frame.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if the button performed this frame. Otherwise, returns <see langword="false"/>.</returns>
        /// <remarks>
        /// For input actions, this depends directly on the interaction(s) driving the action (including the
        /// default interaction if no specific interaction has been added to the action or binding).
        /// </remarks>
        bool ReadWasPerformedThisFrame();
    }
}
