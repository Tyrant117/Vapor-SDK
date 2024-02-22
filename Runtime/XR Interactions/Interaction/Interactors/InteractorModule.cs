using System;
using UnityEngine;

namespace VaporXR.Interaction
{
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Interactors)]
    public abstract class InteractorModule : MonoBehaviour, IAttachPoint
    {
        public Interactor Interactor { get; private set; }

        public VXRInteractionManager InteractionManager => Interactor.InteractionManager;

        public Transform AttachPoint => Interactor.AttachPoint;


        protected virtual void Awake()
        {
            if (TryGetComponent<Interactor>(out var interactor))
            {
                Interactor = interactor;
            }
            else
            {
                Debug.LogError($"Interactor does not exist on GameObject {name}. One must be added for functionality to work.");
            }
        }

        public virtual bool CanHover(Interactable interactable)
        {
            return true;
        }

        public virtual bool CanSelect(Interactable interactable)
        {
            return true;
        }

        public virtual void PrePreProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
        }

        public virtual void PostPreProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
        }

        public virtual void PreProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
        }

        public virtual void PostProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
        }

        public Transform GetAttachTransform(Interactable interactable)
        {
            return Interactor.GetAttachTransform(interactable);
        }
    }
}
