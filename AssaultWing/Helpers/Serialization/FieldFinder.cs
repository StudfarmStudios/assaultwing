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
        private static Dictionary<Tuple<Type, Type, string>, int> g_fieldIndexCache = new Dictionary<Tuple<Type, Type, string>, int>();
        private Type _type;
        private Type _limitationAttribute;
        private FieldInfo[] _fields;
        private bool[] _fieldFounds;

        public FieldFinder(Type type, Type limitationAttribute)
        {
            _type = type;
            _limitationAttribute = limitationAttribute;
            InitializeFields();
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

        private void InitializeFields()
        {
            _fields = Serialization.GetFields(_type, _limitationAttribute, null).ToArray();
        }

        private FieldInfo FindField(string xmlElementName)
        {
            int fieldIndex = FindFieldIndex(xmlElementName);
            if (_fieldFounds[fieldIndex]) throw new MemberSerializationException("Field deserialised twice", xmlElementName);
            _fieldFounds[fieldIndex] = true;
            return _fields[fieldIndex];
        }

        private int FindFieldIndex(string xmlElementName)
        {
            int fieldIndex;
            var cacheKey = Tuple.Create(_type, _limitationAttribute, xmlElementName);
            if (!g_fieldIndexCache.TryGetValue(cacheKey, out fieldIndex))
                g_fieldIndexCache[cacheKey] = fieldIndex = Array.FindIndex(_fields, f => Serialization.GetSerializedName(f) == xmlElementName);
            if (fieldIndex == -1) throw new MemberSerializationException("Cannot deserialise unknown field", xmlElementName);
            return fieldIndex;
        }
    }
}
