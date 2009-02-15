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
                
                if(FindTypeDefinition(f)!=null)
                    defList.Add(f);
            }
            return defList;
        }

        private FileInfo FindTypeDefinition(FileInfo f)
        {
            String name = f.Name;
            if (!name.EndsWith(".xml", StringComparison.CurrentCultureIgnoreCase))
                return null;
            long size = f.Length;
            DateTime creationTime = f.CreationTime;
            Log.Write("Found " + baseClass.Name + " definition " + name + " " + size + "B " + creationTime);
            return f;
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
        /// Loads and returns specified user-defined types.
        /// </summary>
        /// <returns>The loaded types.</returns>
        public object LoadSpecifiedTypes(String fileName)
        {
            Log.Write("fileName:" + fileName);
            FileInfo[] fil =definitionDir.GetFiles(fileName);
            if (fil.Length == 1)
            {
                SaveTemplates();
                return ParseAndLoadFile(FindTypeDefinition(fil[0]));
            }
            else
                throw new InvalidFilterCriteriaException("File name was incorrect");
        }

        /// <summary>
        /// Creates type file templates for each subclass of 'baseClass'.
        /// </summary>
        private void SaveTemplates()
        {
            foreach (Type type in Array.FindAll<Type>(Assembly.GetExecutingAssembly().GetTypes(),
                t => !t.IsAbstract && baseClass.IsAssignableFrom(t)))
            {
                Console.WriteLine("Saving template for " + type.Name);
                string filename = System.IO.Path.Combine(definitionDir.Name,
                    type.Name + "_template.xml");
                TextWriter writer = new StreamWriter(filename);
                System.Xml.XmlWriter xmlWriter = Serialization.GetXmlWriter(writer);
                object instance = Activator.CreateInstance(type);
                Type limitationAttribute = typeof(TypeParameterAttribute);
                Serialization.SerializeXml(xmlWriter, baseClass.Name + "Type", instance, limitationAttribute);
                xmlWriter.Close();
            }
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
        private object ParseAndLoadFile(FileInfo fi)
        {
            Log.Write("Loading " + baseClass.Name + " template from " + fi);
            FileStream fs = new FileStream(fi.FullName, FileMode.Open);
            System.Xml.XmlReader xmlReader = Serialization.GetXmlReader(fs);
            Type limitationAttribute = typeof(TypeParameterAttribute);
            object template = null;
            try
            {
                template = Serialization.DeserializeXml(xmlReader, baseClass.Name + "Type", 
                    baseClass, limitationAttribute);
            }
            catch (MemberSerializationException e)
            {
                //throw new ArgumentException("Error in " + fi.Name + ": " + e.Message + ", " + e.MemberName);
                Log.Write("Error in " + fi.Name + ": " + e.Message + ", " + e.MemberName);
            }
            xmlReader.Close();
            fs.Close();
            return template;
        }
    }
}
