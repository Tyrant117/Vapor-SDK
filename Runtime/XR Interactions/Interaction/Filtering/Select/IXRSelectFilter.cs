using System;
using VaporXR.Interactors;

namespace VaporXR
{
    /// <summary>
    /// Instances that implement this interface are called select filters. Select filters process additional validation checks
    /// after the base class select validation checks are processed.
    /// Add a select filter to the following objects to extend its select validations:
    /// <list type="bullet">
    /// <item>
    /// <description><see cref="VXRInteractionManager"/>: to add a global select filter used to validate all select
    /// interactions in the manager.</description>
    /// </item>
    /// <item>
    /// <description><see cref="XRBaseInteractor"/>: to add an Interactor select filter used to validate the select
    /// interactions in the Interactor.</description>
    /// </item>
    /// <item>
    /// <description><see cref="XRBaseInteractable"/>: to add an Interactable select filter used to validate the
    /// select interactions in the Interactable.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <seealso cref="VXRInteractionManager.startingSelectFilters"/>
    /// <seealso cref="VXRInteractionManager.selectFilters"/>
    /// <seealso cref="XRBaseInteractor.startingSelectFilters"/>
    /// <seealso cref="XRBaseInteractor.selectFilters"/>
    /// <seealso cref="XRBaseInteractable.startingSelectFilters"/>
    /// <seealso cref="XRBaseInteractable.selectFilters"/>
    /// <seealso cref="IXRHoverFilter"/>
    public interface IXRSelectFilter
    {
        /// <summary>
        /// Whether this select filter can process interactions.
        /// Select filters that can process interactions receive calls to <see cref="Process"/>, select filters that
        /// cannot process do not.
        /// </summary>
        /// <remarks>
        /// It's recommended to return <see cref="Behaviour.isActiveAndEnabled"/> when implementing this interface
        /// in a <see cref="MonoBehaviour"/>.
        /// </remarks>
        bool CanProcess { get; }

        /// <summary>
        /// Called by the host object (<see cref="VXRInteractionManager"/>, <see cref="XRBaseInteractor"/> or
        /// <see cref="XRBaseInteractable"/>) to verify if the select interaction between the given Interactor and
        /// Interactable can be performed.
        /// </summary>
        /// <param name="interactor">The Interactor to validate the select interaction.</param>
        /// <param name="interactable">The Interactable to validate the select interaction.</param>
        /// <returns>
        /// Returns <see langword="true"/> when the given Interactor can select the given Interactable. Otherwise,
        /// returns <see langword="false"/>.
        /// </returns>
        bool Process(IVXRSelectInteractor interactor, IXRSelectInteractable interactable);
    }

    /// <summary>
    /// A select filter that forwards its processing to a delegate (<see cref="DelegateToProcess"/>).
    /// Useful to create custom filters by code without needing to create new classes.
    /// </summary>
    /// <seealso cref="XRHoverFilterDelegate"/>
    public sealed class XRSelectFilterDelegate : IXRSelectFilter
    {
        /// <summary>
        /// The delegate to be invoked when processing this filter.
        /// </summary>
        public Func<IVXRSelectInteractor, IXRSelectInteractable, bool> DelegateToProcess { get; set; }

        /// <inheritdoc />
        public bool CanProcess { get; set; } = true;

        /// <summary>
        /// Creates a new select filter delegate.
        /// </summary>
        /// <param name="delegateToProcess">The delegate to be invoked when processing this filter.</param>
        public XRSelectFilterDelegate(Func<IVXRSelectInteractor, IXRSelectInteractable, bool> delegateToProcess)
        {
            if (delegateToProcess == null)
                throw new ArgumentException(nameof(delegateToProcess));

            this.DelegateToProcess = delegateToProcess;
        }

        /// <inheritdoc />
        public bool Process(IVXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return DelegateToProcess.Invoke(interactor, interactable);
        }
    }
}
