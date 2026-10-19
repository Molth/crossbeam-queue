using System.Runtime.InteropServices;

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
    }
}