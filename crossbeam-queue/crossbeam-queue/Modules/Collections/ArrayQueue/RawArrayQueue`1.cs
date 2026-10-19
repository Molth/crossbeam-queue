using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static crossbeam.PaddingHelpers;
using static crossbeam.ArrayQueue;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     A bounded multi-producer multi-consumer queue.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
    [StructLayout(LayoutKind.Sequential, Size = 3 * CACHE_LINE_SIZE)]
    internal struct RawArrayQueue<T>
    {
        /// <summary>
        ///     The head of the queue.
        /// </summary>
        /// <remarks>
        ///     This value is a stamp consisting of an index into the buffer and a lap, but packed into a
        ///     single <see cref="System.UIntPtr" />. The lower bits represent the index, while the upper
        ///     bits represent the lap.
        ///     Elements are removed from the beginning of the queue.
        /// </remarks>
        private CachePaddedAtomicUsize _head;

        /// <summary>
        ///     The tail of the queue.
        /// </summary>
        /// <remarks>
        ///     This value is a stamp consisting of an index into the buffer and a lap, but packed into a
        ///     single <see cref="System.UIntPtr" />. The lower bits represent the index, while the upper
        ///     bits represent the lap.
        ///     Elements are added to the end of the queue.
        /// </remarks>
        private CachePaddedAtomicUsize _tail;

        /// <summary>
        ///     The buffer holding slots.
        /// </summary>
        private readonly Slot<T>[] _buffer;

        /// <summary>
        ///     A stamp with the value of <c>{ lap: 1, index: 0 }</c>.
        /// </summary>
        private readonly nuint _oneLap;

        /// <summary>
        ///     Creates a new bounded queue with the given capacity.
        /// </summary>
        /// <param name="capacity">The requested queue capacity.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="capacity" /> is less than or equal to zero.
        /// </exception>
        public RawArrayQueue(int capacity)
        {
            ThrowHelpers.ThrowIfNegativeOrZero(capacity, ExceptionArgument.capacity);

            _head = new CachePaddedAtomicUsize();
            _tail = new CachePaddedAtomicUsize();

            var buffer = new Slot<T>[capacity];
            for (var i = 0; i < buffer.Length; ++i)
                buffer[i].Stamp = new AtomicUsize((nuint)i);

            _buffer = buffer;
            _oneLap = BitOperationsHelpers.RoundUpToPowerOf2((uint)capacity + 1U);
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
        public bool TryEnqueue(T? item)
        {
            var backoff = new Backoff();
            var tail = _tail.Load(Ordering.Relaxed);

            while (true)
            {
                var index = tail & (_oneLap - 1);
                var lap = tail & ~(_oneLap - 1);

                var newTail = index + 1 < Capacity() ? tail + 1 : unchecked(lap + _oneLap);

                ref var slot = ref _buffer[(int)index];
                var stamp = slot.Stamp.Load(Ordering.Acquire);

                if (tail == stamp)
                {
                    var t = _tail.CompareExchange(newTail, tail);
                    if (t == tail)
                    {
                        slot.Value = item;
                        slot.Stamp.Store(tail + 1, Ordering.Release);
                        return true;
                    }

                    tail = t;
                    backoff.Spin();
                }
                else if (unchecked(stamp + _oneLap) == tail + 1)
                {
                    Interlocked.MemoryBarrier();
                    var head = _head.Load(Ordering.Relaxed);

                    if (unchecked(head + _oneLap) == tail)
                        return false;

                    backoff.Spin();
                    tail = _tail.Load(Ordering.Relaxed);
                }
                else
                {
                    backoff.Snooze();
                    tail = _tail.Load(Ordering.Relaxed);
                }
            }
        }

        /// <summary>
        ///     Adds an element to the end of the queue, replacing the oldest element if necessary.
        /// </summary>
        /// <param name="item">The element to add to the queue. The value can be a null reference.</param>
        /// <returns>
        ///     A result. Call <see cref="ArrayQueue{T}.EnqueueResult.TryGetOverwritten" /> to determine whether the
        ///     oldest element was replaced and retrieve it. If no element was replaced, the output parameter contains
        ///     the default value of <typeparamref name="T" />.
        /// </returns>
        [return: MustBeUsed]
        public ArrayQueue<T>.EnqueueResult Enqueue(T? item)
        {
            var backoff = new Backoff();
            var tail = _tail.Load(Ordering.Relaxed);

            while (true)
            {
                var index = tail & (_oneLap - 1);
                var lap = tail & ~(_oneLap - 1);

                var newTail = index + 1 < Capacity() ? tail + 1 : unchecked(lap + _oneLap);

                ref var slot = ref _buffer[(int)index];
                var stamp = slot.Stamp.Load(Ordering.Acquire);

                if (tail == stamp)
                {
                    var t = _tail.CompareExchange(newTail, tail);
                    if (t == tail)
                    {
                        slot.Value = item;
                        slot.Stamp.Store(tail + 1, Ordering.Release);
                        return ArrayQueue<T>.EnqueueResult.None();
                    }

                    tail = t;
                    backoff.Spin();
                }
                else if (unchecked(stamp + _oneLap) == tail + 1)
                {
                    Interlocked.MemoryBarrier();

                    var head = unchecked(tail - _oneLap);
                    var newHead = unchecked(newTail - _oneLap);

                    var h = _head.CompareExchange(newHead, head);
                    if (h == head)
                    {
                        _tail.Store(newTail, Ordering.SeqCst);

                        var old = slot.Value;
                        slot.Value = item;

                        slot.Stamp.Store(tail + 1, Ordering.Release);
                        return ArrayQueue<T>.EnqueueResult.Some(old);
                    }

                    backoff.Spin();
                    tail = _tail.Load(Ordering.Relaxed);
                }
                else
                {
                    backoff.Snooze();
                    tail = _tail.Load(Ordering.Relaxed);
                }
            }
        }

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
        public bool TryDequeue(out T? item)
        {
            var backoff = new Backoff();
            var head = _head.Load(Ordering.Relaxed);

            while (true)
            {
                var index = head & (_oneLap - 1);
                var lap = head & ~(_oneLap - 1);

                ref var slot = ref _buffer[(int)index];
                var stamp = slot.Stamp.Load(Ordering.Acquire);

                if (head + 1 == stamp)
                {
                    var @new = index + 1 < Capacity() ? head + 1 : unchecked(lap + _oneLap);

                    var h = _head.CompareExchange(@new, head);
                    if (h == head)
                    {
                        item = slot.Value;
                        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                            slot.Value = default;

                        slot.Stamp.Store(unchecked(head + _oneLap), Ordering.Release);
                        return true;
                    }

                    head = h;
                    backoff.Spin();
                }
                else if (stamp == head)
                {
                    Interlocked.MemoryBarrier();
                    var tail = _tail.Load(Ordering.Relaxed);

                    if (tail == head)
                    {
                        item = default;
                        return false;
                    }

                    backoff.Spin();
                    head = _head.Load(Ordering.Relaxed);
                }
                else
                {
                    backoff.Snooze();
                    head = _head.Load(Ordering.Relaxed);
                }
            }
        }

        /// <summary>
        ///     Determines whether the queue is empty.
        /// </summary>
        /// <returns><see langword="true" /> if the queue is empty; otherwise, <see langword="false" />.</returns>
        /// <remarks>
        ///     The result may change as other threads modify the queue. Use this method when only the
        ///     presence or absence of elements is needed.
        /// </remarks>
        public bool IsEmpty()
        {
            var head = _head.Load(Ordering.SeqCst);
            var tail = _tail.Load(Ordering.SeqCst);

            return tail == head;
        }

        /// <summary>
        ///     Determines whether the queue is full.
        /// </summary>
        /// <returns><see langword="true" /> if the queue is full; otherwise, <see langword="false" />.</returns>
        /// <remarks>
        ///     The result may change as other threads modify the queue.
        /// </remarks>
        public bool IsFull()
        {
            var tail = _tail.Load(Ordering.SeqCst);
            var head = _head.Load(Ordering.SeqCst);

            return unchecked(head + _oneLap) == tail;
        }

        /// <summary>
        ///     Returns the number of elements contained in the queue.
        /// </summary>
        /// <returns>The number of elements contained in the queue.</returns>
        /// <remarks>
        ///     The result may change as other threads modify the queue. Use <see cref="IsEmpty" /> when only
        ///     the presence or absence of elements is needed.
        /// </remarks>
        public nuint Count()
        {
            while (true)
            {
                var tail = _tail.Load(Ordering.SeqCst);
                var head = _head.Load(Ordering.SeqCst);

                if (_tail.Load(Ordering.SeqCst) == tail)
                {
                    var hix = head & (_oneLap - 1);
                    var tix = tail & (_oneLap - 1);

                    if (hix < tix)
                        return tix - hix;

                    if (hix > tix)
                        return Capacity() - hix + tix;

                    if (tail == head)
                        return 0;

                    return Capacity();
                }
            }
        }

        /// <summary>
        ///     Returns the capacity of the queue.
        /// </summary>
        /// <returns>The maximum number of elements the queue can hold.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly nuint Capacity() => (nuint)_buffer.Length;
    }
}