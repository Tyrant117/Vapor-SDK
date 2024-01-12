using UnityEngine;

namespace Vapor.Utilities
{
    /// <summary>
    /// Utility methods for locating component instances.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    public static class ComponentLocatorUtility<T> where T : Component
    {
        private static T _componentCache;
        /// <summary>
        /// Cached reference to a found component of type <see cref="T"/>.
        /// </summary>
        public static T ComponentCache => _componentCache;

        /// <summary>
        /// Find or create a new GameObject with component <typeparamref name="T"/>.
        /// </summary>
        /// <returns>Returns the found or created component.</returns>
        /// <remarks>
        /// Does not include inactive GameObjects when finding the component, but if a component was previously created
        /// as a direct result of this class, it will return that component even if the GameObject is now inactive.
        /// </remarks>
        public static T FindOrCreateComponent()
        {
            if (_componentCache != null) return _componentCache;
            
            _componentCache = Find();
            if (_componentCache == null)
            {
                _componentCache = new GameObject(typeof(T).Name, typeof(T)).GetComponent<T>();
            }
            return _componentCache;
        }

        /// <summary>
        /// Find a component <typeparamref name="T"/>.
        /// </summary>
        /// <returns>Returns the found component, or <see langword="null"/> if one could not be found.</returns>
        /// <remarks>
        /// Does not include inactive GameObjects when finding the component, but if a component was previously created
        /// as a direct result of this class, it will return that component even if the GameObject is now inactive.
        /// </remarks>
        public static T FindComponent()
        {
            TryFindComponent(out var component);
            return component;
        }

        /// <summary>
        /// Find a component <typeparamref name="T"/>.
        /// </summary>
        /// <param name="component">When this method returns, contains the found component, or <see langword="null"/> if one could not be found.</param>
        /// <returns>Returns <see langword="true"/> if the component exists, otherwise returns <see langword="false"/>.</returns>
        /// <remarks>
        /// Does not include inactive GameObjects when finding the component, but if a component was previously created
        /// as a direct result of this class, it will return that component even if the GameObject is now inactive.
        /// </remarks>
        public static bool TryFindComponent(out T component)
        {
            if (_componentCache != null)
            {
                component = _componentCache;
                return true;
            }

            _componentCache = Find();
            component = _componentCache;
            return component != null;
        }

        private static T Find() => Object.FindFirstObjectByType<T>();
    }
}