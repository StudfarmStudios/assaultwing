using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.Xna.Framework;
using TypeTriple = System.Tuple<System.Type, System.Type, System.Type>;

namespace AW2.Helpers.Serialization
{
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
    /// (de)serialisation method. The objects will have only those fields
    /// (de)serialised that have the specified attribute. The same limitation attribute
    /// will also apply to fields in deeper levels of (de)serialisation, i.e., when
    /// (de)serialising the fields of the value of a field in the original object.
    /// 
    /// The chosen limitation attribute can be switched to another during (de)serialisation
    /// for any chosen field. To do this, apply LimitationSwitchAttribute.
    /// <seealso cref="LimitationSwitchAttribute"/>
    public static class Serialization
    {
        /// <summary>
        /// Cache for field infos of fields that must be copied deeply.
        /// An array of field infos is stored for
        /// each pair (type1, type2), where type1 is the type whose fields
        /// we are talking about, and type2 is the limiting attribute that
        /// the fields must have, or <c>null</c> to list all fields.
        /// </summary>
        /// <seealso cref="GetFields(object, Type)"/>
        private static readonly Dictionary<TypeTriple, IEnumerable<FieldInfo>> g_typeFields = new Dictionary<TypeTriple, IEnumerable<FieldInfo>>();

        private static Dictionary<Tuple<Type, Type>, bool> g_isAssignableFromCache = new Dictionary<Tuple<Type, Type>, bool>();

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
            var type = obj.GetType();

            // React to SerializedTypeAttribute
            var serializedTypeAttribute = (SerializedTypeAttribute)Attribute.GetCustomAttribute(type, typeof(SerializedTypeAttribute));
            if (serializedTypeAttribute != null)
            {
                type = serializedTypeAttribute.SerializedType;
                obj = Cast(obj, type);
            }

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
        /// Unknown XML elements are reported to <see cref="AW2.Helpers.Log"/>.
        /// If deserialised object is of a type that implements <see cref="IConsistencyCheckable"/>,
        /// the object is made consistent after deserialisation.
        /// <param name="reader">Where to read the serialised data.</param>
        /// <param name="elementName">Name of the XML element where the object is stored.</param>
        /// <param name="objType">Type of the value to deserialise.</param>
        /// <param name="limitationAttribute">Limit the deserialisation to fields with this attribute.</param>
        /// <returns>The deserialised object.</returns>
        public static object DeserializeXml(XmlReader reader, string elementName, Type objType, Type limitationAttribute)
        {
            try
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

                var writtenType = GetWrittenType(reader, elementName, objType);

                // Deserialise
                object returnValue;
                bool emptyXmlElement = reader.IsEmptyElement;
                reader.Read();
                if (emptyXmlElement)
                    returnValue = Serialization.CreateInstance(writtenType);
                else if (writtenType.IsPrimitive || writtenType == typeof(string))
                    returnValue = reader.ReadContentAs(writtenType, null);
                else if (writtenType.IsEnum)
                    returnValue = DeserializeXmlEnum(reader, writtenType);
                else if (writtenType == typeof(Color))
                    returnValue = DeserializeXmlColor(reader, limitationAttribute);
                else if (IsIEnumerable(writtenType))
                    returnValue = DeserializeXmlIEnumerable(reader, objType, limitationAttribute, writtenType);
                else
                    returnValue = DeserializeXmlOther(reader, limitationAttribute, writtenType);

                if (!emptyXmlElement)
                    reader.ReadEndElement();
                if (writtenType != objType)
                    returnValue = Cast(returnValue, objType);
                if (typeof(IConsistencyCheckable).IsAssignableFrom(writtenType))
                    ((IConsistencyCheckable)returnValue).MakeConsistent(limitationAttribute);
                return returnValue;
            }
            catch (MemberSerializationException e)
            {
                e.MemberName = elementName + "." + e.MemberName;
                throw;
            }
        }

        private static Type GetWrittenType(XmlReader reader, string elementName, Type objType)
        {
            var writtenTypeName = reader.GetAttribute("type");
            if (writtenTypeName == null)
            {
                // Default to expected object type or to what SerializedTypeAttribute says.
                var serializedTypeAttribute = (SerializedTypeAttribute)Attribute.GetCustomAttribute(objType, typeof(SerializedTypeAttribute));
                if (serializedTypeAttribute == null) return objType;
                return serializedTypeAttribute.SerializedType;
            }
            var writtenType = Type.GetType(writtenTypeName);
            if (writtenType == null) throw new XmlException("XML suggests unknown type " + writtenTypeName);
            if (!IsAssignableFrom(objType, writtenType))
                throw new XmlException("XML suggests type " + writtenTypeName + " that is not assignable to expected type " + objType.Name);
            return writtenType;
        }

        private static object DeserializeXmlOther(XmlReader reader, Type limitationAttribute, Type writtenType)
        {
            var returnValue = Serialization.CreateInstance(writtenType);
            DeserializeFieldsXml(reader, returnValue, limitationAttribute);
            return returnValue;
        }

        private static object DeserializeXmlEnum(XmlReader reader, Type writtenType)
        {
            string enumStr = reader.ReadContentAsString();
            return Enum.Parse(writtenType, enumStr);
        }

        private static object DeserializeXmlColor(XmlReader reader, Type limitationAttribute)
        {
            byte r = (byte)DeserializeXml(reader, "R", typeof(byte), limitationAttribute);
            byte g = (byte)DeserializeXml(reader, "G", typeof(byte), limitationAttribute);
            byte b = (byte)DeserializeXml(reader, "B", typeof(byte), limitationAttribute);
            byte a = (byte)DeserializeXml(reader, "A", typeof(byte), limitationAttribute);
            return new Color(r, g, b, a);
        }

        private static object DeserializeXmlIEnumerable(XmlReader reader, Type objType, Type limitationAttribute, Type writtenType)
        {
            // Read as 'objType' so that the possibly needed cast to 'writtenType'
            // can be done nicely on the element type and not on the IEnumerable type.
            Type elementType = GetIEnumerableElementType(objType);
            Type listType = typeof(List<>).MakeGenericType(elementType);
            IList array = (IList)Activator.CreateInstance(listType);
            while (reader.IsStartElement("Item"))
            {
                object item = DeserializeXml(reader, "Item", elementType, limitationAttribute);
                array.Add(item);
            }
            if (writtenType.IsArray)
            {
                BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;
                return listType.InvokeMember("ToArray", flags, null, array, new Object[] { });
            }
            else
                return Serialization.CreateInstance(writtenType, array);
        }

        /// <summary>
        /// Returns the instance fields of the given object that optionally have the given attribute.
        /// </summary>
        /// The search includes the object's public and non-public fields declared in its 
        /// type and all base types. Do not modify the returned array.
        /// <param name="obj">The object whose fields to scan for.</param>
        /// <param name="limitationAttribute">If not <c>null</c>, return only fields with this attribute.</param>
        /// <param name="exclusionAttribute">If not <c>null</c>, return only fields without this attribute.</param>
        public static IEnumerable<FieldInfo> GetFields(Type objType, Type limitationAttribute, Type exclusionAttribute)
        {
            var key = new TypeTriple(objType, limitationAttribute, exclusionAttribute);
            IEnumerable<FieldInfo> result;
            if (g_typeFields.TryGetValue(key, out result)) return result;
            result = GetFieldsImpl(objType, limitationAttribute, exclusionAttribute);
            g_typeFields.Add(key, result);
            return result;
        }

        /// <summary>
        /// Returns the declared instance fields of the given object that optionally have the given attribute.
        /// </summary>
        /// The search includes the object's public and non-public fields declared in its 
        /// type (excluding fields declared in any base types). Do not modify the returned array.
        /// <param name="type">The type whose fields to scan for.</param>
        /// <param name="limitationAttribute">If not <c>null</c>, return only fields with this attribute.</param>
        /// <param name="exclusionAttribute">If not <c>null</c>, return only fields without this attribute.</param>
        public static IEnumerable<FieldInfo> GetDeclaredFields(Type type, Type limitationAttribute, Type exclusionAttribute)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field =>
                    (limitationAttribute == null || field.IsDefined(limitationAttribute, false)) &&
                    (exclusionAttribute == null || !field.IsDefined(exclusionAttribute, false)));
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
            settings.DtdProcessing = DtdProcessing.Prohibit;
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
            object copy = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
            var fields = GetFields(type, null, typeof(ExcludeFromDeepCopyAttribute));
            foreach (var field in fields)
            {
                if (field.IsDefined(typeof(ShallowCopyAttribute), false))
                    field.SetValue(copy, field.GetValue(obj));
                else
                    field.SetValue(copy, DeepCopy(field.GetValue(obj)));
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
        /// <seealso cref="AW2.Helpers.LimitedSerializationAttribute"/>
        private static void SerializeFieldsXml(XmlWriter writer, object obj, Type limitationAttribute)
        {
            var type = obj.GetType();
            var fields = Attribute.IsDefined(type, typeof(LimitedSerializationAttribute))
                ? GetFields(type, limitationAttribute, null)
                : GetFields(type, null, null);

            foreach (var field in fields)
            {
                // React to SerializedNameAttribute and remove leading underscore from field name.
                string elementName = field.Name;
                var serializedNameAttribute = (SerializedNameAttribute)Attribute.GetCustomAttribute(field, typeof(SerializedNameAttribute));
                if (serializedNameAttribute != null)
                    elementName = serializedNameAttribute.SerializedName;
                else if (elementName.StartsWith("_"))
                    elementName = elementName.Substring(1);

                // React to LimitationSwitchAttribute
                var fieldLimitationAttribute = limitationAttribute;
                var limitationSwitchAttribute = (LimitationSwitchAttribute)Attribute.GetCustomAttribute(field, typeof(LimitationSwitchAttribute));
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
        /// It is guaranteed that all fields (marked with <c>limitationAttribute</c>)
        /// get a value exactly once, lest an exception is thrown.
        /// <param name="reader">Where to read the serialised values.</param>
        /// <param name="obj">The object whose fields to deserialise.</param>
        /// <param name="limitationAttribute">Limit the deserialisation to fields with this attribute,
        /// or deserialise all fields if limitationAttribute is a null reference.</param>
        /// <seealso cref="AW2.Helpers.LimitedSerializationAttribute"/>
        private static void DeserializeFieldsXml(XmlReader reader, object obj, Type limitationAttribute)
        {
            try
            {
                var type = obj.GetType();
                var fieldFinder = new FieldFinder(obj.GetType(), limitationAttribute);
                // NOTE: There is no guarantee about the order of the fields.
                while (reader.IsStartElement())
                {
                    var field = fieldFinder.Find(reader.Name);
                    var fieldLimitationAttribute = GetFieldLimitationAttribute(field, limitationAttribute);
                    object value = DeserializeXml(reader, reader.Name, field.FieldType, fieldLimitationAttribute);
                    field.SetValue(obj, value);
                }
                fieldFinder.CheckForMissing();
            }
            catch (MemberSerializationException e)
            {
                e.LineNumber = reader.LineNumber();
                throw;
            }
        }

        private static Type GetFieldLimitationAttribute(FieldInfo field, Type limitationAttribute)
        {
            var limitationSwitchAttribute = (LimitationSwitchAttribute)Attribute.GetCustomAttribute(field, typeof(LimitationSwitchAttribute));
            if (limitationSwitchAttribute != null && limitationSwitchAttribute.From == limitationAttribute)
                return limitationSwitchAttribute.To;
            return limitationAttribute;
        }

        private static int LineNumber(this XmlReader reader)
        {
            var lineInfo = reader as IXmlLineInfo;
            return lineInfo != null ? lineInfo.LineNumber : 0;
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

            // System.String has no parameterless constructor so we provide a default value.
            if (type == typeof(string))
                return "";

            // Otherwise 'type' refers to a class. Find its constructors.
            ConstructorInfo[] constructors = type.GetConstructors();
            if (constructors.Length == 0)
                throw new Exception("No constructors found for type " + type.ToString());

            // Call a constructor with a minimal number of parameters.
            Array.Sort(constructors, (cons1, cons2) =>
                cons1.GetParameters().Length.CompareTo(cons2.GetParameters().Length));
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

        /// <summary>
        /// Casts an object to a type using some available cast operator and other magic.
        /// </summary>
        /// <param name="obj">The object to cast.</param>
        /// <param name="type">The type to cast to.</param>
        /// <returns>The cast object.</returns>
        private static object Cast(object obj, Type type)
        {
            Type objType = obj.GetType();
            if (type.IsAssignableFrom(objType)) return obj;

            // Look for cast operator in 'objType'
            var castMethodInfo = GetCastMethod(objType, type);
            if (castMethodInfo != null)
                return castMethodInfo.Invoke(null, new object[] { obj });

            // Fallback for IEnumerable: convert item by item
            Type lElementType = GetIEnumerableElementType(type);
            Type rElementType = GetIEnumerableElementType(objType);
            if (lElementType != null && rElementType != null)
            {
                if (typeof(Array).IsAssignableFrom(objType))
                {
                    var listType = typeof(List<>).MakeGenericType(lElementType);
                    var result = (IList)Activator.CreateInstance(listType);
                    foreach (var item in (IEnumerable)obj)
                        result.Add(Cast(item, lElementType));
                    var toArrayMethod = listType.GetMethod("ToArray");
                    return toArrayMethod.Invoke(result, null);
                }
                else throw new ArgumentException("Don't know how to cast IEnumerable element type " + rElementType + " into " + lElementType);
            }

            throw new InvalidCastException("Cannot cast " + objType.Name + " to " + type.Name);
        }

        /// <summary>
        /// Can a value of type <paramref name="fromType"/> be cast to type <paramref name="toType"/>.
        /// </summary>
        private static bool CanBeCast(Type fromType, Type toType)
        {
            return GetCastMethod(fromType, toType) != null;
        }

        /// <summary>
        /// Returns a method that casts a value of type <paramref name="fromtype"/>
        /// into type <paramref name="toType"/>, or <c>null</c> if no such method exists.
        /// </summary>
        private static MethodInfo GetCastMethod(Type fromType, Type toType)
        {
            Type[] argTypes = new Type[1];
            argTypes[0] = toType;
            BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            var methodInfos =
                from info in fromType.GetMethods(flags).Union(toType.GetMethods(flags))
                let paraminfos = info.GetParameters()
                where (info.Name == "op_Implicit" || info.Name == "op_Explicit") &&
                    info.ReturnType == toType && paraminfos.Length == 1 && paraminfos[0].ParameterType == fromType
                select info;
            return methodInfos.FirstOrDefault();
        }

        /// <summary>
        /// Does a type implement some <see cref="IEnumerable&lt;T&gt;"/> interface
        /// or the <see cref="IEnumerable"/> interface.
        /// </summary>
        private static bool IsIEnumerable(Type type)
        {
            return GetIEnumerableElementType(type) != null;
        }

        /// <summary>
        /// Returns the type parameter of the <see cref="IEnumerable&lt;T&gt;"/> interface
        /// a type implements, or returns <c>typeof(object)</c> if the type implements the
        /// <see cref="IEnumerable"/> interface, or returns <c>null</c> if the type doesn't
        /// implement any of these interfaces.
        /// </summary>
        private static Type GetIEnumerableElementType(Type type)
        {
            var genericElementTypes =
                from iface in type.GetInterfaces()
                where iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                select iface.GetGenericArguments().First();
            if (genericElementTypes.Any()) return genericElementTypes.First();
            if (type.GetInterface("IEnumerable") != null) return typeof(object);
            return null;
        }

        /// <summary>
        /// Returns <c>true</c> if and only if a value of type <paramref name="rType"/>
        /// can be assigned (possibly by casting) to an lvalue of type <paramref name="lType"/>.
        /// </summary>
        private static bool IsAssignableFrom(Type lType, Type rType)
        {
            bool result;
            var cacheKey = Tuple.Create(lType, rType);
            if (g_isAssignableFromCache.TryGetValue(cacheKey, out result)) return result;
            if (lType.IsAssignableFrom(rType))
                result = true;
            else
            {
                var lElementType = GetIEnumerableElementType(lType);
                var rElementType = GetIEnumerableElementType(rType);
                if (lElementType != null && rElementType != null)
                    result = IsAssignableFrom(lElementType, rElementType);
                else
                    result = CanBeCast(rType, lType);
            }
            return g_isAssignableFromCache[cacheKey] = result;
        }

        private static IEnumerable<FieldInfo> GetFieldsImpl(Type objType, Type limitationAttribute, Type exclusionAttribute)
        {
            var fields = GetDeclaredFields(objType, limitationAttribute, exclusionAttribute);
            for (var type = objType.BaseType; type != null; type = type.BaseType)
                fields = fields.Concat(GetDeclaredFields(type, limitationAttribute, exclusionAttribute));
            return fields;
        }

        #endregion // private methods
    }
}
