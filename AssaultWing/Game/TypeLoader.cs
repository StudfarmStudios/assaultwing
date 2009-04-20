//#define TYPELOADER_DEBUG // print diagnostic messages for type loader
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AW2.Helpers;

namespace AW2.Game
{
    /// <summary>
    /// Handles loading and saving template instances that represent user-defined types
    /// of some class hierarchy such as Gob and its subclasses or Weapon and its subclasses.
    /// </summary>
    class TypeLoader
    {
        DirectoryInfo definitionDir;
        Type baseClass;

        /// <summary>
        /// Suffix for producing a type definition template file from a type name.
        /// </summary>
        const string typeDefinitionTemplateSuffix = "_template.xml";

        /// <summary>
        /// Creates a type loader.
        /// </summary>
        /// <param name="baseClass">Base class of the class hierarchy in which the user-defined types reside.</param>
        /// <param name="definitionDir">Name of the directory where the XML-form type definitions are.</param>
        /// <seealso cref="AW2.Helpers.Paths"/>
        public TypeLoader(Type baseClass, string definitionDir)
        {
            this.baseClass = baseClass;
            this.definitionDir = new DirectoryInfo(definitionDir);
#if TYPELOADER_DEBUG
            Log.Write("Checking directory " + this.definitionDir.FullName);
#endif
            this.definitionDir.Create(); // does nothing if the dir exists already
        }

        #region Public interface

        /// <summary>
        /// Loads and returns all user-defined types.
        /// </summary>
        /// <returns>The loaded types.</returns>
        public object[] LoadAllTypes()
        {
            return ParseAndLoad(FindTypeDefinitions());
        }

        /// <summary>
        /// Loads and returns specified user-defined types.
        /// </summary>
        /// <returns>The loaded types.</returns>
        public object LoadSpecifiedTypes(String fileName)
        {
#if TYPELOADER_DEBUG
            Log.Write("Loading type definition from file " + fileName);
#endif
            FileInfo[] fil = definitionDir.GetFiles(fileName);
            if (fil.Length == 1)
            {
                return ParseAndLoadFile(fil[0]);
            }
            else
                throw new InvalidFilterCriteriaException("Ambiguous file name: " + fileName);
        }
        
        /// <summary>
        /// Creates type file templates for each subclass of 'baseClass'.
        /// </summary>
        public void SaveTemplates()
        {
            foreach (Type type in Array.FindAll<Type>(Assembly.GetExecutingAssembly().GetTypes(),
                t => !t.IsAbstract && baseClass.IsAssignableFrom(t)))
            {
#if TYPELOADER_DEBUG
                Log.Write("Saving template for " + type.Name);
#endif
                string filename = System.IO.Path.Combine(definitionDir.FullName,
                    type.Name + typeDefinitionTemplateSuffix);
                TextWriter writer = new StreamWriter(filename);
                System.Xml.XmlWriter xmlWriter = Serialization.GetXmlWriter(writer);
                object instance = Activator.CreateInstance(type);
                Type limitationAttribute = typeof(TypeParameterAttribute);
                Serialization.SerializeXml(xmlWriter, baseClass.Name + "Type", instance, limitationAttribute);
                xmlWriter.Close();
            }
        }

#endregion

        #region Nonpublic parts

        /// <summary>
        /// Returns the list of type definition files.
        /// </summary>
        /// <returns>The list of type definition files.</returns>
        private List<FileInfo> FindTypeDefinitions()
        {
            List<FileInfo> defList = new List<FileInfo>();
            foreach (FileInfo f in definitionDir.GetFiles("*.xml"))
            {
                // Note: DirectoryInfo.GetFiles("*.xml") also includes suffixes
                // that extend "xml", for example "blabla.xml~".
                if (IsTypeDefinition(f))
                    defList.Add(f);
            }
            return defList;
        }

        /// <summary>
        /// Returns <c>true</c> if and only if a file is an actual type definition.
        /// </summary>
        private bool IsTypeDefinition(FileInfo f)
        {
            if (!f.Extension.Equals(".xml", StringComparison.CurrentCultureIgnoreCase))
                return false;
            if (f.Name.EndsWith(typeDefinitionTemplateSuffix, StringComparison.CurrentCultureIgnoreCase))
                return false;
#if TYPELOADER_DEBUG
            long size = f.Length;
            DateTime creationTime = f.CreationTime;
            Log.Write("Found " + baseClass.Name + " definition " + name + " " + size + "B " + creationTime);
#endif
            return true;
        }

        /// <summary>
        /// Creates type file templates for each subclass of 'baseClass'.
        /// </summary>
        private object[] ParseAndLoad(List<FileInfo> list)
        {
            Type listType = typeof(List<>).MakeGenericType(baseClass);
            IList types = (IList)Activator.CreateInstance(listType);
            foreach (FileInfo fi in list)
            {
                types.Add(ParseAndLoadFile(fi));
            }
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;
            return (object[])listType.InvokeMember("ToArray", flags, null, types, new Object[] { });
        }

        /// <summary>
        /// Loads a type template from a file.
        /// </summary>
        /// <param name="fi">The file to load from.</param>
        /// <returns>The loaded type template.</returns>
        protected virtual object ParseAndLoadFile(FileInfo fi)
        {
#if TYPELOADER_DEBUG
            Log.Write("Loading " + baseClass.Name + " template from " + fi);
#endif
            FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read);
            System.Xml.XmlReader xmlReader = Serialization.GetXmlReader(fs);
            Type limitationAttribute = typeof(TypeParameterAttribute);
            object template = null;
            try
            {
                template = Serialization.DeserializeXml(xmlReader, baseClass.Name + "Type",
                    baseClass, limitationAttribute);
            }
#if TYPELOADER_DEBUG
            catch (MemberSerializationException e)
            {
                //throw new ArgumentException("Error in " + fi.Name + ": " + e.Message + ", " + e.MemberName);
                Log.Write("Error in " + fi.Name + ": " + e.Message + ", " + e.MemberName);
            }
#else
            catch (MemberSerializationException) { }
#endif
            xmlReader.Close();
            fs.Close();
            return template;
        }

        #endregion
    }
}
