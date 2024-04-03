using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vapor;
using VaporKeys;

namespace VaporEvents
{
    /// <summary>
    /// A static class for managing keys generated from <see cref="EventKeySo"/> and <see cref="ProviderKeySo"/>
    /// </summary>
    public static class EventKeyUtility
    {
        private static Type _eventKeyType;
        private static Type _providerKeyType;        
        /// <summary>
        /// Gets a list of all <see cref="EventKeySo"/> in the project. Will return null if not in the UNITY_EDITOR
        /// </summary>
        /// <returns></returns>
        public static List<(string, EventKeySo)> GetAllEventKeys()
        {
            var allProviderKeys = RuntimeAssetDatabaseUtility.FindAssetsByType<EventKeySo>();
            return allProviderKeys.Select(so => (so.DisplayName, so)).ToList();
        }
        
        /// <summary>
        /// Gets a list of all event keys defined in the EventKeyKeys script if it exists in the project.
        /// </summary>
        /// <returns>Returns either the EventKeyKeys.DropdownValues or a list with the "None" value if the EventKeyKeys does not exist.</returns>
        public static List<(string, KeyDropdownValue)> GetAllEventKeyValues()
        {
            if (_eventKeyType != null)
            {
                return (List<(string, KeyDropdownValue)>)_eventKeyType.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            }
            
            var assembly = Assembly.Load("VaporKeyDefinitions");
            _eventKeyType = assembly.GetType("VaporKeyDefinitions.EventKeys");
            if (_eventKeyType == null)
            {
                return new List<(string, KeyDropdownValue)>() { ("None", new KeyDropdownValue()) };
            }

            return (List<(string, KeyDropdownValue)>)_eventKeyType.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        }

        /// <summary>
        /// Gets a list of all <see cref="ProviderKeySo"/> in the project. Will return null if not in the UNITY_EDITOR
        /// </summary>
        /// <returns></returns>
        public static List<(string, ProviderKeySo)> GetAllProviderKeys()
        {
            var allProviderKeys = RuntimeAssetDatabaseUtility.FindAssetsByType<ProviderKeySo>();
            return allProviderKeys.Select(so => (so.DisplayName, so)).ToList();
        }

        /// <summary>
        /// Gets a list of all provider keys defined in the ProviderKeyKeys script if it exists in the project.
        /// </summary>
        /// <returns>Returns either the ProviderKeyKeys.DropdownValues or a list with the "None" value if the ProviderKeyKeys does not exist.</returns>
        public static List<(string, KeyDropdownValue)> GetAllProviderKeyValues()
        {
            if (_providerKeyType != null)
            {
                return (List<(string, KeyDropdownValue)>)_providerKeyType.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            }
            
            var assembly = Assembly.Load("VaporKeyDefinitions");
            _providerKeyType = assembly.GetType("VaporKeyDefinitions.ProviderKeys");
            if (_providerKeyType == null)
            {
                return new List<(string, KeyDropdownValue)>() { ("None", new KeyDropdownValue()) };
            }

            return (List<(string, KeyDropdownValue)>)_providerKeyType.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        }        
    }
}
