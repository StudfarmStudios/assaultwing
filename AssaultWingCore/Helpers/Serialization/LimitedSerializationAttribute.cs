using System;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Denotes that a class uses limitation attributes on its fields to define 
    /// (de)serialisable parts of its instances.
    /// </summary>
    /// This attribute is recognised by class Serialization.
    /// <seealso cref="Serialization.SerializeXml"/>
    /// <seealso cref="Serialization.DeserializeXml"/>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class LimitedSerializationAttribute : Attribute
    {
    }
}
