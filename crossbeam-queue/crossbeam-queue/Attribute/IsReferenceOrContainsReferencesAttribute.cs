using System;

// ReSharper disable ALL

namespace crossbeam
{
    /// <summary>
    ///     Indicates that the decorated type contains references or is a reference type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    internal sealed class IsReferenceOrContainsReferencesAttribute : Attribute
    {
    }
}