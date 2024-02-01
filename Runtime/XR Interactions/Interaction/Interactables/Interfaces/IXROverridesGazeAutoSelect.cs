namespace VaporXR
{
    /// <summary>
    /// An interface that represents an interactable that provides
    /// overrides of the default values for hover to select and auto deselect.
    /// </summary>
    /// <seealso cref="XRBaseInteractable"/>
    /// <seealso cref="XRGazeInteractor.GetHoverTimeToSelect"/>
    /// <seealso cref="XRGazeInteractor.GetTimeToAutoDeselect"/>
    public interface IXROverridesGazeAutoSelect
    {
        /// <summary>
        /// Enables this interactable to override the <see cref="VXRRayInteractor.HoverTimeToSelect"/> on a <see cref="VXRGazeInteractor"/>.
        /// </summary>
        /// <seealso cref="GazeTimeToSelect"/>
        /// <seealso cref="XRRayInteractor.hoverToSelect"/>
        public bool OverrideGazeTimeToSelect { get; }

        /// <summary>
        /// Number of seconds for which an <see cref="XRGazeInteractor"/> must hover over this interactable to select it if <see cref="XRRayInteractor.hoverToSelect"/> is enabled.
        /// </summary>
        /// <seealso cref="OverrideGazeTimeToSelect"/>
        /// <seealso cref="XRRayInteractor.hoverTimeToSelect"/>
        public float GazeTimeToSelect { get; }

        /// <summary>
        /// Enables this interactable to override the <see cref="XRRayInteractor.timeToAutoDeselect"/> on a <see cref="XRGazeInteractor"/>.
        /// </summary>
        /// <seealso cref="TimeToAutoDeselectGaze"/>
        /// <seealso cref="XRRayInteractor.autoDeselect"/>
        public bool OverrideTimeToAutoDeselectGaze { get; }

        /// <summary>
        /// Number of seconds that the interactable will remain selected by a <see cref="XRGazeInteractor"/> before being
        /// automatically deselected if <see cref="OverrideTimeToAutoDeselectGaze"/> is true.
        /// </summary>
        /// <seealso cref="OverrideTimeToAutoDeselectGaze"/>
        public float TimeToAutoDeselectGaze { get; }
    }
}
