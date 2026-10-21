using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static crossbeam.PaddingHelpers;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     Contains storage types used by the bounded queue implementation.
    /// </summary>
    internal static class ArrayQueue
    {
        /// <summary>
        ///     A slot in a queue.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
        [StructLayout(LayoutKind.Sequential)]
        [IsReferenceOrContainsReferences]
        public struct Slot<T>
        {
            /// <summary>
            ///     The current stamp.
            ///     <br />
            ///     If the stamp equals the tail, this node will be next written to. If it equals head + 1,
            ///     this node will be next read from.
            /// </summary>
            public AtomicUsize Stamp;

            /// <summary>
            ///     The value in this slot.
            /// </summary>
            public T? Value;
        }

        /// <summary>
        ///     Stores the buffer reference and lap stamp in a cache-line-sized value.
        /// </summary>
        /// <remarks>
        ///     This non-generic backing type must use <see cref="LayoutKind.Explicit" /> because its payload contains a
        ///     managed reference. With <see cref="LayoutKind.Sequential" />, the compiled size does not equal
        ///     <see cref="CACHE_LINE_SIZE" />. Keep the payload as the sole field at offset zero;
        ///     overlapping another field could invalidate the runtime's managed-reference layout. Do not replace this
        ///     with a generic sequential struct and infer its runtime size from the declared <c>Size</c>. If the layout
        ///     changes, verify the actual size of each relevant closed type with <c>Unsafe.SizeOf&lt;T&gt;()</c> on every
        ///     supported runtime and architecture.
        /// </remarks>
        [StructLayout(LayoutKind.Explicit, Size = CACHE_LINE_SIZE)]
        [IsReferenceOrContainsReferences]
        [BindingType(typeof(ArrayWithOneLap))]
        private readonly struct CachePaddedArrayWithOneLap
        {
            /// <summary>
            ///     The buffer reference and lap stamp, starting at offset zero.
            /// </summary>
            [FieldOffset(0)] public readonly ArrayWithOneLap Value;

            /// <summary>
            ///     Creates the padded payload from a buffer and its corresponding lap stamp.
            /// </summary>
            /// <param name="buffer">The array that stores queue slots.</param>
            /// <param name="oneLap">The stamp value representing one complete lap.</param>
            /// <remarks>
            ///     The constructor stores its arguments without validation. The buffer must be non-null, and
            ///     <paramref name="oneLap" /> must be a power of two greater than the buffer length.
            /// </remarks>
            public CachePaddedArrayWithOneLap(Array buffer, nuint oneLap) => Value = new ArrayWithOneLap(buffer, oneLap);

            /// <summary>
            ///     The buffer reference and lap stamp stored by the padded value.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [IsReferenceOrContainsReferences]
            public readonly struct ArrayWithOneLap
            {
                /// <summary>
                ///     The buffer holding slots.
                /// </summary>
                public readonly Array Buffer;

                /// <summary>
                ///     A stamp with the value of <c>{ lap: 1, index: 0 }</c>.
                /// </summary>
                public readonly nuint OneLap;

                /// <summary>
                ///     Stores a buffer reference and its corresponding lap stamp.
                /// </summary>
                /// <param name="buffer">The array that stores queue slots.</param>
                /// <param name="oneLap">The stamp value representing one complete lap.</param>
                public ArrayWithOneLap(Array buffer, nuint oneLap)
                {
                    Buffer = buffer;
                    OneLap = oneLap;
                }
            }
        }

        /// <summary>
        ///     Provides typed access to the cache-line-padded buffer reference and lap stamp.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
        /// <remarks>
        ///     A default-initialized value is not a usable queue buffer: <see cref="Buffer" /> returns null and
        ///     <see cref="OneLap" /> is zero. Construct this value with a valid buffer and its matching lap stamp.
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        [IsReferenceOrContainsReferences]
        [BindingType(typeof(CachePaddedArrayWithOneLap))]
        public readonly struct CachePaddedArrayWithOneLap<T>
        {
            /// <summary>
            ///     The non-generic backing value that preserves the cache-line-sized layout.
            /// </summary>
            private readonly CachePaddedArrayWithOneLap _value;

            /// <summary>
            ///     Creates a typed padded buffer value.
            /// </summary>
            /// <param name="buffer">The array that stores queue slots.</param>
            /// <param name="oneLap">The stamp value representing one complete lap.</param>
            /// <remarks>
            ///     The arguments are stored without validation. <paramref name="buffer" /> must be non-null, and
            ///     <paramref name="oneLap" /> must be the power-of-two lap stamp derived from the queue capacity and
            ///     must exceed <paramref name="buffer" />'s length.
            /// </remarks>
            public CachePaddedArrayWithOneLap(Slot<T>[] buffer, nuint oneLap) => _value = new CachePaddedArrayWithOneLap(buffer, oneLap);

            /// <summary>
            ///     Gets the array that stores the queue slots.
            /// </summary>
            /// <returns>The original backing array; no array copy is made.</returns>
            public Slot<T>[] Buffer
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Unsafe.As<Slot<T>[]>(_value.Value.Buffer);
            }

            /// <summary>
            ///     Gets the stamp value representing one complete lap.
            /// </summary>
            /// <returns>The lap stamp paired with <see cref="Buffer" />.</returns>
            public nuint OneLap
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _value.Value.OneLap;
            }
        }
    }
}