using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     Provides helper methods for validating arguments and
    ///     throwing standard exceptions with consistent messaging.
    /// </summary>
    internal static class ThrowHelpers
    {
#if !NET5_0_OR_GREATER
        /// <summary>
        ///     Throws an <see cref="NullReferenceException" /> if <paramref name="argument" /> is null.
        /// </summary>
        /// <param name="argument">The reference type argument to validate as non-null.</param>
        /// <typeparam name="T">A reference type; value types are not accepted.</typeparam>
        /// <exception cref="NullReferenceException">
        ///     Thrown when <paramref name="argument" /> is null.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNull<T>(T? argument) where T : class
        {
            if (argument == null)
                throw new NullReferenceException(SR.NullReference_MustBeNotNull);
        }
#endif

        /// <summary>
        ///     Throws an <see cref="ArgumentOutOfRangeException" /> if a value is negative or zero.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="paramName">The enum value selecting the exception's parameter name.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNegativeOrZero(int value, ExceptionArgument paramName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(GetArgumentName(paramName), value, SR.ArgumentOutOfRange_MustBePositiveNonZero);
        }

        /// <summary>
        ///     Throws a <see cref="NotSupportedException" />.
        /// </summary>
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNotSupportedException() => throw new NotSupportedException();

        /// <summary>
        ///     Maps a recognized <see cref="ExceptionArgument" /> value to its C# parameter
        ///     name. An unrecognized value maps to <see langword="null" />.
        /// </summary>
        /// <param name="argument">The <see cref="ExceptionArgument" /> value to convert.</param>
        /// <returns>
        ///     The mapped C# parameter name, or <see langword="null" />
        ///     when the enum value is unrecognized.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string? GetArgumentName(ExceptionArgument argument) => argument switch
        {
            ExceptionArgument.capacity => "capacity",
            _ => null
        };
    }
}