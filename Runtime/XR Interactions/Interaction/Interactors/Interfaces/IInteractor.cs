using System.Collections.Generic;
using UnityEngine;

namespace VaporXR.Interaction
{
    public interface IInteractor : IHoverInteractor, ISelectInteractor, IAttachPoint
    {
        VXRInteractionManager InteractionManager { get; set; }
        List<InteractorModule> Modules { get; }
        HashSet<int> InteractionLayers { get; }
        InteractorHandedness Handedness { get; }

        void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase);
        void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase);
        void GetValidTargets(List<Interactable> targets);
        void OnRegistered(InteractorRegisteredEventArgs args);
        void OnUnregistered(InteractorUnregisteredEventArgs args);
        bool TryGetXROrigin(out Transform origin);
        T GetModule<T>() where T : InteractorModule;
        bool TryGetModule<T>(out T module) where T : InteractorModule;
        bool HasModule<T>() where T : InteractorModule;
    }
}
