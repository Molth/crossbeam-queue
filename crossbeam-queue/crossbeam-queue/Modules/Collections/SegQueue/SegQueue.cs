using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static crossbeam.PaddingHelpers;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     Contains constants and storage types used by the unbounded queue implementation.
    /// </summary>
    internal static class SegQueue
    {
        /// <summary>
        ///     Indicates that a value has been written into a slot.
        /// </summary>
        public const nuint WRITE = 1;

        /// <summary>
        ///     Indicates that a value has been read from a slot.
        /// </summary>
        public const nuint READ = 2;

        /// <summary>
        ///     Indicates that the containing block is being destroyed.
        /// </summary>
        public const nuint DESTROY = 4;

        /// <summary>
        ///     The number of queue positions covered by one block lap.
        /// </summary>
        public const nuint LAP = 32;

        /// <summary>
        ///     The maximum number of values a block can hold.
        /// </summary>
        public const nuint BLOCK_CAP = LAP - 1;

        /// <summary>
        ///     The number of lower index bits reserved for metadata.
        /// </summary>
        public const nuint SHIFT = 1;

        /// <summary>
        ///     Indicates that the block is not the last one.
        /// </summary>
        public const nuint HAS_NEXT = 1;

        /// <summary>
        ///     Returns a reference to a slot without checking the index.
        /// </summary>
        /// <param name="buffer">The block slot storage.</param>
        /// <param name="index">The zero-based slot index.</param>
        /// <returns>A reference to the slot at <paramref name="index" />.</returns>
        /// <remarks>
        ///     The caller must ensure that <paramref name="index" /> is less than <see cref="BLOCK_CAP" />.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref Slot<T> GetUnchecked<T>(ref this Slots<T> buffer, nuint index) => ref Unsafe.Add(ref Unsafe.As<Slots<T>, Slot<T>>(ref buffer), (nint)index);

        /// <summary>
        ///     Returns a reference to the queue index stored in a padded position.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
        /// <param name="value">The padded position whose index is requested.</param>
        /// <returns>A reference to the position's existing atomic index; no value is copied.</returns>
        /// <remarks>
        ///     The returned reference aliases the position's storage. Callers must use the atomic operations and memory
        ///     orderings required by the queue algorithm.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref AtomicUsize GetIndexRef<T>(ref this CachePaddedPosition<T> value) => ref value.Value.Value.Index;

        /// <summary>
        ///     Returns a typed reference to the block pointer stored in a padded position.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
        /// <param name="value">The padded position whose block pointer is requested.</param>
        /// <returns>A reference to the position's atomic block pointer; no value is copied.</returns>
        /// <remarks>
        ///     The fixed-size backing position stores this reference as <see cref="AtomicRef{T}" /> with
        ///     <see cref="object" /> as its type argument. Since <see cref="AtomicRef{T}" /> stores its value in a single
        ///     managed-reference field, this method reinterprets that field as an <see cref="AtomicRef{T}" /> for
        ///     <see cref="Block{T}" />. Preserve that representation and store only blocks of the matching
        ///     <typeparamref name="T" />; callers must also use the queue's required atomic memory orderings.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref AtomicRef<Block<T>> GetBlockRef<T>(ref this CachePaddedPosition<T> value) => ref Unsafe.As<AtomicRef<object>, AtomicRef<Block<T>>>(ref value.Value.Value.Block);

        /// <summary>
        ///     A slot in a block.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
        [StructLayout(LayoutKind.Sequential)]
        [IsReferenceOrContainsReferences]
        public struct Slot<T>
        {
            /// <summary>
            ///     The value.
            /// </summary>
            public T? Value;

            /// <summary>
            ///     The state of the slot.
            /// </summary>
            public AtomicUsize State;

            /// <summary>
            ///     Waits until a value has been written into this slot.
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void WaitWrite()
            {
                var backoff = new Backoff();
                while ((State.Load(Ordering.Acquire) & WRITE) == 0)
                    backoff.Snooze();
            }
        }

        /// <summary>
        ///     Stores the fixed number of slots in a block as contiguous fields.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
        [FixedLengthArray(typeof(Slot<>), (int)BLOCK_CAP)]
        [StructLayout(LayoutKind.Sequential)]
        [IsReferenceOrContainsReferences]
        public struct Slots<T>
        {
            /// <summary>
            ///     The numeric suffix of each field is its zero-based slot index.
            /// </summary>
            private Slot<T>
                _slot00,
                _slot01,
                _slot02,
                _slot03,
                _slot04,
                _slot05,
                _slot06,
                _slot07,
                _slot08,
                _slot09,
                _slot10,
                _slot11,
                _slot12,
                _slot13,
                _slot14,
                _slot15,
                _slot16,
                _slot17,
                _slot18,
                _slot19,
                _slot20,
                _slot21,
                _slot22,
                _slot23,
                _slot24,
                _slot25,
                _slot26,
                _slot27,
                _slot28,
                _slot29,
                _slot30;
        }

        /// <summary>
        ///     A block in the linked list.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
        /// <remarks>
        ///     Each block can hold up to <see cref="BLOCK_CAP" /> elements.
        /// </remarks>
        public sealed class Block<T>
        {
            /// <summary>
            ///     The next block in the linked list.
            /// </summary>
            public AtomicRef<Block<T>> Next;

            /// <summary>
            ///     Slots for values.
            /// </summary>
            public Slots<T> Slots;

            /// <summary>
            ///     Waits until the next block has been linked and returns it.
            /// </summary>
            /// <returns>The next block in the linked list.</returns>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public Block<T> WaitNext()
            {
                var backoff = new Backoff();
                while (true)
                {
                    var next = Next.Load(Ordering.Acquire);
                    if (next != null)
                        return next;

                    backoff.Snooze();
                }
            }
        }

        /// <summary>
        ///     Stores a queue index and the block containing that index.
        /// </summary>
        /// <remarks>
        ///     This non-generic backing type must use <see cref="LayoutKind.Explicit" /> because <see cref="Position" />
        ///     contains a managed reference. With <see cref="LayoutKind.Sequential" />, the compiled size does not equal
        ///     <see cref="CACHE_LINE_SIZE" />. Its single payload field starts at offset zero so the runtime can describe
        ///     that reference while the remaining bytes provide padding. Do not add an
        ///     overlapping field or replace this with a generic sequential struct and infer its runtime size from the
        ///     declared <c>Size</c>. If the layout changes, verify the actual size of each relevant closed type with
        ///     <c>Unsafe.SizeOf&lt;T&gt;()</c> on every supported runtime and architecture.
        /// </remarks>
        [StructLayout(LayoutKind.Explicit, Size = CACHE_LINE_SIZE)]
        [IsReferenceOrContainsReferences]
        [BindingType(typeof(Position))]
        public struct CachePaddedPosition
        {
            /// <summary>
            ///     The queue index and block reference, starting at offset zero.
            /// </summary>
            [FieldOffset(0)] public Position Value;

            /// <summary>
            ///     Stores a queue index and the block containing that index.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [IsReferenceOrContainsReferences]
            public struct Position
            {
                /// <summary>
                ///     The index in the queue.
                /// </summary>
                public AtomicUsize Index;

                /// <summary>
                ///     The block in the linked list.
                /// </summary>
                public AtomicRef<object> Block;
            }
        }

        /// <summary>
        ///     Stores a queue index and the block containing that index.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
        [StructLayout(LayoutKind.Sequential)]
        [IsReferenceOrContainsReferences]
        [BindingType(typeof(CachePaddedPosition))]
        public struct CachePaddedPosition<T>
        {
            /// <summary>
            ///     The cache-line-sized backing position used by this element type.
            /// </summary>
            public CachePaddedPosition Value;
        }
    }
}