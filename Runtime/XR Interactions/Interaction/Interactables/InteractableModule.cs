using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporXR.Interaction;

namespace VaporXR
{
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Interactables)]
    public abstract class InteractableModule : MonoBehaviour
    {
        public Interactable Interactable { get; private set; }

        public VXRInteractionManager InteractionManager => Interactable.InteractionManager;

        protected virtual void Awake()
        {
            if(TryGetComponent<Interactable>(out var interactable))
            {
                Interactable = interactable;
            }
            else
            {
                Debug.LogError($"Interactable does not exist on GameObject {name}. One must be added for functionality to work.");
            }
        }


        public virtual bool IsHoverableBy(Interactor interactor)
        {
            return true;
        }

        public virtual bool IsSelectableBy(Interactor interactor)
        {
            return true;
        }

        public virtual void PostProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
        }

        public virtual void PreProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
        }
    }
}
