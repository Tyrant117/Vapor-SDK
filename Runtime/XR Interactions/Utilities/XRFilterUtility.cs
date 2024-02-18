using Vapor.Utilities;
using VaporXR.Interactors;

namespace VaporXR.Utilities
{
    /// <summary>
    /// Utility methods for hover and select filters.
    /// </summary>
    public static class XRFilterUtility
    {
        /// <summary>
        /// Returns the processing result of the given hover filters using the given Interactor and Interactable as
        /// parameters.
        /// </summary>
        /// <param name="filters">The hover filters to process.</param>
        /// <param name="interactor">The Interactor to be validate by the hover filters.</param>
        /// <param name="interactable">The Interactable to be validate by the hover filters.</param>
        /// <returns>
        /// Returns <see langword="true"/> if all processed filters also return <see langword="true"/>, or if
        /// <see cref="filters"/> is empty. Otherwise, returns <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This method will ensure that all changes are buffered when processing, the buffered changes are applied
        /// when the processing is finished.
        /// </remarks>
        public static bool Process(SmallRegistrationList<IXRHoverFilter> filters, IVXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
            if (filters.RegisteredSnapshot.Count == 0)
            {
                return true;
            }

            var alreadyBufferingChanges = filters.BufferChanges;
            filters.BufferChanges = true;
            var result = true;
            try
            {
                foreach (var filter in filters.RegisteredSnapshot)
                {
                    if (!filter.CanProcess || filter.Process(interactor, interactable))
                    {
                        continue;
                    }

                    result = false;
                    break;
                }
            }
            finally
            {
                if (!alreadyBufferingChanges)
                    filters.BufferChanges = false;
            }

            return result;
        }

        /// <summary>
        /// Returns the processing result of the given select filters using the given Interactor and Interactable as
        /// parameters.
        /// </summary>
        /// <param name="filters">The select filters to process.</param>
        /// <param name="interactor">The Interactor to be validate by the select filters.</param>
        /// <param name="interactable">The Interactable to be validate by the select filters.</param>
        /// <returns>
        /// Returns <see langword="true"/> if all processed filters also return <see langword="true"/>, or if
        /// <see cref="filters"/> is empty. Otherwise, returns <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This method will ensure that all changes are buffered when processing, the buffered changes are applied
        /// when the processing is finished.
        /// </remarks>
        public static bool Process(SmallRegistrationList<IXRSelectFilter> filters, IVXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            if (filters.RegisteredSnapshot.Count == 0)
            {
                return true;
            }

            var alreadyBufferingChanges = filters.BufferChanges;
            filters.BufferChanges = true;
            var result = true;
            try
            {
                foreach (var filter in filters.RegisteredSnapshot)
                {
                    if (!filter.CanProcess || filter.Process(interactor, interactable))
                    {
                        continue;
                    }

                    result = false;
                    break;
                }
            }
            finally
            {
                if (!alreadyBufferingChanges)
                    filters.BufferChanges = false;
            }

            return result;
        }

        /// <summary>
        /// Returns the processing result of the given interaction strength filters using the given Interactor and Interactable as
        /// parameters.
        /// </summary>
        /// <param name="filters">The interaction strength filters to process.</param>
        /// <param name="interactor">The Interactor to process by the interaction strength filters.</param>
        /// <param name="interactable">The Interactable to process by the interaction strength filters.</param>
        /// <param name="interactionStrength">The interaction strength before processing.</param>
        /// <returns>Returns the modified interaction strength that is the result of passing the interaction strength through each filter.</returns>
        /// <remarks>
        /// This method will ensure that all changes are buffered when processing, the buffered changes are applied
        /// when the processing is finished.
        /// </remarks>
        public static float Process(SmallRegistrationList<IXRInteractionStrengthFilter> filters, IVXRSelectInteractor interactor, IXRInteractable interactable, float interactionStrength)
        {
            if (filters.RegisteredSnapshot.Count == 0)
                return interactionStrength;

            var alreadyBufferingChanges = filters.BufferChanges;
            filters.BufferChanges = true;
            try
            {
                foreach (var filter in filters.RegisteredSnapshot)
                {
                    if (filter.CanProcess)
                    {
                        interactionStrength = filter.Process(interactor, interactable, interactionStrength);
                    }
                }
            }
            finally
            {
                if (!alreadyBufferingChanges)
                    filters.BufferChanges = false;
            }

            return interactionStrength;
        }
    }
}
