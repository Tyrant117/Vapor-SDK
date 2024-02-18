﻿using System;
using System.Collections.Generic;
using UnityEngine;
using VaporXR.Interactors;

namespace VaporXR
{
    /// <summary>
    /// An interface that represents an Interactable component which
    /// an Interactor component can select.
    /// </summary>
    /// <seealso cref="VXRBaseInteractor"/>
    public interface IXRSelectInteractable : IXRInteractable
    {
        /// <summary>
        /// The event that is called only when the first Interactor begins selecting
        /// this Interactable as the sole selecting Interactor. Subsequent Interactors that
        /// begin selecting this Interactable will not cause this event to be invoked as
        /// long as any others are still selecting.
        /// </summary>
        /// <remarks>
        /// The <see cref="SelectEnterEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="LastSelectExited"/>
        /// <seealso cref="SelectEntered"/>
        event Action<SelectEnterEventArgs> FirstSelectEntered;

        /// <summary>
        /// The event that is called only when the last remaining selecting Interactor
        /// ends selecting this Interactable.
        /// </summary>
        /// <remarks>
        /// The <see cref="SelectExitEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="FirstSelectEntered"/>
        /// <seealso cref="SelectExited"/>
        event Action<SelectExitEventArgs> LastSelectExited;

        /// <summary>
        /// The event that is called when an Interactor begins selecting this Interactable.
        /// </summary>
        /// <remarks>
        /// The <see cref="SelectEnterEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="SelectExited"/>
        event Action<SelectEnterEventArgs> SelectEntered;

        /// <summary>
        /// The event that is called when an Interactor ends selecting this Interactable.
        /// </summary>
        /// <remarks>
        /// The <see cref="SelectExitEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="SelectEntered"/>
        event Action<SelectExitEventArgs> SelectExited;

        /// <summary>
        /// (Read Only) The list of Interactors currently selecting this Interactable (may by empty).
        /// </summary>
        /// <remarks>
        /// You should treat this as a read only view of the list and should not modify it.
        /// It is exposed as a <see cref="List{T}"/> rather than an <see cref="IReadOnlyList{T}"/> to avoid GC Allocations
        /// when enumerating the list.
        /// </remarks>
        /// <seealso cref="IsSelected"/>
        /// <seealso cref="VXRBaseInteractor.InteractablesSelected"/>
        List<IVXRSelectInteractor> InteractorsSelecting { get; }

        /// <summary>
        /// (Read Only) The first interactor that selected this interactable since not being selected by any interactor.
        /// The interactor may not currently be selecting this interactable, which would be the case
        /// when it released while multiple interactors were selecting this interactable.
        /// </summary>
        /// <seealso cref="VXRBaseInteractor.FirstInteractableSelected"/>
        IVXRSelectInteractor FirstInteractorSelecting { get; }


        /// <summary>
        /// Indicates whether this interactable can be selected by an interactor.
        /// </summary>
        bool CanSelect { get; set; }

        /// <summary>
        /// (Read Only) Indicates whether this interactable is currently being selected by any interactor.
        /// </summary>
        /// <remarks>
        /// In other words, returns whether <see cref="InteractorsSelecting"/> contains any interactors.
        /// <example>
        /// <code>interactorsSelecting.Count > 0</code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InteractorsSelecting"/>
        /// <seealso cref="VXRBaseInteractor.HasSelection"/>
        bool IsSelected { get; }

        /// <summary>
        /// Indicates the selection policy of an Interactable.
        /// </summary>
        /// <seealso cref="InteractableSelectMode"/>
        InteractableSelectMode SelectMode { get; }

        /// <summary>
        /// Determines if a given Interactor can select this Interactable.
        /// </summary>
        /// <param name="interactor">Interactor to check for a valid selection with.</param>
        /// <returns>Returns <see langword="true"/> if selection is valid this frame. Returns <see langword="false"/> if not.</returns>
        /// <seealso cref="VXRBaseInteractor.CanSelect"/>
        bool IsSelectableBy(IVXRSelectInteractor interactor);

        /// <summary>
        /// Gets the world position and rotation of the Attach Transform captured during the moment of selection.
        /// </summary>
        /// <param name="interactor">The specific Interactor as context to get the attachment point for.</param>
        /// <returns>Returns the world pose of the attachment point during the moment of selection,
        /// and otherwise the identity <see cref="Pose"/> if it was not selected by it during the current selection stack.</returns>
        /// <seealso cref="GetLocalAttachPoseOnSelect"/>
        /// <seealso cref="IXRInteractable.GetAttachTransform"/>
        /// <seealso cref="VXRBaseInteractor.GetAttachPoseOnSelect"/>
        Pose GetAttachPoseOnSelect(IVXRInteractor interactor);

        /// <summary>
        /// Gets the local position and rotation of the Attach Transform captured during the moment of selection.
        /// </summary>
        /// <param name="interactor">The specific Interactor as context to get the attachment point for.</param>
        /// <returns>Returns the local pose of the attachment point during the moment of selection,
        /// and otherwise the identity <see cref="Pose"/> if it was not selected by it during the current selection stack.</returns>
        /// <seealso cref="GetAttachPoseOnSelect"/>
        /// <seealso cref="IXRInteractable.GetAttachTransform"/>
        /// <seealso cref="VXRBaseInteractor.GetLocalAttachPoseOnSelect"/>
        Pose GetLocalAttachPoseOnSelect(IVXRInteractor interactor);

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// right before the Interactor first initiates selection of an Interactable
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
    }

    /// <summary>
    /// Extension methods for <see cref="IXRSelectInteractable"/>.
    /// </summary>
    /// <seealso cref="IXRSelectInteractable"/>
    public static class XRSelectInteractableExtensions
    {
        /// <summary>
        /// Gets the oldest interactor currently selecting this interactable.
        /// This is a convenience method for when the interactable does not support being selected by multiple interactors at a time.
        /// </summary>
        /// <param name="interactable">The interactable to operate on.</param>
        /// <returns>Returns the oldest interactor currently selecting this interactable.</returns>
        /// <remarks>
        /// Equivalent to <code>interactorsSelecting.Count > 0 ? interactorsSelecting[0] : null</code>
        /// </remarks>
        /// <seealso cref="IXRSelectInteractable.InteractorsSelecting"/>
        public static IVXRSelectInteractor GetOldestInteractorSelecting(this IXRSelectInteractable interactable) =>
            interactable?.InteractorsSelecting.Count > 0 ? interactable.InteractorsSelecting[0] : null;

        /// <summary>
        /// Gets whether the interactable is currently being selected by an interactor associated with the left hand or controller.
        /// </summary>
        /// <param name="interactable">The interactable to operate on.</param>
        /// <returns>Returns <see langword="true"/> if any interactor currently selecting this interactable has <see cref="IXRInteractor.Handedness"/> of <see cref="InteractorHandedness.Left"/>.</returns>
        /// <remarks>
        /// This method will return <see langword="true"/> even if it is not exclusively being selected by the left hand or controller.
        /// In other words, it will still return <see langword="true"/> if the interactable is also being selected by
        /// an interactor associated with the right hand or controller.
        /// </remarks>
        /// <seealso cref="IsSelectedByRight"/>
        /// <seealso cref="IXRInteractor.Handedness"/>
        public static bool IsSelectedByLeft(this IXRSelectInteractable interactable) =>
            IsSelectedBy(interactable, InteractorHandedness.Left);

        /// <summary>
        /// Gets whether the interactable is currently being selected by an interactor associated with the right hand or controller.
        /// </summary>
        /// <param name="interactable">The interactable to operate on.</param>
        /// <returns>Returns <see langword="true"/> if any interactor currently selecting this interactable has <see cref="IXRInteractor.Handedness"/> of <see cref="InteractorHandedness.Right"/>.</returns>
        /// <remarks>
        /// This method will return <see langword="true"/> even if it is not exclusively being selected by the right hand or controller.
        /// In other words, it will still return <see langword="true"/> if the interactable is also being selected by
        /// an interactor associated with the left hand or controller.
        /// </remarks>
        /// <seealso cref="IsSelectedByLeft"/>
        /// <seealso cref="IXRInteractor.Handedness"/>
        public static bool IsSelectedByRight(this IXRSelectInteractable interactable) =>
            IsSelectedBy(interactable, InteractorHandedness.Right);

        private static bool IsSelectedBy(IXRSelectInteractable interactable, InteractorHandedness handedness)
        {
            var interactorsSelecting = interactable.InteractorsSelecting;
            for (var i = 0; i < interactorsSelecting.Count; ++i)
            {
                if (interactorsSelecting[i].Handedness == handedness)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Options for the selection policy of an Interactable.
    /// </summary>
    /// <seealso cref="IXRSelectInteractable.SelectMode"/>
    public enum InteractableSelectMode
    {
        /// <summary>
        /// Allows the Interactable to only be selected by a single Interactor at a time
        /// and allows other Interactors to take selection by automatically deselecting.
        /// </summary>
        Single,

        /// <summary>
        /// Allows for multiple Interactors at a time to select the Interactable.
        /// </summary>
        /// <remarks>
        /// This option can be disabled in the Inspector window by adding the <see cref="CanSelectMultipleAttribute"/>
        /// with a value of <see langword="false"/> to a derived class of <see cref="XRBaseInteractable"/>.
        /// </remarks>
        Multiple,
    }
}