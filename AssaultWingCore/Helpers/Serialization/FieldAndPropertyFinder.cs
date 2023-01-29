using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Helper for class <see cref="Serialization"/>.
    /// </summary>
    internal class FieldAndPropertyFinder
    {
        private static Dictionary<Tuple<Type, Type, string>, int> g_indexCache = new Dictionary<Tuple<Type, Type, string>, int>();
        private Type _type;
        private Type _limitationAttribute;
        private FieldOrPropertyInfo[] _fieldsAndProperties;
        private bool[] _founds;
        private bool _tolerant;

        public FieldAndPropertyFinder(Type type, Type limitationAttribute, bool tolerant)
        {
            _type = type;
            _limitationAttribute = limitationAttribute;
            InitializeFieldsAndProperties();
            _founds = new bool[_fieldsAndProperties.Length];
            _tolerant = tolerant;
        }

        public FieldOrPropertyInfo Find(string xmlElementName)
        {
            return FindFieldOrProperty(xmlElementName);
        }

        public void CheckForMissing()
        {
            int missingIndex = Array.FindIndex(_founds, f => !f);
            if (!_tolerant && missingIndex >= 0) throw new MemberSerializationException("Value not found", _fieldsAndProperties[missingIndex].Name);
        }

        private void InitializeFieldsAndProperties()
        {
            _fieldsAndProperties = Serialization.GetFieldsAndProperties(_type, _limitationAttribute, null).ToArray();
        }

        private FieldOrPropertyInfo FindFieldOrProperty(string xmlElementName)
        {
            var index = FindIndex(xmlElementName);
            if (_tolerant && index < 0) return null;
            if (!_tolerant && _founds[index]) throw new MemberSerializationException("Field deserialised twice", xmlElementName);
            _founds[index] = true;
            return _fieldsAndProperties[index];
        }

        private int FindIndex(string xmlElementName)
        {
            int index;
            var cacheKey = Tuple.Create(_type, _limitationAttribute, xmlElementName);
            if (!g_indexCache.TryGetValue(cacheKey, out index))
                g_indexCache[cacheKey] = index = Array.FindIndex(_fieldsAndProperties, f => Serialization.GetSerializedName(f) == xmlElementName);
            if (index == -1 && !_tolerant) throw new MemberSerializationException("Cannot deserialise unknown field", xmlElementName);
            return index;
        }
    }
}
