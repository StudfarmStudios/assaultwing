using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Represents a gob property that is editable in ArenaEditor.
    /// </summary>
    public class GobPropertyDescriptor : PropertyDescriptor
    {
        private FieldInfo _field;

        public static Func<Type, IEnumerable<Attribute>> GetPropertyAttributes { get; set; }

        public GobPropertyDescriptor(FieldInfo field)
            : base(BeautifyFieldName(field.Name), GetAttributes(field).ToArray())
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

        private static IEnumerable<Attribute> GetAttributes(FieldInfo field)
        {
            if (GetPropertyAttributes != null)
                foreach (var attr in GetPropertyAttributes(field.FieldType)) yield return attr;
            foreach (var attr in field.GetCustomAttributes(false).Cast<Attribute>()) yield return attr;
        }
    }
}
