using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    /// <summary>
    /// Options for how to process and perform movement of an Interactable.
    /// </summary>
    /// <remarks>
    /// Each method of movement has tradeoffs, and different values may be more appropriate
    /// for each type of Interactable object in a project.
    /// </remarks>
    /// <seealso cref="VXRGrabInteractable.movementType"/>
    public enum MovementType
    {
        /// <summary>
        /// Move the Interactable object by setting the velocity and angular velocity of the Rigidbody.
        /// Use this if you don't want the object to be able to move through other Colliders without a Rigidbody
        /// as it follows the Interactor, however with the tradeoff that it can appear to lag behind
        /// and not move as smoothly as <see cref="Instantaneous"/>.
        /// </summary>
        /// <remarks>
        /// Unity sets the velocity values during the FixedUpdate function. This Interactable will move at the
        /// framerate-independent interval of the Physics update, which may be slower than the Update rate.
        /// If the Rigidbody is not set to use interpolation or extrapolation, as the Interactable
        /// follows the Interactor, it may not visually update position each frame and be a slight distance
        /// behind the Interactor or controller due to the difference between the Physics update rate
        /// and the render update rate.
        /// </remarks>
        /// <seealso cref="Rigidbody.velocity"/>
        /// <seealso cref="Rigidbody.angularVelocity"/>
        VelocityTracking,

        /// <summary>
        /// Move the Interactable object by moving the kinematic Rigidbody towards the target position and orientation.
        /// Use this if you want to keep the visual representation synchronized to match its Physics state,
        /// and if you want to allow the object to be able to move through other Colliders without a Rigidbody
        /// as it follows the Interactor.
        /// </summary>
        /// <remarks>
        /// Unity will call the movement methods during the FixedUpdate function. This Interactable will move at the
        /// framerate-independent interval of the Physics update, which may be slower than the Update rate.
        /// If the Rigidbody is not set to use interpolation or extrapolation, as the Interactable
        /// follows the Interactor, it may not visually update position each frame and be a slight distance
        /// behind the Interactor or controller due to the difference between the Physics update rate
        /// and the render update rate. Collisions will be more accurate as compared to <see cref="Instantaneous"/>
        /// since with this method, the Rigidbody will be moved by settings its internal velocity rather than
        /// instantly teleporting to match the Transform pose.
        /// </remarks>
        /// <seealso cref="Rigidbody.MovePosition"/>
        /// <seealso cref="Rigidbody.MoveRotation"/>
        Kinematic,

        /// <summary>
        /// Move the Interactable object by setting the position and rotation of the Transform every frame.
        /// Use this if you want the visual representation to be updated each frame, minimizing latency,
        /// however with the tradeoff that it will be able to move through other Colliders without a Rigidbody
        /// as it follows the Interactor.
        /// </summary>
        /// <remarks>
        /// Unity will set the Transform values each frame, which may be faster than the framerate-independent
        /// interval of the Physics update. The Collider of the Interactable object may be a slight distance
        /// behind the visual as it follows the Interactor due to the difference between the Physics update rate
        /// and the render update rate. Collisions will not be computed as accurately as <see cref="Kinematic"/>
        /// since with this method, the Rigidbody will be forced to instantly teleport poses to match the Transform pose
        /// rather than moving the Rigidbody through setting its internal velocity.
        /// </remarks>
        /// <seealso cref="Transform.position"/>
        /// <seealso cref="Transform.rotation"/>
        Instantaneous,
    }

    /// <summary>
    /// Options for how to calculate an Interactable distance to a location in world space.
    /// </summary>
    /// <seealso cref="VXRBaseInteractable.DistanceCalculationMode"/>
    public enum DistanceCalculationModeType
    {
        /// <summary>
        /// Calculates the distance using the Interactable's transform position.
        /// This option has low performance cost, but it may have low distance calculation accuracy for some objects.
        /// </summary>
        TransformPosition,

        /// <summary>
        /// Calculates the distance using the Interactable's interaction point list using the shortest distance to each.
        /// This option has moderate performance cost and should have moderate distance calculation accuracy for most objects.
        /// </summary>
        /// <seealso cref="XRInteractableUtility.TryGetClosestInteractionPoint"/>
        InteractionPointPosition,

        /// <summary>
        /// Calculates the distance using the Interactable's colliders list using the shortest distance to each.
        /// This option has moderate performance cost and should have moderate distance calculation accuracy for most objects.
        /// </summary>
        /// <seealso cref="XRInteractableUtility.TryGetClosestCollider"/>
        ColliderPosition,

        /// <summary>
        /// Calculates the distance using the Interactable's colliders list using the shortest distance to the closest point of each
        /// (either on the surface or inside the Collider).
        /// This option has high performance cost but high distance calculation accuracy.
        /// </summary>
        /// <remarks>
        /// The Interactable's colliders can only be of type <see cref="BoxCollider"/>, <see cref="SphereCollider"/>, <see cref="CapsuleCollider"/>, or convex <see cref="MeshCollider"/>.
        /// </remarks>
        /// <seealso cref="Collider.ClosestPoint"/>
        /// <seealso cref="XRInteractableUtility.TryGetClosestPointOnCollider"/>
        ColliderVolume,
    }
}
