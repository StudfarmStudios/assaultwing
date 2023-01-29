using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Represents a gob property that is editable in ArenaEditor.
    /// </summary>
    public class GobPropertyDescriptor : PropertyDescriptor
    {
        private FieldOrPropertyInfo _member;

        public static Func<FieldOrPropertyInfo, IEnumerable<Attribute>> GetPropertyAttributes { get; set; }

        public GobPropertyDescriptor(FieldOrPropertyInfo field)
            : base(BeautifyFieldName(field.Name), GetAttributes(field).ToArray())
        {
            _member = field;
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override Type ComponentType { get { return typeof(Gob); } }

        public override object GetValue(object component)
        {
            return _member.GetValue(component);
        }

        public override bool IsReadOnly { get { return false; } }

        public override Type PropertyType { get { return _member.ValueType; } }

        public override void ResetValue(object component)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object component, object value)
        {
            _member.SetValue(component, value);
        }

        public override bool ShouldSerializeValue(object component)
        {
            throw new NotImplementedException();
        }

        private static string BeautifyFieldName(string fieldName)
        {
            return fieldName.Replace("_", "").Capitalize();
        }

        private static IEnumerable<Attribute> GetAttributes(FieldOrPropertyInfo member)
        {
            if (GetPropertyAttributes != null)
                foreach (var attr in GetPropertyAttributes(member)) yield return attr;
            yield return new CategoryAttribute(member.DeclaringType.Name);
            foreach (var attr in member.GetCustomAttributes(false).Cast<Attribute>()) yield return attr;
        }
    }
}
