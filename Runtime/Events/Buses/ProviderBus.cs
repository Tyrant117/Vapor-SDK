using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vapor;

namespace VaporEvents
{
    /// <summary>
    /// Static class used to access providers
    /// </summary>
    public static class ProviderBus
    {
        public static readonly Dictionary<int, IProviderData> ProviderMap = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            ProviderMap.Clear();
        }

        /// <summary>
        /// Gets or creates an instance of the provider at the supplied id. This id should typically be a auto-generated guid, but any integer will work. <br />
        /// <br />The event should always be cached or only used in loading and unloading.
        /// </summary>
        /// <typeparam name="T">The type of provider. Must implement <see cref="IProviderData"/></typeparam>
        /// <param name="providerId">The id of the provider</param>
        /// <returns>The <see cref="IProviderData"/> associated with the id</returns>
        public static T Get<T>(int providerId) where T : IProviderData
        {
            if (ProviderMap.TryGetValue(providerId, out var handler))
            {
                return (T)handler;
            }

            EventLogging.Log($"[Provider Bus] Adding Provider: [{providerId}] of Type: {typeof(T)}");
            ProviderMap.Add(providerId, Activator.CreateInstance<T>());
            return (T)ProviderMap[providerId];
        }

        /// <summary>
        /// Gets or creates an instance of the event at the supplied id. This id should typically be a auto-generated guid, but any string that isn't empty or null will work. <br />
        /// <br />The event should always be cached or only used in loading and unloading. <br />
        /// <b>String/Int collisions will not be detected!</b>
        /// </summary>
        /// <typeparam name="T">The type of provider. Must implement <see cref="IProviderData"/></typeparam>
        /// <param name="eventName">The name of the provider</param>
        /// <returns>The <see cref="IProviderData"/> associated with the name</returns>
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
        /// <br />The event should always be cached or only used in loading and unloading. <br />
        /// </summary>
        /// <typeparam name="T">The type of provider. Must implement <see cref="IProviderData"/></typeparam>
        /// <param name="providerKey">The key of the provider</param>
        /// <returns>The <see cref="IProviderData"/> associated with the key</returns>
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
        
        /// <summary>
        /// Directly attempts to get the component associated with a <see cref="ProviderData{TResult}"/>
        /// </summary>
        /// <param name="providerId">The id of the provider</param>
        /// <typeparam name="T">The type to return. Must inherit from <see cref="Component"/></typeparam>
        /// <returns>The component of type T or null</returns>
        public static T GetComponent<T>(int providerId) where T : Component => Get<ProviderData<Component>>(providerId).Request<T>();
        /// <summary>
        /// Directly attempts to get the component associated with a <see cref="ProviderData{TResult}"/>
        /// </summary>
        /// <param name="providerName">The name of the provider</param>
        /// <typeparam name="T">The type to return. Must inherit from <see cref="Component"/></typeparam>
        /// <returns>The component of type T or null</returns>
        public static T GetComponent<T>(string providerName) where T : Component => Get<ProviderData<Component>>(providerName).Request<T>();
        /// <summary>
        /// Directly attempts to get the component associated with a <see cref="ProviderData{TResult}"/>
        /// </summary>
        /// <param name="providerKey">The key of the provider</param>
        /// <typeparam name="T">The type to return. Must inherit from <see cref="Component"/></typeparam>
        /// <returns>The component of type T or null</returns>
        public static T GetComponent<T>(ProviderKeySo providerKey) where T : Component => Get<ProviderData<Component>>(providerKey).Request<T>();

        /// <summary>
        /// Retrieves the component of type T from a provider once its value is not null.
        /// </summary>
        /// <param name="providerId">The id of the provider</param>
        /// <param name="callback">The callback to fire once the result is not null</param>
        /// <typeparam name="T">The type to return. Must inherit from <see cref="Component"/></typeparam>
        /// <returns>An enumerator that should be used in a <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/></returns>
        public static IEnumerator GetComponentRoutine<T>(int providerId, Action<T> callback) where T : Component => Get<ProviderData<Component>>(providerId).RequestRoutine(callback);
        /// <summary>
        /// Retrieves the component of type T from a provider once its value is not null.
        /// </summary>
        /// <param name="providerName">The name of the provider</param>
        /// <param name="callback">The callback to fire once the result is not null</param>
        /// <typeparam name="T">The type to return. Must inherit from <see cref="Component"/></typeparam>
        /// <returns>An enumerator that should be used in a <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/></returns>
        public static IEnumerator GetComponentRoutine<T>(string providerName, Action<T> callback) where T : Component => Get<ProviderData<Component>>(providerName).RequestRoutine(callback);
        /// <summary>
        /// Retrieves the component of type T from a provider once its value is not null.
        /// </summary>
        /// <param name="providerKey">The key of the provider</param>
        /// <param name="callback">The callback to fire once the result is not null</param>
        /// <typeparam name="T">The type to return. Must inherit from <see cref="Component"/></typeparam>
        /// <returns>An enumerator that should be used in a <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/></returns>
        public static IEnumerator GetComponentRoutine<T>(ProviderKeySo providerKey, Action<T> callback) where T : Component => Get<ProviderData<Component>>(providerKey).RequestRoutine(callback);
    }
}
