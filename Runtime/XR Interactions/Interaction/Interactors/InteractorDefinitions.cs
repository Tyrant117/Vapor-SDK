using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    /// <summary>
    /// Enum used to determine how the socket should scale the interactable.
    /// </summary>
    /// <seealso cref="VXRSocketInteractor.SocketScaleMode"/>
    public enum SocketScaleMode
    {
        /// <summary>
        /// The interactable will not be scaled when attached to the socket.
        /// </summary>
        None,

        /// <summary>
        /// The interactable will be scaled to a fixed size when attached to the socket.
        /// The actual size is defined by the <see cref="XRSocketInteractor.fixedScale"/> value.
        /// </summary>
        Fixed,

        /// <summary>
        /// The interactable will be scaled to fit the size of the socket when attached.
        /// The scaling is dynamic, computed using the interactable's bounds, with a target size defined by <see cref="XRSocketInteractor.targetBoundsSize"/>.
        /// </summary>
        StretchedToFitSize,
    }

    /// <summary>
    /// Sets which shape of physics cast to use for the cast when detecting collisions.
    /// </summary>
    public enum OverlapDetectionModeType
    {
        /// <summary>
        /// Uses <see cref="OverlapSphereCommand"/> to detect collisions.
        /// </summary>
        Sphere,

        /// <summary>
        /// Uses <see cref="OverlapBoxCommand"/> to detect collisions.
        /// </summary>
        Box,
    }

    /// <summary>
    /// Sets which shape of physics cast to use for the cast when detecting collisions.
    /// </summary>
    public enum HitDetectionModeType
    {
        /// <summary>
        /// Uses <see cref="Physics.Raycast"/> Ray cast to detect collisions.
        /// </summary>
        Raycast,

        /// <summary>
        /// Uses <see cref="Physics.SphereCast"/> Sphere Cast to detect collisions.
        /// </summary>
        SphereCast,

        /// <summary>
        /// Uses cone casting to detect collisions.
        /// </summary>
        ConeCast,
    }

    /// <summary>
    /// Sets whether ray cast queries hit Trigger colliders and include or ignore snap volume trigger colliders.
    /// </summary>
    public enum QuerySnapVolumeInteraction
    {
        /// <summary>
        /// Queries never report Trigger hits that are registered with a snap volume.
        /// </summary>
        Ignore,

        /// <summary>
        /// Queries always report Trigger hits that are registered with a snap volume.
        /// </summary>
        Collide,
    }

    /// <summary>
    /// This defines the type of input that triggers an interaction.
    /// </summary>
    public enum InputTriggerType
    {
        /// <summary>
        /// Unity will consider the input active while the button is pressed.
        /// A user can hold the button before the interaction is possible
        /// and still trigger the interaction when it is possible.
        /// </summary>
        /// <remarks>
        /// When multiple interactors select an interactable at the same time and that interactable's
        /// <see cref="InteractableSelectMode"/> is set to <see cref="InteractableSelectMode.Single"/>, you may
        /// experience undesired behavior of selection repeatedly passing between the interactors and the select
        /// interaction events firing each frame. State Change is the recommended and default option. 
        /// </remarks>
        State,

        /// <summary>
        /// Unity will consider the input active only on the frame the button is pressed,
        /// and if successful remain engaged until the input is released.
        /// A user must press the button while the interaction is possible to trigger the interaction.
        /// They will not trigger the interaction if they started pressing the button before the interaction was possible.
        /// </summary>
        /// <seealso cref="InteractionState.activatedThisFrame"/>
        StateChange,

        /// <summary>
        /// The interaction starts on the frame the input is pressed
        /// and remains engaged until the second time the input is pressed.
        /// </summary>
        Toggle,

        /// <summary>
        /// The interaction starts on the frame the input is pressed
        /// and remains engaged until the second time the input is released.
        /// </summary>
        Sticky,
    }

    /// <summary>
    /// Sets which trajectory path Unity uses for the cast when detecting collisions.
    /// </summary>
    /// <seealso cref="VXRRayInteractor.LineType"/>
    public enum LineModeType
    {
        /// <summary>
        /// Performs a single ray cast into the Scene with a set ray length.
        /// </summary>
        StraightLine,

        /// <summary>
        /// Samples the trajectory of a projectile to generate a projectile curve.
        /// </summary>
        ProjectileCurve,

        /// <summary>
        /// Uses a control point and an end point to create a quadratic Bézier curve.
        /// </summary>
        BezierCurve,
    }
}
