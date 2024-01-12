using VaporXR.UI;

namespace VaporXR
{
    /// <summary>
    /// Matches the UI Model to the state of the Interactor.
    /// </summary>
    public interface IUIInteractor
    {
        /// <summary>
        /// Updates the current UI Model to match the state of the Interactor.
        /// </summary>
        /// <param name="model">The returned model that will match this Interactor.</param>
        void UpdateUIModel(ref TrackedDeviceModel model);

        /// <summary>
        /// Attempts to retrieve the current UI Model.
        /// </summary>
        /// <param name="model">The returned model that reflects the UI state of this Interactor.</param>
        /// <returns>Returns <see langword="true"/> if the model was able to retrieved. Otherwise, returns <see langword="false"/>.</returns>
        bool TryGetUIModel(out TrackedDeviceModel model);
    }
}
