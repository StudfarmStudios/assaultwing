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

namespace AW2.Graphics.Content
{
    /// <summary>
    /// Content manager specifically for Assault Wing.
    /// </summary>
    public class AWContentManager : ContentManager
    {
        private Dictionary<string, object> _loadedContent = new Dictionary<string, object>();
        private Dictionary<string, object> _loadedModelSkeletons = new Dictionary<string, object>();
        private bool _ignoreGraphicsContent;

        public AWContentManager(IServiceProvider serviceProvider)
            : base(serviceProvider, ".\\")
        {
            _ignoreGraphicsContent = serviceProvider.GetService(typeof(IGraphicsDeviceService)) == null;
        }

        public bool Exists<T>(string assetName)
        {
            return File.Exists(GetAssetFilename<T>(assetName));
        }

        /// <summary>
        /// Loads an asset that has been processed by the Content Pipeline.
        /// Repeated calls to load the same asset will return the same object instance.
        /// </summary>
        public override T Load<T>(string assetName)
        {
            if (assetName == null) throw new ArgumentNullException("assetName");
            object item;
            if (typeof(T) == typeof(ModelGeometry))
            {
                if (_loadedModelSkeletons.TryGetValue(assetName, out item)) return (T)item;
                var assetFilename = GetAssetFilename<T>(assetName);
                item = XNBReader.Read<T>(assetFilename);
                _loadedModelSkeletons.Add(assetName, item);
            }
            else
            {
                if (_loadedContent.TryGetValue(assetName, out item)) return (T)item;
                if (_ignoreGraphicsContent) return default(T);
                var assetFullName = GetAssetFullName<T>(assetName);
                item = ReadAsset<T>(assetFullName, null);
                _loadedContent.Add(assetName, item);
            }
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
                // Skip texture names that are part of 3D models.
                if (!IsModelTextureFilename(filename)) yield return Path.GetFileNameWithoutExtension(filename);
            }
        }

        public void LoadAllGraphicsContent()
        {
            foreach (var filename in Directory.GetFiles(Paths.MODELS, "*.xnb"))
                if (!IsModelTextureFilename(filename)) Load<Model>(Path.GetFileNameWithoutExtension(filename));
            foreach (var filename in Directory.GetFiles(Paths.TEXTURES, "*.xnb"))
                Load<Texture2D>(Path.GetFileNameWithoutExtension(filename));
            foreach (var filename in Directory.GetFiles(Paths.FONTS, "*.xnb"))
                Load<SpriteFont>(Path.GetFileNameWithoutExtension(filename));
            foreach (var filename in Directory.GetFiles(Paths.SHADERS, "*.xnb"))
                Load<Effect>(Path.GetFileNameWithoutExtension(filename));
        }

        private static string GetAssetFilename<T>(string assetName)
        {
            return Path.ChangeExtension(GetAssetFullName<T>(assetName), ".xnb");
        }

        private bool IsModelTextureFilename(string filename)
        {
            // Note: This works only if texture asset names never end in "_0".
            // The foolproof way to do the skipping is to skip the asset names
            // mentioned inside other asset files.
            var match = Regex.Match(filename, @"^.*[\\/][^\\/]*?(_0)?\.xnb$", RegexOptions.IgnoreCase);
            return match.Groups[1].Length != 0;
        }

        private static string GetAssetFullName<T>(string assetName)
        {
            return assetName.Contains(@"\")
                ? assetName
                : Path.Combine(GetAssetPath(assetName, typeof(T)), assetName);
        }

        private static string GetAssetPath(string assetName, Type type)
        {
            if (typeof(Texture).IsAssignableFrom(type)) return Paths.TEXTURES;
            else if (type == typeof(Model) || type == typeof(ModelGeometry)) return Paths.MODELS;
            else if (type == typeof(SpriteFont)) return Paths.FONTS;
            else if (type == typeof(Effect)) return Paths.SHADERS;
            else if (type == typeof(Song) || type == typeof(SoundEffect))
            {
                // Hack!
                // Everything which ends with 2 digits + extension is a sound
                var chars = assetName.ToCharArray(assetName.Length - 2, 2);
                if (Char.IsNumber(chars[0]) && Char.IsNumber(chars[1]))
                    return Paths.SOUNDS;
                return Paths.MUSIC;
            }
            //else if (type == typeof(Video)) return Paths.VIDEO;
            throw new ArgumentException("Cannot load content of unexpected type " + type.Name);
        }
    }
}
