using System;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Marks a field of a class as describing an instance's state during gameplay.
    /// </summary>
    /// This attribute is meant for use with Serialization.SerializeXml and 
    /// Serialization.DeserializeXml as limiting the (de)serialisation of an object.
    /// <seealso cref="Serialization.SerializeXml"/>
    /// <seealso cref="Serialization.DeserializeXml"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class RuntimeStateAttribute : Attribute
    {
    }
}
