using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Interaction;
using VaporXR.Utilities;

namespace VaporXR
{
    public class VXRRaycastSorter : VXRSorter
    {
        private const int MaxRaycastHits = 10;
        private static readonly RaycastHit[] s_ConeCastScratch = new RaycastHit[MaxRaycastHits];
        /// <summary>
        /// Reusable list of optimal raycast hits, for lookup during cone casting.
        /// </summary>
        private static readonly HashSet<Collider> s_OptimalHits = new();

        #region Inspector
#pragma warning disable IDE0051 // Remove unused private members
        private bool IsHitModeRaycast => _hitDetectionType == HitDetectionModeType.Raycast;
        private bool IsHitModeSphere => _hitDetectionType == HitDetectionModeType.SphereCast;
        private bool IsHitModeCone => _hitDetectionType == HitDetectionModeType.ConeCast;
#pragma warning restore IDE0051 // Remove unused private members

        [FoldoutGroup("Raycast"), SerializeField]
        [RichTextTooltip("The type of hit detection to use for the ray cast.\n" +
                         "<mth>Raycast:</mth> Uses <cls>Physics</cls> RayCast to detect collisions.\n" +
                         "<mth>SphereCast:</mth> Uses <cls>Physics</cls> SphereCast to detect collisions.\n" +
                         "<mth>ConeCast:</mth> Uses cone casting to detect collisions.")]
        private HitDetectionModeType _hitDetectionType = HitDetectionModeType.Raycast;
        [FoldoutGroup("Raycast"), SerializeField, Range(0.01f, 0.25f), ShowIf("$IsHitModeSphere")]
        [RichTextTooltip("The radius used for sphere casting.")]
        private float _sphereCastRadius = 0.1f;
        [FoldoutGroup("Raycast"), SerializeField, Range(0f, 180f), ShowIf("$IsHitModeCone")]
        [RichTextTooltip("The angle in degrees of the cone used for cone casting. Will use regular ray casting if set to 0.")]
        private float _coneCastAngle = 6f;
        [FoldoutGroup("Raycast"), SerializeField]
        [RichTextTooltip("The layer mask used for limiting ray cast targets.")]
        private LayerMask _raycastMask = 1;
        [FoldoutGroup("Raycast"), SerializeField]
        [RichTextTooltip("The type of interaction with trigger colliders via ray cast.")]
        private QueryTriggerInteraction _raycastTriggerInteraction = QueryTriggerInteraction.Ignore;
        [FoldoutGroup("Raycast"), SerializeField]
        [RichTextTooltip("Whether ray cast should include or ignore hits on trigger colliders that are snap volume colliders, even if the ray cast is set to ignore triggers.\n" +
                         "If you are not using gaze assistance or XR Interactable Snap Volume components, you should set this property to <itf>QuerySnapVolumeInteraction</itf>.<mth>Ignore</mth> to avoid the performance cost.")]
        private QuerySnapVolumeInteraction _raycastSnapVolumeInteraction = QuerySnapVolumeInteraction.Collide;
        [FoldoutGroup("Raycast"), SerializeField]
        [RichTextTooltip("Whether Unity considers only the closest Interactable as a valid target for interaction.")]
        private bool _hitClosestOnly;
        [FoldoutGroup("Raycast"), SerializeField]
        [RichTextTooltip("Gets or sets the max distance of ray cast when the line type is a straight line. Increasing this value will make the line reach further.")]
        private float _maxRaycastDistance = 30f;
        #endregion

        #region Fields
        private readonly RaycastHit[] _raycastHits = new RaycastHit[MaxRaycastHits];
        private int _raycastHitsCount;
        private readonly RaycastHitComparer _raycastHitComparer = new();
        #endregion

        #region - Interaction -
        public override Interactable ProcessSorter(Interactor interactor, IXRTargetFilter filter = null)
        {
            EvaluateContacts();

            // Determine the Interactables that this Interactor could possibly interact with this frame
            GetValidTargets(interactor, _frameValidTargets, filter);
            CurrentNearestValidTarget = (_frameValidTargets.Count > 0) ? _frameValidTargets[0] : null;
            return CurrentNearestValidTarget;
        }

        public override void GetValidTargets(Interactor interactor, List<Interactable> targets, IXRTargetFilter filter = null)
        {
            _frameValidTargets.Clear();
            if (!isActiveAndEnabled || !IsActive)
            {
                return;
            }

            if (filter != null && filter.CanProcess)
            {
                filter.Process(interactor, PossibleTargets, _frameValidTargets);
            }
            else
            {
                _frameValidTargets.AddRange(PossibleTargets);
            }

            if (interactor.OverrideSorterInteractionLayer)
            {
                foreach (var validCollisionTarget in _frameValidTargets)
                {
                    if (HasInteractionLayerOverlap(interactor, validCollisionTarget))
                    {
                        validCollisionTarget.LastSorterType = GetSorterType();
                        targets.Add(validCollisionTarget);
                    }
                }
            }
            else
            {
                foreach (var validCollisionTarget in _frameValidTargets)
                {
                    if (HasInteractionLayerOverlap(validCollisionTarget))
                    {
                        validCollisionTarget.LastSorterType = GetSorterType();
                        targets.Add(validCollisionTarget);
                    }
                }
            }
        }

        [BurstCompile]
        private int FilteredConeCast(in Vector3 from, float coneCastAngleDegrees, in Vector3 direction, in Vector3 origin,
            RaycastHit[] results, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            s_OptimalHits.Clear();

            // Set up all the sphere casts
            var obstructionDistance = math.min(maxDistance, 1000f);

            // Raycast looking for obstructions and any optimal targets
            var hitCounter = 0;
            var optimalHits = _localPhysicsScene.Raycast(origin, direction, s_ConeCastScratch, obstructionDistance, layerMask, queryTriggerInteraction);
            if (optimalHits > 0)
            {
                for (var i = 0; i < optimalHits; ++i)
                {
                    var hitInfo = s_ConeCastScratch[i];
                    if (hitInfo.distance > obstructionDistance)
                    {
                        continue;
                    }

                    // If an obstruction is found, then reject anything behind it
                    if (!_interactionManager.TryGetInteractableForCollider(hitInfo.collider, out _))
                    {
                        obstructionDistance = math.min(hitInfo.distance, obstructionDistance);

                        // Since we are rejecting anything past the obstruction, we push its distance back to allow for objects in the periphery to be selected first
                        hitInfo.distance += 1.5f;
                    }

                    results[hitCounter] = hitInfo;
                    s_OptimalHits.Add(hitInfo.collider);
                    hitCounter++;
                }
            }

            // Now do a series of sphere casts that increase in size.
            // We don't process obstructions here
            // We don't do ultra-fine cone rejection instead add horizontal distance to the spherecast depth
            var angleRadius = math.tan(math.radians(coneCastAngleDegrees) * 0.5f);
            var currentOffset = (origin - from).magnitude;
            while (currentOffset < obstructionDistance)
            {
                BurstPhysicsUtils.GetConecastParameters(angleRadius, currentOffset, obstructionDistance, direction, out var originOffset, out var endRadius, out var castMax);

                // Spherecast
                var initialResults = _localPhysicsScene.SphereCast(origin + originOffset, endRadius, direction, s_ConeCastScratch, castMax, layerMask, queryTriggerInteraction);

                // Go through each result
                for (var i = 0; (i < initialResults && hitCounter < results.Length); i++)
                {
                    var hit = s_ConeCastScratch[i];

                    // Range check
                    if (hit.distance > obstructionDistance)
                        continue;

                    // If it's an optimal hit, then skip it
                    if (s_OptimalHits.Contains(hit.collider))
                        continue;

                    // It must have an interactable
                    if (!_interactionManager.TryGetInteractableForCollider(hit.collider, out _))
                        continue;

                    if (Mathf.Approximately(hit.distance, 0f) && hit.point == Vector3.zero)
                    {
                        // Sphere cast can return hits where point is (0, 0, 0) in error.
                        continue;
                    }

                    // Adjust distance by distance from ray center for default sorting
                    BurstPhysicsUtils.GetConecastOffset(origin, hit.point, direction, out var hitToRayDist);

                    // We penalize these off-center hits by a meter + whatever horizontal offset they have
                    hit.distance += currentOffset + 1f + (hitToRayDist);
                    results[hitCounter] = hit;
                    hitCounter++;
                }
                currentOffset += castMax;
            }

            s_OptimalHits.Clear();
            Array.Clear(s_ConeCastScratch, 0, 10);
            return hitCounter;
        }
        #endregion

        #region - Contacts -
        protected override void EvaluateContacts()
        {
            _stayedColliders.Clear();
            var queryTriggerInteraction = _raycastSnapVolumeInteraction == QuerySnapVolumeInteraction.Collide
                ? QueryTriggerInteraction.Collide
                : _raycastTriggerInteraction;

            switch (_hitDetectionType)
            {
                case HitDetectionModeType.Raycast:
                    _raycastHitsCount = _localPhysicsScene.Raycast(_attachPoint.position, _attachPoint.forward,
                        _raycastHits, _maxRaycastDistance, _raycastMask, queryTriggerInteraction);
                    break;

                case HitDetectionModeType.SphereCast:
                    _raycastHitsCount = _localPhysicsScene.SphereCast(_attachPoint.position, _sphereCastRadius, _attachPoint.forward,
                        _raycastHits, _maxRaycastDistance, _raycastMask, queryTriggerInteraction);
                    break;

                case HitDetectionModeType.ConeCast:
                    _raycastHitsCount = FilteredConeCast(_attachPoint.position, _coneCastAngle, _attachPoint.forward, _attachPoint.position,
                        _raycastHits, _maxRaycastDistance, _raycastMask, queryTriggerInteraction);
                    break;
            }

            if (_raycastHitsCount > 0)
            {
                var baseQueryHitsTriggers = _raycastTriggerInteraction == QueryTriggerInteraction.Collide ||
                    (_raycastTriggerInteraction == QueryTriggerInteraction.UseGlobal && Physics.queriesHitTriggers);

                if (_raycastSnapVolumeInteraction == QuerySnapVolumeInteraction.Ignore && baseQueryHitsTriggers)
                {
                    // Filter out Snap Volume trigger collider hits
                    _raycastHitsCount = FilterTriggerColliders(_interactionManager, _raycastHits, _raycastHitsCount, snapVolume => snapVolume != null);
                }
                else if (_raycastSnapVolumeInteraction == QuerySnapVolumeInteraction.Collide && !baseQueryHitsTriggers)
                {
                    // Filter out trigger collider hits that are not Snap Volume snap colliders
                    _raycastHitsCount = FilterTriggerColliders(_interactionManager, _raycastHits, _raycastHitsCount, snapVolume => snapVolume == null);
                }

                // Sort all the hits by distance along the curve since the results of the 3D ray cast are not ordered.
                // Sorting is done after filtering above for performance.
                SortingHelpers.Sort(_raycastHits, _raycastHitComparer, _raycastHitsCount);
            }

            if (_raycastHitsCount > 0)
            {
                for (var i = 0; i < _raycastHitsCount; ++i)
                {
                    var raycastHit = _raycastHits[i];
                    _stayedColliders.Add(raycastHit.collider);

                    // Stop after the first if enabled
                    if (_hitClosestOnly)
                    {
                        break;
                    }
                }
            }

            _contactMonitor.UpdateStayedColliders(_stayedColliders);

            static int FilterTriggerColliders(VXRInteractionManager interactionManager, RaycastHit[] raycastHits, int count, Func<VXRInteractableSnapVolume, bool> removeRule)
            {
                for (var index = 0; index < count; ++index)
                {
                    var hitCollider = raycastHits[index].collider;
                    if (hitCollider.isTrigger)
                    {
                        interactionManager.TryGetInteractableForCollider(hitCollider, out _, out var snapVolume);
                        if (removeRule(snapVolume))
                        {
                            RemoveAt(raycastHits, index, count);
                            --count;
                            --index;
                        }
                    }
                }

                return count;
            }

            static void RemoveAt<T>(T[] array, int index, int count) where T : struct
            {
                Array.Copy(array, index + 1, array, index, count - index - 1);
                Array.Clear(array, count - 1, 1);
            }
        }

        protected override void OnContactAdded(Interactable interactable)
        {
            if (PossibleTargets.Contains(interactable))
            {
                return;
            }

            PossibleTargets.Add(interactable);
        }

        protected override void OnContactRemoved(Interactable interactable)
        {
            PossibleTargets.Remove(interactable);
        }

        protected override void ResetCollidersAndValidTargets()
        {
            PossibleTargets.Clear();
            _stayedColliders.Clear();
            _contactMonitor.UpdateStayedColliders(_stayedColliders);
        }
        #endregion

        public override int GetSorterType()
        {
            return (int)Type.Raycast;
        }

        private void OnDrawGizmosSelected()
        {
            var transformData = _attachPoint != null ? _attachPoint : transform;
            var gizmoStart = transformData.position;
            var gizmoEnd = gizmoStart + (transformData.forward * _maxRaycastDistance);
            Gizmos.color = new Color(58 / 255f, 122 / 255f, 248 / 255f, 237 / 255f);

            switch (_hitDetectionType)
            {
                case HitDetectionModeType.Raycast:
                    // Draw the raycast line
                    Gizmos.DrawLine(gizmoStart, gizmoEnd);
                    break;

                case HitDetectionModeType.SphereCast:
                    {
                        var gizmoUp = transformData.up * _sphereCastRadius;
                        var gizmoSide = transformData.right * _sphereCastRadius;
                        Gizmos.DrawWireSphere(gizmoStart, _sphereCastRadius);
                        Gizmos.DrawLine(gizmoStart + gizmoSide, gizmoEnd + gizmoSide);
                        Gizmos.DrawLine(gizmoStart - gizmoSide, gizmoEnd - gizmoSide);
                        Gizmos.DrawLine(gizmoStart + gizmoUp, gizmoEnd + gizmoUp);
                        Gizmos.DrawLine(gizmoStart - gizmoUp, gizmoEnd - gizmoUp);
                        Gizmos.DrawWireSphere(gizmoEnd, _sphereCastRadius);
                        break;
                    }

                case HitDetectionModeType.ConeCast:
                    {
                        var coneRadius = Mathf.Tan(_coneCastAngle * Mathf.Deg2Rad * 0.5f) * _maxRaycastDistance;
                        var gizmoUp = transformData.up * coneRadius;
                        var gizmoSide = transformData.right * coneRadius;
                        Gizmos.DrawLine(gizmoStart, gizmoEnd);
                        Gizmos.DrawLine(gizmoStart, gizmoEnd + gizmoSide);
                        Gizmos.DrawLine(gizmoStart, gizmoEnd - gizmoSide);
                        Gizmos.DrawLine(gizmoStart, gizmoEnd + gizmoUp);
                        Gizmos.DrawLine(gizmoStart, gizmoEnd - gizmoUp);
                        Gizmos.DrawWireSphere(gizmoEnd, coneRadius);
                        break;
                    }
            }
        }
    }
}
