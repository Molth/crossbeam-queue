using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static crossbeam.PaddingHelpers;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     A bounded multi-producer multi-consumer queue.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [IsReferenceOrContainsReferences]
    [BindingType(typeof(RawArrayQueue<>))]
    internal struct CachePaddedArrayQueue<T>
    {
        /// <summary>
        ///     Padding intended to keep the queue from sharing cache lines with adjacent data.
        /// </summary>
        private readonly CachePadding _padding;

        /// <summary>
        ///     The internal queue implementation wrapped by this type.
        /// </summary>
        private RawArrayQueue<T> _handle;

        /// <summary>
        ///     Creates a new bounded queue with the given capacity.
        /// </summary>
        /// <param name="capacity">The requested queue capacity.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="capacity" /> is less than or equal to zero.
        /// </exception>
        public CachePaddedArrayQueue(int capacity)
        {
            _padding = new CachePadding();
            _handle = new RawArrayQueue<T>(capacity);
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
        ///     Gets a value that indicates whether the queue is full.
        /// </summary>
        /// <value><see langword="true" /> if the queue is full; otherwise, <see langword="false" />.</value>
        /// <remarks>
        ///     The value may change as other threads modify the queue.
        /// </remarks>
        public bool IsFull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _handle.IsFull();
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
            get => (int)_handle.Count();
        }

        /// <summary>
        ///     Gets the capacity of the queue.
        /// </summary>
        /// <value>The maximum number of elements the queue can hold.</value>
        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)_handle.Capacity();
        }

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnqueue(T? item) => _handle.TryEnqueue(item);

        /// <summary>
        ///     Adds an element to the end of the queue, replacing the oldest element if necessary.
        /// </summary>
        /// <param name="item">The element to add to the queue. The value can be a null reference.</param>
        /// <returns>
        ///     A result. Call <see cref="ArrayQueue{T}.EnqueueResult.TryGetOverwritten" /> to determine whether the
        ///     oldest element was replaced and retrieve it. When that method returns <see langword="false" />, its
        ///     <c>overwritten</c> output parameter contains the default value of <typeparamref name="T" />.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: MustBeUsed]
        public ArrayQueue<T>.EnqueueResult Enqueue(T? item) => _handle.Enqueue(item);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T? item) => _handle.TryDequeue(out item);
    }
}