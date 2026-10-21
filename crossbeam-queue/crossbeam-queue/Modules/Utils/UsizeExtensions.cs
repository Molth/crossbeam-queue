using System.Runtime.CompilerServices;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     Provides extension methods for wrapping arithmetic on native-sized unsigned integers.
    /// </summary>
    /// <remarks>
    ///     Overflow and underflow wrap modulo 2^N, where N is the width of <see cref="System.UIntPtr" />
    ///     on the current process. These operations do not throw for arithmetic overflow.
    /// </remarks>
    internal static class UsizeExtensions
    {
        /// <summary>
        ///     Adds <paramref name="other" /> to <paramref name="value" /> using wrapping arithmetic.
        /// </summary>
        /// <param name="value">The value to add to.</param>
        /// <param name="other">The value to add.</param>
        /// <returns>The sum, wrapped to the width of <see cref="System.UIntPtr" /> if it overflows.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint WrappingAdd(this nuint value, nuint other) => unchecked(value + other);

        /// <summary>
        ///     Subtracts <paramref name="other" /> from <paramref name="value" /> using wrapping arithmetic.
        /// </summary>
        /// <param name="value">The value to subtract from.</param>
        /// <param name="other">The value to subtract.</param>
        /// <returns>The difference, wrapped to the width of <see cref="System.UIntPtr" /> if it underflows.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint WrappingSub(this nuint value, nuint other) => unchecked(value - other);
    }
}