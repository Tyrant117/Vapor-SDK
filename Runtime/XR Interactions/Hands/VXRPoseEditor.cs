using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;
using VaporInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VaporXR
{
    public class VXRPoseEditor : MonoBehaviour
    {
        [System.Serializable, DrawWithVapor]
        public class FingerEditor
        {
            public VXRFinger Finger;
            [HideInInspector]
            public List<Pose> IdlePoses = new();
            
            [SerializeField, Range(-90,90), OnValueChanged("OnFingerPose1")]
            private float _bend1;
            [SerializeField, Range(-20,20), OnValueChanged("OnFingerPose1")]
            private float _sway1;
            
            [SerializeField, Range(-90,90), OnValueChanged("OnFingerPose2")]
            private float _bend2;
            [SerializeField, Range(-20,20), OnValueChanged("OnFingerPose2")]
            private float _sway2;
            
            [SerializeField, Range(-90,90), OnValueChanged("OnFingerPose3")]
            private float _bend3;
            [SerializeField, Range(-20,20), OnValueChanged("OnFingerPose3")]
            private float _sway3;


            private void OnFingerPose1()
            {
                if (Finger.Joints.Count < 1)
                    return;
                var j1 = Finger.Joints[0];
                var start = IdlePoses[0];
                var newRot = Quaternion.identity;
                switch (Finger.CurlAxis)
                {
                    case VXRFinger.BendAxis.LocalX:
                        newRot *= Quaternion.Euler(-_bend1, 0, 0);
                        break;
                    case VXRFinger.BendAxis.LocalY:
                        newRot *= Quaternion.Euler(0, -_bend1, 0);
                        break;
                    case VXRFinger.BendAxis.LocalZ:
                        newRot *= Quaternion.Euler(0, 0, -_bend1);
                        break;
                }
                switch (Finger.SwayAxis)
                {
                    case VXRFinger.BendAxis.LocalX:
                        newRot *= Quaternion.Euler(-_sway1, 0, 0);
                        break;
                    case VXRFinger.BendAxis.LocalY:
                        newRot *= Quaternion.Euler(0, -_sway1, 0);
                        break;
                    case VXRFinger.BendAxis.LocalZ:
                        newRot *= Quaternion.Euler(0, 0, -_sway1);
                        break;
                }
                j1.localRotation = start.rotation * newRot;
                if (Finger.Finger == HandFinger.Thumb)
                {
                    float frac = Mathf.Clamp01(_bend1 / 90f);
                    j1.localPosition = Vector3.Lerp(Finger.ThumbTravelOpen.localPosition, Finger.ThumbTravelClosed.localPosition, frac);
                }
            }
            private void OnFingerPose2()
            {
                if (Finger.Joints.Count < 2)
                    return;
                var j1 = Finger.Joints[1];
                var start = IdlePoses[1];
                var newRot = Quaternion.identity;
                switch (Finger.CurlAxis)
                {
                    case VXRFinger.BendAxis.LocalX:
                        newRot *= Quaternion.Euler(-_bend2, 0, 0);
                        break;
                    case VXRFinger.BendAxis.LocalY:
                        newRot *= Quaternion.Euler(0, -_bend2, 0);
                        break;
                    case VXRFinger.BendAxis.LocalZ:
                        newRot *= Quaternion.Euler(0, 0, -_bend2);
                        break;
                }
                switch (Finger.SwayAxis)
                {
                    case VXRFinger.BendAxis.LocalX:
                        newRot *= Quaternion.Euler(-_sway2, 0, 0);
                        break;
                    case VXRFinger.BendAxis.LocalY:
                        newRot *= Quaternion.Euler(0, -_sway2, 0);
                        break;
                    case VXRFinger.BendAxis.LocalZ:
                        newRot *= Quaternion.Euler(0, 0, -_sway2);
                        break;
                }
                j1.localRotation = start.rotation * newRot;
            }
            private void OnFingerPose3()
            {
                if (Finger.Joints.Count < 3)
                    return;
                var j1 = Finger.Joints[2];
                var start = IdlePoses[2];
                var newRot = Quaternion.identity;
                switch (Finger.CurlAxis)
                {
                    case VXRFinger.BendAxis.LocalX:
                        newRot *= Quaternion.Euler(-_bend3, 0, 0);
                        break;
                    case VXRFinger.BendAxis.LocalY:
                        newRot *= Quaternion.Euler(0, -_bend3, 0);
                        break;
                    case VXRFinger.BendAxis.LocalZ:
                        newRot *= Quaternion.Euler(0, 0, -_bend3);
                        break;
                }
                switch (Finger.SwayAxis)
                {
                    case VXRFinger.BendAxis.LocalX:
                        newRot *= Quaternion.Euler(-_sway3, 0, 0);
                        break;
                    case VXRFinger.BendAxis.LocalY:
                        newRot *= Quaternion.Euler(0, -_sway3, 0);
                        break;
                    case VXRFinger.BendAxis.LocalZ:
                        newRot *= Quaternion.Euler(0, 0, -_sway3);
                        break;
                }
                j1.localRotation = start.rotation * newRot;
            }
        }


#if UNITY_EDITOR
        [FoldoutGroup("Components"), SerializeField]
        public VXRHand Hand;
        
        [FoldoutGroup("Loading"), SerializeField]
        private HandPoseDatum _loadPose;
        [FoldoutGroup("Loading"), Button]
        private void LoadPose()
        {
            Hand.SetPose(_loadPose.Value);
        }
        
        [FoldoutGroup("Saving"), SerializeField]
        private string _poseName;
        [FoldoutGroup("Saving"), SerializeField, FolderPath]
        private string _folderPath;
        [FoldoutGroup("Saving"), Button]
        private void SavePose()
        {
            if (!AssetDatabase.IsValidFolder(_folderPath))
            {
                Debug.LogError($"Invalid Folder Path: {_folderPath}");
                return;
            }


            var pose = Hand.SavePose();
            var asset = ScriptableObject.CreateInstance<HandPoseDatum>();
            asset.Value = pose;
            asset.name = _poseName;
            AssetDatabase.CreateAsset(asset, $"{_folderPath}/{_poseName}.asset");
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        [FoldoutGroup("Posing"), SerializeField]
        private GameObject _editorHoldPosingTarget;
        [FoldoutGroup("Posing"), SerializeField]
        private GameObject _editorHoverPosingTarget;
        [FoldoutGroup("Posing"), Button]
        private void DynamicGrab()
        {
            if (_editorHoldPosingTarget == null && _editorHoverPosingTarget == null)
            {
                Debug.LogError($"{gameObject.name}: editorPosingTarget cannot be null.");
                return;
            }

            var layerIndex = _editorHoldPosingTarget != null ? _editorHoldPosingTarget.gameObject.layer : _editorHoverPosingTarget.gameObject.layer;
            foreach (var finger in Hand.GetFingers())
            {
                finger.BendFingerUntilHit(200, 1 << layerIndex);
            }
        }

        [FoldoutGroup("Posing/Fingers"), SerializeField]
        private FingerEditor _thumb;
        [FoldoutGroup("Posing/Fingers"), SerializeField]
        private FingerEditor _index;
        [FoldoutGroup("Posing/Fingers"), SerializeField]
        private FingerEditor _middle;
        [FoldoutGroup("Posing/Fingers"), SerializeField]
        private FingerEditor _ring;
        [FoldoutGroup("Posing/Fingers"), SerializeField]
        private FingerEditor _pinky;
        

        [FoldoutGroup("Posing"), Button]
        private void CloseEditor()
        {
            if (_editorHoldPosingTarget != null || _editorHoverPosingTarget != null)
            {
                var targ = _editorHoldPosingTarget != null ? _editorHoldPosingTarget.gameObject : _editorHoverPosingTarget.gameObject;
                _editorHoldPosingTarget = null;
                _editorHoverPosingTarget = null;
                Selection.activeObject = targ;
            }

            DestroyImmediate(this);
        }

        
        public void SetFingerPoses(HandPose open)
        {
            var start = 0;
            foreach (var finger in Hand.GetFingers())
            {
                switch (finger.Finger)
                {
                    case HandFinger.Thumb:
                    {
                        _thumb.Finger = finger;
                        _thumb.IdlePoses.Clear();
                        for (var i = 0; i < finger.Joints.Count; i++)
                        {
                            _thumb.IdlePoses.Add(open.FingerPoses[start + i]);
                        }
                        start += finger.Joints.Count;
                        break;
                    }
                    case HandFinger.Index:
                    {
                        _index.Finger = finger;
                        _index.IdlePoses.Clear();
                        for (var i = 0; i < finger.Joints.Count; i++)
                        {
                            _index.IdlePoses.Add(open.FingerPoses[start + i]);
                        }
                        start += finger.Joints.Count;
                        break;
                    }
                    case HandFinger.Middle:
                    {
                        _middle.Finger = finger;
                        _middle.IdlePoses.Clear();
                        for (var i = 0; i < finger.Joints.Count; i++)
                        {
                            _middle.IdlePoses.Add(open.FingerPoses[start + i]);
                        }
                        start += finger.Joints.Count;
                        break;
                    }
                    case HandFinger.Ring:
                    {
                        _ring.Finger = finger;
                        _ring.IdlePoses.Clear();
                        for (var i = 0; i < finger.Joints.Count; i++)
                        {
                            _ring.IdlePoses.Add(open.FingerPoses[start + i]);
                        }
                        start += finger.Joints.Count;
                        break;
                    }
                    case HandFinger.Pinky:
                    {
                        _pinky.Finger = finger;
                        _pinky.IdlePoses.Clear();
                        for (var i = 0; i < finger.Joints.Count; i++)
                        {
                            _pinky.IdlePoses.Add(open.FingerPoses[start + i]);
                        }
                        start += finger.Joints.Count;
                        break;
                    }
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            const float sphereSize = 0.01f;
            foreach (var joint in Hand.Thumb.Joints)
            {
                Gizmos.DrawWireSphere(joint.position, sphereSize);
            }
            foreach (var joint in Hand.Index.Joints)
            {
                Gizmos.DrawWireSphere(joint.position, sphereSize);
            }
            foreach (var joint in Hand.Middle.Joints)
            {
                Gizmos.DrawWireSphere(joint.position, sphereSize);
            }
            foreach (var joint in Hand.Ring.Joints)
            {
                Gizmos.DrawWireSphere(joint.position, sphereSize);
            }
            foreach (var joint in Hand.Pinky.Joints)
            {
                Gizmos.DrawWireSphere(joint.position, sphereSize);
            }
            
            foreach (var finger in Hand.OptionalFingers)
            {
                foreach (var joint in finger.Joints)
                {
                    Gizmos.DrawWireSphere(joint.position, sphereSize);
                }
            }
        }
#endif
    }
}
