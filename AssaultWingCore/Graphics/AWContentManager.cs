using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using AW2.Helpers;

namespace AW2.Graphics
{
    /// <summary>
    /// Content manager specifically for Assault Wing.
    /// </summary>
    public class AWContentManager : ContentManager
    {
        private IDictionary<string, object> _loadedContent = new Dictionary<string, object>();

        public AWContentManager(IServiceProvider serviceProvider)
            : base(serviceProvider, ".\\")
        { }

        public bool Exists<T>(string assetName)
        {
            return File.Exists(GetAssetFullName<T>(assetName) + ".xnb");
        }

        /// <summary>
        /// Loads an asset that has been processed by the Content Pipeline.
        /// </summary>
        /// Repeated calls to load the same asset will return the same object instance.
        public override T Load<T>(string assetName)
        {
            if (assetName == null) throw new ArgumentNullException("assetName");
            var assetFullName = GetAssetFullName<T>(assetName);
            object item;
            if (_loadedContent.TryGetValue(assetFullName, out item)) return (T)item;
            item = ReadAsset<T>(assetFullName, null);
            _loadedContent.Add(assetFullName, item);
            return (T)item;
        }

        /// <summary>
        /// Returns the names of all assets. Only XNB files are considered
        /// as containing assets. Unprocessed XML files are ignored.
        /// </summary>
        public IEnumerable<string> GetAssetNames()
        {
            foreach (var filename in Directory.GetFiles(RootDirectory, "*.xnb", SearchOption.AllDirectories))
            {
                // We skip texture names that are part of 3D models.
                // Note: This works only if texture asset names never end in "_0".
                // The foolproof way to do the skipping is to skip the asset names
                // mentioned inside other asset files.
                var match = Regex.Match(filename, @"^.*[\\/]([^\\/]*?)(_0)?\.xnb$", RegexOptions.IgnoreCase);
                if (match.Groups[2].Length == 0) yield return match.Groups[1].Value;
            }
        }

        private static string GetAssetFullName<T>(string assetName)
        {
            string assetFullName;
            if (assetName.Contains(@"\"))
                assetFullName = assetName;
            else
            {
                string assetPath = null;
                var type = typeof(T);
                if (typeof(Texture).IsAssignableFrom(type)) assetPath = Paths.TEXTURES;
                else if (type == typeof(Model)) assetPath = Paths.MODELS;
                else if (type == typeof(SpriteFont)) assetPath = Paths.FONTS;
                else if (type == typeof(Effect)) assetPath = Paths.SHADERS;
                else if (type == typeof(Song) || type == typeof(SoundEffect))
                {
                    // Hack!
                    // Everything which ends with 2 digits + extension is a sound
                    char[] chars = assetName.ToCharArray(assetName.Length - 2, 2);

                    if (Char.IsNumber(chars[0]) && Char.IsNumber(chars[1]))
                    {
                        assetPath = Paths.SOUNDS;
                    }
                    else
                    {
                        assetPath = Paths.MUSIC;
                    }
                }
                else if (type == typeof(Video)) assetPath = Paths.VIDEO;
                else throw new ArgumentException("Cannot load content of unexpected type " + type.Name);
                assetFullName = Path.Combine(assetPath, assetName);
            }
            return assetFullName;
        }
    }
}
