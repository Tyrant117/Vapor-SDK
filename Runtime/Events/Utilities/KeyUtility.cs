using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Vapor;
using VaporKeys;

namespace VaporEvents
{
    public static class KeyUtility
    {
        private static Type _keyType;
        
        public static List<(string, EventKeySo)> GetAllEventKeys()
        {
            var allProviderKeys = RuntimeAssetDatabaseUtility.FindAssetsByType<EventKeySo>();
            return allProviderKeys.Select(so => (so.DisplayName, so)).ToList();
        }
        
        public static List<(string, KeyDropdownValue)> GetAllEventKeyValues()
        {
            if (_keyType != null)
            {
                var result = _keyType.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                return (List<(string, KeyDropdownValue)>)result;
            }
            
            var assembly = Assembly.Load("VaporKeyDefinitions");
            _keyType = assembly.GetType("VaporKeyDefinitions.EventKeyKeys");
            if (_keyType != null)
            {
                var result = _keyType.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                return (List<(string, KeyDropdownValue)>)result;
            }
            else
            {
                return new List<(string, KeyDropdownValue)>() { ("None", new KeyDropdownValue()) };
            }
        }

        public static List<(string, ProviderKeySo)> GetAllProviderKeys()
        {
            var allProviderKeys = RuntimeAssetDatabaseUtility.FindAssetsByType<ProviderKeySo>();
            return allProviderKeys.Select(so => (so.DisplayName, so)).ToList();
        }

        public static List<(string, KeyDropdownValue)> GetAllProviderKeyValues()
        {
            if (_keyType != null)
            {
                var result = _keyType.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                return (List<(string, KeyDropdownValue)>)result;
            }
            
            var assembly = Assembly.Load("VaporKeyDefinitions");
            _keyType = assembly.GetType("VaporKeyDefinitions.ProviderKeyKeys");
            if (_keyType != null)
            {
                var result = _keyType.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                return (List<(string, KeyDropdownValue)>)result;
            }
            else
            {
                return new List<(string, KeyDropdownValue)>() { ("None", new KeyDropdownValue()) };
            }
        }
    }
}
