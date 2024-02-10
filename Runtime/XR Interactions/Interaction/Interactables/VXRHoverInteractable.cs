using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporInspector;

namespace VaporXR
{
    public class VXRHoverInteractable : VXRBaseInteractable
    {
        #region Inspector
        [FoldoutGroup("Posing"), SerializeField]
        private bool _overrideHoverPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_overrideHoverPose")]
        private HandPoseDatum _hoverPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_overrideHoverPose")]
        private float _hoverPoseDuration;
        #endregion

        protected override void Awake()
        {
            base.Awake();
            CanHover = true;
            CanSelect = false;
            FocusMode = InteractableFocusMode.None;
        }

        public bool TryGetOverrideHoverPose(out HandPoseDatum pose, out float duration)
        {
            pose = _hoverPose;
            duration = _hoverPoseDuration;
            return _overrideHoverPose;
        }
    }
}
