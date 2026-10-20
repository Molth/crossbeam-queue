// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     Contains constant strings used for exception messages.
    /// </summary>
    internal static class SR
    {
        /// <summary>
        ///     The message used when an argument must be greater than zero.
        /// </summary>
        public const string ArgumentOutOfRange_MustBePositiveNonZero = "The value must be greater than zero.";

#if !NET5_0_OR_GREATER
        /// <summary>
        ///     The message used when a required reference argument is null.
        /// </summary>
        public const string NullReference_MustBeNotNull = "The argument must not be null.";
#endif
    }
}