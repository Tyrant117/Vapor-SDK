using System;
using UnityEngine;
using VaporKeys;

namespace VaporEvents
{
    [Serializable]
    public class ChangedEventDataReceiver<T>
    {
        [SerializeField]
        private KeyDropdownValue key;

        private EventData<T> _eventData;

        public void Subscribe(Action<object, T> callback)
        {
            Debug.Assert(!key.IsNone,$"Subscribe key is set to None");
            _eventData ??= EventBus.Get<EventData<T>>(key);
            _eventData.Subscribe(callback);
        }

        public void Unsubscribe(Action<object, T> callback)
        {
            _eventData ??= EventBus.Get<EventData<T>>(key);
            _eventData.Unsubscribe(callback);
        }
    }
    
    [Serializable]
    public class ChangedEventDataReceiver<T1, T2>
    {
        [SerializeField]
        private KeyDropdownValue key;

        private EventData<T1, T2> _eventData;

        public void Subscribe(Action<object, T1, T2> callback)
        {
            Debug.Assert(!key.IsNone,$"Subscribe key is set to None");
            _eventData ??= EventBus.Get<EventData<T1, T2>>(key);
            _eventData.Subscribe(callback);
        }

        public void Unsubscribe(Action<object, T1, T2> callback)
        {
            _eventData ??= EventBus.Get<EventData<T1, T2>>(key);
            _eventData.Unsubscribe(callback);
        }
    }
    
    [Serializable]
    public class ChangedEventDataReceiver<T1, T2, T3>
    {
        [SerializeField]
        private KeyDropdownValue key;

        private EventData<T1, T2, T3> _eventData;

        public void Subscribe(Action<object, T1, T2, T3> callback)
        {
            Debug.Assert(!key.IsNone,$"Subscribe key is set to None");
            _eventData ??= EventBus.Get<EventData<T1, T2, T3>>(key);
            _eventData.Subscribe(callback);
        }

        public void Unsubscribe(Action<object, T1, T2, T3> callback)
        {
            _eventData ??= EventBus.Get<EventData<T1, T2, T3>>(key);
            _eventData.Unsubscribe(callback);
        }
    }
    
    [Serializable]
    public class ChangedEventDataReceiver<T1, T2, T3, T4>
    {
        [SerializeField]
        private KeyDropdownValue key;

        private EventData<T1, T2, T3, T4> _eventData;

        public void Subscribe(Action<object, T1, T2, T3, T4> callback)
        {
            Debug.Assert(!key.IsNone,$"Subscribe key is set to None");
            _eventData ??= EventBus.Get<EventData<T1, T2, T3, T4>>(key);
            _eventData.Subscribe(callback);
        }

        public void Unsubscribe(Action<object, T1, T2, T3, T4> callback)
        {
            _eventData ??= EventBus.Get<EventData<T1, T2, T3, T4>>(key);
            _eventData.Unsubscribe(callback);
        }
    }
}
