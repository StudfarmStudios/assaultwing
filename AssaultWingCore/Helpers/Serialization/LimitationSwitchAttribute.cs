using System;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Marks the (de)serialisation of a field of a class to switch from one limitation attribute
    /// to another.
    /// </summary>
    /// This attribute is recognised by class Serialization.
    /// When (de)serialisation reaches a field marked with this attribute, its limitation
    /// attribute can change.
    /// <seealso cref="Serialization.SerializeXml"/>
    /// <seealso cref="Serialization.DeserializeXml"/>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class LimitationSwitchAttribute : Attribute
    {
        /// <summary>
        /// The limitation attribute type to which the switch applies.
        /// </summary>
        public Type From { get; private set; }

        /// <summary>
        /// The target limitation attribute type of the switch.
        /// </summary>
        public Type To { get; private set; }

        /// <summary>
        /// Creates a switch from one (de)serialisation limitation attribute to another.
        /// </summary>
        /// <param name="fromAttribute">The switch is applied only when the field is
        /// (de)serialised with this limitation attribute.</param>
        /// <param name="toAttribute">If the switch applies, the (de)serialisation of
        /// the field is done with this limitation attribute.</param>
        /// <exception cref="ArgumentException">Either parameter isn't an attribute.</exception>
        public LimitationSwitchAttribute(Type fromAttribute, Type toAttribute)
        {
            if (!typeof(Attribute).IsAssignableFrom(fromAttribute) ||
                !typeof(Attribute).IsAssignableFrom(toAttribute))
                throw new ArgumentException("Parameters are not attributes");
            From = fromAttribute;
            To = toAttribute;
        }
    }
}
