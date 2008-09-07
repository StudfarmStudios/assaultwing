using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using System.Collections;
using Microsoft.Xna.Framework.Graphics;
using TypePair = System.Collections.Generic.KeyValuePair<System.Type, System.Type>;

namespace AW2.Helpers
{
    /// <summary>
    /// Marks a field of a class as describing a property of a user-defined type,
    /// as opposed to describing an instance's state during gameplay.
    /// </summary>
    /// This attribute is meant for use with Serialization.Serialize and Serialization.Deserialize
    /// as limiting the (de)serialisation of an object.
    /// <see cref="Serialization.SerializeXml"/>
    /// <see cref="Serialization.DeserializeXml"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class TypeParameterAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks a field of a class as describing an instance's state during gameplay.
    /// </summary>
    /// This attribute is meant for use with Serialization.SerializeXml and 
    /// Serialization.DeserializeXml as limiting the (de)serialisation of an object.
    /// <see cref="Serialization.SerializeXml"/>
    /// <see cref="Serialization.DeserializeXml"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class RuntimeStateAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks the (de)serialisation of a field of a class to switch from one limitation attribute
    /// to another.
    /// </summary>
    /// This attribute is recognised by class Serialization.
    /// When (de)serialisation reaches a field marked with this attribute, its limitation
    /// attribute can change.
    /// <see cref="Serialization.SerializeXml"/>
    /// <see cref="Serialization.DeserializeXml"/>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class LimitationSwitchAttribute : Attribute
    {
        Type fromAttribute;
        Type toAttribute;

        /// <summary>
        /// The limitation attribute type to which the switch applies.
        /// </summary>
        public Type From { get { return fromAttribute; } }

        /// <summary>
        /// The target limitation attribute type of the switch.
        /// </summary>
        public Type To { get { return toAttribute; } }

        /// <summary>
        /// Creates a switch from one (de)serialisation limitation attribute to another.
        /// </summary>
        /// <param name="fromAttribute">The switch is applied only when the field is
        /// (de)serialised with this limitation attribute.</param>
        /// <param name="toAttribute">If the switch applies, the (de)serialisation of
        /// the field is done with this limitation attribute.</param>
        /// <exception cref="ArgumentException">Either parameter isn't an attribute.</exception>
        public LimitationSwitchAttribute(Type fromAttribute, Type toAttribute)
            : base()
        {
            if (!typeof(Attribute).IsAssignableFrom(fromAttribute) ||
                !typeof(Attribute).IsAssignableFrom(toAttribute))
                throw new ArgumentException("Parameters are not attributes");
            this.fromAttribute = fromAttribute;
            this.toAttribute = toAttribute;
        }
    }

    /// <summary>
    /// Makes a field of a class be (de)serialised by a custom name.
    /// </summary>
    /// This attribute is meant for use with Serialization.SerializeXml and 
    /// Serialization.DeserializeXml as a means to give custom names for XML elements. 
    /// Without this attribute, the elements are named exactly as their corresponding fields.
    /// <see cref="Serialization.SerializeXml"/>
    /// <see cref="Serialization.DeserializeXml"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializedNameAttribute : Attribute
    {
        string serializedName;

        /// <summary>
        /// The name of the XML element that stores the field.
        /// </summary>
        public string SerializedName { get { return serializedName; } }

        /// <summary>
        /// Creates a custom (de)serialisation name for a field.
        /// </summary>
        /// <param name="serializedName">The name of the XML element that stores the field.</param>
        public SerializedNameAttribute(string serializedName)
        {
            if (string.IsNullOrEmpty(serializedName))
                throw new ArgumentException("Null or empty XML element name");
            this.serializedName = serializedName;
        }
    }

    /// <summary>
    /// Denotes that a class uses limitation attributes on its fields to define 
    /// (de)serialisable parts of its instances.
    /// </summary>
    /// This attribute is recognised by class Serialization.
    /// <see cref="Serialization.SerializeXml"/>
    /// <see cref="Serialization.DeserializeXml"/>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class LimitedSerializationAttribute : Attribute
    {
    }

    /// <summary>
    /// A type whose instances are consistent only under certain conditions
    /// that concern some fields of the instance.
    /// </summary>
    /// Implement this interface for types whose fields must be checked and
    /// possibly corrected after an operation that changes the field values.
    public interface IConsistencyCheckable
    {
        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        void MakeConsistent(Type limitationAttribute);
    }

    /// <summary>
    /// Provides methods that help serialise objects.
    /// </summary>
    /// These serialisation and deserialisation methods work only with each other
    /// and not with .NET's serialisation classes.
    /// 
    /// Objects are serialised to XML so that each element name corresponds to a field name
    /// and every element has attribute 'type' that tells the runtime type of the value
    /// stored to that field. The value can be of any type that can be casted to the field's
    /// type. The field's type is not stored in the XML. Only instance fields are serialised,
    /// no properties, methods or static members.
    /// 
    /// Partial serialisation is supported via <b>limitation attributes</b>. 
    /// To limit the (de)serialisation of an object to a certain set of fields, apply
    /// an attribute to those fields and then pass the attribute's type to the 
    /// (de)serialisation method. The object's will have only those fields
    /// (de)serialised that have the specified attribute. The same limitation attribute
    /// will also apply to fields in deeper levels of (de)serialisation, i.e., when
    /// (de)serialising the fields of the value of a field in the original object.
    /// 
    /// The chosen limitation attribute can be switched to another during (de)serialisation
    /// for any chosen field. To do this, apply LimitationSwitchAttribute.
    /// <see cref="LimitationSwitchAttribute"/>
    class Serialization
    {
        /// <summary>
        /// Cache for field infos. An array of field infos is stored for
        /// each pair (type1, type2), where type1 is the type whose fields
        /// we are talking about, and type2 is the limiting attribute that
        /// the fields must have, or <c>null</c> to list all fields.
        /// </summary>
        /// <see cref="GetFields(object, Type)"/>
        static Dictionary<TypePair, FieldInfo[]> typeFields = new Dictionary<TypePair, FieldInfo[]>();

        #region public methods

        /// <summary>
        /// Writes out a part of an object to an XML stream.
        /// </summary>
        /// <param name="writer">Where to write the serialised data.</param>
        /// <param name="elementName">Name of the XML element where the object is stored.</param>
        /// <param name="obj">The object whose partial state to serialise.</param>
        /// <param name="limitationAttribute">Limit the serialisation to fields with this attribute.</param>
        public static void SerializeXml(XmlWriter writer, string elementName, object obj, Type limitationAttribute)
        {
            if (obj == null)
                throw new Exception("Won't serialise a null obj");
            Type type = obj.GetType();
            writer.WriteStartElement(elementName);
            writer.WriteAttributeString("type", type.AssemblyQualifiedName);
            if (type.IsPrimitive || type == typeof(string))
                writer.WriteValue(obj);
            else if (type.IsEnum)
                writer.WriteValue(obj.ToString());
            else if (type == typeof(Color))
            {
                Color color = (Color)obj;
                SerializeXml(writer, "R", color.R, limitationAttribute);
                SerializeXml(writer, "G", color.G, limitationAttribute);
                SerializeXml(writer, "B", color.B, limitationAttribute);
                SerializeXml(writer, "A", color.A, limitationAttribute);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                IEnumerable enumerable = (IEnumerable)obj;
                foreach (object item in enumerable)
                    SerializeXml(writer, "Item", item, limitationAttribute);
            }
            else
                SerializeFieldsXml(writer, obj, limitationAttribute);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Restores the specified part of the state of a serialised object from an XML stream.
        /// </summary>
        /// Fields that don't have a serialised value are left untouched.
        /// Unknown XML elements are reported to AW2.Helpers.Log.
        /// If deserialised object is of a type that implements IConsistencyCheckable,
        /// the object is made consistent after deserialisation.
        /// <param name="reader">Where to read the serialised data.</param>
        /// <param name="elementName">Name of the XML element where the object is stored.</param>
        /// <param name="objType">Type of the value to deserialise.</param>
        /// <param name="limitationAttribute">Limit the deserialisation to fields with this attribute.</param>
        /// <returns>The deserialised object.</returns>
        public static object DeserializeXml(XmlReader reader, string elementName, Type objType, Type limitationAttribute)
        {
            // Sanity checks
            if (limitationAttribute != null &&
                !typeof(Attribute).IsAssignableFrom(limitationAttribute))
                throw new ArgumentException("Expected an attribute, got " + limitationAttribute.Name);

            // XML consistency checks
            if (!reader.IsStartElement())
                throw new XmlException("Deserialisation expected start element");
            if (!reader.IsStartElement(elementName))
                throw new XmlException("Deserialisation expected start element " + elementName + " but got " + reader.Name);

            // Find out type of value in XML
            string writtenTypeName = reader.GetAttribute("type");
            Type writtenType = Type.GetType(writtenTypeName);
            if (writtenType == null)
                throw new XmlException("XML suggests unknown type " + writtenTypeName);
            if (!objType.IsAssignableFrom(writtenType))
                throw new XmlException("XML suggests type " + writtenTypeName + " that is not assignable to expected type " + objType.Name);

            // Deserialise
            if (reader.IsEmptyElement)
            {
                reader.Read();
                return Serialization.CreateInstance(writtenType);
            }
            reader.Read();
            object returnValue;
            Type iEnumerableElementType = null;

            // Is it a primitive type?
            if (writtenType.IsPrimitive || writtenType == typeof(string))
                returnValue = reader.ReadContentAs(writtenType, null);
 
            // Is it an enum?
            else if (writtenType.IsEnum)
            {
                string enumStr = reader.ReadContentAsString();
                returnValue = Enum.Parse(writtenType, enumStr);
            }

            // Is it a Color?
            else if (writtenType == typeof(Color))
            {
                byte r = (byte)DeserializeXml(reader, "R", typeof(byte), limitationAttribute);
                byte g = (byte)DeserializeXml(reader, "G", typeof(byte), limitationAttribute);
                byte b = (byte)DeserializeXml(reader, "B", typeof(byte), limitationAttribute);
                byte a = (byte)DeserializeXml(reader, "A", typeof(byte), limitationAttribute);
                returnValue = new Color(r, g, b, a);
            }

            // Is it an array?
            else if (writtenType.IsArray)
            {
                Type elementType = writtenType.GetElementType();
                Type listType = typeof(List<>).MakeGenericType(elementType);
                IList list = (IList)Activator.CreateInstance(listType);
                while (reader.IsStartElement("Item"))
                {
                    object item = DeserializeXml(reader, "Item", elementType, limitationAttribute);
                    list.Add(item);
                }
                BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;
                returnValue = listType.InvokeMember("ToArray", flags, null, list, new Object[] { });
            }

            // Is it IEnumerable<T> for some type T?
            else if (Array.Exists(writtenType.GetInterfaces(), delegate(Type iface)
                {
                    bool good = iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>);
                    if (good) iEnumerableElementType = iface.GetGenericArguments()[0];
                    return good;
                }))
            {
                Type elementType = iEnumerableElementType;
                Type listType = typeof(List<>).MakeGenericType(elementType);
                IList array = (IList)Activator.CreateInstance(listType);
                while (reader.IsStartElement("Item"))
                {
                    object item = DeserializeXml(reader, "Item", elementType, limitationAttribute);
                    array.Add(item);
                }
                returnValue = Serialization.CreateInstance(writtenType, array);
            }

            // Otherwise the value is an object that is just a collection of fields
            else
            {
                returnValue = Serialization.CreateInstance(writtenType);
                DeserializeFieldsXml(reader, returnValue, limitationAttribute);
            }

            reader.ReadEndElement();

            if (typeof(IConsistencyCheckable).IsAssignableFrom(writtenType))
                ((IConsistencyCheckable)returnValue).MakeConsistent(limitationAttribute);
            return returnValue;
        }

        /// <summary>
        /// Returns the instance fields of the given object that optionally have the given attribute.
        /// </summary>
        /// The search includes the object's public and non-public fields declared in its 
        /// type and all base types. Do not modify the returned array.
        /// <param name="obj">The object whose fields to scan for.</param>
        /// <param name="limitationAttribute">Return fields with this attribute.
        /// If set to null, returns all fields.</param>
        /// <returns>The instance fields of given object that have the given attribute,
        /// or all of its fields if <i>limitationAttribute</i> is a null reference.</returns>
        /// <exception cref="ArgumentException"><i>limitationAttribute</i> is not 
        /// a null reference or an attribute.</exception>
        public static FieldInfo[] GetFields(object obj, Type limitationAttribute)
        {
            Type objType = obj.GetType();
            TypePair key = new TypePair(objType, limitationAttribute);

            // Look up the result from the cache.
            FieldInfo[] result;
            if (typeFields.TryGetValue(key, out result))
                return result;

            // Sanity checks
            if (limitationAttribute != null &&
                !typeof(Attribute).IsAssignableFrom(limitationAttribute))
                throw new ArgumentException("Expected an attribute, got " + limitationAttribute.Name);

            // Type.GetFields() only gives out fields that the type itself can see.
            // Therefore, to reach private members of base classes, we need to check
            // each base class in turn.
            List<FieldInfo> fields = new List<FieldInfo>();
            for (Type type = objType; type != null; type = type.BaseType)
                fields.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly |
                                               BindingFlags.Public | BindingFlags.NonPublic));
            Predicate<FieldInfo> fieldChoice = delegate(FieldInfo field)
            {
                return limitationAttribute == null || field.IsDefined(limitationAttribute, false);
            };
            result = Array.FindAll<FieldInfo>(fields.ToArray(), fieldChoice);
            typeFields[key] = result;
            return result;
        }

        /// <summary>
        /// Returns an XML writer that uses the given text writer.
        /// </summary>
        /// <param name="writer">The text writer to base the XML writer on.</param>
        /// <returns>The XML writer.</returns>
        public static XmlWriter GetXmlWriter(TextWriter writer)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = Encoding.UTF8;
            settings.CloseOutput = true;
            return XmlWriter.Create(writer, settings);
        }

        /// <summary>
        /// Returns an XML reader that uses the given file stream.
        /// </summary>
        /// <param name="stream">The file stream to base the XML reader on.</param>
        /// <returns>The XML reader.</returns>
        public static XmlReader GetXmlReader(FileStream stream)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;
            settings.CloseInput = true;
            settings.ProhibitDtd = false;
            return XmlReader.Create(stream, settings);
        }

        /// <summary>
        /// Returns a deep copy of an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>A deep copy of the object.</returns>
        public static object DeepCopy(object obj)
        {
            if (obj == null) return null;
            Type type = obj.GetType();

            // Value types are always copied by value.
            if (type.IsValueType)
                return obj;

            // Strings are immutable; share reference.
            if (type == typeof(string))
                return obj;

            // Array requires recursion.
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                Type listType = typeof(List<>).MakeGenericType(elementType);
                IList list = (IList)Activator.CreateInstance(listType);
                Array array = (Array)obj;
                foreach (object item in array)
                {
                    object itemCopy = DeepCopy(item);
                    list.Add(itemCopy);
                }
                BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;
                return listType.InvokeMember("ToArray", flags, null, list, new Object[] { });
            }

            // IEnumerable<T> for some type T requires recursion, too.
            Type iEnumerableElementType = null;
            if (Array.Exists(type.GetInterfaces(), delegate(Type iface)
                {
                    bool good = iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>);
                    if (good) iEnumerableElementType = iface.GetGenericArguments()[0];
                    return good;
                }))
            {
                Type elementType = iEnumerableElementType;
                Type listType = typeof(List<>).MakeGenericType(elementType);
                IList list = (IList)Activator.CreateInstance(listType);
                foreach (object item in (IEnumerable)obj)
                {
                    object itemCopy = DeepCopy(item);
                    list.Add(itemCopy);
                }
                return CreateInstance(type, list);
            }

            // Other class instances are copied field by field.
            object copy = Serialization.CreateInstance(type);
            FieldInfo[] fields = GetFields(obj, null);
            foreach (FieldInfo field in fields)
            {
                object fieldValue = field.GetValue(obj);
                field.SetValue(copy, DeepCopy(fieldValue));
            }
            return copy;
        }

        #endregion // public methods

        #region private methods

        /// <summary>
        /// Writes out fields of an object into an XML writer.
        /// </summary>
        /// You can limit the serialisation to fields that have the specified limitation attribute.
        /// This limitation is applied only on instances of classes that have 
        /// LimitedSerializationAttribute.
        /// <param name="writer">Where to write the serialised values.</param>
        /// <param name="obj">The object whose fields to serialise.</param>
        /// <param name="limitationAttribute">Limit the serialisation to fields with this attribute,
        /// or serialise all fields if limitationAttribute is a null reference.</param>
        /// <see cref="AW2.Helpers.LimitedSerializationAttribute"/>
        private static void SerializeFieldsXml(XmlWriter writer, object obj, Type limitationAttribute)
        {
            Type type = obj.GetType();
            FieldInfo[] fields = Attribute.IsDefined(type, typeof(LimitedSerializationAttribute))
                ? GetFields(obj, limitationAttribute)
                : GetFields(obj, null);

            foreach (FieldInfo field in fields)
            {
                // React to SerializedNameAttribute
                string elementName = field.Name;
                SerializedNameAttribute serializedNameAttribute = (SerializedNameAttribute)Attribute.GetCustomAttribute(field, typeof(SerializedNameAttribute));
                if (serializedNameAttribute != null)
                {
                    elementName = serializedNameAttribute.SerializedName;
                }

                // React to LimitationSwitchAttribute
                Type fieldLimitationAttribute = limitationAttribute;
                LimitationSwitchAttribute limitationSwitchAttribute = (LimitationSwitchAttribute)Attribute.GetCustomAttribute(field, typeof(LimitationSwitchAttribute));
                if (limitationSwitchAttribute != null)
                {
                    if (limitationSwitchAttribute.From == limitationAttribute)
                        fieldLimitationAttribute = limitationSwitchAttribute.To;
                }

                SerializeXml(writer, elementName, field.GetValue(obj), fieldLimitationAttribute);
            }
        }

        /// <summary>
        /// Reads in fields of an object from an XML reader.
        /// </summary>
        /// You can limit the deserialisation to fields that have the specified limitation attribute.
        /// This limitation is applied only on instances of classes that have 
        /// LimitedSerializationAttribute.
        ///
        /// The XML reader is assumed to be positioned on the first child element of 
        /// the root element of the serialised 'obj' to be read. At successful return, 
        /// the XML reader will be positioned at the end element of the serialised 'obj'.
        /// A log message is produced if an unknown serialised field is encountered.
        /// No checks are made to see if all fields get a value or if a field is 
        /// assigned a value more than once.
        /// <param name="reader">Where to read the serialised values.</param>
        /// <param name="obj">The object whose fields to deserialise.</param>
        /// <param name="limitationAttribute">Limit the deserialisation to fields with this attribute,
        /// or deserialise all fields if limitationAttribute is a null reference.</param>
        /// <see cref="AW2.Helpers.LimitedSerializationAttribute"/>
        private static void DeserializeFieldsXml(XmlReader reader, object obj, Type limitationAttribute)
        {
            Type type = obj.GetType();
            FieldInfo[] fields = Attribute.IsDefined(type, typeof(LimitedSerializationAttribute))
                ? GetFields(obj, limitationAttribute)
                : GetFields(obj, null);

            // Read in serialised fields of the given object.
            // We have no guarantee of the order of the fields.
            while (reader.IsStartElement())
            {
                bool fieldFound = false;
                foreach (FieldInfo field in fields)
                {
                    // React to SerializedNameAttribute
                    string elementName = field.Name;
                    SerializedNameAttribute serializedNameAttribute = (SerializedNameAttribute)Attribute.GetCustomAttribute(field, typeof(SerializedNameAttribute));
                    if (serializedNameAttribute != null)
                    {
                        elementName = serializedNameAttribute.SerializedName;
                    }

                    if (reader.Name.Equals(elementName))
                    {
                        Type fieldLimitationAttribute = limitationAttribute;

                        // React to LimitationSwitchAttribute
                        LimitationSwitchAttribute limitationSwitchAttribute = (LimitationSwitchAttribute)Attribute.GetCustomAttribute(field, typeof(LimitationSwitchAttribute));
                        if (limitationSwitchAttribute != null)
                        {
                            if (limitationSwitchAttribute.From == limitationAttribute)
                                fieldLimitationAttribute = limitationSwitchAttribute.To;
                        }

                        fieldFound = true;
                        object value = DeserializeXml(reader, elementName, field.FieldType, fieldLimitationAttribute);
                        field.SetValue(obj, value);
                        break;
                    }
                }
                if (!fieldFound)
                {
                    Log.Write("Skipping unknown XML element " + reader.Name);
                    reader.Skip();
                }
            }
        }

        /// <summary>
        /// Creates an arbitrary instance of a type.
        /// </summary>
        /// If the type has a parameterless constructor, it is called.
        /// Otherwise some other constructor is called with arbitrary parameters.
        /// Use with caution!
        /// <param name="type">The type.</param>
        /// <returns>An arbitrary instance of the type.</returns>
        private static object CreateInstance(Type type)
        {
            // Structs have default values.
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            // Otherwise 'type' refers to a class. Find its constructors.
            ConstructorInfo[] constructors = type.GetConstructors();
            if (constructors.Length == 0)
                throw new Exception("No constructors found for type " + type.ToString());

            // Call a constructor with a minimal number of parameters.
            Array.Sort(constructors, delegate(ConstructorInfo x, ConstructorInfo y)
            {
                return Comparer.Default.Compare(x.GetParameters().Length, y.GetParameters().Length);
            });
            ParameterInfo[] paramTypes = constructors[0].GetParameters();
            List<object> parameters = new List<object>();
            foreach (ParameterInfo paramType in paramTypes)
                parameters.Add(CreateInstance(paramType.ParameterType));
            return constructors[0].Invoke(parameters.ToArray());
        }

        /// <summary>
        /// Creates an instance of a type that implements IEnumerable&lt;T&gt;,
        /// initialising it with items.
        /// </summary>
        /// If the type has a parameterless constructor, it is called.
        /// Otherwise some other constructor is called with arbitrary parameters.
        /// Use with caution!
        /// <param name="type">The type that implements IEnumerable&lt;T&gt;.</param>
        /// <param name="items">The items the new instance will contain.</param>
        /// <returns>An instance of the type, containing the items.</returns>
        private static object CreateInstance(Type type, IEnumerable items)
        {
            // Try to create the enumerable object by calling a constructor
            // that takes our items. Otherwise try creating an empty
            // enumerable object and adding the items one by one.
            BindingFlags bindingFlags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance;
            if (type.GetConstructor(new Type[] { typeof(IEnumerable) }) != null)
                return Activator.CreateInstance(type, items);
            else if (type.GetMember("Add", MemberTypes.Method, bindingFlags).Length > 0)
            {
                object returnValue = Activator.CreateInstance(type);
                foreach (object item in items)
                    type.InvokeMember("Add", bindingFlags, null, returnValue, new object[] { item });
                return returnValue;
            }
            else
                throw new ArgumentException("Don't know how to add items to container of type "
                    + type.ToString());
        }

        #endregion // private methods
    }
}
