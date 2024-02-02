using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    public class VXRHoverInteractable : VXRBaseInteractable
    {
        protected override void Awake()
        {
            base.Awake();
            CanHover = true;
            CanSelect = false;
            FocusMode = InteractableFocusMode.None;
        }
    }
}
