using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     Provides atomic operations on a native-sized unsigned integer (<see cref="System.UIntPtr" />).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [BindingType(typeof(Interlocked))]
    internal struct AtomicUsize
    {
        /// <summary>
        ///     The native-sized unsigned integer accessed by the atomic operations.
        /// </summary>
        private nuint _value;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AtomicUsize" /> structure.
        /// </summary>
        /// <param name="value">The initial value of the integer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AtomicUsize(nuint value) => _value = value;

        /// <summary>
        ///     Loads the value using the specified memory ordering.
        /// </summary>
        /// <param name="order">The memory ordering applied to the load.</param>
        /// <returns>The loaded value.</returns>
        /// <exception cref="System.NotSupportedException">
        ///     Thrown when <paramref name="order" /> is <see cref="Ordering.Release" /> or an unsupported value.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nuint Load(Ordering order) => AtomicHelpers.Load(ref _value, order);

        /// <summary>
        ///     Stores the value using the specified memory ordering.
        /// </summary>
        /// <param name="value">The value to store.</param>
        /// <param name="order">The memory ordering applied to the store.</param>
        /// <exception cref="System.NotSupportedException">
        ///     Thrown when <paramref name="order" /> is <see cref="Ordering.Acquire" /> or an unsupported value.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Store(nuint value, Ordering order) => AtomicHelpers.Store(ref _value, value, order);

        /// <summary>
        ///     Bitwise "ors" two native-sized unsigned integers and replaces the first integer with the result,
        ///     as an atomic operation.
        /// </summary>
        /// <param name="value">The value to OR with the current integer.</param>
        /// <returns>The original value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nuint Or(nuint value) => InterlockedHelpers.Or(ref _value, value);

        /// <summary>
        ///     Compares two values for equality and, if they are equal, replaces the first value.
        /// </summary>
        /// <param name="value">The value to store if the comparison succeeds.</param>
        /// <param name="comparand">The value to compare with the current value.</param>
        /// <returns>The original value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nuint CompareExchange(nuint value, nuint comparand) => InterlockedHelpers.CompareExchange(ref _value, value, comparand);
    }
}