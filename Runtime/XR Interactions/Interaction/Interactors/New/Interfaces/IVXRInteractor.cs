using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR.Interactors
{
    public interface IVXRInteractor : IAttachPoint
    {
        VXRInteractionManager InteractionManager { get; set; }
        VXRCompositeInteractor Composite { get; }
        InteractionLayerMask InteractionLayers { get; }
        InteractorHandedness Handedness { get; }

        void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase);
        void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase);
        void GetValidTargets(List<IVXRInteractable> targets);
        void OnRegistered(InteractorRegisteredEventArgs args);
        void OnUnregistered(InteractorUnregisteredEventArgs args);
        bool TryGetSelectInteractor(out IVXRSelectInteractor interactor);
        bool TryGetHoverInteractor(out IVXRHoverInteractor interactor);
        bool TryGetXROrigin(out Transform origin);
    }
}
