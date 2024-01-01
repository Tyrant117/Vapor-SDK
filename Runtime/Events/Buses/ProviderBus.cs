using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vapor;
using VaporKeys;

namespace VaporEvents
{
    public static class ProviderBus
    {
        public static readonly Dictionary<int, IProviderData> ProviderMap = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            ProviderMap.Clear();
        }

        /// <summary>
        /// Gets or creates an instance of the event at the supplied id. This id should typically be a auto-generated guid, but any integer will work. <br />
        /// The event should always be cached or only used in loading and unloading. <br />
        /// <b>String/Int collisions will not be detected!</b>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventID"></param>
        /// <returns></returns>
        public static T Get<T>(int eventID) where T : IProviderData
        {
            if (ProviderMap.TryGetValue(eventID, out var handler))
            {
                return (T)handler;
            }

            EventLogging.Log($"[Provider Bus] Adding Provider: [{eventID}] of Type: {typeof(T)}");
            ProviderMap.Add(eventID, Activator.CreateInstance<T>());
            return (T)ProviderMap[eventID];
        }

        /// <summary>
        /// Gets or creates an instance of the event at the supplied id. This id should typically be a auto-generated guid, but any string that isnt empty or null will work. <br />
        /// The event should always be cached or only used in loading and unloading. <br />
        /// <b>String/Int collisions will not be detected!</b>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static T Get<T>(string eventName) where T : IProviderData
        {
            var eventID = eventName.GetKeyHashCode();
            if (ProviderMap.TryGetValue(eventID, out var handler))
            {
                return (T)handler;
            }

            EventLogging.Log($"[Provider Bus] Adding Provider: [{eventName}] of Type: {typeof(T)}");
            ProviderMap.Add(eventID, Activator.CreateInstance<T>());
            return (T)ProviderMap[eventID];
        }

        /// <summary>
        /// Gets or creates an instance of the event at the supplied id. This id should typically be a auto-generated guid, but any string that isnt empty or null will work. <br />
        /// The event should always be cached or only used in loading and unloading. <br />
        /// <b>String/Int collisions will not be detected!</b>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Get<T>(ProviderKeySo providerKey) where T : IProviderData
        {
            var eventID = providerKey.Key;
            if (ProviderMap.TryGetValue(eventID, out var handler))
            {
                return (T)handler;
            }

            EventLogging.Log($"[Provider Bus] Adding Provider: [{providerKey.name}] of Type: {typeof(T)}");
            ProviderMap.Add(eventID, Activator.CreateInstance<T>());
            return (T)ProviderMap[eventID];
        }

        public static T GetComponent<T>(int eventID) where T : Component => Get<ProviderData<Component>>(eventID).Request<T>();
        public static T GetComponent<T>(string eventName) where T : Component => Get<ProviderData<Component>>(eventName).Request<T>();
        public static T GetComponent<T>(ProviderKeySo providerKey) where T : Component => Get<ProviderData<Component>>(providerKey).Request<T>();

        public static IEnumerator GetComponentRoutine<T>(int eventID, Action<T> callback) where T : Component => Get<ProviderData<Component>>(eventID).RequestRoutine(callback);
        public static IEnumerator GetComponentRoutine<T>(string eventName, Action<T> callback) where T : Component => Get<ProviderData<Component>>(eventName).RequestRoutine(callback);
        public static IEnumerator GetComponentRoutine<T>(ProviderKeySo providerKey, Action<T> callback) where T : Component => Get<ProviderData<Component>>(providerKey).RequestRoutine(callback);
    }
}
