using System;
using System.Diagnostics;
using System.Reflection;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// <seealso cref="System.Reflection.MemberInfo"/>
    /// </summary>
    public class FieldOrPropertyInfo
    {
        private static readonly object[] EmptyObjectArray = new object[0];

        private FieldInfo _fieldInfo;
        private PropertyInfo _propertyInfo;

        public string Name { get { return _fieldInfo != null ? _fieldInfo.Name : _propertyInfo.Name; } }
        public MemberInfo MemberInfo { get { return _fieldInfo != null ? (MemberInfo)_fieldInfo : _propertyInfo; } }
        public Type ValueType { get { return _fieldInfo != null ? _fieldInfo.FieldType : _propertyInfo.PropertyType; } }
        public Type DeclaringType { get { return MemberInfo.DeclaringType; } }

        public FieldOrPropertyInfo(FieldInfo fieldInfo)
        {
            Debug.Assert(fieldInfo != null);
            _fieldInfo = fieldInfo;
        }

        public FieldOrPropertyInfo(PropertyInfo propertyInfo)
        {
            Debug.Assert(propertyInfo != null);
            Debug.Assert(propertyInfo.GetIndexParameters().Length == 0, "Indexers are not supported");
            _propertyInfo = propertyInfo;
        }

        public object GetValue(object obj)
        {
            if (_fieldInfo != null) return _fieldInfo.GetValue(obj);
            return _propertyInfo.GetValue(obj, EmptyObjectArray);
        }

        public void SetValue(object obj, object value)
        {
            if (_fieldInfo != null) _fieldInfo.SetValue(obj, value);
            else _propertyInfo.SetValue(obj, value, EmptyObjectArray);
        }

        public bool IsDefined(Type attributeType, bool inherit)
        {
            return MemberInfo.IsDefined(attributeType, inherit);
        }

        public object[] GetCustomAttributes(bool inherit)
        {
            return MemberInfo.GetCustomAttributes(inherit);
        }
    }
}
