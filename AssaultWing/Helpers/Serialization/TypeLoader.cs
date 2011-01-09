using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using AW2.Helpers;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Handles loading and saving template instances that represent user-defined types
    /// of some class hierarchy such as Gob and its subclasses or Weapon and its subclasses.
    /// </summary>
    public class TypeLoader
    {
        private DirectoryInfo _definitionDir;
        private Type _baseClass;

        /// <summary>
        /// Suffix for producing a type definition template file from a type name.
        /// </summary>
        private const string TEMPLATE_FILENAME_SUFFIX = "template";

        /// <summary>
        /// Creates a type loader.
        /// </summary>
        /// <param name="baseClass">Base class of the class hierarchy in which the user-defined types reside.</param>
        /// <param name="definitionDir">Name of the directory where the XML-form type definitions are.</param>
        /// <seealso cref="AW2.Helpers.Paths"/>
        public TypeLoader(Type baseClass, string definitionDir)
        {
            _baseClass = baseClass;
            _definitionDir = new DirectoryInfo(definitionDir);
            _definitionDir.Create(); // does nothing if the dir exists already
        }

        #region Public interface

        /// <summary>
        /// Loads a type template from file, or null on error.
        /// </summary>
        public static object LoadTemplate(string filename, Type baseClass, Type limitationAttribute)
        {
            var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            var xmlReader = Serialization.GetXmlReader(fs);
            object template = null;
            try
            {
                template = Serialization.DeserializeXml(xmlReader, baseClass.Name + "Type",
                    baseClass, limitationAttribute);
            }
            catch (MemberSerializationException e)
            {
                Log.Write("Error in {0} line {1}: {2}, {3}", filename, e.LineNumber, e.Message, e.MemberName);
                return null;
            }
            catch (Exception e)
            {
                Log.Write("Error in " + filename + ": " + e.Message);
                return null;
            }
            finally
            {
                xmlReader.Close();
                fs.Close();
            }
            return template;
        }

        public static void SaveTemplate(object template, string filename, Type baseClass, Type limitationAttribute)
        {
            var writer = new StreamWriter(filename);
            var xmlWriter = Serialization.GetXmlWriter(writer);
            Serialization.SerializeXml(xmlWriter, baseClass.Name + "Type", template, limitationAttribute, baseClass);
            xmlWriter.Close();
        }

        public static string GetFilename(object template, string templateName)
        {
            var safeTemplateName = Regex.Replace(templateName, "[^a-z]", "_", RegexOptions.IgnoreCase);
            return string.Format("{0}_{1}.xml", template.GetType().Name, safeTemplateName);
        }

        public IEnumerable<string> GetTemplateFilenames()
        {
            // Note: DirectoryInfo.GetFiles("*.xml") also includes suffixes
            // that extend "xml", for example "blabla.xml~".
            return
                from fileInfo in _definitionDir.GetFiles("*.xml")
                where IsTemplateFile(fileInfo)
                select fileInfo.FullName;
        }

        public IEnumerable<object> LoadTemplates()
        {
            var templates = GetTemplateFilenames().Select(filename => LoadTemplate(filename));
            templates = templates.ToList(); // immediate evaluation
            if (templates.Contains(null)) throw new ApplicationException("Error: Some templates failed to load. Previous log entries contain the details.");
            return templates;
        }

        /// <summary>
        /// Creates one example file for each template type.
        /// </summary>
        public void SaveTemplateExamples()
        {
            foreach (var type in GetTemplateTypes())
            {
                var instance = Activator.CreateInstance(type);
                SaveTemplate(instance, typeof(TypeParameterAttribute), TEMPLATE_FILENAME_SUFFIX);
            }
        }

        /// <summary>
        /// Deletes files that look like they were created by <see cref="SaveTemplates"/>.
        /// </summary>
        public void DeleteTemplates()
        {
            foreach (var filename in Directory.GetFiles(_definitionDir.FullName))
                if (filename.EndsWith("_" + TEMPLATE_FILENAME_SUFFIX + ".xml"))
                {
                    try
                    {
                        File.Delete(filename);
                    }
                    catch (IOException)
                    {
                        // The file is in use, tough luck.
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // We don't have permission or the file is read-only, tough luck.
                    }
                }
        }

        public void SaveTemplate(object template, Type limitationAttribute, string templateName)
        {
            var filename = GetFilename(template, templateName);
            string path = System.IO.Path.Combine(_definitionDir.FullName, filename);
            SaveTemplate(template, path, _baseClass, limitationAttribute);
        }

        #endregion

        #region Nonpublic parts

        private IEnumerable<Type> GetTemplateTypes()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => !t.IsAbstract && _baseClass.IsAssignableFrom(t));
        }

        private bool IsTemplateFile(FileInfo f)
        {
            if (!f.Extension.Equals(".xml", StringComparison.CurrentCultureIgnoreCase))
                return false;
            if (f.Name.EndsWith(TEMPLATE_FILENAME_SUFFIX, StringComparison.CurrentCultureIgnoreCase))
                return false;
            return true;
        }

        protected virtual object LoadTemplate(string filename)
        {
            return LoadTemplate(filename, _baseClass, typeof(TypeParameterAttribute));
        }

        #endregion
    }
}
