using System;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Marks a field of a class as describing a property of a user-defined type,
    /// as opposed to describing an instance's state during gameplay.
    /// </summary>
    /// This attribute is meant for use with Serialization.Serialize and Serialization.Deserialize
    /// as limiting the (de)serialisation of an object.
    /// <seealso cref="Serialization.SerializeXml"/>
    /// <seealso cref="Serialization.DeserializeXml"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class TypeParameterAttribute : Attribute
    {
    }
}
