using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Xna.Framework;
using TypeTuple = System.Tuple<System.Type, System.Type>;
using TypeTriple = System.Tuple<System.Type, System.Type, System.Type>;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Provides methods that help serialise objects.
    /// </summary>
    /// These serialisation and deserialisation methods work only with each other
    /// and not with .NET's serialisation classes.
    /// 
    /// Objects are serialised to XML so that each element name corresponds to a field or property name
    /// and every element has attribute 'type' that tells the runtime type of the value
    /// stored to that member. The value can be of any type that can be casted to the type of the
    /// member. The type of the member is not stored in the XML. Only instance members are serialised.
    /// 
    /// Partial serialisation is supported via <b>limitation attributes</b>. 
    /// To limit the (de)serialisation of an object to a certain set of members, apply
    /// an attribute to those members and then pass the attribute's type to the 
    /// (de)serialisation method. The objects will have only those members
    /// (de)serialised that have the specified attribute. The same limitation attribute
    /// will also apply to members in deeper levels of (de)serialisation, i.e., when
    /// (de)serialising the members of the value of a member in the original object.
    /// 
    /// The chosen limitation attribute can be switched to another during (de)serialisation
    /// for any chosen member. To do this, apply <see cref="LimitationSwitchAttribute"/>.
    public static class Serialization
    {
        /// <summary>
        /// Keys are (TYPE1, TYPE2, TYPE3) where
        /// TYPE1 is the type whose fields we are talking about,
        /// TYPE2 is the limiting attribute that the members must have, or null to include all members, and
        /// TYPE3 is the exclusion attribute that the members must not have, or null to not exclude any members.
        /// </summary>
        /// <seealso cref="GetFieldsAndProperties(object, Type)"/>
        private static readonly Dictionary<TypeTriple, IEnumerable<FieldOrPropertyInfo>> g_typeFieldsAndProperties = new Dictionary<TypeTriple, IEnumerable<FieldOrPropertyInfo>>();

        /// <summary>
        /// Keys are (TYPE1, TYPE2) where
        /// TYPE1 is the type whose fields we are talking about,
        /// TYPE2 is the exclusion attribute that the fields must not have, or null to not exclude any fields.
        /// </summary>
        /// <seealso cref="GetFields(object, Type)"/>
        private static readonly Dictionary<TypeTuple, IEnumerable<FieldInfo>> g_typeFields = new Dictionary<TypeTuple, IEnumerable<FieldInfo>>();

        private static Dictionary<Tuple<Type, Type>, bool> g_isAssignableFromCache = new Dictionary<Tuple<Type, Type>, bool>();
        private static Dictionary<string, Type> g_typeNameToType = new Dictionary<string, Type>();

        #region public methods

        /// <summary>
        /// Writes out a part of an object to an XML stream.
        /// </summary>
        /// <param name="writer">Where to write the serialised data.</param>
        /// <param name="elementName">Name of the XML element where the object is stored.</param>
        /// <param name="obj">The object whose partial state to serialise.</param>
        /// <param name="limitationAttribute">Limit the serialisation to members with this attribute.</param>
        /// <param name="baseType">Expected type of the object. The given object must be convertible to this type.
        /// If null, then the type of the given object is the base type.</param>
        public static void SerializeXml(XmlWriter writer, string elementName, object obj, Type limitationAttribute, Type baseType = null)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            var type = GetSerializedType(obj.GetType());
            var castObj = Cast(obj, type);

            if (baseType == null) baseType = type;
            writer.WriteStartElement(elementName);
            if (type != baseType) writer.WriteAttributeString("type", type.FullName);
            if (type.IsPrimitive || type == typeof(string))
                writer.WriteValue(castObj);
            else if (type.IsEnum)
                writer.WriteValue(castObj.ToString());
            else if (type == typeof(Color))
            {
                var color = (Color)castObj;
                SerializeXml(writer, "R", color.R, limitationAttribute);
                SerializeXml(writer, "G", color.G, limitationAttribute);
                SerializeXml(writer, "B", color.B, limitationAttribute);
                SerializeXml(writer, "A", color.A, limitationAttribute);
            }
            else if (type == typeof(TimeSpan))
            {
                var timeSpan = (TimeSpan)castObj;
                writer.WriteValue(timeSpan.TotalSeconds);
            }
            else if (type == typeof(Curve))
            {
                var curve = (Curve)castObj;
                var curveKeys = new StringBuilder();
                foreach (var curveKey in curve.Keys) curveKeys.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                    "{0} {1} {2} {3} {4}\n", curveKey.Position, curveKey.Value, curveKey.TangentIn, curveKey.TangentOut, curveKey.Continuity);
                writer.WriteString(curveKeys.ToString());
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type))
                foreach (object item in (IEnumerable)castObj)
                    SerializeXml(writer, "Item", item, limitationAttribute, typeof(object));
            else
                SerializeMembersXml(writer, castObj, limitationAttribute);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Restores the specified part of the state of a serialised object from an XML stream.
        /// </summary>
        /// Members that don't have a serialised value are left untouched.
        /// Unknown XML elements are reported to <see cref="AW2.Helpers.Log"/>.
        /// If the deserialised object is of a type that implements <see cref="IConsistencyCheckable"/>,
        /// the object is made consistent after deserialisation.
        /// <param name="reader">Where to read the serialised data.</param>
        /// <param name="elementName">Name of the XML element where the object is stored.</param>
        /// <param name="objType">Type of the value to deserialise.</param>
        /// <param name="limitationAttribute">Limit the deserialisation to members with this attribute.</param>
        /// <param name="tolerant">If true, errors are not raised for missing or extra XML elements.</param>
        /// <returns>The deserialised object.</returns>
        public static object DeserializeXml(XmlReader reader, string elementName, Type objType, Type limitationAttribute, bool tolerant)
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
                    returnValue = DeserializeXmlColor(reader, limitationAttribute, tolerant);
                else if (writtenType == typeof(TimeSpan))
                    returnValue = TimeSpan.FromSeconds(reader.ReadContentAsDouble());
                else if (writtenType == typeof(Curve))
                    returnValue = DeserializeXmlCurve(reader, limitationAttribute, tolerant);
                else if (IsIEnumerable(writtenType))
                    returnValue = DeserializeXmlIEnumerable(reader, objType, limitationAttribute, writtenType, tolerant);
                else
                    returnValue = DeserializeXmlOther(reader, limitationAttribute, writtenType, tolerant);

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
            if (writtenTypeName == null) return GetSerializedType(objType);
            Type writtenType;
            if (!g_typeNameToType.TryGetValue(writtenTypeName, out writtenType))
            {
                writtenType = Type.GetType(writtenTypeName)
                    ?? AppDomain.CurrentDomain.GetAssemblies()
                        .Select(assembly => assembly.GetType(writtenTypeName))
                        .Where(type => type != null)
                        .FirstOrDefault();
                if (writtenType == null)
                    throw new XmlException("XML suggests unknown type " + writtenTypeName,
                        null, reader.LineNumber(), reader.LinePosition());
                g_typeNameToType.Add(writtenTypeName, writtenType);
            }
            if (!IsAssignableFrom(objType, writtenType))
                throw new XmlException("XML suggests type " + writtenTypeName + " that is not assignable to expected type " + objType.Name,
                    null, reader.LineNumber(), reader.LinePosition());
            return writtenType;
        }

        private static object DeserializeXmlOther(XmlReader reader, Type limitationAttribute, Type writtenType, bool tolerant)
        {
            var returnValue = Serialization.CreateInstance(writtenType);
            DeserializeFieldsAndPropertiesXml(reader, returnValue, limitationAttribute, tolerant);
            return returnValue;
        }

        private static object DeserializeXmlEnum(XmlReader reader, Type writtenType)
        {
            string enumStr = reader.ReadContentAsString();
            return Enum.Parse(writtenType, enumStr);
        }

        private static object DeserializeXmlColor(XmlReader reader, Type limitationAttribute, bool tolerant)
        {
            byte r = (byte)DeserializeXml(reader, "R", typeof(byte), limitationAttribute, tolerant);
            byte g = (byte)DeserializeXml(reader, "G", typeof(byte), limitationAttribute, tolerant);
            byte b = (byte)DeserializeXml(reader, "B", typeof(byte), limitationAttribute, tolerant);
            byte a = (byte)DeserializeXml(reader, "A", typeof(byte), limitationAttribute, tolerant);
            return new Color(r, g, b, a);
        }

        private static object DeserializeXmlCurve(XmlReader reader, Type limitationAttribute, bool tolerant)
        {
            var curve = new Curve();
            curve.PostLoop = curve.PreLoop = CurveLoopType.Constant;
            foreach (var keyLine in reader.ReadContentAsString().Split('\n'))
            {
                if (keyLine.Trim() == "") continue;
                var errorMessage = "Invalid curve key '" + keyLine + "'";
                var errorElementName = "[text content]";
                var match = Regex.Match(keyLine, @"\s*(\S+)\s*(\S+)\s*(\S+)\s*(\S+)\s*(\S+)\s*");
                if (!match.Success) throw new MemberSerializationException(errorMessage, errorElementName);
                float position = 0, value = 0, tangentIn = 0, tangentOut = 0;
                CurveContinuity continuity = 0;
                var success =
                    float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out position) &&
                    float.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                    float.TryParse(match.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out tangentIn) &&
                    float.TryParse(match.Groups[4].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out tangentOut) &&
                    Enum.TryParse<CurveContinuity>(match.Groups[5].Value, out continuity);
                if (!success) throw new MemberSerializationException(errorMessage, errorElementName);
                curve.Keys.Add(new CurveKey(position, value, tangentIn, tangentOut, continuity));
            }
            return curve;
        }

        private static object DeserializeXmlIEnumerable(XmlReader reader, Type objType, Type limitationAttribute, Type writtenType, bool tolerant)
        {
            // Read as 'objType' so that the possibly needed cast to 'writtenType'
            // can be done nicely on the element type and not on the IEnumerable type.
            Type elementType = GetIEnumerableElementType(objType);
            Type listType = typeof(List<>).MakeGenericType(elementType);
            IList array = (IList)Activator.CreateInstance(listType);
            while (reader.IsStartElement("Item"))
            {
                object item = DeserializeXml(reader, "Item", elementType, limitationAttribute, tolerant);
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

        public static string GetSerializedName(FieldOrPropertyInfo member)
        {
            var serializedNameAttribute = (SerializedNameAttribute)Attribute.GetCustomAttribute(member.MemberInfo, typeof(SerializedNameAttribute));
            var name = new StringBuilder(serializedNameAttribute != null ? serializedNameAttribute.SerializedName : member.Name);
            if (name[0] == '_') name.Remove(0, 1);
            name[0] = char.ToLower(name[0]);
            return name.ToString();
        }

        /// <summary>
        /// Returns all fields of the type except for the fields that have <paramref name="exclusionAttribute"/> attached.
        /// </summary>
        public static IEnumerable<FieldInfo> GetFields(Type objType, Type exclusionAttribute)
        {
            var key = new TypeTuple(objType, exclusionAttribute);
            IEnumerable<FieldInfo> result;
            if (g_typeFields.TryGetValue(key, out result)) return result;
            result = GetFieldsCore(objType, exclusionAttribute);
            g_typeFields.Add(key, result);
            return result;
        }

        /// <summary>
        /// Returns the serialisable members of a type.
        /// If <paramref name="limitationAttribute"/> is not null and the type has <paramref name="LimitedSerializationAttribute"/>,
        /// then all public and non-public members that have the limitation attribute are returned.
        /// Otherwise only public members are returned.
        /// The search includes members declared in the type and all its base types.
        /// Members with <paramref name="exclusionAttribute"/> are excluded, unless the parameter is null.
        /// </summary>
        public static IEnumerable<FieldOrPropertyInfo> GetFieldsAndProperties(Type objType, Type limitationAttribute, Type exclusionAttribute)
        {
            var key = new TypeTriple(objType, limitationAttribute, exclusionAttribute);
            IEnumerable<FieldOrPropertyInfo> result;
            if (g_typeFieldsAndProperties.TryGetValue(key, out result)) return result;
            result = Attribute.IsDefined(objType, typeof(LimitedSerializationAttribute), true)
                ? GetFieldsAndPropertiesCore(objType, limitationAttribute, exclusionAttribute)
                : GetFieldsAndPropertiesCore(objType, null, exclusionAttribute);
            g_typeFieldsAndProperties.Add(key, result);
            return result;
        }

        /// <summary>
        /// Returns declared instance fields and properties of the given object.
        /// The result doesn't include members declared in any base types.
        /// If <paramref name="limitationAttribute"/> is null, only public members are returned.
        /// Otherwise all public and non-public members with the limitation attribute are returned.
        /// If <paramref name="exclusionAttribute"/> is not null, only members without the exclusion
        /// attribute are returned.
        /// </summary>
        public static IEnumerable<FieldOrPropertyInfo> GetDeclaredFieldsAndProperties(Type type, Type limitationAttribute, Type exclusionAttribute)
        {
            var flags = limitationAttribute == null
                ? BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public
                : BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;
            return
                from member in type.GetFields(flags).Cast<MemberInfo>().Union(type.GetProperties(flags))
                where (limitationAttribute == null || member.IsDefined(limitationAttribute, false)) &&
                      (exclusionAttribute == null || !member.IsDefined(exclusionAttribute, false))
                select member is FieldInfo
                    ? new FieldOrPropertyInfo((FieldInfo)member)
                    : new FieldOrPropertyInfo((PropertyInfo)member);
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
            if (Array.Exists(type.GetInterfaces(), delegate (Type iface)
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

            // Other class instances are copied member by member.
            var copy = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
            var members = GetFields(type, typeof(ExcludeFromDeepCopyAttribute));
            foreach (var member in members)
            {

                if (member.IsDefined(typeof(ShallowCopyAttribute), false))
                    member.SetValue(copy, member.GetValue(obj));
                else
                    member.SetValue(copy, DeepCopy(member.GetValue(obj)));
            }
            return copy;
        }

        public static bool DeepEquals(object a, object b)
        {
            if (a == null || b == null) return a == null && b == null;
            var type = a.GetType();
            if (type != b.GetType()) return false;
            if (type.IsValueType) return a.Equals(b);
            if (type == typeof(string)) return a == b;
            if (type.GetInterfaces().Contains(typeof(IEnumerable)))
            {
                var enumerableA = (IEnumerable)a;
                var enumerableB = (IEnumerable)b;
                var enumA = enumerableA.GetEnumerator();
                var enumB = enumerableB.GetEnumerator();
                while (enumA.MoveNext())
                {
                    if (!enumB.MoveNext()) return false; // B has too few elements
                    if (!DeepEquals(enumA.Current, enumB.Current)) return false; // elements in A and B differ
                }
                if (enumB.MoveNext()) return false; // B has too many elements
                return true;
            }
            // The remaining cases are non-enumerable reference types; compare them member by member.
            var members = GetFields(type, typeof(ExcludeFromDeepCopyAttribute));
            foreach (var member in members)
            {
                var valueA = member.GetValue(a);
                var valueB = member.GetValue(b);
                if (member.IsDefined(typeof(ShallowCopyAttribute), false) && valueA != valueB) return false;
                else if (!DeepEquals(valueA, valueB)) return false;
            }
            return true;
        }

        #endregion // public methods

        #region private methods

        /// <summary>
        /// Writes out fields and properties of an object into an XML writer.
        /// If the type of the object or one of its ancestors has <see cref="LimitedSerializationAttribute"/>
        /// then serialisation is limited to members that have the specified limitation attribute.
        /// </summary>
        /// <param name="writer">Where to write the serialised values.</param>
        /// <param name="obj">The object whose members to serialise.</param>
        /// <param name="limitationAttribute">Limit the serialisation to members with this attribute,
        /// or serialise all members if limitationAttribute is a null reference.</param>
        private static void SerializeMembersXml(XmlWriter writer, object obj, Type limitationAttribute)
        {
            var members = GetFieldsAndProperties(obj.GetType(), limitationAttribute, null);
            foreach (var member in members)
                SerializeXml(writer,
                    GetSerializedName(member),
                    member.GetValue(obj),
                    GetLimitationAttribute(member, limitationAttribute),
                    GetSerializedType(member.ValueType));
        }

        /// <summary>
        /// Reads in fields and properties of an object from an XML reader.
        /// </summary>
        /// <remarks>
        /// <para>
        /// You can limit the deserialisation to members that have the specified limitation attribute.
        /// This limitation is applied only to instances of classes that have 
        /// LimitedSerializationAttribute.
        ///</para>
        ///<para>
        /// The XML reader is assumed to be positioned on the first child element of 
        /// the root element of the serialised 'obj' to be read. At successful return, 
        /// the XML reader will be positioned at the end element of the serialised 'obj'.
        /// A log message is produced if an unknown serialised member is encountered.
        /// It is guaranteed that all fields and properties (marked with <paramref name="limitationAttribute"/>
        /// get a value exactly once, lest an exception is thrown.
        /// </para>
        /// </remarks>
        /// <param name="reader">Where to read the serialised values.</param>
        /// <param name="obj">The object whose members to deserialise.</param>
        /// <param name="limitationAttribute">Limit the deserialisation to members with this attribute,
        /// or deserialise all members if limitationAttribute is a null reference.</param>
        /// <seealso cref="AW2.Helpers.LimitedSerializationAttribute"/>
        private static void DeserializeFieldsAndPropertiesXml(XmlReader reader, object obj, Type limitationAttribute, bool tolerant)
        {
            try
            {
                var type = obj.GetType();
                var finder = new FieldAndPropertyFinder(obj.GetType(), limitationAttribute, tolerant);
                // NOTE: There is no guarantee about the order of the members.
                while (reader.IsStartElement())
                {
                    var member = finder.Find(reader.Name);
                    if (member == null)
                    {
                        reader.Skip();
                        continue;
                    }
                    var memberLimitationAttribute = GetLimitationAttribute(member, limitationAttribute);
                    var value = DeserializeXml(reader, reader.Name, member.ValueType, memberLimitationAttribute, tolerant);
                    member.SetValue(obj, value);
                }
                finder.CheckForMissing();
            }
            catch (MemberSerializationException e)
            {
                e.LineNumber = reader.LineNumber();
                throw;
            }
        }

        private static Type GetLimitationAttribute(FieldOrPropertyInfo member, Type limitationAttribute)
        {
            var limitationSwitchAttribute = (LimitationSwitchAttribute)Attribute.GetCustomAttribute(member.MemberInfo, typeof(LimitationSwitchAttribute));
            if (limitationSwitchAttribute != null && limitationSwitchAttribute.From == limitationAttribute)
                return limitationSwitchAttribute.To;
            return limitationAttribute;
        }

        private static int LineNumber(this XmlReader reader)
        {
            var lineInfo = reader as IXmlLineInfo;
            return lineInfo != null ? lineInfo.LineNumber : 0;
        }

        private static int LinePosition(this XmlReader reader)
        {
            var lineInfo = reader as IXmlLineInfo;
            return lineInfo != null ? lineInfo.LinePosition : 0;
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
            if (type.IsValueType) return Activator.CreateInstance(type);

            // System.String has no parameterless constructor so we provide a default value.
            if (type == typeof(string)) return "";

            // Otherwise 'type' refers to a class. Find its constructors.
            var constructors = type.GetConstructors();
            if (constructors.Length == 0) throw new ApplicationException("No constructors found for type " + type.ToString());

            // Call a constructor with a minimal number of parameters.
            Array.Sort(constructors, (cons1, cons2) =>
                cons1.GetParameters().Length.CompareTo(cons2.GetParameters().Length));
            var paramTypes = constructors[0].GetParameters();
            var parameters = new List<object>();
            foreach (var paramType in paramTypes)
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
            var objType = obj.GetType();
            if (type.IsAssignableFrom(objType)) return obj;

            // Look for cast operator in 'objType'
            var castMethodInfo = GetCastMethod(objType, type);
            if (castMethodInfo != null)
                return castMethodInfo.Invoke(null, new object[] { obj });

            // Fallback for IEnumerable: convert item by item
            var lElementType = GetIEnumerableElementType(type);
            var rElementType = GetIEnumerableElementType(objType);
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

        private static Type GetSerializedType(Type type)
        {
            var serializedTypeAttribute = (SerializedTypeAttribute)Attribute.GetCustomAttribute(type, typeof(SerializedTypeAttribute));
            return serializedTypeAttribute != null
                ? serializedTypeAttribute.SerializedType
                : type;
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

        private static IEnumerable<FieldInfo> GetFieldsCore(Type objType, Type exclusionAttribute)
        {
            var result = GetDeclaredFields(objType, exclusionAttribute);
            for (var type = objType.BaseType; type != null; type = type.BaseType)
                result = result.Concat(GetDeclaredFields(type, exclusionAttribute));
            return result;
        }

        private static IEnumerable<FieldOrPropertyInfo> GetFieldsAndPropertiesCore(Type objType, Type limitationAttribute, Type exclusionAttribute)
        {
            var result = GetDeclaredFieldsAndProperties(objType, limitationAttribute, exclusionAttribute);
            for (var type = objType.BaseType; type != null; type = type.BaseType)
                result = result.Concat(GetDeclaredFieldsAndProperties(type, limitationAttribute, exclusionAttribute));
            return result;
        }

        private static IEnumerable<FieldInfo> GetDeclaredFields(Type type, Type exclusionAttribute)
        {
            var flags = BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;
            return type.GetFields(flags).Where(field => !field.IsDefined(exclusionAttribute, false));
        }

        #endregion // private methods
    }
}
