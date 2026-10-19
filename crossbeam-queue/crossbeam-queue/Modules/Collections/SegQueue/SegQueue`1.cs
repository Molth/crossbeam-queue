using System;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     An unbounded multi-producer multi-consumer queue.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
    /// <remarks>
    ///     The queue is implemented as a linked list of segments, each of which stores multiple elements.
    ///     The queue has no element-count limit, but it allocates segments as needed and is somewhat slower
    ///     than <see cref="ArrayQueue{T}" />.
    ///     <br />
    ///     The implementation is based on the Rust source:
    ///     <c>https://github.com/crossbeam-rs/crossbeam/blob/main/crossbeam-queue/src/seg_queue.rs</c>.
    /// </remarks>
    [BindingType(typeof(CachePaddedSegQueue<>))]
    public sealed class SegQueue<T>
    {
        /// <summary>
        ///     The internal queue implementation wrapped by this type.
        /// </summary>
        private CachePaddedSegQueue<T> _handle;

        /// <summary>
        ///     Initializes a new unbounded queue and its block-reuse pool.
        /// </summary>
        /// <param name="capacity">The maximum number of unused blocks retained for reuse.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="capacity" /> is less than or equal to zero.
        /// </exception>
        public SegQueue(int capacity) => _handle = new CachePaddedSegQueue<T>(capacity);

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
        ///     Gets the number of elements contained in the queue.
        /// </summary>
        /// <value>The number of elements contained in the queue.</value>
        /// <remarks>
        ///     The value may change as other threads modify the queue. Use <see cref="IsEmpty" /> when only
        ///     the presence or absence of elements is needed.
        /// </remarks>
        public int Count => _handle.Count;

        /// <summary>
        ///     Adds an element to the end of the queue.
        /// </summary>
        /// <param name="item">The element to add to the queue. The value can be a null reference.</param>
        public void Enqueue(T? item) => _handle.Enqueue(item);

        /// <summary>
        ///     Attempts to remove an element from the beginning of the queue.
        /// </summary>
        /// <param name="result">
        ///     When this method returns <see langword="true" />, contains the removed element; otherwise,
        ///     contains the default value of <typeparamref name="T" />.
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if an element was removed; otherwise, <see langword="false" /> if the queue is empty.
        /// </returns>
        public bool TryDequeue(out T? result) => _handle.TryDequeue(out result);
    }
}