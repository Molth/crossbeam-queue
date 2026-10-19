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
        ///     A slot in a block.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
        [StructLayout(LayoutKind.Sequential)]
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
        [StructLayout(LayoutKind.Sequential)]
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

            /// <summary>
            ///     Returns a reference to a slot without checking the index.
            /// </summary>
            /// <param name="slots">The block slot storage.</param>
            /// <param name="index">The zero-based slot index.</param>
            /// <returns>A reference to the slot at <paramref name="index" />.</returns>
            /// <remarks>
            ///     The caller must ensure that <paramref name="index" /> is less than <see cref="BLOCK_CAP" />.
            /// </remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ref Slot<T> GetUnchecked(ref Slots<T> slots, nuint index) => ref Unsafe.Add(ref slots._slot00, (nint)index);
        }

        /// <summary>
        ///     A block in the linked list.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
        /// <remarks>
        ///     Each block can hold up to <see cref="BLOCK_CAP" /> elements.
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
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
        /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
        [StructLayout(LayoutKind.Sequential, Size = CACHE_LINE_SIZE)]
        public struct CachePaddedPosition<T>
        {
            /// <summary>
            ///     The index in the queue.
            /// </summary>
            public AtomicUsize Index;

            /// <summary>
            ///     The block in the linked list.
            /// </summary>
            public AtomicRef<Block<T>> Block;
        }
    }
}