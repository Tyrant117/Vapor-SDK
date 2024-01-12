using System.Collections.Generic;
using Unity.XR.CoreUtils;
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
        class LocomotionProviderData
        {
            public LocomotionState state;
            public int locomotionEndFrame;
        }

        /// <summary>
        /// The XR Origin controlled by the <see cref="VXRBodyTransformer"/> for locomotion.
        /// </summary>
        public XROrigin xrOrigin
        {
            get => m_XRBodyTransformer.XROrigin;
            set => m_XRBodyTransformer.XROrigin = value;
        }

        VXRBodyTransformer m_XRBodyTransformer;

        readonly Dictionary<LocomotionProvider, LocomotionProviderData> m_ProviderDataMap =
            new Dictionary<LocomotionProvider, LocomotionProviderData>();

        static readonly List<LocomotionProvider> s_ProvidersToRemove = new List<LocomotionProvider>();

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Awake()
        {
            m_XRBodyTransformer = GetComponent<VXRBodyTransformer>();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Update()
        {
            s_ProvidersToRemove.Clear();
            foreach (var kvp in m_ProviderDataMap)
            {
                var provider = kvp.Key;
                if (provider == null)
                {
                    s_ProvidersToRemove.Add(provider);
                    continue;
                }

                var providerData = kvp.Value;
                if (providerData.state == LocomotionState.Preparing && provider.CanStartMoving)
                {
                    StartLocomotion(provider, providerData);
                }
                else if (providerData.state == LocomotionState.Ended && Time.frameCount > providerData.locomotionEndFrame)
                {
                    providerData.state = LocomotionState.Idle;
                }
            }

            foreach (var provider in s_ProvidersToRemove)
            {
                m_ProviderDataMap.Remove(provider);
            }
        }

        internal bool TryPrepareLocomotion(LocomotionProvider provider)
        {
            if (!m_ProviderDataMap.TryGetValue(provider, out var providerData))
            {
                // We can skip checking state because it is assumed to be Idle if it is not in the map.
                providerData = new LocomotionProviderData();
                m_ProviderDataMap[provider] = providerData;
            }
            else if (GetProviderLocomotionState(provider).IsActive())
            {
                return false;
            }

            providerData.state = LocomotionState.Preparing;
            return true;
        }

        internal bool TryStartLocomotion(LocomotionProvider provider)
        {
            if (!m_ProviderDataMap.TryGetValue(provider, out var providerData))
            {
                // We can skip checking state because it is assumed to be Idle if it is not in the map.
                providerData = new LocomotionProviderData();
                m_ProviderDataMap[provider] = providerData;
            }
            else if (GetProviderLocomotionState(provider) == LocomotionState.Moving)
            {
                return false;
            }

            StartLocomotion(provider, providerData);
            return true;
        }

        void StartLocomotion(LocomotionProvider provider, LocomotionProviderData providerData)
        {
            providerData.state = LocomotionState.Moving;
            provider.OnLocomotionStart(m_XRBodyTransformer);
        }

        internal bool TryEndLocomotion(LocomotionProvider provider)
        {
            if (!m_ProviderDataMap.TryGetValue(provider, out var providerData))
                return false;

            var locomotionState = providerData.state;
            if (!locomotionState.IsActive())
                return false;

            providerData.state = LocomotionState.Ended;
            providerData.locomotionEndFrame = Time.frameCount;
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
            return m_ProviderDataMap.TryGetValue(provider, out var providerData) ? providerData.state : LocomotionState.Idle;
        }
    }
}
