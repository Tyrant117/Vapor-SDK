using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using VaporXR.Interaction;

namespace VaporXR.Interaction
{
    public interface ISelectInteractable
    {
        #region Properties
        bool CanBeSelected { get; }
        bool IsSelected { get; }
        InteractableSelectMode SelectMode { get; set; }
        List<Interactor> InteractorsSelecting { get; }
        Interactor FirstInteractorSelecting { get; }
        bool IsFocused { get; }
        bool CanBeFocused { get; }
        List<IXRInteractionGroup> InteractionGroupsFocusing { get; }
        IXRInteractionGroup FirstInteractionGroupFocusing { get; }
        IXRFilterList<IXRSelectFilter> SelectFilters { get; }
        IXRFilterList<IXRInteractionStrengthFilter> InteractionStrengthFilters { get; }
        IReadOnlyBindableVariable<float> LargestInteractionStrength { get; }
        
        (int Before, int After) SelectCountBeforeAndAfterChange { get; }
        #endregion

        #region Events
        Func<bool> SelectableActive { get; set; }
        InteractableFocusMode FocusMode { get; }

        event Action<SelectEnterEventArgs> FirstSelectEntered;
        event Action<SelectEnterEventArgs> SelectEntering;
        event Action<SelectEnterEventArgs> SelectEntered;

        event Action<SelectExitEventArgs> SelectExiting;
        event Action<SelectExitEventArgs> SelectExited;
        event Action<SelectExitEventArgs> LastSelectExited;

        event Action<FocusEnterEventArgs> FirstFocusEntered;
        event Action<FocusEnterEventArgs> FocusEntered;

        event Action<FocusExitEventArgs> FocusExited;
        event Action<FocusExitEventArgs> LastFocusExited;
        #endregion

        #region Methods
        /// <summary>
        /// Determines if a given Interactor can select this Interactable.
        /// </summary>
        /// <param name="interactor">Interactor to check for a valid selection with.</param>
        /// <returns>Returns <see langword="true"/> if selection is valid this frame. Returns <see langword="false"/> if not.</returns>
        /// <seealso cref="VXRBaseInteractor.CanSelect"/>
        bool IsSelectableBy(Interactor interactor);
        /// <summary>
        /// Determines whether this Interactable is currently being selected by the Interactor.
        /// </summary>
        /// <param name="interactor">Interactor to check.</param>
        /// <returns>Returns <see langword="true"/> if this Interactable is currently being selected by the Interactor.
        /// Otherwise, returns <seealso langword="false"/>.</returns>
        /// <remarks>
        /// In other words, returns whether <see cref="InteractorsSelecting"/> contains <paramref name="interactor"/>.
        /// </remarks>
        /// <seealso cref="InteractorsSelecting"/>
        bool IsSelectedBy(Interactor interactor);

        Pose GetAttachPoseOnSelect(Interactor interactor);
        float GetInteractionStrength(Interactor interactor);
        Pose GetLocalAttachPoseOnSelect(Interactor interactor);

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method to signal to update the interaction strength.
        /// </summary>
        /// <param name="updatePhase">The update phase during which this method is called.</param>
        /// <seealso cref="GetInteractionStrength"/>
        /// <seealso cref="IXRInteractionStrengthInteractable.ProcessInteractionStrength"/>
        void ProcessInteractionStrength(XRInteractionUpdateOrder.UpdatePhase updatePhase);
        #endregion

        #region Event Callbacks
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method right
        /// before the Interactor first initiates selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is initiating the selection.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectEntered(SelectEnterEventArgs)"/>
        void OnSelectEntering(SelectEnterEventArgs args);
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interactor first initiates selection of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is initiating the selection.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectExited(SelectExitEventArgs)"/>
        void OnSelectEntered(SelectEnterEventArgs args);
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// right before the Interactor ends selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is ending the selection.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectExited(SelectExitEventArgs)"/>
        void OnSelectExiting(SelectExitEventArgs args);
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interactor ends selection of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is ending the selection.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectEntered(SelectEnterEventArgs)"/>
        void OnSelectExited(SelectExitEventArgs args);

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method right
        /// before the Interaction group first gains focus of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interaction group that is initiating focus.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnFocusEntered(FocusEnterEventArgs)"/>
        void OnFocusEntering(FocusEnterEventArgs args);
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interaction group first gains focus of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interaction group that is initiating the focus.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnFocusExited(FocusExitEventArgs)"/>
        void OnFocusEntered(FocusEnterEventArgs args);
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// right before the Interaction group loses focus of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interaction group that is losing focus.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnFocusExited(FocusExitEventArgs)"/>
        void OnFocusExiting(FocusExitEventArgs args);
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interaction group loses focus of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interaction group that is losing focus.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnFocusEntered(FocusEnterEventArgs)"/>
        void OnFocusExited(FocusExitEventArgs args);
        #endregion
    }
}
