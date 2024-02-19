using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using VaporXR.Interactors;

namespace VaporXR
{
    public interface IVXRSelectInteractable : IVXRInteractable
    {
        bool CanBeSelected { get; }
        bool IsSelected { get; }
        InteractableSelectMode SelectMode { get; set; }
        List<IVXRSelectInteractor> InteractorsSelecting { get; }
        IVXRSelectInteractor FirstInteractorSelecting { get; }
        bool IsFocused { get; }
        bool CanBeFocused { get; }
        List<IXRInteractionGroup> InteractionGroupsFocusing { get; }
        IXRInteractionGroup FirstInteractionGroupFocusing { get; }
        IXRFilterList<IXRSelectFilter> SelectFilters { get; }
        IXRFilterList<IXRInteractionStrengthFilter> InteractionStrengthFilters { get; }
        IReadOnlyBindableVariable<float> LargestInteractionStrength { get; }
        Func<bool> SelectableActive { get; set; }
        (int Before, int After) SelectCountBeforeAndAfterChange { get; }

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

        Pose GetAttachPoseOnSelect(IVXRInteractor interactor);
        float GetInteractionStrength(IVXRSelectInteractor interactor);
        Pose GetLocalAttachPoseOnSelect(IVXRInteractor interactor);
        bool IsSelectableBy(IVXRSelectInteractor interactor);
        bool IsSelectedBy(IVXRSelectInteractor interactor);
        void OnFocusEntered(FocusEnterEventArgs args);
        void OnFocusEntering(FocusEnterEventArgs args);
        void OnFocusExited(FocusExitEventArgs args);
        void OnFocusExiting(FocusExitEventArgs args);
        void OnSelectEntered(SelectEnterEventArgs args);
        void OnSelectEntering(SelectEnterEventArgs args);
        void OnSelectExited(SelectExitEventArgs args);
        void OnSelectExiting(SelectExitEventArgs args);

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method to signal to update the interaction strength.
        /// </summary>
        /// <param name="updatePhase">The update phase during which this method is called.</param>
        /// <seealso cref="GetInteractionStrength"/>
        /// <seealso cref="IXRInteractionStrengthInteractable.ProcessInteractionStrength"/>
        void ProcessInteractionStrength(XRInteractionUpdateOrder.UpdatePhase updatePhase);
    }
}
