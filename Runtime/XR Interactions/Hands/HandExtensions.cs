using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace VaporXR
{
    public static class HandExtensions
    {
        /// <summary>
        /// Returns the current hand position data as a <see cref="HandPose"/>. Optionally the data can be relative to a supplied transform.
        /// </summary>
        /// <param name="hand">The hand to record the pose data from.</param>
        /// <param name="relativeTo">The transform the recorded data should be relative to.</param>
        /// <returns>The pose data that was saved.</returns>
        public static HandPose SavePose(this VXRHand hand, Transform relativeTo = null)
        {
            var pose = new HandPose();
            if (relativeTo)
            {
                var relPose = relativeTo.GetWorldPose();
                var handPose = hand.HandPosingAnchor.GetWorldPose();

                // Get the offset between the relative and hand positions.
                // Convert the offset into a local position by multiplying by the LocalRotation Offset
                var offset = relPose.position - handPose.position;
                var localPosition = Quaternion.Inverse(handPose.rotation) * offset;

                // Calculate the local rotation of the relativeTo transform.
                var localRotation = Quaternion.Inverse(handPose.rotation) * relPose.rotation;
                pose.PoseOffset = new Pose(localPosition, localRotation);
            }
            else
            {
                pose.PoseOffset = hand.HandPosingAnchor.GetLocalPose();
            }

            foreach (var finger in hand.GetFingers())
            {
                foreach (var joint in finger.Joints)
                {
                    pose.FingerPoses.Add(joint.GetLocalPose());
                }
            }

            return pose;
        }

        /// <summary>
        /// Sets the hand to the supplied pose. Optionally relative to a supplied transform.
        /// </summary>
        /// <param name="hand">The hand to set the pose of.</param>
        /// <param name="pose">The pose to set.</param>
        /// <param name="relativeTo">The transform that the pose should be relative to.</param>
        public static void SetPose(this VXRHand hand, HandPose pose, Transform relativeTo = null)
        {
            if (relativeTo && relativeTo != hand.transform)
            {
                var relPose = relativeTo.GetWorldPose();
                // Calculate the new world pose with respect for the rotation of the relative pose.
                var worldPosition = relPose.position + relPose.rotation * pose.PoseOffset.position;

                // Calculate the new world rotation with respect for the relative rotation.
                var worldRotation = relPose.rotation * pose.PoseOffset.rotation;

                hand.HandPosingAnchor.SetPositionAndRotation(worldPosition, worldRotation);
            }

            if (pose.FingerPoses.Count == 0)
                return;

            var flatIndex = 0;
            foreach (var finger in hand.GetFingers())
            {
                finger.SetLocalPose(pose, ref flatIndex);
                //for (var i = 0; i < finger.Joints.Count; i++)
                //{
                //    finger.Joints[i].SetLocalPose(pose.FingerPoses[flatIndex]);
                //    flatIndex++;
                //}
            }
        }

        public static void Lerp(this HandPose poseToSet, HandPose from, HandPose to, float fraction)
        {
            HandPose.Lerp(poseToSet, from, to, fraction);
        }
    }
}
