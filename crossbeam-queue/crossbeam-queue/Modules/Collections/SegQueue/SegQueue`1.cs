using System;
using System.Collections.Concurrent;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     An unbounded multi-producer multi-consumer queue.
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
    ///         The queue stores elements in a linked list of blocks,
    ///         with multiple element slots in each block.
    ///     </para>
    ///     <para>
    ///         The queue has no element-count limit.
    ///         <br />
    ///         Its <c>capacity</c> constructor argument sets the
    ///         maximum number of unused blocks retained for reuse; it does not limit the number of
    ///         queued elements.
    ///         <br />
    ///         Blocks are allocated as needed.
    ///     </para>
    ///     <para>
    ///         Allocating blocks as the queue grows makes it somewhat slower than
    ///         <see cref="ArrayQueue{T}" />.
    ///     </para>
    ///     <para>Source:</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Crossbeam Rust implementation:<br />
    ///                 <c>https://github.com/crossbeam-rs/crossbeam/blob/main/crossbeam-queue/src/seg_queue.rs</c>
    ///             </description>
    ///         </item>
    ///     </list>
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