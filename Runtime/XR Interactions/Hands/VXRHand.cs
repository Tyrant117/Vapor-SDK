using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR;
using VaporInspector;

namespace VaporXR
{
    public class VXRHand : MonoBehaviour, IPoseSource
    {
        #region Inspector
        [FoldoutGroup("Physics"), SerializeField]
        private bool _usePhysicsHand = true;
        [FoldoutGroup("Physics"), SerializeField] 
        private Transform _trackedHand;
        [FoldoutGroup("Physics"), SerializeField, AutoReference] 
        private Rigidbody _rigidbody;
        [FoldoutGroup("Physics"), SerializeField]
        private GameObject _colliderHand;

        [FoldoutGroup("Fingers"), SerializeField]
        private Transform _handPosingAnchor;
        [FoldoutGroup("Fingers"), SerializeField]
        private VXRFinger _thumb;
        [FoldoutGroup("Fingers"), SerializeField]
        private VXRFinger _index;
        [FoldoutGroup("Fingers"), SerializeField]
        private VXRFinger _middle;
        [FoldoutGroup("Fingers"), SerializeField]
        private VXRFinger _ring;
        [FoldoutGroup("Fingers"), SerializeField]
        private VXRFinger _pinky;
        [FoldoutGroup("Fingers"), SerializeField]
        private List<VXRFinger> _optionalFingers;

        [FoldoutGroup("Poses"), SerializeField] 
        private HandPoseDatumProperty _flexedPoseDatum;
        [FoldoutGroup("Poses"), SerializeField] 
        private HandPoseDatumProperty _idlePoseDatum;
        [FoldoutGroup("Poses"), SerializeField] 
        private HandPoseDatumProperty _closedPoseDatum;
        
        [FoldoutGroup("Input"), SerializeField, AutoReference] private VXRInputDeviceUpdateProvider _updateProvider;
        [FoldoutGroup("Input"), SerializeField] private ButtonInputProvider _thumbTouchInput;
        [FoldoutGroup("Input"), SerializeField] private ButtonInputProvider _thumbDownInput;
        [FoldoutGroup("Input"), SerializeField] private Axis1DInputProvider _indexInput;
        [FoldoutGroup("Input"), SerializeField] private Axis1DInputProvider _gripInput;

        [FoldoutGroup("Editor"), Button]
        private void TogglePoseEditor()
        {
            if (TryGetComponent<VXRPoseEditor>(out var editor))
            {
                DestroyImmediate(editor);
            }
            else
            {
                editor = gameObject.AddComponent<VXRPoseEditor>();
                editor.Hand = this;
            }
        }
        [FoldoutGroup("Editor"), Button]
        private void SetFingerPosesEditor()
        {
            if (TryGetComponent<VXRPoseEditor>(out var editor))
            {
                editor.SetFingerPoses(_idlePoseDatum.Value);
            }
        }
        #endregion

        #region Properties
        public Transform HandPosingAnchor => _handPosingAnchor;
        public VXRFinger Thumb => _thumb;
        public VXRFinger Index => _index;
        public VXRFinger Middle => _middle;
        public VXRFinger Ring => _ring;
        public VXRFinger Pinky => _pinky;
        public List<VXRFinger> OptionalFingers => _optionalFingers;
        public List<VXRFinger> Fingers { get; } = new();
        #endregion

        #region Fields
        private bool _hasRigidbody;
        
        private Coroutine _handPoseRoutine;
        private static readonly WaitForEndOfFrame s_WaitForEndOfFrame = new();

        private bool _idlePosing;
        private HandPose _currentPose;
        private HandPose _interpolatedPose;
        private HandPose _dynamicPose;
        #endregion

        #region Events
        public event Action<VXRHand> PosingComplete;
        #endregion

        private void Awake()
        {
            _hasRigidbody = _rigidbody != null;
            Assert.IsTrue(!_usePhysicsHand || _hasRigidbody, $"Attempting to use a physics hand without a rigidbody. A rigibody must be added to {name}.");
            
            _PopulateFingers();
            _interpolatedPose = _flexedPoseDatum.Value.Copy();
            _dynamicPose = _flexedPoseDatum.Value.Copy();

            SetFingerPoses();


            void _PopulateFingers()
            {
                if (_thumb) { _thumb.Finger = HandFinger.Thumb; Fingers.Add(_thumb); }
                if (_index) { _index.Finger = HandFinger.Index; Fingers.Add(_index); }
                if (_middle) { _middle.Finger = HandFinger.Middle; Fingers.Add(_middle); }
                if (_ring) { _ring.Finger = HandFinger.Ring; Fingers.Add(_ring); }
                if (_pinky) { _pinky.Finger = HandFinger.Pinky; Fingers.Add(_pinky); }

                foreach (var optionalFinger in _optionalFingers)
                {
                    Fingers.Add(optionalFinger);
                }
            }
        }

        private void OnEnable()
        {
            _thumbTouchInput.BindToUpdateEvent(_updateProvider);
            _thumbDownInput.BindToUpdateEvent(_updateProvider);
            _indexInput.BindToUpdateEvent(_updateProvider);
            _gripInput.BindToUpdateEvent(_updateProvider);
        }

        private void OnDisable()
        {
            _thumbTouchInput.UnbindUpdateEvent();
            _thumbDownInput.UnbindUpdateEvent();
            _indexInput.UnbindUpdateEvent();
            _gripInput.UnbindUpdateEvent();
        }

        private void SetFingerPoses()
        {
            var start = 0;
            foreach (var finger in GetFingers())
            {
                start = finger.SetOpenAndClosedPoses(start, _flexedPoseDatum.Value,_idlePoseDatum.Value, _closedPoseDatum.Value);
            }
        }

        public List<VXRFinger> GetFingers()
        {
            if (Fingers.Count > 0)
            {
                return Fingers;
            }
            
            var result = new List<VXRFinger>();
            if (Thumb) { result.Add(Thumb); }
            if (Index) { result.Add(Index); }
            if (Middle) { result.Add(Middle); }
            if (Ring) { result.Add(Ring); }
            if (Pinky) { result.Add(Pinky); }
            result.AddRange(OptionalFingers);
            return result;
        }

        private void Start()
        {
            FallbackToIdle();
        }

        private void Update()
        {
            if(_usePhysicsHand) return;
            
            TrackPosition();
        }

        private void FixedUpdate()
        {
            if (!_usePhysicsHand) return;

            if (_rigidbody.isKinematic)
            {
                TrackPosition();
            }
            else
            {
                TrackVelocity();
            }
        }

        private void LateUpdate()
        {
            if (!_idlePosing) return;

            foreach (var finger in Fingers)
            {
                switch (finger.Finger)
                {
                    case HandFinger.Thumb:
                        if (_thumbDownInput.IsHeld)
                        {
                            finger.SmoothBend(1);
                        }
                        else if (_thumbTouchInput.IsHeld)
                        {
                            finger.SmoothBend(0.5f);
                        }
                        else
                        {
                            finger.Breathe();
                        }

                        break;
                    case HandFinger.Index:
                        var indexBend = _indexInput.CurrentValue;
                        var indexBreathe = indexBend <= 0.1f;
                        if (indexBreathe)
                        {
                            finger.Breathe();
                        }
                        else
                        {
                            finger.SmoothBend(indexBend);
                        }

                        break;
                    case HandFinger.Middle:
                    case HandFinger.Ring:
                    case HandFinger.Pinky:
                        var gripBend = _gripInput.CurrentValue;
                        var gripBreathe = gripBend <= 0.1f;
                        if (gripBreathe)
                        {
                            finger.Breathe();
                        }
                        else
                        {
                            finger.SmoothBend(gripBend);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #region - Tracking -
        private void TrackPosition()
        {
            if (_hasRigidbody)
            {
                _trackedHand.GetPositionAndRotation(out var pos, out var rot);
                _rigidbody.Move(pos, rot);
            }
            else
            {
                transform.SetWorldPose(_trackedHand.GetWorldPose());
            }
        }

        private void TrackVelocity()
        {
            _rigidbody.velocity *= 0.05f; // Dampen previous velocity.
            var posDelta = _trackedHand.position - transform.position;
            if (posDelta.sqrMagnitude > 2.25f)
            {
                // Snap the hand to tracking target.
                _rigidbody.MovePosition(_trackedHand.position);
            }
            else
            {
                var velocity = posDelta / Time.fixedDeltaTime;
                // Filter out invalid velocities
                if (!float.IsNaN(velocity.x))
                {
                    _rigidbody.AddForce(velocity, ForceMode.VelocityChange);
                }
            }

            _rigidbody.angularVelocity *= 0.05f; // Dampen previous angular velocity.
            var rotDelta = _trackedHand.rotation * Quaternion.Inverse(transform.rotation);
            rotDelta.ToAngleAxis(out var angle, out var axis);
            if (angle > Mathf.Epsilon)
            {
                var angularVelocity = axis * (angle * Mathf.Deg2Rad) / Time.fixedDeltaTime;
                // Filter out invalid velocities
                if (!float.IsNaN(angularVelocity.x))
                {
                    _rigidbody.AddTorque(angularVelocity, ForceMode.VelocityChange);
                }
            }
        }
        #endregion

        #region - Posing -
        public void SetHandPose(HandPose pose, Transform relativeTo = null, float duration = 0)
        {
            _idlePosing = false;
            if (duration > 0)
            {
                if(_handPoseRoutine != null)
                {
                    StopCoroutine(_handPoseRoutine);
                }

                _handPoseRoutine = StartCoroutine(PoseHandOverTime(_currentPose, pose, relativeTo, duration));
            }
            else
            {
                this.SetPose(pose, relativeTo);
                _currentPose = pose;
            }
        }

        private IEnumerator PoseHandOverTime(HandPose from, HandPose to, Transform relativeTo, float duration)
        {
            float deltaTime = 0;
            while (deltaTime < duration)
            {
                // Smooth Lerp
                _interpolatedPose.Lerp(from, to, Mathf.Pow(deltaTime / duration, 0.5f));
                this.SetPose(_interpolatedPose, relativeTo);
                yield return s_WaitForEndOfFrame;
                deltaTime += Time.deltaTime;
            }

            this.SetPose(to, relativeTo);
            _currentPose = to;
            _handPoseRoutine = null;
            PosingComplete?.Invoke(this);
        }

        public void FallbackToIdle(float duration = 0)
        {
            if (duration > 0)
            {
                PosingComplete += OnIdlePosingComplete;
                SetHandPose(_idlePoseDatum.Value, duration: duration);
            }
            else
            {
                SetHandPose(_idlePoseDatum.Value);
                _idlePosing = true;
            }
        }

        private void OnIdlePosingComplete(VXRHand hand)
        {
            PosingComplete -= OnIdlePosingComplete;
            _idlePosing = true;
        }
        #endregion
    }
}
