using System;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     Indicates that the value returned by
    ///     a method must be checked by the caller,
    ///     and must not be ignored or discarded.
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue)]
    internal sealed class MustBeUsedAttribute : Attribute
    {
    }
}