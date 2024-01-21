using System;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Denotes that the value of a field can be shared by reference.
    /// The absence of this attribute suggests that a deep copy should be taken.
    /// This field has no effect for value typed fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ShallowCopyAttribute : Attribute
    {
    }
}
