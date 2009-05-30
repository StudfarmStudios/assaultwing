using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using Microsoft.Xna.Framework.Graphics;
using AW2.Graphics;
using AW2.Sound;

namespace AW2.Game
{
    /// <summary>
    /// A game arena, i.e., a rectangular area where gobs exist and interact.
    /// </summary>
    /// Class Arena uses limited (de)serialisation for saving and loading arenas.
    /// Therefore only those fields that describe the arenas initial state -- not 
    /// fields that describe the arenas state during gameplay -- should be marked as 
    /// 'type parameters' by <b>TypeParameterAttribute</b>.
    /// <see cref="AW2.Helpers.TypeParameterAttribute"/>
    [LimitedSerialization]
    public class Arena : IConsistencyCheckable
    {
        #region Arena fields

        /// <summary>
        /// Arena File name is needed for arena loading.
        /// </summary>
        string fileName;

        /// <summary>
        /// Layers of the arena, containing initial gobs and parallaxes.
        /// </summary>
        [TypeParameter]
        List<ArenaLayer> layers;

        /// <summary>
        /// Human-readable name of the arena.
        /// </summary>
        [TypeParameter]
        string name;

        /// <summary>
        /// Dimensions of the arena, i.e. maximum coordinates for gobs.
        /// </summary>
        /// Minimum coordinates are always (0,0).
        [TypeParameter]
        Vector2 dimensions;

        #region Lighting settings for the arena

        [TypeParameter]
        Vector3 light0DiffuseColor;

        [TypeParameter]
        Vector3 light0Direction;

        [TypeParameter]
        bool light0Enabled;

        [TypeParameter]
        Vector3 light0SpecularColor;

        [TypeParameter]
        Vector3 light1DiffuseColor;

        [TypeParameter]
        Vector3 light1Direction;

        [TypeParameter]
        bool light1Enabled;

        [TypeParameter]
        Vector3 light1SpecularColor;

        [TypeParameter]
        Vector3 light2DiffuseColor;

        [TypeParameter]
        Vector3 light2Direction;

        [TypeParameter]
        bool light2Enabled;

        [TypeParameter]
        Vector3 light2SpecularColor;

        [TypeParameter]
        Vector3 fogColor;

        [TypeParameter]
        bool fogEnabled;

        [TypeParameter]
        float fogEnd;

        [TypeParameter]
        float fogStart;

        #endregion

        [TypeParameter]
        List<BackgroundMusic> backgroundmusic;
        
        #endregion // Arena fields

        #region Arena properties

        /// <summary>
        /// The name of the arena.
        /// </summary>
        public string Name { get { return name; } set { name = value; } }

        /// <summary>
        /// The file name of the arena.
        /// </summary>
        public string FileName { get { return fileName; } set { fileName = value; } }

        /// <summary>
        /// The width and height of the arena.
        /// </summary>
        /// The allowed range of gob X-coordinates is from 0 to arena width.
        /// The allowed range of gob Y-coordinates is from 0 to arena height.
        public Vector2 Dimensions { get { return dimensions; } set { dimensions = value; } }

        /// <summary>
        /// The layers of the arena.
        /// </summary>
        public List<ArenaLayer> Layers { get { return layers; } }

        /// <summary>
        /// The bgmusics the arena contains when it is activated.
        /// </summary>
        public List<BackgroundMusic> BackgroundMusic { get { return backgroundmusic; } }

        #endregion // Arena properties

        /// <summary>
        /// Creates an uninitialised arena.
        /// </summary>
        /// This constructor is only for serialisation.
        public Arena()
        {
            this.name = "dummyarena";
            this.dimensions = new Vector2(4000, 4000);
            layers = new List<ArenaLayer>();
            layers.Add(new ArenaLayer());
            this.backgroundmusic = new List<BackgroundMusic>();
            this.light0DiffuseColor = Vector3.Zero;
            this.light0Direction = -Vector3.UnitZ;
            this.light0Enabled = true;
            this.light0SpecularColor = Vector3.Zero;
            this.light1DiffuseColor = Vector3.Zero;
            this.light1Direction = -Vector3.UnitZ;
            this.light1Enabled = false;
            this.light1SpecularColor = Vector3.Zero;
            this.light2DiffuseColor = Vector3.Zero;
            this.light2Direction = -Vector3.UnitZ;
            this.light2Enabled = false;
            this.light2SpecularColor = Vector3.Zero;
            this.fogColor = Vector3.Zero;
            this.fogEnabled = false;
            this.fogEnd = 1.0f;
            this.fogStart = 0.0f;
        }

        /// <summary>
        /// Sets lighting for the effect.
        /// </summary>
        /// <param name="effect">The effect to modify.</param>
        public void PrepareEffect(BasicEffect effect)
        {
            //effect.TextureEnabled = true;
            effect.DirectionalLight0.DiffuseColor = light0DiffuseColor;
            effect.DirectionalLight0.Direction = light0Direction;
            effect.DirectionalLight0.Enabled = light0Enabled;
            effect.DirectionalLight0.SpecularColor = light0SpecularColor;
            effect.DirectionalLight1.DiffuseColor = light1DiffuseColor;
            effect.DirectionalLight1.Direction = light1Direction;
            effect.DirectionalLight1.Enabled = light1Enabled;
            effect.DirectionalLight1.SpecularColor = light1SpecularColor;
            effect.DirectionalLight2.DiffuseColor = light2DiffuseColor;
            effect.DirectionalLight2.Direction = light2Direction;
            effect.DirectionalLight2.Enabled = light2Enabled;
            effect.DirectionalLight2.SpecularColor = light2SpecularColor;
            effect.FogColor = fogColor;
            effect.FogEnabled = fogEnabled;
            effect.FogEnd = fogEnd;
            effect.FogStart = fogStart;
            effect.LightingEnabled = true;
        }

        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public void MakeConsistent(Type limitationAttribute)
        {
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                // Make sure there's no null references.
                layers = layers ?? new List<ArenaLayer>();
                name = name ?? "unknown arena";
                backgroundmusic = backgroundmusic ?? new List<BackgroundMusic>();

                dimensions = Vector2.Max(dimensions, new Vector2(500));

                light0DiffuseColor = Vector3.Clamp(light0DiffuseColor, Vector3.Zero, Vector3.One);
                light0Direction.Normalize();
                light0SpecularColor = Vector3.Clamp(light0SpecularColor, Vector3.Zero, Vector3.One);
                light1DiffuseColor = Vector3.Clamp(light1DiffuseColor, Vector3.Zero, Vector3.One);
                light1Direction.Normalize();
                light1SpecularColor = Vector3.Clamp(light1SpecularColor, Vector3.Zero, Vector3.One);
                light2DiffuseColor = Vector3.Clamp(light2DiffuseColor, Vector3.Zero, Vector3.One);
                light2Direction.Normalize();
                light2SpecularColor = Vector3.Clamp(light2SpecularColor, Vector3.Zero, Vector3.One);
                fogColor = Vector3.Clamp(fogColor, Vector3.Zero, Vector3.One);
                fogEnd = MathHelper.Max(fogEnd, 0);
                fogStart = MathHelper.Max(fogStart, 0);
            }
        }

        #endregion
    }
}
