using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Helper for class <see cref="Serialization"/>.
    /// </summary>
    internal class FieldFinder
    {
        private FieldInfo[] _fields;
        private bool[] _fieldFounds;

        public FieldFinder(Type type, Type limitationAttribute)
        {
            _fields = Attribute.IsDefined(type, typeof(LimitedSerializationAttribute))
                ? Serialization.GetFields(type, limitationAttribute, null).ToArray()
                : Serialization.GetFields(type, null, null).ToArray();
            _fieldFounds = new bool[_fields.Length];
        }

        public FieldInfo Find(string xmlElementName)
        {
            return FindField(xmlElementName);
        }

        public void CheckForMissing()
        {
            int missingIndex = Array.FindIndex(_fieldFounds, f => !f);
            if (missingIndex >= 0) throw new MemberSerializationException("Value not found", _fields[missingIndex].Name);
        }

        private FieldInfo FindField(string xmlElementName)
        {
            for (int fieldI = 0; fieldI < _fields.Length; ++fieldI)
            {
                var field = _fields[fieldI];

                // React to SerializedNameAttribute
                string elementName = field.Name;
                var serializedNameAttribute = (SerializedNameAttribute)Attribute.GetCustomAttribute(field, typeof(SerializedNameAttribute));
                if (serializedNameAttribute != null)
                    elementName = serializedNameAttribute.SerializedName;
                else if (elementName.StartsWith("_"))
                    elementName = elementName.Substring(1);

                if (xmlElementName.Equals(elementName))
                {
                    if (_fieldFounds[fieldI]) throw new MemberSerializationException("Field deserialised twice", xmlElementName);
                    _fieldFounds[fieldI] = true; 
                    return _fields[fieldI];
                }
            }
            throw new MemberSerializationException("Cannot deserialise unknown field", xmlElementName);
        }
    }
}
