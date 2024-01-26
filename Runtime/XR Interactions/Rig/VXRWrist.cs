using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using Vapor;
using VaporInspector;

namespace VaporXR
{
    public class VXRWrist : MonoBehaviour
    {
        #region Inspector
        [FoldoutGroup(""), SerializeField]
        private GameObject _uiPrefab;
        [FoldoutGroup(""), SerializeField]
        private Transform _attachPoint;
        [FoldoutGroup(""), SerializeField]
        private Transform _followTarget;
        [FoldoutGroup(""), SerializeField]
        private Transform _gazeTarget;
        [FoldoutGroup(""), SerializeField]
        [RichTextTooltip("Only show menu if gaze to menu origin's divergence angle is below this value.")]
        private float _menuVisibleGazeAngleDivergenceThreshold = 35f;

        [FoldoutGroup("Setup"), Button]
        private void ToggleSetup()
        {
            if(_attachPoint.childCount == 1)
            {
                DestroyImmediate(_attachPoint.GetChild(0).gameObject);
            }
            else
            {
#if UNITY_EDITOR
                PrefabUtility.InstantiatePrefab(_uiPrefab, _attachPoint);
#else
                GameObject.Instantiate(_uiPrefab, _attachPoint, false);
#endif
            }
        }
        [FoldoutGroup("Setup"), SerializeField]
        private List<RectTransform> _setupTargets = new();
        [FoldoutGroup("Setup"), SerializeField]
        private float _setupRadius = 256;
        [FoldoutGroup("Setup"), SerializeField, Range(0,360f)]
        private float _setupAngleDistribution = 360;
        [FoldoutGroup("Setup"), SerializeField, Range(0, 360f)]
        private float _setupStartOffset = 0;
        [FoldoutGroup("Setup"), Button]
        private void CreateCircleMenu()
        {
            Debug.Log("Pressed");
            float radius = _setupRadius; // Adjust this value based on your requirements
            float angleStep = _setupAngleDistribution / _setupTargets.Count;
            float offsetAngle = _setupStartOffset;

            for (int i = 0; i < _setupTargets.Count; i++)
            {
                float angle = (i * angleStep + offsetAngle) * Mathf.Deg2Rad;

                float x = radius * Mathf.Cos(angle);
                float y = radius * Mathf.Sin(angle);

                Vector3 position = new(x, y, 0f);

                _setupTargets[i].anchoredPosition = position;
            }
        }
        #endregion

        

        float _menuVisibilityDotThreshold;
        /// <summary>
        /// Only show menu if gaze to menu origin's divergence angle is below this value.
        /// </summary>
        public float MenuVisibleGazeDivergenceThreshold
        {
            get => _menuVisibleGazeAngleDivergenceThreshold;
            set
            {
                _menuVisibleGazeAngleDivergenceThreshold = value;
                _menuVisibilityDotThreshold = AngleToDot(value);
            }
        }




        private GameObject _ui;
        private bool _isHidden;
        private float _gazeCounter;

        private void Awake()
        {
            _ui = GameObject.Instantiate(_uiPrefab, _attachPoint, false);
            _menuVisibilityDotThreshold = AngleToDot(_menuVisibleGazeAngleDivergenceThreshold);
        }

        private void LateUpdate()
        {
            var gazing = IsGazingAtWrist();
            if (gazing)
            {
                _gazeCounter += Time.deltaTime;
            }
            else
            {
                _gazeCounter = 0;
            }

            if (_isHidden)
            {
                if (_gazeCounter > 1f)
                {
                    Show();
                }
            }
            else
            {
                if (!gazing)
                {
                    Hide();
                }
            }
            transform.SetWorldPose(_followTarget.GetWorldPose());
        }

        private void Show()
        {
            _ui.SetActive(true);
            _isHidden = false;
        }

        private void Hide()
        {
            _ui.SetActive(false);
            _isHidden = true;
        }

        private bool IsGazingAtWrist()
        {
            var gazeToObject = (_attachPoint.position - _gazeTarget.position).normalized;
            var gazeDirection = _gazeTarget.forward;
            var facingToward = Vector3.Dot(-_attachPoint.forward, _gazeTarget.forward) < 0;
            return facingToward && Vector3.Dot(gazeToObject, gazeDirection) >= _menuVisibilityDotThreshold;
        }

        static float AngleToDot(float angleDeg)
        {
            return Mathf.Cos(Mathf.Deg2Rad * angleDeg);
        }
    }
}
