using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporEvents
{
    public static class SingletonBus
    {
        public static readonly Dictionary<Type, MonoBehaviour> SingletonMap = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            SingletonMap.Clear();
        }

        /// <summary>
        /// Gets or creates an instance of the event at the supplied id. This id should typically be a auto-generated guid, but any integer will work. <br />
        /// The event should always be cached or only used in loading and unloading. <br />
        /// <b>String/Int collisions will not be detected!</b>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventID"></param>
        /// <returns></returns>
        public static T Get<T>() where T : MonoBehaviour
        {
            if (SingletonMap.TryGetValue(typeof(T), out var handler) && handler != null)
            {
                return (T)handler;
            }

            EventLogging.Log($"[Singleton Bus] Adding Provider: [{nameof(T)}] of Type: {typeof(T)}");
            var go = new GameObject();
            var comp = go.AddComponent<T>();
            SingletonMap.Add(typeof(T), comp);
            return (T)SingletonMap[typeof(T)];
        }
    }
}
