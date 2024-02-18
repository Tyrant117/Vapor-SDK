using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    public static class InputUsages
    {
        public const string RightHand = "RightHand";
        public const string LeftHand = "LeftHand";

        // Buttons
        public const string PrimaryButton = "<XRController>{{{0}}}/{{PrimaryButton}}";
        public const string PrimaryTouch = "<XRController>{{{0}}}/{{PrimaryTouch}}";

        public const string SecondaryButton = "<XRController>{{{0}}}/{{SecondaryButton}}";
        public const string SecondaryTouch = "<XRController>{{{0}}}/{{SecondaryTouch}}";

        public const string TriggerButton = "<XRController>{{{0}}}/{{TriggerButton}}";
        public const string GripButton = "<XRController>{{{0}}}/{{GripButton}}";

        public const string MenuButton = "<XRController>{{{0}}}/{{Menu}}";
        public const string SystemButton = "<XRController>{{{0}}}/{{SystemButton}}";

        public const string Primary2DAxisClick = "<XRController>{{{0}}}/{{Primary2DAxisClick}}";
        public const string Primary2DAxisTouch = "<XRController>{{{0}}}/{{Primary2DAxisTouch}}";

        // 1D Axis
        public const string TriggerAxis = "<XRController>{{{0}}}/{{Trigger}}";
        public const string GripAxis = "<XRController>{{{0}}}/{{Grip}}";

        // 2D Axis
        public const string Primary2DAxis = "<XRController>{{{0}}}/{{Primary2DAxis}}";

        public static string Format(string hand, string usage)
        {
            return string.Format(usage, hand);
        }
    }
}
