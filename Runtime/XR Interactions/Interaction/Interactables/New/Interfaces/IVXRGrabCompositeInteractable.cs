using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR.Interactables
{
    public interface IVXRGrabCompositeInteractable : IVXRCompositeInteractable
    {
        bool TrackRotation { get; }
        bool TrackPosition { get; }
        bool TrackScale { get; }

        void SetTargetLocalScale(Vector3 localScale);
    }
}
