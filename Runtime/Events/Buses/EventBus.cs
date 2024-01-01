using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor;
using VaporKeys;

namespace VaporEvents
{
    public static class EventBus
    {
        public static readonly Dictionary<int, IEventData> EventMap = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            EventMap.Clear();
        }

        /// <summary>
        /// Gets or creates an instance of the event at the supplied id. This id should typically be a auto-generated guid, but any string that isn't empty or null will work. <br />
        /// The event should always be cached or only used in loading and unloading.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventID"></param>
        /// <returns></returns>
        public static T Get<T>(int eventID) where T : IEventData
        {
            if (EventMap.TryGetValue(eventID, out var handler))
            {
                return (T)handler;
            }

            EventLogging.Log($"[Event Bus] Adding Event: [{eventID}] of Type: {typeof(T)}");
            EventMap.Add(eventID, Activator.CreateInstance<T>());
            return (T)EventMap[eventID];
        }

        /// <summary>
        /// Gets or creates an instance of the event at the supplied id. This id should typically be a auto-generated guid, but any string that isn't empty or null will work. <br />
        /// The event should always be cached or only used in loading and unloading. <br />
        /// <b>String/Int collisions will not be detected!</b>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static T Get<T>(string eventName) where T : IEventData
        {
            var eventID = eventName.GetKeyHashCode();
            if (EventMap.TryGetValue(eventID, out var handler))
            {
                return (T)handler;
            }

            EventLogging.Log($"[Event Bus] Adding Event: [{eventName}] of Type: {typeof(T)}");
            EventMap.Add(eventID, Activator.CreateInstance<T>());
            return (T)EventMap[eventID];
        }

        /// <summary>
        /// Gets or creates an instance of the event at the supplied id. This id should typically be a auto-generated guid, but any string that isn't empty or null will work. <br />
        /// The event should always be cached or only used in loading and unloading. <br />
        /// <b>String/Int collisions will not be detected!</b>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventKey"></param>
        /// <returns></returns>
        public static T Get<T>(EventKeySo eventKey) where T : IEventData
        {
            var eventID = eventKey.Key;
            if (EventMap.TryGetValue(eventID, out var handler))
            {
                return (T)handler;
            }

            EventLogging.Log($"[Event Bus] Adding Event: [{eventKey.name}] of Type: {typeof(T)}");
            EventMap.Add(eventID, Activator.CreateInstance<T>());
            return (T)EventMap[eventID];
        }
    }
}
