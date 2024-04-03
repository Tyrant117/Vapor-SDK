using System;
using System.Collections.Generic;
using System.Reflection;

namespace VaporKeys
{
    public static class KeyUtility
    {
        private static readonly Dictionary<string, Type> _cachedKeyTypes = new();

        public static List<(string, KeyDropdownValue)> GetAllKeysOfNamedType(string nameOfType, string assemblyName = "VaporKeyDefinitions")
        {
            if (_cachedKeyTypes.TryGetValue(nameOfType, out var type))
            {
                return (List<(string, KeyDropdownValue)>)type.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            }

            var assembly = Assembly.Load($"{assemblyName}");
            type = assembly.GetType($"{assemblyName}.{nameOfType}");
            if (type == null)
            {
                return new List<(string, KeyDropdownValue)>() { ("None", new KeyDropdownValue()) };
            }
            _cachedKeyTypes.Add(nameOfType, type);
            return (List<(string, KeyDropdownValue)>)type.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        }

        public static List<(string, KeyDropdownValue)> GetAllKeysOfInternalAndNamedType(string nameOfType, Type internalType, string assemblyName = "VaporKeyDefinitions")
        {
            var result = new List<(string, KeyDropdownValue)>();
            result.AddRange((List<(string, KeyDropdownValue)>)internalType.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null));

            if (_cachedKeyTypes.TryGetValue(nameOfType, out var type))
            {
                result.AddRange((List<(string, KeyDropdownValue)>)type.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null));
                return result;
            }

            var assembly = Assembly.Load($"{assemblyName}");
            type = assembly.GetType($"{assemblyName}.{nameOfType}");
            if (type == null)
            {
                return result;
            }
            _cachedKeyTypes.Add(nameOfType, type);
            result.AddRange((List<(string, KeyDropdownValue)>)type.GetField("DropdownValues", BindingFlags.Public | BindingFlags.Static)?.GetValue(null));
            return result;
        }

        public static List<int> GetAllValuesOfNamedType(string nameOfType, string assemblyName = "VaporKeyDefinitions")
        {
            if (_cachedKeyTypes.TryGetValue(nameOfType, out var type))
            {
                return (List<int>)type.GetField("Values", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            }

            var assembly = Assembly.Load($"{assemblyName}");
            type = assembly.GetType($"{assemblyName}.{nameOfType}");
            if (type == null)
            {
                return new List<int>() { 0 };
            }
            _cachedKeyTypes.Add(nameOfType, type);
            return (List<int>)type.GetField("Values", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        }

        public static List<int> GetAllValuesOfInternalAndNamedType(string nameOfType, string assemblyName = "VaporKeyDefinitions", params Type[] internalTypes)
        {
            var result = new List<int>();
            foreach (var internalType in internalTypes)
            {
                result.AddRange((List<int>)internalType.GetField("Values", BindingFlags.Public | BindingFlags.Static)?.GetValue(null));
            }

            if (_cachedKeyTypes.TryGetValue(nameOfType, out var type))
            {
                result.AddRange((List<int>)type.GetField("Values", BindingFlags.Public | BindingFlags.Static)?.GetValue(null));
                return result;
            }

            var assembly = Assembly.Load($"{assemblyName}");
            type = assembly.GetType($"{assemblyName}.{nameOfType}");
            if (type == null)
            {
                return result;
            }
            _cachedKeyTypes.Add(nameOfType, type);
            result.AddRange((List<int>)type.GetField("Values", BindingFlags.Public | BindingFlags.Static)?.GetValue(null));
            return result;
        }
    }
}
