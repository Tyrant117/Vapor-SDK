using UnityEngine;
using VaporXR.Utilities;

namespace VaporXR.Locomotion.Teleportation
{
    /// <summary>
    /// Filter for a <see cref="TeleportationMultiAnchorVolume"/> that designates the anchor furthest from the user
    /// as the teleportation destination. Distance calculation uses the camera position projected onto the XZ plane of
    /// the XR Origin.
    /// </summary>
    public class FurthestTeleportationAnchorFilter : ScriptableObject, ITeleportationVolumeAnchorFilter
    {
        /// <inheritdoc/>
        public int GetDestinationAnchorIndex(TeleportationMultiAnchorVolume teleportationVolume)
        {
            var anchorIndex = -1;
            var furthestSqDistance = -1f;
            var userPosition = teleportationVolume.teleportationProvider.Mediator.xrOrigin.GetCameraFloorWorldPosition();
            var anchors = teleportationVolume.anchorTransforms;
            for (var i = 0; i < anchors.Count; ++i)
            {
                var sqrDistance = (anchors[i].position - userPosition).sqrMagnitude;
                if (sqrDistance > furthestSqDistance)
                {
                    anchorIndex = i;
                    furthestSqDistance = sqrDistance;
                }
            }

            return anchorIndex;
        }
    }
}