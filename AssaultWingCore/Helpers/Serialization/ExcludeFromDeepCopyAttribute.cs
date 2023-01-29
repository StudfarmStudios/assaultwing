using System;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Denotes that the value of a field is to be skipped during <see cref="Serialization.DeepCopy"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ExcludeFromDeepCopyAttribute : Attribute
    {
    }
}
