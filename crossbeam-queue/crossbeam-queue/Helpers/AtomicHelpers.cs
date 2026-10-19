using System.Runtime.CompilerServices;
using System.Threading;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     Provides atomic operations for variables that are shared by multiple threads.
    /// </summary>
    internal static class AtomicHelpers
    {
        /// <summary>
        ///     Loads a managed reference using the specified memory ordering.
        /// </summary>
        /// <param name="location">The reference to read.</param>
        /// <param name="order">The memory ordering applied to the load.</param>
        /// <returns>The loaded reference.</returns>
        /// <exception cref="System.NotSupportedException">
        ///     Thrown when <paramref name="order" /> is <see cref="Ordering.Release" /> or an unsupported value.
        /// </exception>
        public static object? Load(ref object? location, Ordering order)
        {
            switch (order)
            {
                case Ordering.Relaxed:
                case Ordering.Acquire:
                case Ordering.AcqRel:
                    return Volatile.Read(ref location);

                case Ordering.SeqCst:
                    return InterlockedHelpers.Read(ref location);

                case Ordering.Release:
                default:
                    ThrowHelpers.ThrowNotSupportedException();
                    return default;
            }
        }

        /// <summary>
        ///     Stores a managed reference using the specified memory ordering.
        /// </summary>
        /// <param name="location">The reference to update.</param>
        /// <param name="value">The new reference.</param>
        /// <param name="order">The memory ordering applied to the store.</param>
        /// <exception cref="System.NotSupportedException">
        ///     Thrown when <paramref name="order" /> is <see cref="Ordering.Acquire" /> or an unsupported value.
        /// </exception>
        public static void Store(ref object? location, object? value, Ordering order)
        {
            switch (order)
            {
                case Ordering.Relaxed:
                case Ordering.Release:
                case Ordering.AcqRel:
                    Volatile.Write(ref location, value);
                    return;

                case Ordering.SeqCst:
                    InterlockedHelpers.Exchange(ref location, value);
                    return;

                case Ordering.Acquire:
                default:
                    ThrowHelpers.ThrowNotSupportedException();
                    return;
            }
        }

        /// <summary>
        ///     Loads a native-sized unsigned integer using the specified memory ordering.
        /// </summary>
        /// <param name="location">The integer to load.</param>
        /// <param name="order">The memory ordering applied to the load.</param>
        /// <returns>The loaded value.</returns>
        /// <exception cref="System.NotSupportedException">
        ///     Thrown when <paramref name="order" /> is <see cref="Ordering.Release" /> or an unsupported value.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint Load(ref nuint location, Ordering order)
        {
            switch (order)
            {
                case Ordering.Relaxed:
                case Ordering.Acquire:
                case Ordering.AcqRel:
                    return Volatile.Read(ref location);

                case Ordering.SeqCst:
                    return InterlockedHelpers.Read(ref location);

                case Ordering.Release:
                default:
                    ThrowHelpers.ThrowNotSupportedException();
                    return default;
            }
        }

        /// <summary>
        ///     Stores a native-sized unsigned integer using the specified memory ordering.
        /// </summary>
        /// <param name="location">The integer to update.</param>
        /// <param name="value">The value to store.</param>
        /// <param name="order">The memory ordering applied to the store.</param>
        /// <exception cref="System.NotSupportedException">
        ///     Thrown when <paramref name="order" /> is <see cref="Ordering.Acquire" /> or an unsupported value.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Store(ref nuint location, nuint value, Ordering order)
        {
            switch (order)
            {
                case Ordering.Relaxed:
                case Ordering.Release:
                case Ordering.AcqRel:
                    Volatile.Write(ref location, value);
                    return;

                case Ordering.SeqCst:
                    InterlockedHelpers.Exchange(ref location, value);
                    return;

                case Ordering.Acquire:
                default:
                    ThrowHelpers.ThrowNotSupportedException();
                    return;
            }
        }
    }
}