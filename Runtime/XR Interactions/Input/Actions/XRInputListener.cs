using System;
using UnityEngine;
using VaporInspector;

namespace VaporXR
{
    [Serializable, DrawWithVapor]
    public class XRInputListenerBool
    {
        [SerializeField]
        private XRInputListenerBoolSo _boolListener;

        private bool _enabled;

        public void Enable()
        {
            if (_boolListener)
            {
                _boolListener.Enable();
                _enabled = true;
            }
        }

        public void Disable()
        {
            if (_boolListener)
            {
                _boolListener.Disable();
            }
            _enabled = false;
        }

        public bool ReadValue()
        {
            return _enabled && _boolListener.ReadValue();
        }
    }

    [Serializable, DrawWithVapor]
    public class XRInputListenerFloat
    {
        [SerializeField]
        private XRInputListenerFloatSo _floatListener;

        private bool _enabled;

        public void Enable()
        {
            if (_floatListener)
            {
                _floatListener.Enable();
                _enabled = true;
            }
        }

        public void Disable()
        {
            if (_floatListener)
            {
                _floatListener.Disable();
            }
            _enabled = false;
        }

        public float ReadValue()
        {
            return _enabled ? _floatListener.ReadValue() : 0f;
        }
    }

    [Serializable, DrawWithVapor]
    public class XRInputListenerVector2
    {
        [SerializeField]
        private XRInputListenerVector2So _vector2Listener;

        private bool _enabled;

        public void Enable()
        {
            if(_vector2Listener)
            {
                _vector2Listener.Enable();
                _enabled = true;
            }
        }

        public void Disable()
        {
            if (_vector2Listener)
            {
                _vector2Listener.Disable();                
            }
            _enabled = false;
        }

        public Vector2 ReadValue()
        {
            return _enabled ? _vector2Listener.ReadValue() : Vector2.zero;
        }
    }
}
