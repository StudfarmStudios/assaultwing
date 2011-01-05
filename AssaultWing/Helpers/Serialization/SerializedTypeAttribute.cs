using System;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Makes a type be serialised via conversion to another type.
    /// </summary>
    /// This attribute is meant for use with <see cref="Serialization.SerializeXml"/> and 
    /// <see cref="Serialization.DeserializeXml"/> as a means to disguise a type in its
    /// serialised form as some other type. This is helpful when one wants to change the
    /// type of a field while still keeping the serialised form the same. Deserialisation
    /// works both from the original type and the type specified by this attribute.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SerializedTypeAttribute : Attribute
    {
        /// <summary>
        /// The type of the value that is stored as an XML element.
        /// </summary>
        public Type SerializedType { get; private set; }

        /// <summary>
        /// Creates a custom (de)serialisation type for a field.
        /// </summary>
        /// <param name="serializedType">The type to store the field's value as.
        /// There must exist explicit conversions between the field's type
        /// and <paramref name="serializedType"/>.</param>
        public SerializedTypeAttribute(Type serializedType)
        {
            if (serializedType == null) throw new ArgumentNullException("Cannot serialise via null type");
            SerializedType = serializedType;
        }
    }
}
