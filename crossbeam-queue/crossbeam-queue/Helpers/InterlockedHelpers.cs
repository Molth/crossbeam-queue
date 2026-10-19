using System;
using System.Runtime.CompilerServices;
using System.Threading;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     Provides atomic operations for variables that are shared by multiple threads.
    /// </summary>
    internal static class InterlockedHelpers
    {
        /// <summary>
        ///     Returns a managed reference, loaded as an atomic operation.
        /// </summary>
        /// <param name="location">The reference to read.</param>
        /// <returns>The loaded reference.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object? Read(ref object? location) => CompareExchange(ref location, null, null);

        /// <summary>
        ///     Sets a managed reference to a specified value and returns the original value, as an atomic operation.
        /// </summary>
        /// <param name="location">The reference to update.</param>
        /// <param name="value">The new reference.</param>
        /// <returns>The original reference in <paramref name="location" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object? Exchange(ref object? location, object? value) => Interlocked.Exchange(ref location, value);

        /// <summary>
        ///     Compares two managed references for equality and, if they are equal, replaces the first one,
        ///     as an atomic operation.
        /// </summary>
        /// <param name="location">The reference to update.</param>
        /// <param name="value">The new reference.</param>
        /// <param name="comparand">The reference to compare.</param>
        /// <returns>The original reference in <paramref name="location" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object? CompareExchange(ref object? location, object? value, object? comparand) => Interlocked.CompareExchange(ref location, value, comparand);

        /// <summary>
        ///     Returns a native-sized unsigned value, loaded as an atomic operation.
        /// </summary>
        /// <param name="location">The value to read.</param>
        /// <returns>The loaded value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint Read(ref nuint location) => CompareExchange(ref location, default, default);

        /// <summary>
        ///     Sets a native-sized unsigned integer to a specified value and returns the original value, as an atomic operation.
        /// </summary>
        /// <param name="location">The value to update.</param>
        /// <param name="value">The new value.</param>
        /// <returns>The original value of <paramref name="location" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint Exchange(ref nuint location, nuint value)
        {
#if NET7_0_OR_GREATER
            return Interlocked.Exchange(ref location, value);
#else
            return Environment.Is64BitProcess ? (nuint)Interlocked.Exchange(ref Unsafe.As<nuint, long>(ref location), (long)value) : (nuint)Interlocked.Exchange(ref Unsafe.As<nuint, int>(ref location), (int)value);
#endif
        }

        /// <summary>
        ///     Bitwise "ors" two native-sized unsigned integers and replaces the first integer with the result,
        ///     as an atomic operation.
        /// </summary>
        /// <param name="location">The integer to update.</param>
        /// <param name="value">The value to OR with the current integer.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint Or(ref nuint location, nuint value)
        {
#if NET5_0_OR_GREATER
            return Environment.Is64BitProcess ? (nuint)Interlocked.Or(ref Unsafe.As<nuint, long>(ref location), (long)value) : (nuint)Interlocked.Or(ref Unsafe.As<nuint, int>(ref location), (int)value);
#else
            var current = Read(ref location);
            while (true)
            {
                var newValue = current | value;
                var oldValue = CompareExchange(ref location, newValue, current);
                if (oldValue == current)
                    return oldValue;

                current = oldValue;
            }
#endif
        }

        /// <summary>
        ///     Compares two native-sized unsigned integers for equality and, if they are equal, replaces the first one, as an
        ///     atomic operation.
        /// </summary>
        /// <param name="location">The value to update.</param>
        /// <param name="value">The new value.</param>
        /// <param name="comparand">The value to compare.</param>
        /// <returns>The original value of <paramref name="location" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint CompareExchange(ref nuint location, nuint value, nuint comparand)
        {
#if NET7_0_OR_GREATER
            return Interlocked.CompareExchange(ref location, value, comparand);
#else
            return Environment.Is64BitProcess ? (nuint)Interlocked.CompareExchange(ref Unsafe.As<nuint, long>(ref location), (long)value, (long)comparand) : (nuint)Interlocked.CompareExchange(ref Unsafe.As<nuint, int>(ref location), (int)value, (int)comparand);
#endif
        }
    }
}