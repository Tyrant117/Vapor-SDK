using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Interactors;

namespace VaporXR
{
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Interactors)]
    public abstract class VXRCompositeInteractor : MonoBehaviour, IAttachPoint
    {
        protected virtual bool RequiresHoverInteractor => false;
        protected virtual bool RequiresSelectInteractor => false;

        
        [FoldoutGroup("Components"), SerializeField, ShowIf("$RequiresHoverInteractor"), InlineButton("AddHoverInteractor", label: "+")]
        private VXRHoverInteractor _hover;
        [FoldoutGroup("Components"), SerializeField, ShowIf("$RequiresSelectInteractor"), InlineButton("AddSelectInteractor",label: "+")]
        private VXRSelectInteractor _select;
        [FoldoutGroup("Components"), SerializeField]
        private Transform _attachPoint;

#pragma warning disable IDE0051 // Remove unused private members
        private void AddHoverInteractor() { if (!_hover) _hover = gameObject.AddComponent<VXRHoverInteractor>(); }
        private void AddSelectInteractor() { if (!_select) _select = gameObject.AddComponent<VXRSelectInteractor>(); }
#pragma warning restore IDE0051 // Remove unused private members

        public VXRHoverInteractor Hover => _hover;
        public bool HasHover => Hover != null && Hover.HasHover;
        public VXRSelectInteractor Select => _select;
        public bool HasSelection => Select != null && Select.HasSelection;

        public Transform AttachPoint { get => _attachPoint; protected set => _attachPoint = value; }

        protected VXRInteractionManager _interactionManager;
        public VXRInteractionManager InteractionManager => _interactionManager;


        protected virtual void Awake()
        {
            if (!_attachPoint)
            {
                _attachPoint = transform;
            }
            _FindCreateInteractionManager();

            void _FindCreateInteractionManager()
            {
                if (_interactionManager == null)
                {
                    _interactionManager = ComponentLocatorUtility<VXRInteractionManager>.FindOrCreateComponent();
                }
            }
        }

        public virtual bool IsHovering(IXRInteractable interactable)
        {
            return _hover != null && _hover.IsHovering(interactable);
        }

        public virtual bool CanHover(IXRHoverInteractable interactable)
        {
            return true;
        }

        public virtual bool IsSelecting(IXRInteractable interactable)
        {
            return _select != null && _select.IsSelecting(interactable);
        }

        public virtual bool CanSelect(IXRSelectInteractable interactable)
        {
            return true;
        }

        public Transform GetAttachTransform(IXRInteractable interactable)
        {
            return _attachPoint;
        }
    }
}
