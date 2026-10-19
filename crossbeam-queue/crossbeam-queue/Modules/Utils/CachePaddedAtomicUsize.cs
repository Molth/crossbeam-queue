using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static crossbeam.PaddingHelpers;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     Stores a native-sized unsigned integer in a cache-line-sized structure.
    /// </summary>
    /// <remarks>
    ///     Cache padding is intended to keep frequently accessed atomic values from sharing cache
    ///     lines. This structure reserves <see cref="CACHE_LINE_SIZE" /> bytes but does not guarantee
    ///     cache-line address alignment.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Size = CACHE_LINE_SIZE)]
    [BindingType(typeof(AtomicUsize))]
    internal struct CachePaddedAtomicUsize
    {
        /// <summary>
        ///     The atomic value stored in this structure.
        /// </summary>
        private AtomicUsize _value;

        /// <summary>
        ///     Loads the value using the specified memory ordering.
        /// </summary>
        /// <param name="order">The memory ordering applied to the load.</param>
        /// <returns>The loaded value.</returns>
        /// <exception cref="System.NotSupportedException">
        ///     Thrown when <paramref name="order" /> is <see cref="Ordering.Release" /> or an unsupported value.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nuint Load(Ordering order) => _value.Load(order);

        /// <summary>
        ///     Stores the value using the specified memory ordering.
        /// </summary>
        /// <param name="value">The value to store.</param>
        /// <param name="order">The memory ordering applied to the store.</param>
        /// <exception cref="System.NotSupportedException">
        ///     Thrown when <paramref name="order" /> is <see cref="Ordering.Acquire" /> or an unsupported value.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Store(nuint value, Ordering order) => _value.Store(value, order);

        /// <summary>
        ///     Compares two values for equality and, if they are equal, replaces the first value.
        /// </summary>
        /// <param name="value">The value to store if the comparison succeeds.</param>
        /// <param name="comparand">The value to compare with the current value.</param>
        /// <returns>The original value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nuint CompareExchange(nuint value, nuint comparand) => _value.CompareExchange(value, comparand);
    }
}