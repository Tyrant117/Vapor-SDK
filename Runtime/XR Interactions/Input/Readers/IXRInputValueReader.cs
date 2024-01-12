using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    /// <summary>
    /// Base interface for all input value readers.
    /// </summary>
    /// <remarks>
    /// This empty interface is needed to allow the <c>RequireInterface</c> attribute to be used.
    /// Generic attributes aren't supported until C# 11, so we can't use a typed interface with the <c>RequireInterface</c> attribute yet.
    /// </remarks>
    public interface IXRInputValueReader
    {
    }

    /// <summary>
    /// Interface which allows for callers to read the current value from an input source.
    /// </summary>
    /// <typeparam name="TValue">Type of the value to read, such as <see cref="Vector2"/> or <see langword="float"/>.</typeparam>
    public interface IXRInputValueReader<TValue> : IXRInputValueReader where TValue : struct
    {
        /// <summary>
        /// Read the current value from the input source.
        /// </summary>
        /// <returns>Returns the current value from the input source. May return <c>default(TValue)</c> if unused or no source is set.</returns>
        TValue ReadValue();

        /// <summary>
        /// Try to read the current value from the input source.
        /// </summary>
        /// <param name="value">When this method returns, contains the current value from the input source. May return <c>default(TValue)</c> if unused or no source is set.</param>
        /// <returns>Returns <see langword="true"/> if the current value was able to be read (and for actions, also if in progress).</returns>
        /// <remarks>
        /// You can use the return value of this method instead of only using <see cref="ReadValue"/> in order to avoid doing
        /// any work when the input action is not in progress, such as when the control is not actuated.
        /// This can be useful for performance reasons.
        /// <br />
        /// If an input processor on an input action returns a different value from the default <typeparamref name="TValue"/>
        /// when the input action is not in progress, the <see langword="out"/> <paramref name="value"/> returned by
        /// this method may not be <c>default(TValue)</c> as is typically the case for <c>Try</c>- methods. If you need
        /// to support processors that return a different value from the default when the control is not actuated,
        /// you should use <see cref="ReadValue()"/> instead of using the return value of this method to skip input handling.
        /// </remarks>
        bool TryReadValue(out TValue value);
    }
}
