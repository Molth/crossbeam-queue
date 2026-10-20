using System;
using System.Runtime.CompilerServices;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     Provides unchecked array access used by queue internals.
    /// </summary>
    internal static class ArrayExtensions
    {
        /// <summary>
        ///     Returns a reference to an array element without checking the index.
        /// </summary>
        /// <typeparam name="T">The array element type.</typeparam>
        /// <param name="array">The array whose element is accessed.</param>
        /// <param name="index">The zero-based element index.</param>
        /// <returns>A reference to the element at <paramref name="index" />.</returns>
        /// <exception cref="NullReferenceException"><paramref name="array" /> is <see langword="null" />.</exception>
        /// <remarks>
        ///     The caller must pass a non-null array and ensure that <paramref name="index" /> is less than its length.
        ///     This method also bypasses array covariance checks, so writing through the returned reference is safe only
        ///     when the array's runtime element type can store values of <typeparamref name="T" />.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetUnchecked<T>(this T[] array, nuint index) => ref Unsafe.Add(ref MemoryMarshalHelpers.GetArrayDataReference(array), (nint)index);
    }
}