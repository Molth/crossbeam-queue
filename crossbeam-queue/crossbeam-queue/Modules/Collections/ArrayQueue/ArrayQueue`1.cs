using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     A bounded multi-producer multi-consumer queue.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
    /// <remarks>
    ///     <para>
    ///         This type is not intended as a drop-in replacement for <see cref="ConcurrentQueue{T}" />.
    ///         It does not provide the following members:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <see cref="ConcurrentQueue{T}.TryPeek" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="ConcurrentQueue{T}.GetEnumerator" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="ConcurrentQueue{T}.Clear" />
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         The queue allocates a fixed-capacity buffer when constructed.
    ///         <br />
    ///         The buffer determines the maximum number of elements the queue can hold.
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <see cref="TryEnqueue" /> returns <see langword="false" /> when the queue is full.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="Enqueue" /> replaces the oldest element when the queue is full and returns it in an
    ///                 <see cref="ArrayQueue{T}.EnqueueResult" />, allowing the queue to act as a ring buffer.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         The preallocated buffer makes this queue somewhat faster than <see cref="SegQueue{T}" />.
    ///     </para>
    ///     <para>
    ///         The Rust implementation is based on Dmitry Vyukov's bounded MPMC queue.
    ///     </para>
    ///     <para>Sources:</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Crossbeam Rust implementation:<br />
    ///                 <c>https://github.com/crossbeam-rs/crossbeam/blob/main/crossbeam-queue/src/array_queue.rs</c>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Dmitry Vyukov's bounded MPMC queue:<br />
    ///                 <c>http://www.1024cores.net/home/lock-free-algorithms/queues/bounded-mpmc-queue</c>
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    [BindingType(typeof(CachePaddedArrayQueue<>))]
    public sealed class ArrayQueue<T>
    {
        /// <summary>
        ///     The cache-padded queue implementation wrapped by this type.
        /// </summary>
        private CachePaddedArrayQueue<T> _handle;

        /// <summary>
        ///     Creates a new bounded queue with the given capacity.
        /// </summary>
        /// <param name="capacity">The requested queue capacity.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="capacity" /> is less than or equal to zero.
        /// </exception>
        public ArrayQueue(int capacity) => _handle = new CachePaddedArrayQueue<T>(capacity);

        /// <summary>
        ///     Gets a value that indicates whether the queue is empty.
        /// </summary>
        /// <value><see langword="true" /> if the queue is empty; otherwise, <see langword="false" />.</value>
        /// <remarks>
        ///     The value may change as other threads modify the queue. Use this property when only the
        ///     presence or absence of elements is needed.
        /// </remarks>
        public bool IsEmpty => _handle.IsEmpty;

        /// <summary>
        ///     Gets a value that indicates whether the queue is full.
        /// </summary>
        /// <value><see langword="true" /> if the queue is full; otherwise, <see langword="false" />.</value>
        /// <remarks>
        ///     The value may change as other threads modify the queue.
        /// </remarks>
        public bool IsFull => _handle.IsFull;

        /// <summary>
        ///     Gets the number of elements contained in the queue.
        /// </summary>
        /// <value>The number of elements contained in the queue.</value>
        /// <remarks>
        ///     The value may change as other threads modify the queue. Use <see cref="IsEmpty" /> when only
        ///     the presence or absence of elements is needed.
        /// </remarks>
        public int Count => _handle.Count;

        /// <summary>
        ///     Gets the capacity of the queue.
        /// </summary>
        /// <value>The maximum number of elements the queue can hold.</value>
        public int Capacity => _handle.Capacity;

        /// <summary>
        ///     Attempts to add an element to the end of the queue.
        /// </summary>
        /// <param name="item">
        ///     The element to add to the queue.
        ///     The value can be a null reference.
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if the element was enqueued;
        ///     otherwise, <see langword="false" /> if the queue is full.
        /// </returns>
        public bool TryEnqueue(T? item) => _handle.TryEnqueue(item);

        /// <summary>
        ///     Adds an element to the end of the queue, replacing the oldest element if necessary.
        /// </summary>
        /// <param name="item">The element to add to the queue. The value can be a null reference.</param>
        /// <returns>
        ///     A result. Call <see cref="EnqueueResult.TryGetOverwritten" /> to determine whether the
        ///     oldest element was replaced and retrieve it. When that method returns <see langword="false" />, its
        ///     <c>overwritten</c> output parameter contains the default value of <typeparamref name="T" />.
        /// </returns>
        [return: MustBeUsed]
        public EnqueueResult Enqueue(T? item) => _handle.Enqueue(item);

        /// <summary>
        ///     Attempts to remove an element from the beginning of the queue.
        /// </summary>
        /// <param name="item">
        ///     When this method returns <see langword="true" />, contains the removed element;
        ///     otherwise, contains the default value of <typeparamref name="T" />.
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if an element was removed;
        ///     otherwise, <see langword="false" /> if the queue is empty.
        /// </returns>
        public bool TryDequeue(out T? item) => _handle.TryDequeue(out item);

        /// <summary>
        ///     Describes whether <see cref="Enqueue" /> replaced an element and provides the replaced element.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        [IsReferenceOrContainsReferences]
        public readonly struct EnqueueResult
        {
            /// <summary>
            ///     Indicates whether <see cref="Enqueue" /> replaced the oldest element.
            /// </summary>
            private readonly bool _hasOverwritten;

            /// <summary>
            ///     The oldest element replaced by <see cref="Enqueue" />,
            ///     or the default value of the element type when no element was replaced.
            /// </summary>
            private readonly T? _overwritten;

            /// <summary>
            ///     Initializes a result with the replacement status and replaced element.
            /// </summary>
            /// <param name="hasOverwritten">
            ///     <see langword="true" /> if an element was replaced;
            ///     otherwise, <see langword="false" />.
            /// </param>
            /// <param name="overwritten">
            ///     The replaced element,
            ///     or the default value of the element type when no element was replaced.
            /// </param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private EnqueueResult(bool hasOverwritten, T? overwritten)
            {
                _hasOverwritten = hasOverwritten;
                _overwritten = overwritten;
            }

            /// <summary>
            ///     Gets the oldest element replaced by <see cref="Enqueue" />.
            /// </summary>
            /// <param name="overwritten">
            ///     When this method returns <see langword="true" />, contains the replaced element,
            ///     which may be <see langword="null" />; otherwise, contains the default value of
            ///     the element type.
            /// </param>
            /// <returns>
            ///     <see langword="true" /> if <see cref="Enqueue" /> replaced an element;
            ///     otherwise, <see langword="false" />.
            /// </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetOverwritten(out T? overwritten)
            {
                overwritten = _overwritten;
                return _hasOverwritten;
            }

            /// <summary>
            ///     Creates a result that contains an element replaced by <see cref="Enqueue" />.
            /// </summary>
            /// <param name="value">The replaced element.</param>
            /// <returns>A result that indicates an element was replaced.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static EnqueueResult Some(T? value) => new(true, value);

            /// <summary>
            ///     Creates a result that indicates <see cref="Enqueue" /> did not replace an element.
            /// </summary>
            /// <returns>A result that indicates no element was replaced.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static EnqueueResult None() => new(false, default);
        }
    }
}