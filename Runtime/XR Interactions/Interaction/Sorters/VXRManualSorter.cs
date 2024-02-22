using System.Collections.Generic;
using VaporXR.Interaction;
using VaporXR.Utilities;

namespace VaporXR
{
    /// <summary>
    /// This sorter will only store intercatbles that have been directly added to it through code.
    /// </summary>
    public class VXRManualSorter : VXRSorter
    {
        private bool _contactsSortedThisFrame;

        #region - Interaction -
        public override Interactable ProcessSorter(Interactor interactor, IXRTargetFilter filter = null)
        {
            // Determine the Interactables that this Interactor could possibly interact with this frame
            GetValidTargets(interactor, _frameValidTargets, filter);
            CurrentNearestValidTarget = (_frameValidTargets.Count > 0) ? _frameValidTargets[0] : null;
            return CurrentNearestValidTarget;
        }

        public override void GetValidTargets(Interactor interactor, List<Interactable> targets, IXRTargetFilter filter = null)
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

            if (!_contactsSortedThisFrame)
            {
                // Sort valid targets
                SortingHelpers.SortByDistanceToInteractor(this, PossibleTargets, _sortedValidTargets);
                _contactsSortedThisFrame = true;
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
        protected override void EvaluateContacts() { }
        protected override void OnContactAdded(Interactable interactable) { }
        protected override void OnContactRemoved(Interactable interactable) { }

        public override void ManualAddTarget(Interactable interactable)
        {
            base.ManualAddTarget(interactable);
            _contactsSortedThisFrame = false;
        }

        public override bool ManualRemoveTarget(Interactable interactable)
        {
            if (base.ManualRemoveTarget(interactable))
            {
                _contactsSortedThisFrame = false;
                return true;
            }
            return false;
        }

        protected override void ResetCollidersAndValidTargets()
        {
            PossibleTargets.Clear();
            _sortedValidTargets.Clear();
            _contactsSortedThisFrame = false;
            _stayedColliders.Clear();
            _contactMonitor.UpdateStayedColliders(_stayedColliders);
        }                    
        #endregion
    }
}
