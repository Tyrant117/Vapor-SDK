using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Interactors;
using VaporXR.Utilities;

namespace VaporXR
{
    public class VXROverlapSorter : VXRSorter
    {
        private const int MaxOverlapHits = 25;


        #region Inspector
#pragma warning disable IDE0051 // Remove unused private members
        private bool IsOverlapModeSphere => _overlapMode == OverlapDetectionModeType.Sphere;
        private bool IsOverlapModeBox => _overlapMode == OverlapDetectionModeType.Box;
#pragma warning restore IDE0051 // Remove unused private members
       
        [FoldoutGroup("Overlap"), SerializeField]
        private OverlapDetectionModeType _overlapMode;
        [FoldoutGroup("Overlap"), SerializeField, ShowIf("$IsOverlapModeSphere")]
        private float _overlapSphereRadius = 0.1f;
        [FoldoutGroup("Overlap"), SerializeField, ShowIf("$IsOverlapModeBox")]
        private Vector3 _overlapBoxSize = new(0.1f, 0.05f, 0.1f);

        [FoldoutGroup("Overlap"), SerializeField]
        private LayerMask _overlapMask = 1; // Default
        [FoldoutGroup("Overlap"), SerializeField]
        [RichTextTooltip("The type of interaction with trigger colliders when overlapping.")]
        private QueryTriggerInteraction _overlapTriggerInteraction = QueryTriggerInteraction.Ignore;
        #endregion

        #region Fields
        private bool _firstFrame = true;
        private bool _overlapContactsSortedThisFrame;

        private Vector3 _lastCastOrigin = Vector3.zero;
        private readonly Collider[] _overlapHits = new Collider[MaxOverlapHits];
        private readonly RaycastHit[] _castHits = new RaycastHit[MaxOverlapHits];
        #endregion

        #region - Interaction -
        public override IXRInteractable ProcessSorter(IVXRInteractor interactor, IXRTargetFilter filter = null)
        {
            EvaluateContacts();

            // Determine the Interactables that this Interactor could possibly interact with this frame
            GetValidTargets(interactor, _frameValidTargets, filter);
            CurrentNearestValidTarget = (_frameValidTargets.Count > 0) ? _frameValidTargets[0] : null;
            return CurrentNearestValidTarget;
        }

        public override void GetValidTargets(IVXRInteractor interactor, List<IXRInteractable> targets, IXRTargetFilter filter = null)
        {
            _frameValidTargets.Clear();
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (PossibleTargets.Count == 0)
            {
                return;
            }

            if (!_overlapContactsSortedThisFrame)
            {
                // Sort valid targets
                SortingHelpers.SortByDistanceToInteractor(this, PossibleTargets, _sortedValidTargets);
                _overlapContactsSortedThisFrame = true;
            }

            if (filter != null && filter.CanProcess)
            {
                filter.Process(interactor, _sortedValidTargets, _frameValidTargets);
            }
            else
            {
                _frameValidTargets.AddRange(_sortedValidTargets);
            }

            foreach (var validCollisionTarget in _frameValidTargets)
            {
                if (HasInteractionLayerOverlap(interactor, validCollisionTarget))
                {
                    targets.Add(validCollisionTarget);
                }
            }
        }
        #endregion

        #region - Contacts -
        protected override void EvaluateContacts()
        {
            if (_overlapMode == OverlapDetectionModeType.Sphere)
            {
                EvaluateSphereOverlap();
            }
            else
            {
                EvaluateBoxOverlap();
            }            
        }

        private void EvaluateSphereOverlap()
        {
            _overlapContactsSortedThisFrame = false;
            _stayedColliders.Clear();

            // Hover Check
            Vector3 interactorPosition = _attachPoint.position;
            Vector3 overlapStart = _lastCastOrigin;
            Vector3 interFrameEnd = interactorPosition;
            float grabRadius = _overlapSphereRadius;

            BurstPhysicsUtils.GetSphereOverlapParameters(overlapStart, interFrameEnd, out var normalizedOverlapVector, out var overlapSqrMagnitude, out var overlapDistance);

            // If no movement is recorded.
            // Check if sphere cast size is sufficient for proper cast, or if first frame since last frame poke position will be invalid.
            if (_firstFrame || overlapSqrMagnitude < 0.001f)
            {
                var numberOfOverlaps = _localPhysicsScene.OverlapSphere(interFrameEnd, grabRadius, _overlapHits,
                    _overlapMask, _overlapTriggerInteraction);

                for (var i = 0; i < numberOfOverlaps; ++i)
                {
                    _stayedColliders.Add(_overlapHits[i]);
                }
            }
            else
            {
                var numberOfOverlaps = _localPhysicsScene.SphereCast(
                    overlapStart,
                    grabRadius,
                    normalizedOverlapVector,
                    _castHits,
                    overlapDistance,
                    _overlapMask,
                    _overlapTriggerInteraction);

                for (var i = 0; i < numberOfOverlaps; ++i)
                {
                    _stayedColliders.Add(_castHits[i].collider);
                }
            }

            _contactMonitor.UpdateStayedColliders(_stayedColliders);

            _lastCastOrigin = interactorPosition;
            _firstFrame = false;
        }

        private void EvaluateBoxOverlap()
        {
            _overlapContactsSortedThisFrame = false;
            _stayedColliders.Clear();

            //Transform directAttachTransform = GetAttachTransform(null);
            // Hover Check
            Vector3 interactorPosition = _attachPoint.position;//directAttachTransform.TransformPoint(_overlapAnchor.position);
            Vector3 overlapStart = _lastCastOrigin;
            Vector3 interFrameEnd = interactorPosition;
            var boxSize = _overlapBoxSize / 2f;

            BurstPhysicsUtils.GetSphereOverlapParameters(overlapStart, interFrameEnd, out var normalizedOverlapVector, out var overlapSqrMagnitude, out var overlapDistance);

            // If no movement is recorded.
            // Check if sphere cast size is sufficient for proper cast, or if first frame since last frame poke position will be invalid.
            if (_firstFrame || overlapSqrMagnitude < 0.001f)
            {
                var numberOfOverlaps = _localPhysicsScene.OverlapBox(interFrameEnd, boxSize, _overlapHits, _attachPoint.rotation,
                    _overlapMask, _overlapTriggerInteraction);

                for (var i = 0; i < numberOfOverlaps; ++i)
                {
                    _stayedColliders.Add(_overlapHits[i]);
                }
            }
            else
            {
                var numberOfOverlaps = _localPhysicsScene.BoxCast(
                    overlapStart,
                    boxSize,
                    normalizedOverlapVector,
                    _castHits,
                    _attachPoint.rotation,
                    overlapDistance,
                    _overlapMask,
                    _overlapTriggerInteraction);

                for (var i = 0; i < numberOfOverlaps; ++i)
                {
                    _stayedColliders.Add(_castHits[i].collider);
                }
            }

            _contactMonitor.UpdateStayedColliders(_stayedColliders);

            _lastCastOrigin = interactorPosition;
            _firstFrame = false;
        }

        protected override void OnContactAdded(IXRInteractable interactable)
        {
            if (PossibleTargets.Contains(interactable))
            {
                return;
            }

            PossibleTargets.Add(interactable);
            _overlapContactsSortedThisFrame = false;
        }

        protected override void OnContactRemoved(IXRInteractable interactable)
        {
            if (PossibleTargets.Remove(interactable))
            {
                _overlapContactsSortedThisFrame = false;
            }
        }        

        protected override void ResetCollidersAndValidTargets()
        {
            PossibleTargets.Clear();
            _sortedValidTargets.Clear();
            _firstFrame = true;
            _overlapContactsSortedThisFrame = false;
            _stayedColliders.Clear();
            _contactMonitor.UpdateStayedColliders(_stayedColliders);
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            if (_attachPoint)
            {
                Gizmos.color = Color.white;
                switch (_overlapMode)
                {
                    case OverlapDetectionModeType.Sphere:
                        Gizmos.DrawWireSphere(_attachPoint.position, _overlapSphereRadius);
                        break;
                    case OverlapDetectionModeType.Box:
                        Gizmos.DrawWireCube(_attachPoint.position, _overlapBoxSize);
                        break;
                }
            }
        }        
    }
}
