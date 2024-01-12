using Unity.XR.CoreUtils.Bindings.Variables;

namespace VaporXR
{
    /// <summary>
    /// This provider interface allows a source component to populate <see cref="VaporXR.PokeStateData"/> upon request to
    /// a component that is bound to the <see cref="PokeStateData"/> bindable variable that provides
    /// state data about a poke interaction. Typically this is needed by an affordance listener for poke.
    /// </summary>
    public interface IPokeStateDataProvider
    {
        /// <summary>
        /// <see cref="IReadOnlyBindableVariable{T}"/> that updates whenever the poke logic state is evaluated.
        /// </summary>
        /// <seealso cref="VaporXR.PokeStateData"/>
        IReadOnlyBindableVariable<PokeStateData> PokeStateData { get; }
    }
}
