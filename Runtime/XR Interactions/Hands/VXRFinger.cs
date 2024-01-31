using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using VaporInspector;

namespace VaporXR
{
    public class VXRFinger : MonoBehaviour
    {
        /// <summary>
        /// The local bend axis of the finger joints.
        /// </summary>
        public enum BendAxis
        {
            LocalX,
            LocalY,
            LocalZ
        }

        [SerializeField] private HandFinger _finger;
        [SerializeField] private BendAxis _curlAxis = BendAxis.LocalX;
        [SerializeField] private BendAxis _swayAxis = BendAxis.LocalZ;
        [SerializeField] private List<Transform> _joints = new();

        [SerializeField] private Transform _rootJoint;
        [SerializeField] private Transform _tip;
        [SerializeField] private float _tipRadius = 0.01f;

        [SerializeField, ShowIf("$IsThumb")]
        private Transform _thumbTravelOpen;
        [SerializeField, ShowIf("$IsThumb")]
        private Transform _thumbTravelClosed;

        [Button]
        private void AutomaticSetup()
        {
            _rootJoint = transform;
            var j = _rootJoint;
            while (j.childCount > 0)
            {
                j = j.GetChild(0);
            }
            _tip = j;
            FindAndSetJoints();
        }

        [Button]
        private void FindAndSetJoints()
        {
            if (!_rootJoint || !_tip)
            {
                Debug.LogError("Root and tip joint must be set.");
                return;
            }

            _joints ??= new();
            _joints.Clear();
            var joint = _rootJoint;
            while (joint != _tip)
            {
                _joints.Add(joint);
                joint = joint.GetChild(0);
            }
        }

        private bool IsThumb => _finger == HandFinger.Thumb;
        public BendAxis CurlAxis => _curlAxis;
        public BendAxis SwayAxis => _swayAxis;
        public List<Transform> Joints => _joints;
        public HandFinger Finger { get => _finger; set => _finger = value; }
        public Transform ThumbTravelOpen => _thumbTravelOpen;
        public Transform ThumbTravelClosed => _thumbTravelClosed;

        private bool _isBreathing;
        private float _bend;
        private readonly List<Pose> _flexedPoses = new();
        private readonly List<Pose> _openPoses = new();
        private readonly List<Pose> _closedPoses = new();

        // Dynamic Bending
        private PhysicsScene _localPhysicsScene;
        private float _lastHitBend;
        private readonly Collider[] _results = new Collider[2];

        private void Awake()
        {
            _localPhysicsScene = gameObject.scene.GetPhysicsScene();
        }

        public int SetOpenAndClosedPoses(int start, HandPose flexed, HandPose open, HandPose closed)
        {
            for (var i = 0; i < Joints.Count; i++)
            {
                _flexedPoses.Add(flexed.FingerPoses[start + i]);
                _openPoses.Add(open.FingerPoses[start + i]);
                _closedPoses.Add(closed.FingerPoses[start + i]);
            }

            return start + Joints.Count;
        }

        private void LateUpdate()
        {
            if (!_isBreathing) return;
            LerpJointsOpenToClosed(0.1f + Mathf.Sin(Time.time / 2) * 0.05f);
        }

        public bool SmoothBend(float bend)
        {
            _isBreathing = false;
            if (Mathf.Approximately(_bend, bend))
            {
                LerpJointsOpenToClosed(bend);
                return true;
            }
            else
            {
                LerpJointsOpenToClosed(Mathf.MoveTowards(_bend, bend, 7 * Time.deltaTime));
                return false;
            }
        }

        private void LerpJointsOpenToClosed(float bend)
        {
            _bend = bend;
            for (var i = 0; i < Joints.Count; i++)
            {
                Joints[i].SetLocalPositionAndRotation(
                    Vector3.Lerp(_openPoses[i].position, _closedPoses[i].position, bend),
                    Quaternion.Slerp(_openPoses[i].rotation, _closedPoses[i].rotation, bend));
            }
        }
        
        private void LerpJointsFlexedToClosed(float bend)
        {
            _bend = bend;
            for (var i = 0; i < Joints.Count; i++)
            {
                Joints[i].SetLocalPositionAndRotation(
                    Vector3.Lerp(_flexedPoses[i].position, _closedPoses[i].position, bend),
                    Quaternion.Slerp(_flexedPoses[i].rotation, _closedPoses[i].rotation, bend));
            }
        }

        public void Breathe()
        {
            if (_isBreathing)
            {
                return;
            }
            
            StartCoroutine(ReturnToBreathing());
        }

        private IEnumerator ReturnToBreathing()
        {
            _isBreathing = true;
            while (!Mathf.Approximately(_bend, 0.1f) && _isBreathing)
            {
                LerpJointsOpenToClosed(Mathf.MoveTowards(_bend, 0.1f, 7 * Time.deltaTime));
                yield return null;
            }
        }

        #region - Dynamic -
        public bool BendFingerUntilHit(int steps, int layerMask)
        {
            _isBreathing = false;
            _bend = 0;
            LerpJointsFlexedToClosed(_bend);
            _lastHitBend = 0;

            for (float i = 0; i <= steps / 5f; i++)
            {
                _results[0] = null;
                _lastHitBend = i / (steps / 5f);
                LerpJointsFlexedToClosed(_lastHitBend);
                _localPhysicsScene.OverlapSphere(_tip.position, _tipRadius, _results, layerMask, QueryTriggerInteraction.Ignore);

                if (_results[0] != null)
                {
                    _lastHitBend = Mathf.Clamp01(_lastHitBend);
                    if (i == 0)
                    {
                        _bend = _lastHitBend;
                        return true;
                    }

                    break;
                }
            }


            _lastHitBend -= (5f / steps);
            for (int i = 0; i <= steps / 10f; i++)
            {
                _results[0] = null;
                _lastHitBend += (1f / steps);
                LerpJointsFlexedToClosed(_lastHitBend);
                _localPhysicsScene.OverlapSphere(_tip.position, _tipRadius, _results, layerMask, QueryTriggerInteraction.Ignore);


                if (_results[0] != null)
                {
                    _lastHitBend = Mathf.Clamp01(_lastHitBend);
                    _bend = _lastHitBend;
                    return true;
                }

                if (_lastHitBend >= 1)
                {
                    _lastHitBend = Mathf.Clamp01(_lastHitBend);
                    _bend = _lastHitBend;
                    return true;
                }
            }

            _lastHitBend = Mathf.Clamp01(_lastHitBend);
            _bend = _lastHitBend;
            return false;
        }
        #endregion
    }
}
