using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Helpers.Collections;

namespace AW2.Graphics
{
    /// <summary>
    /// Content manager specifically for Assault Wing.
    /// </summary>
    public class AWContentManager : ContentManager
    {
        IDictionary<string, object> loadedContent = new Dictionary<string, object>();

        /// <summary>
        /// Creates a new content manager for Assault Wing.
        /// </summary>
        public AWContentManager(IServiceProvider serviceProvider)
            : base(serviceProvider)
        { }

        /// <summary>
        /// Loads an asset that has been processed by the Content Pipeline.
        /// </summary>
        /// Repeated calls to load the same asset will return the same object instance.
        public override T Load<T>(string assetName)
        {
            string assetFullName;
            if (assetName.Contains('\\'))
                assetFullName = assetName;
            else
            {
                string assetPath = null;
                var type = typeof(T);
                if (typeof(Texture).IsAssignableFrom(type)) assetPath = Paths.Textures;
                else if (type == typeof(Model)) assetPath = Paths.Models;
                else if (type == typeof(SpriteFont)) assetPath = Paths.Fonts;
                else if (type == typeof(Effect)) assetPath = Paths.Shaders;
                else throw new ArgumentException("Cannot load content of unexpected type " + type.Name);
                assetFullName = Path.Combine(assetPath, assetName);
            }
            object item;
            loadedContent.TryGetValue(assetFullName, out item);
            if (item != null) return (T)item;
            item = ReadAsset<T>(assetFullName, null);
            loadedContent.Add(assetFullName, item);
            return (T)item;
        }

        /// <summary>
        /// Disposes of allocated resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            foreach (var value in loadedContent.Values)
                if (value is IDisposable) ((IDisposable)value).Dispose();
            base.Dispose(disposing);
        }
    }
}
