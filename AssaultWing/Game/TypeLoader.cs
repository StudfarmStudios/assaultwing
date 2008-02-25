using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AW2.Helpers;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;

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
        /// Creates a type loader.
        /// </summary>
        /// <param name="baseClass">Base class of the class hierarchy in which the user-defined types reside.</param>
        /// <param name="definitionDir">Name of the directory where the XML-form type definitions are.</param>
        public TypeLoader(Type baseClass, string definitionDir)
        {
            this.baseClass = baseClass;
            this.definitionDir = new DirectoryInfo(definitionDir);
            Log.Write("Checking directory " + this.definitionDir.FullName);
            this.definitionDir.Create(); // does nothing if the dir exists already
        }

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
                String name = f. Name;
                if (!name.EndsWith(".xml", StringComparison.CurrentCultureIgnoreCase))
                    continue;
                long size = f.Length;
                DateTime creationTime = f.CreationTime;
                Log.Write("Found " + baseClass.Name + " definition " + name + " " + size + "B " + creationTime);

                defList.Add(f);
            }
            return defList;
        }

        /// <summary>
        /// Returns the name of a subclass of 'baseClass' based on the name of
        /// the type definition file.
        /// </summary>
        /// <param name="fi">The type definition file.</param>
        /// <returns>The name of the subclass of 'baseClass'.</returns>
        private string parseSubclass(FileInfo fi)
        {
            string className = fi.Name.Split('#','.')[0];
            return className;
        }

        /// <summary>
        /// Returns the name of a user-defined type based on the name of 
        /// the type definition file.
        /// </summary>
        /// <param name="fi">The type definition file.</param>
        /// <returns>The name of the user-defined type.</returns>
        private string parseType(FileInfo fi)
        {
            string typeName = fi.Name.Split('#', '.')[1];
            return typeName;
        }

        /// <summary>
        /// Loads and returns all user-defined types.
        /// </summary>
        /// <returns>The loaded types.</returns>
        public object[] LoadAllTypes()
        {
            SaveTemplates();
            return ParseAndLoad(FindTypeDefinitions());
        }

        /// <summary>
        /// Creates type file templates for each subclass of 'baseClass'.
        /// </summary>
        private void SaveTemplates()
        {
            foreach (Type type in Array.FindAll<Type>(Assembly.GetExecutingAssembly().GetTypes(),
                delegate(Type t) { return !t.IsAbstract && baseClass.IsAssignableFrom(t); }))
            {
                Console.WriteLine("Saving template for " + type.Name);
                string filename = System.IO.Path.Combine(definitionDir.Name,
                    type.Name + "#example_" + type.Name + "_template.xml");
                TextWriter writer = new StreamWriter(filename);
                System.Xml.XmlWriter xmlWriter = Serialization.GetXmlWriter(writer);
                object instance = Activator.CreateInstance(type);
                Type limitationAttribute = typeof(TypeParameterAttribute);
                Serialization.SerializeXml(xmlWriter, baseClass.Name + "Type", instance, limitationAttribute);
                xmlWriter.Close();
            }
        }

        private object[] ParseAndLoad(List<FileInfo> list)
        {
            Type listType = typeof(List<>).MakeGenericType(baseClass);
            IList types = (IList)Activator.CreateInstance(listType);
            foreach (FileInfo fi in list)
            {
                Log.Write("Loading type " + parseType(fi) + " of subclass " + parseSubclass(fi));
                FileStream fs = new FileStream(fi.FullName, FileMode.Open);
                System.Xml.XmlReader xmlReader = Serialization.GetXmlReader(fs);
                Type limitationAttribute = typeof(TypeParameterAttribute);
                object template = Serialization.DeserializeXml(xmlReader, baseClass.Name + "Type", 
                    baseClass, limitationAttribute);
                xmlReader.Close();
                types.Add(template);
            }
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;
            return (object[])listType.InvokeMember("ToArray", flags, null, types, new Object[] { });
        }
    }
}
