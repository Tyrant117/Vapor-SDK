using Unity.XR.CoreUtils;
using UnityEngine;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// Container for an <see cref="Unity.XR.CoreUtils.XROrigin"/> that can be transformed using the user's body as a frame of reference.
    /// </summary>
    /// <seealso cref="IXRBodyTransformation"/>
    public class XRMovableBody
    {
        /// <summary>
        /// The XR Origin whose <see cref="XROrigin.Origin"/> is transformed to move the body.
        /// </summary>
        public VXROrigin XROrigin { get; private set; }

        /// <summary>
        /// The Transform component of the <see cref="XROrigin.Origin"/> of the <see cref="XROrigin"/>.
        /// This is the Transform component that is manipulated to move the body.
        /// </summary>
        public Transform OriginTransform => XROrigin.Origin.transform;

        /// <summary>
        /// The object that determines the position of the user's body.
        /// </summary>
        public IXRBodyPositionEvaluator BodyPositionEvaluator { get; private set; }

        /// <summary>
        /// Object that can be used to perform movement that is constrained by collision (optional, may be <see langword="null"/>).
        /// </summary>
        public IConstrainedXRBodyManipulator ConstrainedManipulator { get; private set; }

        /// <summary>
        /// Initializes a new instance of a movable body.
        /// </summary>
        /// <param name="xrOrigin">The XR Origin associated with the body.</param>
        /// <param name="bodyPositionEvaluator">The object that determines the position of the user's body.</param>
        public XRMovableBody(VXROrigin xrOrigin, IXRBodyPositionEvaluator bodyPositionEvaluator)
        {
            XROrigin = xrOrigin;
            BodyPositionEvaluator = bodyPositionEvaluator;
        }

        /// <summary>
        /// Gets the position of where the user's body is grounded (e.g. their feet), in the local space of the
        /// <see cref="OriginTransform"/>, based on the <see cref="BodyPositionEvaluator"/>.
        /// </summary>
        /// <returns>Returns the position of where the user's body is grounded, in the local space of the <see cref="OriginTransform"/>.</returns>
        public Vector3 GetBodyGroundLocalPosition()
        {
            return BodyPositionEvaluator.GetBodyGroundLocalPosition(XROrigin);
        }

        /// <summary>
        /// Gets the world position of where the user's body is grounded (e.g. their feet), based on the
        /// <see cref="BodyPositionEvaluator"/>.
        /// </summary>
        /// <returns>Returns the world position of where the user's body is grounded.</returns>
        public Vector3 GetBodyGroundWorldPosition()
        {
            return BodyPositionEvaluator.GetBodyGroundWorldPosition(XROrigin);
        }

        /// <summary>
        /// Links the given constrained manipulator to this body. This sets <see cref="ConstrainedManipulator"/> to
        /// <paramref name="manipulator"/> and calls <see cref="IConstrainedXRBodyManipulator.OnLinkedToBody"/> on the
        /// manipulator.
        /// </summary>
        /// <param name="manipulator">The constrained manipulator to link.</param>
        /// <remarks>
        /// If <see cref="ConstrainedManipulator"/> is already not <see langword="null"/> when this is called, this
        /// first calls <see cref="IConstrainedXRBodyManipulator.OnUnlinkedFromBody"/> on <see cref="ConstrainedManipulator"/>.
        /// Also, if the given <paramref name="manipulator"/> already has a <see cref="IConstrainedXRBodyManipulator.linkedBody"/>
        /// set, this calls <see cref="UnlinkConstrainedManipulator"/> on that body.
        /// </remarks>
        /// <seealso cref="UnlinkConstrainedManipulator"/>
        public void LinkConstrainedManipulator(IConstrainedXRBodyManipulator manipulator)
        {
            ConstrainedManipulator?.OnUnlinkedFromBody();
            manipulator.linkedBody?.UnlinkConstrainedManipulator();
            ConstrainedManipulator = manipulator;
            ConstrainedManipulator.OnLinkedToBody(this);
        }

        /// <summary>
        /// Unlinks the assigned constrained manipulator from this body, if there is one. This calls
        /// <see cref="IConstrainedXRBodyManipulator.OnUnlinkedFromBody"/> on the manipulator and sets
        /// <see cref="ConstrainedManipulator"/> to <see langword="null"/>.
        /// </summary>
        /// <seealso cref="LinkConstrainedManipulator"/>
        public void UnlinkConstrainedManipulator()
        {
            ConstrainedManipulator?.OnUnlinkedFromBody();
            ConstrainedManipulator = null;
        }
    }
}