using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    public class VXRFootGrounder : MonoBehaviour
    {
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private Transform _body;
        [SerializeField] private Vector3 _footGroundDirection;
        
        private float _footSpacing;
        private Vector3 _oldPosition;
        private Vector3 _currentPosition;
        private Vector3 _newPosition;
        
        private Vector3 _oldNormal;
        private Vector3 _currentNormal;
        private Vector3 _newNormal;

        private readonly RaycastHit[] _groundingResults = new  RaycastHit[1];
        private Transform _transform;
        private Quaternion _footNormal;
        
        private void Start()
        {
            _transform = transform;
            _footSpacing = _transform.localPosition.x;
            _currentPosition = _newPosition = _oldPosition = _transform.position;
            _currentNormal = _newNormal = _oldNormal = _transform.up;
            _footNormal = Quaternion.Euler(_footGroundDirection);
        }

        private void Update()
        {
            _transform.position = _currentPosition;
            _transform.rotation =  Quaternion.FromToRotation(_footGroundDirection, _currentNormal);
            
            var ray = new Ray(_body.position + (_body.right * _footSpacing), Vector3.down);
            var hits = Physics.RaycastNonAlloc(ray, _groundingResults, 1.5f, _groundMask);
            if (hits == 1)
            {
                _currentPosition = _groundingResults[0].point;
                _currentNormal = _groundingResults[0].normal;
            }
        }
    }
}
