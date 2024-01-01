using UnityEngine;
using VaporKeys;

namespace VaporEvents
{
    [System.Serializable]
    public class ChangedEventDataSender<T>
    {
        [SerializeField]
        private KeyDropdownValue key;

        private EventData<T> _eventData;

        public void RaiseEvent(object sender, T value)
        {
            Debug.Assert(!key.IsNone,$"{sender}: RaiseEvent is set to None");
            _eventData ??= EventBus.Get<EventData<T>>(key);
            _eventData.RaiseEvent(sender, value);
        }
    }
    
    [System.Serializable]
    public class ChangedEventDataSender<T1, T2>
    {
        [SerializeField]
        private KeyDropdownValue key;

        private EventData<T1, T2> _eventData;

        public void RaiseEvent(object sender, T1 value1, T2 value2)
        {
            Debug.Assert(!key.IsNone,$"{sender}: RaiseEvent is set to None");
            _eventData ??= EventBus.Get<EventData<T1, T2>>(key);
            _eventData.RaiseEvent(sender, value1, value2);
        }
    }
    
    [System.Serializable]
    public class ChangedEventDataSender<T1, T2, T3>
    {
        [SerializeField]
        private KeyDropdownValue key;

        private EventData<T1, T2, T3> _eventData;

        public void RaiseEvent(object sender, T1 value1, T2 value2, T3 value3)
        {
            Debug.Assert(!key.IsNone,$"{sender}: RaiseEvent is set to None");
            _eventData ??= EventBus.Get<EventData<T1, T2, T3>>(key);
            _eventData.RaiseEvent(sender, value1, value2, value3);
        }
    }
    
    [System.Serializable]
    public class ChangedEventDataSender<T1, T2, T3, T4>
    {
        [SerializeField]
        private KeyDropdownValue key;

        private EventData<T1, T2, T3, T4> _eventData;

        public void RaiseEvent(object sender, T1 value1, T2 value2, T3 value3, T4 value4)
        {
            Debug.Assert(!key.IsNone,$"{sender}: RaiseEvent is set to None");
            _eventData ??= EventBus.Get<EventData<T1, T2, T3, T4>>(key);
            _eventData.RaiseEvent(sender, value1, value2, value3, value4);
        }
    }
}
