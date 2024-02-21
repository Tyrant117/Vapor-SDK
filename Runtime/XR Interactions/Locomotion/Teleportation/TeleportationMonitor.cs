using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Pool;
using VaporXR.Interaction;
using Object = UnityEngine.Object;

namespace VaporXR.Locomotion.Teleportation
{
    /// <summary>
    /// Use this class to maintain a list of Interactors that are potentially influenced by teleportation
    /// and subscribe to the event when teleportation occurs. Uses the events invoked by
    /// <see cref="TeleportationProvider"/> to detect teleportation.
    /// </summary>
    /// <remarks>
    /// Used by the XR Grab Interactable to cancel out the effect of the teleportation from its tracked velocity
    /// so it does not release at unintentionally high energy.
    /// </remarks>
    /// <seealso cref="XRGrabInteractable"/>
    public class TeleportationMonitor
    {
        /// <summary>
        /// Calls the methods in its invocation list when one of the Interactors monitored has been influenced by teleportation.
        /// The <see cref="Pose"/> event args represents the amount the <see cref="XROrigin"/> rig was translated and rotated.
        /// </summary>
        public event Action<Pose> teleported;

        /// <summary>
        /// The list of interactors monitored that are influenced by teleportation.
        /// Consists of those that are a child GameObject of the <see cref="XROrigin"/> rig.
        /// </summary>
        /// <remarks>
        /// There will typically only ever be one <see cref="TeleportationProvider"/> in the scene.
        /// </remarks>
        Dictionary<TeleportationProvider, List<IInteractor>> m_TeleportInteractors;

        /// <summary>
        /// The <see cref="Pose"/> of the <see cref="XROrigin"/> rig before teleportation.
        /// Used to calculate the teleportation delta using this as reference.
        /// </summary>
        Dictionary<LocomotionMediator, Pose> m_OriginPosesBeforeTeleport;

        static readonly LinkedPool<Dictionary<TeleportationProvider, List<IInteractor>>> s_TeleportInteractorsPool =
            new LinkedPool<Dictionary<TeleportationProvider, List<IInteractor>>>(() => new Dictionary<TeleportationProvider, List<IInteractor>>());

        static readonly LinkedPool<Dictionary<LocomotionMediator, Pose>> s_OriginPosesBeforeTeleportPool =
            new LinkedPool<Dictionary<LocomotionMediator, Pose>>(() => new Dictionary<LocomotionMediator, Pose>());

        /// <summary>
        /// Cached reference to <see cref="TeleportationProvider"/> instances found.
        /// </summary>
        static TeleportationProvider[] s_TeleportationProvidersCache;

        /// <summary>
        /// Adds <paramref name="interactor"/> to monitor. If it is a child of the XR Origin, <see cref="teleported"/>
        /// will be invoked when the player teleports.
        /// </summary>
        /// <param name="interactor">The Interactor to add.</param>
        /// <seealso cref="RemoveInteractor"/>
        public void AddInteractor(IInteractor interactor)
        {
            if (interactor == null)
                throw new ArgumentNullException(nameof(interactor));

            var interactorTransform = interactor.transform;
            if (interactorTransform == null)
                return;

            if (!FindTeleportationProviders())
                return;

            foreach (var teleportationProvider in s_TeleportationProvidersCache)
            {
                if (!TryGetOriginTransform(teleportationProvider, out var originTransform))
                    continue;

                if (!interactorTransform.IsChildOf(originTransform))
                    continue;

                if (m_TeleportInteractors == null)
                    m_TeleportInteractors = s_TeleportInteractorsPool.Get();

                if (!m_TeleportInteractors.TryGetValue(teleportationProvider, out var interactors))
                {
                    interactors = new List<IInteractor>();
                    m_TeleportInteractors.Add(teleportationProvider, interactors);
                }

                Debug.Assert(!interactors.Contains(interactor));
                interactors.Add(interactor);

                if (interactors.Count == 1)
                {
                    teleportationProvider.LocomotionStarted += OnBeginTeleportation;
                    teleportationProvider.LocomotionEnded += OnEndTeleportation;
                }
            }
        }

        /// <summary>
        /// Removes <paramref name="interactor"/> from monitor.
        /// </summary>
        /// <param name="interactor">The Interactor to remove.</param>
        /// <seealso cref="AddInteractor"/>
        public void RemoveInteractor(IInteractor interactor)
        {
            if (interactor == null)
                throw new ArgumentNullException(nameof(interactor));

            var totalInteractors = 0;
            if (m_TeleportInteractors != null)
            {
                foreach (var kvp in m_TeleportInteractors)
                {
                    var teleportationProvider = kvp.Key;
                    var interactors = kvp.Value;

                    if (interactors.Remove(interactor) && interactors.Count == 0 && teleportationProvider != null)
                    {
                        teleportationProvider.LocomotionStarted -= OnBeginTeleportation;
                        teleportationProvider.LocomotionEnded -= OnEndTeleportation;
                    }

                    totalInteractors += interactors.Count;
                }
            }

            // Release back to the pool
            if (totalInteractors == 0)
            {
                if (m_TeleportInteractors != null)
                {
                    s_TeleportInteractorsPool.Release(m_TeleportInteractors);
                    m_TeleportInteractors = null;
                }

                if (m_OriginPosesBeforeTeleport != null)
                {
                    s_OriginPosesBeforeTeleportPool.Release(m_OriginPosesBeforeTeleport);
                    m_OriginPosesBeforeTeleport = null;
                }
            }
        }

        static bool TryGetOriginTransform(LocomotionProvider locomotionProvider, out Transform originTransform)
        {
            // Correct version of locomotionProvider?.system?.xrOrigin?.Origin?.transform
            if (locomotionProvider != null)
            {
                var system = locomotionProvider.Mediator;
                return TryGetOriginTransform(system, out originTransform);
            }

            originTransform = null;
            return false;
        }

        static bool TryGetOriginTransform(LocomotionMediator mediator, out Transform originTransform)
        {
            // Correct version of system?.xrOrigin?.Origin?.transform
            if (mediator != null)
            {
                var xrOrigin = mediator.XROrigin;
                if (xrOrigin != null)
                {
                    var origin = xrOrigin.Origin;
                    if (origin != null)
                    {
                        originTransform = origin.transform;
                        return true;
                    }
                }
            }

            originTransform = null;
            return false;
        }

        static bool FindTeleportationProviders()
        {
            if (s_TeleportationProvidersCache == null)
#if UNITY_2023_1_OR_NEWER
                s_TeleportationProvidersCache = Object.FindObjectsByType<TeleportationProvider>(FindObjectsSortMode.None);
#else
                s_TeleportationProvidersCache = Object.FindObjectsOfType<TeleportationProvider>();
#endif

            return s_TeleportationProvidersCache.Length > 0;
        }

        void OnBeginTeleportation(LocomotionProvider provider)
        {
            var mediator = provider.Mediator;
            if (!TryGetOriginTransform(mediator, out var originTransform))
                return;

            if (m_OriginPosesBeforeTeleport == null)
                m_OriginPosesBeforeTeleport = s_OriginPosesBeforeTeleportPool.Get();

            m_OriginPosesBeforeTeleport[mediator] = originTransform.GetWorldPose();
        }

        void OnEndTeleportation(LocomotionProvider provider)
        {
            var mediator = provider.Mediator;
            if (!TryGetOriginTransform(mediator, out var originTransform))
                return;

            if (m_OriginPosesBeforeTeleport == null)
                return;

            if (!m_OriginPosesBeforeTeleport.TryGetValue(mediator, out var originPoseBeforeTeleport))
                return;

            var translated = originTransform.position - originPoseBeforeTeleport.position;
            var rotated = originTransform.rotation * Quaternion.Inverse(originPoseBeforeTeleport.rotation);

            teleported?.Invoke(new Pose(translated, rotated));
        }
    }
}
