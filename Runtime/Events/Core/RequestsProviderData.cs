using System;
using UnityEngine;
using VaporKeys;

namespace VaporEvents
{
    [Serializable]
    public class RequestsProviderData<TResult>
    {
        [SerializeField]
        private KeyDropdownValue key;
        
        private ProviderData<TResult> _providerData;
        private Coroutine _requestRoutine;

        public void RequestComponent(MonoBehaviour requester, Action<TResult> callback)
        {
            Debug.Assert(!key.IsNone,$"{requester}: RequestComponent is set to None");
            _providerData ??= ProviderBus.Get<ProviderData<TResult>>(key);
            _requestRoutine = requester.StartCoroutine(_providerData.RequestRoutine(callback));
        }

        public void StopRequesting(MonoBehaviour requester)
        {
            requester.StopCoroutine(_requestRoutine);
        }
    }
    
    [Serializable]
    public class RequestsProviderData<TValue1, TResult>
    {
        [SerializeField]
        private KeyDropdownValue key;
        
        private ProviderData<TValue1, TResult> _providerData;
        private Coroutine _requestRoutine;

        public void RequestComponent(MonoBehaviour requester, TValue1 value1, Action<TResult> callback)
        {
            Debug.Assert(!key.IsNone,$"{requester}: RequestComponent is set to None");
            _providerData ??= ProviderBus.Get<ProviderData<TValue1, TResult>>(key);
            _requestRoutine = requester.StartCoroutine(_providerData.RequestRoutine(value1, callback));
        }

        public void StopRequesting(MonoBehaviour requester)
        {
            requester.StopCoroutine(_requestRoutine);
        }
    }
    
    [Serializable]
    public class RequestsProviderData<TValue1, TValue2, TResult>
    {
        [SerializeField]
        private KeyDropdownValue key;
        
        private ProviderData<TValue1, TValue2, TResult> _providerData;
        private Coroutine _requestRoutine;

        public void RequestComponent(MonoBehaviour requester, TValue1 value1, TValue2 value2, Action<TResult> callback)
        {
            Debug.Assert(!key.IsNone,$"{requester}: RequestComponent is set to None");
            _providerData ??= ProviderBus.Get<ProviderData<TValue1, TValue2, TResult>>(key);
            _requestRoutine = requester.StartCoroutine(_providerData.RequestRoutine(value1, value2, callback));
        }

        public void StopRequesting(MonoBehaviour requester)
        {
            requester.StopCoroutine(_requestRoutine);
        }
    }
    
    [Serializable]
    public class RequestsProviderData<TValue1, TValue2, TValue3, TResult>
    {
        [SerializeField]
        private KeyDropdownValue key;
        
        private ProviderData<TValue1, TValue2, TValue3, TResult> _providerData;
        private Coroutine _requestRoutine;

        public void RequestComponent(MonoBehaviour requester, TValue1 value1, TValue2 value2, TValue3 value3, Action<TResult> callback)
        {
            Debug.Assert(!key.IsNone,$"{requester}: RequestComponent is set to None");
            _providerData ??= ProviderBus.Get<ProviderData<TValue1, TValue2, TValue3, TResult>>(key);
            _requestRoutine = requester.StartCoroutine(_providerData.RequestRoutine(value1, value2, value3, callback));
        }

        public void StopRequesting(MonoBehaviour requester)
        {
            requester.StopCoroutine(_requestRoutine);
        }
    }
    
    [Serializable]
    public class RequestsProviderData<TValue1, TValue2, TValue3, TValue4, TResult>
    {
        [SerializeField]
        private KeyDropdownValue key;
        
        private ProviderData<TValue1, TValue2, TValue3, TValue4, TResult> _providerData;
        private Coroutine _requestRoutine;

        public void RequestComponent(MonoBehaviour requester, TValue1 value1, TValue2 value2, TValue3 value3, TValue4 value4, Action<TResult> callback)
        {
            Debug.Assert(!key.IsNone,$"{requester}: RequestComponent is set to None");
            _providerData ??= ProviderBus.Get<ProviderData<TValue1, TValue2, TValue3, TValue4, TResult>>(key);
            _requestRoutine = requester.StartCoroutine(_providerData.RequestRoutine(value1, value2, value3, value4, callback));
        }

        public void StopRequesting(MonoBehaviour requester)
        {
            requester.StopCoroutine(_requestRoutine);
        }
    }
}
