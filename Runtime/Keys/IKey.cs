
namespace VaporKeys
{
    /// <summary>
    /// Base interface that gives unique integer key functionality.
    /// </summary>
    public interface IKey
    {
        /// <summary>
        /// The unique key.
        /// </summary>
        int Key { get; }
        /// <summary>
        /// Method that refreshes the value of the key before a rebuild.
        /// </summary>
        void ForceRefreshKey();
        /// <summary>
        /// The display name of the key shown in dropdowns.
        /// </summary>
        string DisplayName { get; }
        /// <summary>
        /// If true, the key will be excluded when rebuilding.
        /// </summary>
        bool IsDeprecated { get; }
    }
}
