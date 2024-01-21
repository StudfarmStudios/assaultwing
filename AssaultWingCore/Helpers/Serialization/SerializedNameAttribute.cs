using System;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Makes a field of a class be (de)serialised by a custom name.
    /// </summary>
    /// This attribute is meant for use with Serialization.SerializeXml and 
    /// Serialization.DeserializeXml as a means to give custom names for XML elements. 
    /// Without this attribute, the elements are named exactly as their corresponding fields.
    /// <seealso cref="Serialization.SerializeXml"/>
    /// <seealso cref="Serialization.DeserializeXml"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializedNameAttribute : Attribute
    {
        /// <summary>
        /// The name of the XML element that stores the field.
        /// </summary>
        public string SerializedName { get; private set; }

        /// <summary>
        /// Creates a custom (de)serialisation name for a field.
        /// </summary>
        /// <param name="serializedName">The name of the XML element that stores the field.</param>
        public SerializedNameAttribute(string serializedName)
        {
            if (string.IsNullOrEmpty(serializedName))
                throw new ArgumentException("Null or empty XML element name");
            SerializedName = serializedName;
        }
    }
}
