using UnityEngine;

namespace VaporXR.Locomotion.Teleportation
{
    /// <summary>
    /// An area is a teleportation destination which teleports the user to their pointed
    /// location on a surface.
    /// </summary>
    /// <seealso cref="TeleportationAnchor"/>
    public class TeleportationArea : BaseTeleportationInteractable
    {
        /// <inheritdoc />
        protected override bool GenerateTeleportRequest(VXRBaseInteractor interactor, RaycastHit raycastHit, ref TeleportRequest teleportRequest)
        {
            if (raycastHit.collider == null)
                return false;

            teleportRequest.destinationPosition = raycastHit.point;
            teleportRequest.destinationRotation = transform.rotation;
            return true;
        }
    }
}
