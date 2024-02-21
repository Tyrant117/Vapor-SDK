using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;

namespace VaporXR.Interaction
{
    [DisallowMultipleComponent]
    public class PokeInteractorModule : InteractorModule
    {
        private readonly struct PokeCollision
        {
            public readonly Collider Collider;
            public readonly Interactable Interactable;
            public readonly IXRPokeFilter Filter;
            public readonly bool HasPokeFilter;

            public PokeCollision(Collider collider, Interactable interactable, IXRPokeFilter filter)
            {
                Collider = collider;
                Interactable = interactable;
                Filter = filter;
                HasPokeFilter = filter != null;
            }
        }

        #region Inspector
        [SerializeField]
        private VXRPokeSorter _pokeSorter;
        #endregion

        #region Properties
        /// <summary>
        /// Distance along the poke interactable interaction axis that allows for a poke to be triggered sooner/with less precision.
        /// </summary>
        public float PokeInteractionOffset => _pokeSorter.PokeInteractionOffset;

        private BindableVariable<PokeStateData> _pokeStateData = new();
        /// <inheritdoc />
        public IReadOnlyBindableVariable<PokeStateData> PokeStateData => _pokeStateData;

        /// <summary>
        /// The tracker used to compute the velocity of the attach point.
        /// This behavior automatically updates this velocity tracker each frame during <see cref="PreprocessInteractor"/>.
        /// </summary>
        /// <seealso cref="GetAttachPointVelocity"/>
        /// <seealso cref="GetAttachPointAngularVelocity"/>
        protected IAttachPointVelocityTracker AttachPointVelocityTracker { get; set; } = new AttachPointVelocityTracker();
        #endregion


        #region - Velocity Tracking -
        /// <summary>
        /// Last computed default attach point velocity, based on multi-frame sampling of the pose in world space.
        /// </summary>
        /// <returns>Returns the transformed attach point linear velocity.</returns>
        /// <seealso cref="GetAttachPointAngularVelocity"/>
        public Vector3 GetAttachPointVelocity()
        {
            if (Interactor.TryGetXROrigin(out var origin))
            {
                return AttachPointVelocityTracker.GetAttachPointVelocity(origin);
            }
            return AttachPointVelocityTracker.GetAttachPointVelocity();
        }

        /// <summary>
        /// Last computed default attach point angular velocity, based on multi-frame sampling of the pose in world space.
        /// </summary>
        /// <returns>Returns the transformed attach point angular velocity.</returns>
        /// <seealso cref="GetAttachPointVelocity"/>
        public Vector3 GetAttachPointAngularVelocity()
        {
            if (Interactor.TryGetXROrigin(out var origin))
            {
                return AttachPointVelocityTracker.GetAttachPointAngularVelocity(origin);
            }
            return AttachPointVelocityTracker.GetAttachPointAngularVelocity();
        }
        #endregion
    }
}
