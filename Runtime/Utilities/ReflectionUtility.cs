using System;
using System.Collections.Generic;
using System.Reflection;

namespace Vapor.Utilities
{
    public static class ReflectionUtility
    {
        private static Assembly[] s_Assemblies;
        private static List<Type[]> s_TypesPerAssembly;
        private static List<Dictionary<string, Type>> s_AssemblyTypeMaps;

        private static Assembly[] GetCachedAssemblies() { return s_Assemblies ??= AppDomain.CurrentDomain.GetAssemblies(); }

        private static List<Type[]> GetCachedTypesPerAssembly()
        {
            if (s_TypesPerAssembly != null) return s_TypesPerAssembly;
            
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            s_TypesPerAssembly = new List<Type[]>(assemblies.Length);
            foreach (var assembly in assemblies)
            {
                try
                {
                    s_TypesPerAssembly.Add(assembly.GetTypes());
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip any assemblies that don't load properly -- suppress errors
                }
            }

            return s_TypesPerAssembly;
        }

        private static List<Dictionary<string, Type>> GetCachedAssemblyTypeMaps()
        {
            if (s_AssemblyTypeMaps == null)
            {
                var typesPerAssembly = GetCachedTypesPerAssembly();
                s_AssemblyTypeMaps = new List<Dictionary<string, Type>>(typesPerAssembly.Count);
                foreach (var types in typesPerAssembly)
                {
                    try
                    {
                        var typeMap = new Dictionary<string, Type>();
                        foreach (var type in types)
                        {
                            typeMap[type.FullName] = type;
                        }

                        s_AssemblyTypeMaps.Add(typeMap);
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // Skip any assemblies that don't load properly -- suppress errors
                    }
                }
            }

            return s_AssemblyTypeMaps;
        }
        
        /// <summary>
        /// Caches type information from all currently loaded assemblies.
        /// </summary>
        public static void PreWarmTypeCache() { GetCachedAssemblyTypeMaps(); }
        
        /// <summary>
        /// Executes a delegate function for every assembly that can be loaded.
        /// </summary>
        /// <remarks>
        /// `ForEachAssembly` iterates through all assemblies and executes a method on each one.
        /// If an <see cref="ReflectionTypeLoadException"/> is thrown, it is caught and ignored.
        /// </remarks>
        /// <param name="callback">The callback method to execute for each assembly.</param>
        public static void ForEachAssembly(Action<Assembly> callback)
        {
            var assemblies = GetCachedAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    callback(assembly);
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip any assemblies that don't load properly -- suppress errors
                }
            }
        }
        
        /// <summary>
        /// Executes a delegate function for each type in every assembly.
        /// </summary>
        /// <param name="callback">The callback to execute.</param>
        public static void ForEachType(Action<Type> callback)
        {
            var typesPerAssembly = GetCachedTypesPerAssembly();
            foreach (var types in typesPerAssembly)
            {
                foreach (var type in types)
                {
                    callback(type);
                }
            }
        }

        /// <summary>
        /// Search all assemblies for a type that matches a given predicate delegate.
        /// </summary>
        /// <param name="predicate">The predicate function. Must return <see langword="true"/> for the type that matches the search.</param>
        /// <returns>The first type for which <paramref name="predicate"/> returns <see langword="true"/>, or `null` if no matching type exists.</returns>
        public static Type FindType(Func<Type, bool> predicate)
        {
            var typesPerAssembly = GetCachedTypesPerAssembly();
            foreach (var types in typesPerAssembly)
            {
                foreach (var type in types)
                {
                    if (predicate(type))
                        return type;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Find a type in any assembly by its full name.
        /// </summary>
        /// <param name="fullName">The name of the type as returned by <see cref="Type.FullName"/>.</param>
        /// <returns>The type found, or null if no matching type exists.</returns>
        public static Type FindTypeByFullName(string fullName)
        {
            var typesPerAssembly = GetCachedAssemblyTypeMaps();
            foreach (var assemblyTypes in typesPerAssembly)
            {
                if (assemblyTypes.TryGetValue(fullName, out var type))
                    return type;
            }

            return null;
        }
        
        /// <summary>
        /// Search all assemblies for a set of types that matches any one of a set of predicates.
        /// </summary>
        /// <remarks>
        /// This function tests each predicate against each type in each assembly. If the predicate returns
        /// <see langword="true"/> for a type, then that <see cref="Type"/> object is assigned to the corresponding index of
        /// the <paramref name="resultList"/>. If a predicate returns <see langword="true"/> for more than one type, then the
        /// last matching result is used. If no type matches the predicate, then that index of <paramref name="resultList"/>
        /// is left unchanged.
        /// </remarks>
        /// <param name="predicates">The predicate functions. A predicate function must return <see langword="true"/>
        /// for the type that matches the search and should only match one type.</param>
        /// <param name="resultList">The list to which found types will be added. The list must have
        /// the same number of elements as the <paramref name="predicates"/> list.</param>
        public static void FindTypesBatch(List<Func<Type, bool>> predicates, List<Type> resultList)
        {
            var typesPerAssembly = GetCachedTypesPerAssembly();
            for (var i = 0; i < predicates.Count; i++)
            {
                var predicate = predicates[i];
                foreach (var assemblyTypes in typesPerAssembly)
                {
                    foreach (var type in assemblyTypes)
                    {
                        if (predicate(type))
                            resultList[i] = type;
                    }
                }
            }
        }

        /// <summary>
        /// Searches all assemblies for a set of types by their <see cref="Type.FullName"/> strings.
        /// </summary>
        /// <remarks>
        /// If a type name in <paramref name="typeNames"/> is not found, then the corresponding index of <paramref name="resultList"/>
        /// is set to `null`.
        /// </remarks>
        /// <param name="typeNames">A list containing the <see cref="Type.FullName"/> strings of the types to find.</param>
        /// <param name="resultList">An empty list to which any matching <see cref="Type"/> objects are added. A
        /// result in <paramref name="resultList"/> has the same index as corresponding name in <paramref name="typeNames"/>.</param>
        public static void FindTypesByFullNameBatch(List<string> typeNames, List<Type> resultList)
        {
            var assemblyTypeMap = GetCachedAssemblyTypeMaps();
            foreach (var typeName in typeNames)
            {
                var found = false;
                foreach (var typeMap in assemblyTypeMap)
                {
                    if (typeMap.TryGetValue(typeName, out var type))
                    {
                        resultList.Add(type);
                        found = true;
                        break;
                    }
                }

                // If a type can't be found, add a null entry to the list to ensure indexes match
                if (!found)
                    resultList.Add(null);
            }
        }

        /// <summary>
        /// Searches for a type by assembly simple name and its <see cref="Type.FullName"/>.
        /// an assembly with the given simple name and returns the type with the given full name in that assembly
        /// </summary>
        /// <param name="assemblyName">Simple name of the assembly (<see cref="Assembly.GetName()"/>).</param>
        /// <param name="typeName">Full name of the type to find (<see cref="Type.FullName"/>).</param>
        /// <returns>The type if found, otherwise null</returns>
        public static Type FindTypeInAssemblyByFullName(string assemblyName, string typeName)
        {
            var assemblies = GetCachedAssemblies();
            var assemblyTypeMaps = GetCachedAssemblyTypeMaps();
            for (var i = 0; i < assemblies.Length; i++)
            {
                if (assemblies[i].GetName().Name != assemblyName)
                    continue;

                return assemblyTypeMaps[i].TryGetValue(typeName, out var type) ? type : null;
            }

            return null;
        }
    }
}