using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     Provides atomic operations on a managed reference to type <typeparamref name="T" />.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [BindingType(typeof(Interlocked))]
    internal struct AtomicRef<T> where T : class?
    {
        /// <summary>
        ///     Gets the value to the underlying object.
        /// </summary>
        private object? _value;

        /// <summary>
        ///     Initializes a new instance of this class.
        /// </summary>
        /// <param name="value">The initial value of the reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AtomicRef(T? value) => _value = value;

        /// <summary>
        ///     Returns a value, loaded as an atomic operation.
        /// </summary>
        /// <param name="order">The memory ordering applied to the load.</param>
        /// <returns>The loaded value.</returns>
        /// <exception cref="System.NotSupportedException">Ordering is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? Load(Ordering order) => (T?)AtomicHelpers.Load(ref _value, order);

        /// <summary>
        ///     Sets a value to a specified value, as an atomic operation.
        /// </summary>
        /// <param name="value">The value to store.</param>
        /// <param name="order">The memory ordering applied to the store.</param>
        /// <exception cref="System.NotSupportedException">Ordering is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Store(T? value, Ordering order) => AtomicHelpers.Store(ref _value, value, order);

        /// <summary>
        ///     Compares two references for equality and, if they are equal, replaces the first reference.
        /// </summary>
        /// <param name="value">The reference to store if the comparison succeeds.</param>
        /// <param name="comparand">The reference to compare with the current reference.</param>
        /// <returns>The original reference.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? CompareExchange(T? value, T? comparand) => (T?)InterlockedHelpers.CompareExchange(ref _value, value, comparand);
    }
}