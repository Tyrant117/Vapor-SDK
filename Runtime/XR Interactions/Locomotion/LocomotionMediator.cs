using System.Collections.Generic;
using UnityEngine;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// Behavior that mediates user locomotion by providing <see cref="LocomotionProvider"/> components with access to the
    /// <see cref="VXRBodyTransformer"/> linked to this behavior. This behavior manages the <see cref="LocomotionState"/>
    /// for each provider based on its requests for the Body Transformer.
    /// </summary>
    [RequireComponent(typeof(VXRBodyTransformer))]
    public class LocomotionMediator : MonoBehaviour
    {
        private class LocomotionProviderData
        {
            public LocomotionState State;
            public int LocomotionEndFrame;
        }
        
        private static readonly List<LocomotionProvider> s_ProvidersToRemove = new();

        /// <summary>
        /// The XR Origin controlled by the <see cref="VXRBodyTransformer"/> for locomotion.
        /// </summary>
        public VXROrigin XROrigin
        {
            get => _xrBodyTransformer.XROrigin;
            set => _xrBodyTransformer.XROrigin = value;
        }

        private VXRBodyTransformer _xrBodyTransformer;
        private readonly Dictionary<LocomotionProvider, LocomotionProviderData> _providerDataMap = new();

        protected void Awake()
        {
            _xrBodyTransformer = GetComponent<VXRBodyTransformer>();
        }

        protected void Update()
        {
            s_ProvidersToRemove.Clear();
            foreach (var kvp in _providerDataMap)
            {
                var provider = kvp.Key;
                if (provider == null)
                {
                    s_ProvidersToRemove.Add(provider);
                    continue;
                }

                var providerData = kvp.Value;
                if (providerData.State == LocomotionState.Preparing && provider.CanStartMoving)
                {
                    StartLocomotion(provider, providerData);
                }
                else if (providerData.State == LocomotionState.Ended && Time.frameCount > providerData.LocomotionEndFrame)
                {
                    providerData.State = LocomotionState.Idle;
                }
            }

            foreach (var provider in s_ProvidersToRemove)
            {
                _providerDataMap.Remove(provider);
            }
        }

        public bool TryPrepareLocomotion(LocomotionProvider provider)
        {
            if (!_providerDataMap.TryGetValue(provider, out var providerData))
            {
                // We can skip checking state because it is assumed to be Idle if it is not in the map.
                providerData = new LocomotionProviderData();
                _providerDataMap[provider] = providerData;
            }
            else if (GetProviderLocomotionState(provider).IsActive())
            {
                return false;
            }

            providerData.State = LocomotionState.Preparing;
            return true;
        }

        public bool TryStartLocomotion(LocomotionProvider provider)
        {
            if (!_providerDataMap.TryGetValue(provider, out var providerData))
            {
                // We can skip checking state because it is assumed to be Idle if it is not in the map.
                providerData = new LocomotionProviderData();
                _providerDataMap[provider] = providerData;
            }
            else if (GetProviderLocomotionState(provider) == LocomotionState.Moving)
            {
                return false;
            }

            StartLocomotion(provider, providerData);
            return true;
        }

        private void StartLocomotion(LocomotionProvider provider, LocomotionProviderData providerData)
        {
            providerData.State = LocomotionState.Moving;
            provider.OnLocomotionStart(_xrBodyTransformer);
        }

        public bool TryEndLocomotion(LocomotionProvider provider)
        {
            if (!_providerDataMap.TryGetValue(provider, out var providerData))
            {
                return false;
            }

            var locomotionState = providerData.State;
            if (!locomotionState.IsActive())
            {
                return false;
            }

            providerData.State = LocomotionState.Ended;
            providerData.LocomotionEndFrame = Time.frameCount;
            provider.OnLocomotionEnd();
            return true;
        }

        /// <summary>
        /// Queries the state of locomotion for the given provider.
        /// </summary>
        /// <param name="provider">The provider whose locomotion state to query.</param>
        /// <returns>Returns the state of locomotion for the given <paramref name="provider"/>. This returns
        /// <see cref="LocomotionState.Idle"/> if the provider is not managed by this mediator.</returns>
        public LocomotionState GetProviderLocomotionState(LocomotionProvider provider)
        {
            return _providerDataMap.TryGetValue(provider, out var providerData) ? providerData.State : LocomotionState.Idle;
        }
    }
}
