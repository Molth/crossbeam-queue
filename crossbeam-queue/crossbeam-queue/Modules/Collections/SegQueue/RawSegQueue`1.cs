using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static crossbeam.SegQueue;

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
    [StructLayout(LayoutKind.Sequential)]
    internal struct RawSegQueue<T>
    {
        /// <summary>
        ///     The head of the queue.
        /// </summary>
        private CachePaddedPosition<T> _head;

        /// <summary>
        ///     The tail of the queue.
        /// </summary>
        private CachePaddedPosition<T> _tail;

        /// <summary>
        ///     The pool of blocks returned after they are no longer in use.
        /// </summary>
        private RawArrayQueue<Block<T>> _pool;

        /// <summary>
        ///     Initializes a new unbounded queue and its block-reuse pool.
        /// </summary>
        /// <param name="capacity">The maximum number of unused blocks retained for reuse.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="capacity" /> is less than or equal to zero.
        /// </exception>
        public RawSegQueue(int capacity)
        {
            _head = new CachePaddedPosition<T>();
            _tail = new CachePaddedPosition<T>();

            _pool = new RawArrayQueue<Block<T>>(capacity);
        }

        /// <summary>
        ///     Adds an element to the end of the queue.
        /// </summary>
        /// <param name="value">The element to add to the queue. The value can be a null reference.</param>
        public void Enqueue(T? value)
        {
            var backoff = new Backoff();
            var tail = _tail.Index.Load(Ordering.Acquire);
            var block = _tail.Block.Load(Ordering.Acquire);
            Block<T>? nextBlock = null;

            try
            {
                while (true)
                {
                    var offset = (tail >> (int)SHIFT) % LAP;

                    if (offset == BLOCK_CAP)
                    {
                        backoff.Snooze();
                        tail = _tail.Index.Load(Ordering.Acquire);
                        block = _tail.Block.Load(Ordering.Acquire);
                        continue;
                    }

                    if (offset + 1 == BLOCK_CAP && nextBlock == null)
                        nextBlock = RentBlock();

                    if (block == null)
                    {
                        var @new = RentBlock();

                        if (_tail.Block.CompareExchange(@new, block) == block)
                        {
                            _head.Block.Store(@new, Ordering.Release);
                            block = @new;
                        }
                        else
                        {
                            nextBlock = @new;
                            tail = _tail.Index.Load(Ordering.Acquire);
                            block = _tail.Block.Load(Ordering.Acquire);
                            continue;
                        }
                    }

                    var newTail = unchecked(tail + (1 << (int)SHIFT));

                    var t = _tail.Index.CompareExchange(newTail, tail);

                    if (t == tail)
                    {
                        if (offset + 1 == BLOCK_CAP)
                        {
                            var nextBlockUnwrap = nextBlock!;
                            nextBlock = null;
                            var nextIndex = unchecked(newTail + (1 << (int)SHIFT));

                            _tail.Block.Store(nextBlockUnwrap, Ordering.Release);
                            _tail.Index.Store(nextIndex, Ordering.Release);
                            block.Next.Store(nextBlockUnwrap, Ordering.Release);
                        }

                        ref var slot = ref Slots<T>.GetUnchecked(ref block.Slots, offset);
                        slot.Value = value;
                        slot.State.Or(WRITE);

                        return;
                    }
                    else
                    {
                        tail = t;
                        block = _tail.Block.Load(Ordering.Acquire);
                        backoff.Spin();
                    }
                }
            }
            finally
            {
                if (nextBlock != null)
                    ReturnBlock(nextBlock);
            }
        }

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
        public bool TryDequeue(out T? result)
        {
            var backoff = new Backoff();
            var head = _head.Index.Load(Ordering.Acquire);
            var block = _head.Block.Load(Ordering.Acquire);

            while (true)
            {
                var offset = (head >> (int)SHIFT) % LAP;

                if (offset == BLOCK_CAP)
                {
                    backoff.Snooze();
                    head = _head.Index.Load(Ordering.Acquire);
                    block = _head.Block.Load(Ordering.Acquire);
                    continue;
                }

                var newHead = unchecked(head + (1 << (int)SHIFT));

                if ((newHead & HAS_NEXT) == 0)
                {
                    Interlocked.MemoryBarrier();
                    var tail = _tail.Index.Load(Ordering.Relaxed);

                    if (head >> (int)SHIFT == tail >> (int)SHIFT)
                    {
                        result = default;
                        return false;
                    }

                    if ((head >> (int)SHIFT) / LAP != (tail >> (int)SHIFT) / LAP)
                        newHead |= HAS_NEXT;
                }

                if (block == null)
                {
                    backoff.Snooze();
                    head = _head.Index.Load(Ordering.Acquire);
                    block = _head.Block.Load(Ordering.Acquire);
                    continue;
                }

                var h = _head.Index.CompareExchange(newHead, head);

                if (h == head)
                {
                    if (offset + 1 == BLOCK_CAP)
                    {
                        var next = block.WaitNext();
                        var nextIndex = unchecked((newHead & ~HAS_NEXT) + (1 << (int)SHIFT));
                        if (next.Next.Load(Ordering.Relaxed) != null)
                            nextIndex |= HAS_NEXT;

                        _head.Block.Store(next, Ordering.Release);
                        _head.Index.Store(nextIndex, Ordering.Release);
                    }

                    ref var slot = ref Slots<T>.GetUnchecked(ref block.Slots, offset);
                    slot.WaitWrite();

                    var value = slot.Value;
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                        slot.Value = default;

                    if (offset + 1 == BLOCK_CAP)
                        Destroy(block, 0);
                    else if ((slot.State.Or(READ) & DESTROY) != 0)
                        Destroy(block, offset + 1);

                    result = value;
                    return true;
                }

                head = h;
                block = _head.Block.Load(Ordering.Acquire);
                backoff.Spin();
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
            var head = _head.Index.Load(Ordering.SeqCst);
            var tail = _tail.Index.Load(Ordering.SeqCst);

            return head >> (int)SHIFT == tail >> (int)SHIFT;
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
                var tail = _tail.Index.Load(Ordering.SeqCst);
                var head = _head.Index.Load(Ordering.SeqCst);

                if (_tail.Index.Load(Ordering.SeqCst) == tail)
                {
                    tail &= unchecked((nuint)~((1 << (int)SHIFT) - 1));
                    head &= unchecked((nuint)~((1 << (int)SHIFT) - 1));

                    if (((tail >> (int)SHIFT) & (LAP - 1)) == LAP - 1)
                        tail = unchecked(tail + (1 << (int)SHIFT));

                    if (((head >> (int)SHIFT) & (LAP - 1)) == LAP - 1)
                        head = unchecked(head + (1 << (int)SHIFT));

                    var lap = (head >> (int)SHIFT) / LAP;
                    tail = unchecked(tail - ((lap * LAP) << (int)SHIFT));
                    head = unchecked(head - ((lap * LAP) << (int)SHIFT));

                    tail >>= (int)SHIFT;
                    head >>= (int)SHIFT;

                    return unchecked(tail - head - tail / LAP);
                }
            }
        }

        /// <summary>
        ///     Sets the <see cref="DESTROY" /> bit on the first unread slot at or after <paramref name="start" />.
        ///     If a thread is using that slot, it continues the destruction process after reading it; otherwise, this
        ///     method returns the block to the reuse pool.
        /// </summary>
        /// <param name="block">The block to destroy or return to the reuse pool.</param>
        /// <param name="start">The first slot to inspect.</param>
        private void Destroy(Block<T> block, nuint start)
        {
            for (var i = start; i < BLOCK_CAP - 1; ++i)
            {
                ref var slot = ref Slots<T>.GetUnchecked(ref block.Slots, i);

                if ((slot.State.Load(Ordering.Acquire) & READ) == 0 && (slot.State.Or(DESTROY) & READ) == 0)
                    return;
            }

            ReturnBlock(block);
        }

        /// <summary>
        ///     Reuses a block from the pool or allocates a new block when the pool is empty.
        /// </summary>
        /// <returns>A cleared block ready to be linked into the queue.</returns>
        private Block<T> RentBlock()
        {
            if (_pool.TryDequeue(out var block))
            {
                block!.Slots = new Slots<T>();
                return block;
            }

            return new Block<T>();
        }

        /// <summary>
        ///     Clears a block's next pointer and attempts to retain it in the bounded reuse pool.
        /// </summary>
        /// <param name="block">The block to return after it is no longer in use.</param>
        private void ReturnBlock(Block<T> block)
        {
            block.Next.Store(null, Ordering.Relaxed);

            _pool.TryEnqueue(block);
        }
    }
}