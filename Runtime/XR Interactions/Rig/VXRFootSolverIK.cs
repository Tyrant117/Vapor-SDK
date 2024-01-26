using System;
using UnityEngine;

namespace VaporXR
{
    public class VXRFootSolverIK : MonoBehaviour
    {
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private Transform _body;
        [SerializeField] private VXRFootSolverIK _otherFoot;
        [SerializeField] private float _speed = 4;
        [SerializeField] private float _stepDistance = 0.2f;
        [SerializeField] private float _stepLength = 0.2f;
        [SerializeField] float _sideStepLength = 0.1f;
        
        [SerializeField] private float _stepHeight = 0.3f;
        [SerializeField] private Vector3 _footOffset;

        [SerializeField] private Vector3 _footRotOffset;
        [SerializeField] private float _footYPosOffset = 0.1f;

        [SerializeField] private float _rayStartYOffset = 0;
        [SerializeField] private float _rayLength = 1.5f;
        
        public bool IsMoving => _lerp < 1;
        
        private float _footSpacing;
        private Vector3 _oldPosition;
        private Vector3 _currentPosition;
        private Vector3 _newPosition;
        
        private Vector3 _oldNormal;
        private Vector3 _currentNormal;
        private Vector3 _newNormal;
        private float _lerp;
        
        private readonly RaycastHit[] _groundingResults = new  RaycastHit[1];
        private Transform _transform;
        private bool _isMovingForward;

        private void Start()
        {
            _transform = transform;
            _footSpacing = _transform.localPosition.x;
            _currentPosition = _newPosition = _oldPosition = _transform.position;
            _currentNormal = _newNormal = _oldNormal = _transform.up;
            _lerp = 1;
        }

        private void Update()
        {
            _transform.position = _currentPosition + Vector3.up * _footYPosOffset;
            _transform.up = _currentNormal;
            transform.localRotation = Quaternion.Euler(_footRotOffset);
            
            var ray = new Ray(_body.position + (_body.right * _footSpacing) + Vector3.up * _rayStartYOffset, Vector3.down);
            var hits = Physics.RaycastNonAlloc(ray, _groundingResults, _rayLength, _groundMask);
            if (hits == 1)
            {
                if (Vector3.Distance(_newPosition, _groundingResults[0].point) > _stepDistance && !_otherFoot.IsMoving && _lerp >= 1)
                {
                    _lerp = 0;
                    var direction = Vector3.ProjectOnPlane(_groundingResults[0].point - _currentPosition,Vector3.up).normalized;
                    var angle = Vector3.Angle(_body.forward, _body.InverseTransformDirection(direction));
                    
                    _isMovingForward = angle is < 50 or > 130;
                    if(_isMovingForward)
                    {
                        _newPosition = _groundingResults[0].point + direction * _stepLength + _footOffset;
                        _newNormal = _groundingResults[0].normal;
                    }
                    else
                    {
                        _newPosition = _groundingResults[0].point + direction * _sideStepLength + _footOffset;
                        _newNormal = _groundingResults[0].normal;
                    }
                }
            }
            
            if (_lerp < 1)
            {
                var tempPosition = Vector3.Lerp(_oldPosition, _newPosition, _lerp);
                tempPosition.y += Mathf.Sin(_lerp * Mathf.PI) * _stepHeight;

                _currentPosition = tempPosition;
                _currentNormal = Vector3.Lerp(_oldNormal, _newNormal, _lerp);
                _lerp += Time.deltaTime * _speed;
            }
            else
            {
                _oldPosition = _newPosition;
                _oldNormal = _newNormal;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawLine(_body.position, _body.position + Vector3.down * 1.5f + Vector3.up * _rayStartYOffset);
        }
    }
}
