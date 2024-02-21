using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporXR.Interaction;

namespace VaporXR.Interaction
{
    //public interface ISelectInteractor : IInteractor, IXRGroupMember, IXRInteractionStrengthInteractor
    //{
    //    bool IsSelectActive { get; }
    //    /// <summary>
    //    /// (Read Only) Indicates whether this Interactor is currently selecting an Interactable.
    //    /// </summary>
    //    /// <remarks>
    //    /// In other words, returns whether <see cref="InteractablesSelected"/> contains any Interactables.
    //    /// <example>
    //    /// <code>interactablesSelected.Count > 0</code>
    //    /// </example>
    //    /// </remarks>
    //    /// <seealso cref="InteractablesSelected"/>
    //    /// <seealso cref="Interactable.IsSelected"/>
    //    bool HasSelection { get; }
    //    /// <summary>
    //    /// (Read Only) The list of Interactables that are currently being selected (may by empty).
    //    /// </summary>
    //    /// <remarks>
    //    /// This should be treated as a read only view of the list and should not be modified by external callers.
    //    /// It is exposed as a <see cref="List{T}"/> rather than an <see cref="IReadOnlyList{T}"/> to avoid GC Allocations
    //    /// when enumerating the list.
    //    /// </remarks>
    //    /// <seealso cref="HasSelection"/>
    //    /// <seealso cref="Interactable.InteractorsSelecting"/>
    //    List<Interactable> InteractablesSelected { get; }
    //    /// <summary>
    //    /// (Read Only) The first Interactable selected since not having any selection.
    //    /// This Interactor may not currently be selecting the Interactable, which would be the case
    //    /// when it was released while multiple Interactables were selected.
    //    /// </summary>
    //    /// <seealso cref="Interactable.FirstInteractorSelecting"/>
    //    Interactable FirstInteractableSelected { get; }
    //    bool KeepSelectedTargetValid { get; }
    //    /// <summary>
    //    /// Defines whether this interactor is performing a manual interaction or not.
    //    /// </summary>
    //    /// <seealso cref="StartManualInteraction(Interactable)"/>
    //    /// <seealso cref="EndManualInteraction"/>
    //    bool IsPerformingManualInteraction { get; }
    //    /// <summary>
    //    /// The list of select filters in this object.
    //    /// Used as additional select validations for this Interactor.
    //    /// </summary>
    //    /// <remarks>
    //    /// While processing select filters, all changes to this list don't have an immediate effect. Theses changes are
    //    /// buffered and applied when the processing is finished.
    //    /// Calling <see cref="IXRFilterList{T}.MoveTo"/> in this list will throw an exception when this list is being processed.
    //    /// </remarks>
    //    /// <seealso cref="ProcessSelectFilters"/>
    //    IXRFilterList<IXRSelectFilter> SelectFilters { get; }
    //    LogicalInputState LogicalSelectState { get; }
    //    Func<XRIneractionActiveState> SelectActive { get; set; }
    //    Func<MovementType> SelectedInteractableMovementTypeOverride { get; set; }

    //    /// <summary>
    //    /// The event that is called when this Interactor begins entering selecting an Interactable.
    //    /// </summary>
    //    /// <remarks>
    //    /// The <see cref="SelectEnterEventArgs"/> passed to each listener is only valid while the event is invoked,
    //    /// do not hold a reference to it.
    //    /// </remarks>
    //    event Action<SelectEnterEventArgs> SelectEntering;
    //    /// <summary>
    //    /// The event that is called when this Interactor begins selecting an Interactable.
    //    /// </summary>
    //    /// <remarks>
    //    /// The <see cref="SelectEnterEventArgs"/> passed to each listener is only valid while the event is invoked,
    //    /// do not hold a reference to it.
    //    /// </remarks>
    //    event Action<SelectEnterEventArgs> SelectEntered;
    //    /// <summary>
    //    /// The event that is called when this Interactor is exiting selecting an Interactable.
    //    /// </summary>
    //    /// <remarks>
    //    /// The <see cref="SelectEnterEventArgs"/> passed to each listener is only valid while the event is invoked,
    //    /// do not hold a reference to it.
    //    /// </remarks>
    //    event Action<SelectExitEventArgs> SelectExiting;
    //    /// <summary>
    //    /// The event that is called when this Interactor ends selecting an Interactable.
    //    /// </summary>
    //    /// <remarks>
    //    /// The <see cref="SelectEnterEventArgs"/> passed to each listener is only valid while the event is invoked,
    //    /// do not hold a reference to it.
    //    /// </remarks>
    //    event Action<SelectExitEventArgs> SelectExited;

    //    bool CanSelect(Interactable interactable);
    //    bool IsSelecting(Interactable interactable);
    //    void OnSelectEntered(SelectEnterEventArgs args);
    //    void OnSelectEntering(SelectEnterEventArgs args);
    //    void OnSelectExited(SelectExitEventArgs args);
    //    void OnSelectExiting(SelectExitEventArgs args);
    //}
}
