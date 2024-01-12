using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using VaporInspector;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// Locomotion provider that allows the user to climb a <see cref="ClimbInteractable"/> by selecting it.
    /// Climb locomotion moves the XR Origin counter to movement of the last selecting interactor, with optional
    /// movement constraints along each axis of the interactable.
    /// </summary>
    /// <seealso cref="ClimbInteractable"/>
    public class ClimbProvider : LocomotionProvider
    {
        #region Inspector
        [SerializeField, FoldoutGroup("Climb Settings")] 
        [RichTextTooltip("Climb locomotion settings. Can be overridden by the Climb Interactable used for locomotion.")]
        private ClimbSettingsDatumProperty _climbSettings = new(new ClimbSettings());
        #endregion

        #region Properties
        /// <summary>
        /// Climb locomotion settings. Can be overridden by the <see cref="ClimbInteractable"/> used for locomotion.
        /// </summary>
        public ClimbSettingsDatumProperty ClimbSettings { get => _climbSettings; set => _climbSettings = value; }

        /// <summary>
        /// The interactable that is currently grabbed and driving movement. This will be <see langword="null"/> if
        /// there is no active climb.
        /// </summary>
        public ClimbInteractable ClimbAnchorInteractable
        {
            get
            {
                if (_grabbedClimbables.Count > 0)
                    return _grabbedClimbables[_grabbedClimbables.Count - 1];
                return null;
            }
        }

        /// <summary>
        /// The interactor that is currently grabbing and driving movement. This will be <see langword="null"/> if
        /// there is no active climb.
        /// </summary>
        public VXRBaseInteractor ClimbAnchorInteractor => _grabbingInteractors.Count > 0 ? _grabbingInteractors[^1] : null;

        /// <summary>
        /// The transformation that is used by this component to apply climb movement.
        /// </summary>
        public XROriginMovement Transformation { get; set; } = new() { forceUnconstrained = true };
        #endregion

        #region Fields
        // These are parallel lists, where each interactor and its grabbed interactable share the same index in each list.
        // The last item in each list represents the most recent selection, which is the only one that actually drives movement.
        private readonly List<VXRBaseInteractor> _grabbingInteractors = new();
        private readonly List<ClimbInteractable> _grabbedClimbables = new();

        private Vector3 _interactorAnchorWorldPosition;
        private Vector3 _interactorAnchorClimbSpacePosition;
        #endregion

        #region Events
        /// <summary>
        /// Calls the methods in its invocation list when the provider updates <see cref="ClimbAnchorInteractable"/>
        /// and <see cref="ClimbAnchorInteractor"/>. This can be invoked from either <see cref="StartClimbGrab"/> or
        /// <see cref="FinishClimbGrab"/>. This is not invoked when climb locomotion ends.
        /// </summary>
        public event Action<ClimbProvider> ClimbAnchorUpdated;
        #endregion

        protected override void Awake()
        {
            base.Awake();
            if (_climbSettings?.Value == null)
                _climbSettings = new ClimbSettingsDatumProperty(new ClimbSettings());
        }

        /// <summary>
        /// Starts a grab as part of climbing <paramref name="climbInteractable"/>, using the position of
        /// <paramref name="interactor"/> to drive movement.
        /// </summary>
        /// <param name="climbInteractable">The object to climb.</param>
        /// <param name="interactor">The interactor that initiates the grab and drives movement.</param>
        /// <remarks>
        /// This puts the <see cref="LocomotionProvider.LocomotionState"/> in the <see cref="LocomotionState.Preparing"/>
        /// state if locomotion has not already started. The phase will then enter the <see cref="LocomotionState.Moving"/>
        /// state in the next <see cref="Update"/>.
        /// </remarks>
        public void StartClimbGrab(ClimbInteractable climbInteractable, VXRBaseInteractor interactor)
        {
            var xrOrigin = Mediator.xrOrigin?.Origin;
            if (xrOrigin == null)
            {
                return;
            }

            _grabbingInteractors.Add(interactor);
            _grabbedClimbables.Add(climbInteractable);
            UpdateClimbAnchor(climbInteractable, interactor);

            TryPrepareLocomotion();
        }

        /// <summary>
        /// Finishes the grab driven by <paramref name="interactor"/>. If this was the most recent grab then movement
        /// will now be driven by the next most recent grab.
        /// </summary>
        /// <param name="interactor">The interactor whose grab to finish.</param>
        /// <remarks>
        /// If there is no other active grab to fall back on, this will put the <see cref="LocomotionProvider.LocomotionState"/>
        /// in the <see cref="LocomotionState.Ended"/> state in the next <see cref="Update"/>.
        /// </remarks>
        public void FinishClimbGrab(VXRBaseInteractor interactor)
        {
            var interactionIndex = _grabbingInteractors.IndexOf(interactor);
            if (interactionIndex < 0)
                return;

            Assert.AreEqual(_grabbingInteractors.Count, _grabbedClimbables.Count);

            if (interactionIndex > 0 && interactionIndex == _grabbingInteractors.Count - 1)
            {
                // If this was the most recent grab then the interactor driving movement will change,
                // so we need to update the anchor position.
                var newLastIndex = interactionIndex - 1;
                UpdateClimbAnchor(_grabbedClimbables[newLastIndex], _grabbingInteractors[newLastIndex]);
            }

            _grabbingInteractors.RemoveAt(interactionIndex);
            _grabbedClimbables.RemoveAt(interactionIndex);
        }

        private void UpdateClimbAnchor(ClimbInteractable climbInteractable, VXRBaseInteractor interactor)
        {
            var climbTransform = climbInteractable.climbTransform;
            _interactorAnchorWorldPosition = interactor.transform.position;
            _interactorAnchorClimbSpacePosition = climbTransform.InverseTransformPoint(_interactorAnchorWorldPosition);
            ClimbAnchorUpdated?.Invoke(this);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void Update()
        {
            if (!IsLocomotionActive)
                return;

            // Use the most recent interaction to drive movement
            if (_grabbingInteractors.Count > 0)
            {
                if (LocomotionState == LocomotionState.Preparing)
                    TryStartLocomotionImmediately();

                Assert.AreEqual(_grabbingInteractors.Count, _grabbedClimbables.Count);

                var lastIndex = _grabbingInteractors.Count - 1;
                var currentInteractor = _grabbingInteractors[lastIndex];
                var currentClimbInteractable = _grabbedClimbables[lastIndex];
                if (currentInteractor == null || currentClimbInteractable == null)
                {
                    FinishLocomotion();
                    return;
                }

                StepClimbMovement(currentClimbInteractable, currentInteractor);
            }
            else
            {
                FinishLocomotion();
            }
        }

        private void StepClimbMovement(ClimbInteractable currentClimbInteractable, VXRBaseInteractor currentInteractor)
        {
            // Move rig such that climb interactor position stays constant
            var activeClimbSettings = GetActiveClimbSettings(currentClimbInteractable);
            var allowFreeXMovement = activeClimbSettings.allowFreeXMovement;
            var allowFreeYMovement = activeClimbSettings.allowFreeYMovement;
            var allowFreeZMovement = activeClimbSettings.allowFreeZMovement;
            var interactorWorldPosition = currentInteractor.transform.position;
            Vector3 movement;

            if (allowFreeXMovement && allowFreeYMovement && allowFreeZMovement)
            {
                // No need to check position relative to climbable object if movement is unconstrained
                movement = _interactorAnchorWorldPosition - interactorWorldPosition;
            }
            else
            {
                var climbTransform = currentClimbInteractable.climbTransform;
                var interactorClimbSpacePosition = climbTransform.InverseTransformPoint(interactorWorldPosition);
                var movementInClimbSpace = _interactorAnchorClimbSpacePosition - interactorClimbSpacePosition;

                if (!allowFreeXMovement)
                    movementInClimbSpace.x = 0f;

                if (!allowFreeYMovement)
                    movementInClimbSpace.y = 0f;

                if (!allowFreeZMovement)
                    movementInClimbSpace.z = 0f;

                movement = climbTransform.TransformVector(movementInClimbSpace);
            }

            Transformation.motion = movement;
            TryQueueTransformation(Transformation);
        }

        private void FinishLocomotion()
        {
            TryEndLocomotion();
            _grabbingInteractors.Clear();
            _grabbedClimbables.Clear();
        }

        private ClimbSettings GetActiveClimbSettings(ClimbInteractable climbInteractable)
        {
            if (climbInteractable.climbSettingsOverride.Value != null)
                return climbInteractable.climbSettingsOverride;

            return _climbSettings;
        }
    }
}