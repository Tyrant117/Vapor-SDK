﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// Locomotion provider that allows the user to move as if grabbing the whole world around them.
    /// When the controller moves, the XR Origin counter-moves in order to keep the controller fixed relative to the world.
    /// </summary>
    /// <seealso cref="TwoHandedGrabMoveProvider"/>
    /// <seealso cref="LocomotionProvider"/>
    public partial class GrabMoveProvider : ConstrainedMoveProvider
    {
        [SerializeField]
        Transform m_ControllerTransform;
        /// <summary>
        /// The controller Transform that will drive grab movement with its local position. Will use this GameObject's
        /// Transform if not set.
        /// </summary>
        public Transform controllerTransform
        {
            get => m_ControllerTransform;
            set
            {
                m_ControllerTransform = value;
                GatherControllerInteractors();
            }
        }

        [SerializeField]
        bool m_EnableMoveWhileSelecting;
        /// <summary>
        /// Controls whether to allow grab move locomotion while the controller is selecting an interactable.
        /// </summary>
        public bool enableMoveWhileSelecting
        {
            get => m_EnableMoveWhileSelecting;
            set => m_EnableMoveWhileSelecting = value;
        }

        [SerializeField]
        float m_MoveFactor = 1f;
        /// <summary>
        /// The ratio of actual movement distance to controller movement distance.
        /// </summary>
        public float moveFactor
        {
            get => m_MoveFactor;
            set => m_MoveFactor = value;
        }

        [SerializeField]
        ButtonInputProvider m_GrabMoveInput;

        /// <summary>
        /// Input data that will be used to perform grab movement while held.
        /// If the source is an Input Action, it must have a button-like interaction where phase equals performed when pressed.
        /// Typically a <see cref="ButtonControl"/> Control or a Value type action with a Press interaction.
        /// </summary>
        public ButtonInputProvider grabMoveInput
        {
            get => m_GrabMoveInput;
            set => m_GrabMoveInput = value;
        }

        /// <summary>
        /// Controls whether this provider can move the XR Origin.
        /// </summary>
        public bool canMove { get; set; } = true;

        bool m_IsMoving;

        Vector3 m_PreviousControllerLocalPosition;

        readonly List<IXRSelectInteractor> m_ControllerInteractors = new List<IXRSelectInteractor>();

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (m_ControllerTransform == null)
                m_ControllerTransform = transform;

            GatherControllerInteractors();
        }

        /// <inheritdoc/>
        protected override Vector3 ComputeDesiredMove(out bool attemptingMove)
        {
            attemptingMove = false;
            var xrOrigin = Mediator.XROrigin?.Origin;
            var wasMoving = m_IsMoving;
            m_IsMoving = canMove && IsGrabbing() && xrOrigin != null;
            if (!m_IsMoving)
                return Vector3.zero;

            var controllerLocalPosition = controllerTransform.localPosition;
            if (!wasMoving && m_IsMoving) // Cannot simply check locomotionPhase because it might always be in moving state, due to gravity application mode
            {
                // Do not move the first frame of grab
                m_PreviousControllerLocalPosition = controllerLocalPosition;
                return Vector3.zero;
            }

            attemptingMove = true;
            var originTransform = xrOrigin.transform;
            var move = originTransform.TransformVector(m_PreviousControllerLocalPosition - controllerLocalPosition) * m_MoveFactor;
            m_PreviousControllerLocalPosition = controllerLocalPosition;
            return move;
        }

        /// <summary>
        /// Determines whether grab move is active.
        /// </summary>
        /// <returns>Whether grab move is active.</returns>
        public bool IsGrabbing()
        {
            var isPerformed = m_GrabMoveInput.IsHeld;
            return isPerformed && (m_EnableMoveWhileSelecting || !ControllerHasSelection());
        }

        void GatherControllerInteractors()
        {
            m_ControllerInteractors.Clear();
            if (m_ControllerTransform != null)
                m_ControllerTransform.transform.GetComponentsInChildren(m_ControllerInteractors);
        }

        bool ControllerHasSelection()
        {
            for (var index = 0; index < m_ControllerInteractors.Count; ++index)
            {
                var interactor = m_ControllerInteractors[index];
                if (interactor.HasSelection)
                    return true;
            }

            return false;
        }
    }
}