using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporEvents;
using VaporInspector;

namespace VaporXR
{
    public class VXRPoseSource : MonoBehaviour, IPoseSource
    {
        [BoxGroup("Components"), SerializeField, AutoReference]
        private VXRBaseInteractable _interactable;

        [FoldoutGroup("Hover"), SerializeField]
        private HandPoseDatum _hoverPose;
        [FoldoutGroup("Hover"), SerializeField]
        private Transform _hoverPoseRelativeTo;
        [FoldoutGroup("Hover"), SerializeField]
        private float _hoverPoseDuration;

        [FoldoutGroup("Grab"), SerializeField]
        private HandPoseDatum _selectPose;
        [FoldoutGroup("Grab"), SerializeField]
        private Transform _selecPoseRelativeTo;
        [FoldoutGroup("Grab"), SerializeField]
        private float _selecPoseDuration;

        private VXRHand _cachedRightHand;
        private VXRHand _cachedLeftHand;

        private void OnEnable()
        {
            if(_hoverPose)
            {
                _interactable.HoverEntered += OnHoverEntered;
            }
            if (_selectPose)
            {
                _interactable.SelectEntered += OnSelectEntered;
            }
        }

        private void OnDisable()
        {
            if (_hoverPose)
            {
                _interactable.HoverEntered -= OnHoverEntered;
            }
            if (_selectPose)
            {
                _interactable.SelectEntered -= OnSelectEntered;
            }
        }

        private void OnHoverEntered(HoverEnterEventArgs obj)
        {
            switch (obj.interactorObject.Handedness)
            {
                case InteractorHandedness.None:
                    break;
                case InteractorHandedness.Left:
                    if (!_cachedLeftHand)
                    { 
                        _cachedLeftHand = ProviderBus.GetComponent<VXRHand>("Left Hand");
                    }
                    _cachedLeftHand.RequestHandPose(HandPoseType.Hover, this, _hoverPose.Value, _hoverPoseRelativeTo, _hoverPoseDuration);
                    break;
                case InteractorHandedness.Right:
                    if (!_cachedRightHand)
                    {
                        _cachedRightHand = ProviderBus.GetComponent<VXRHand>("Right Hand");
                    }
                    _cachedRightHand.RequestHandPose(HandPoseType.Hover, this, _hoverPose.Value, _hoverPoseRelativeTo, _hoverPoseDuration);
                    break;
                default:
                    break;
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs obj)
        {
            switch (obj.interactorObject.Handedness)
            {
                case InteractorHandedness.None:
                    break;
                case InteractorHandedness.Left:
                    if (!_cachedLeftHand)
                    {
                        _cachedLeftHand = ProviderBus.GetComponent<VXRHand>("Left Hand");
                    }
                    _cachedLeftHand.RequestHandPose(HandPoseType.Grab, this, _selectPose.Value, _selecPoseRelativeTo, _selecPoseDuration);
                    break;
                case InteractorHandedness.Right:
                    if (!_cachedRightHand)
                    {
                        _cachedRightHand = ProviderBus.GetComponent<VXRHand>("Right Hand");
                    }
                    _cachedRightHand.RequestHandPose(HandPoseType.Grab, this, _selectPose.Value, _selecPoseRelativeTo, _selecPoseDuration);
                    break;
                default:
                    break;
            }
        }        
    }
}
