using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    public class GobPropertyDescriptor : PropertyDescriptor
    {
        private FieldInfo _field;

        public GobPropertyDescriptor(FieldInfo field)
            : base(BeautifyFieldName(field.Name), field.GetCustomAttributes(false).Cast<Attribute>().ToArray())
        {
            _field = field;
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override Type ComponentType { get { return typeof(Gob); } }

        public override object GetValue(object component)
        {
            return _field.GetValue(component);
        }

        public override bool IsReadOnly { get { return false; } }

        public override Type PropertyType { get { return _field.FieldType; } }

        public override void ResetValue(object component)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object component, object value)
        {
            _field.SetValue(component, value);
        }

        public override bool ShouldSerializeValue(object component)
        {
            throw new NotImplementedException();
        }

        private static string BeautifyFieldName(string fieldName)
        {
            return fieldName.Replace("_", "").Capitalize();
        }
    }
}
