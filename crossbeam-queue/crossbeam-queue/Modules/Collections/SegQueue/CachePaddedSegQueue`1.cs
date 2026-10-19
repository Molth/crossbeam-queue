using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static crossbeam.PaddingHelpers;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     An unbounded multi-producer multi-consumer queue.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [BindingType(typeof(RawSegQueue<>))]
    internal struct CachePaddedSegQueue<T>
    {
        /// <summary>
        ///     Padding intended to keep the queue from sharing cache lines with adjacent data.
        /// </summary>
        private readonly CachePadding _padding;

        /// <summary>
        ///     The internal queue implementation wrapped by this type.
        /// </summary>
        private RawSegQueue<T> _handle;

        /// <summary>
        ///     Initializes a new unbounded queue and its block-reuse pool.
        /// </summary>
        /// <param name="capacity">The maximum number of unused blocks retained for reuse.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="capacity" /> is less than or equal to zero.
        /// </exception>
        public CachePaddedSegQueue(int capacity)
        {
            _padding = new CachePadding();
            _handle = new RawSegQueue<T>(capacity);
        }

        /// <summary>
        ///     Gets a value that indicates whether the queue is empty.
        /// </summary>
        /// <value><see langword="true" /> if the queue is empty; otherwise, <see langword="false" />.</value>
        /// <remarks>
        ///     The value may change as other threads modify the queue. Use this property when only the
        ///     presence or absence of elements is needed.
        /// </remarks>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle.IsEmpty();
        }

        /// <summary>
        ///     Gets the number of elements contained in the queue.
        /// </summary>
        /// <value>The number of elements contained in the queue.</value>
        /// <remarks>
        ///     The value may change as other threads modify the queue. Use <see cref="IsEmpty" /> when only
        ///     the presence or absence of elements is needed.
        /// </remarks>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => unchecked((int)_handle.Count());
        }

        /// <summary>
        ///     Adds an element to the end of the queue.
        /// </summary>
        /// <param name="item">The element to add to the queue. The value can be a null reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T? result) => _handle.TryDequeue(out result);
    }
}