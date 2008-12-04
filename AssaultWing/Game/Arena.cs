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
    /// An arena layer.
    /// </summary>
    /// Arena layers are a means to visualise depth in spite of the
    /// orthogonal projection of 3D graphics.
    [LimitedSerialization]
    public class ArenaLayer : IConsistencyCheckable
    {
        [TypeParameter]
        bool isGameplayLayer;
        [TypeParameter]
        float z;
        [TypeParameter]
        string parallaxName;

        // This field will be serialised so that the gobs have their runtime state
        // (positions, movements, etc.) serialised and not their type parameters.
        [TypeParameter]
        [LimitationSwitch(typeof(TypeParameterAttribute), typeof(RuntimeStateAttribute))]
        List<Gob> gobs;

        /// <summary>
        /// Gobs in this arena layer, sorted in 2D draw order from back to front,
        /// exclusive of gobs that are not drawn in 2D.
        /// </summary>
        /// 2D draw order is alphabetic order primarily by decreasing <c>Gob.LayerDepth2D</c>
        /// and secondarily by natural order of <c>Gob.DrawMode2D</c>.
        List<Gob> gobsSort2D;

        /// <summary>
        /// Is this the arena layer where gameplay takes place.
        /// </summary>
        /// It is assumed that only one layer in each arena is the gameplay layer.
        /// If several layers claim to be gameplay layers, any one of them can be
        /// considered as <i>the</i> gameplay layer.
        public bool IsGameplayLayer { get { return isGameplayLayer; } }

        /// <summary>
        /// Z coordinate of the layer.
        /// </summary>
        /// The Z coordinate of the gameplay layer is 0. Negative coordinates
        /// are farther away from the camera.
        public float Z { get { return z; } }

        /// <summary>
        /// Name of the texture to use as parallax or <c>null</c> for no parallax.
        /// </summary>
        /// Note: Deserialising the empty string to <c>ParallaxName</c> will
        /// result in <c>ParallaxName</c> getting <c>null</c> as its value, i.e.
        /// there will be no parallax.
        public string ParallaxName { get { return parallaxName; } }

        /// <summary>
        /// Creates an uninitialised arena layer.
        /// </summary>
        /// This constructor is only for serialisation.
        public ArenaLayer()
        {
            isGameplayLayer = true;
            z = 0;
            parallaxName = "dummysprite";
            gobs = new List<Gob>();
            gobsSort2D = new List<Gob>();
            AddGob(Gob.CreateGob("dummygobtype"));
        }

        /// <summary>
        /// Creates an arena layer.
        /// </summary>
        /// <param name="isGameplayLayer">Is the layer the gameplay layer.</param>
        /// <param name="z">Depth of the layer.</param>
        /// <param name="parallaxName">The name of the layer's parallax, or <c>null</c>.</param>
        public ArenaLayer(bool isGameplayLayer, float z, string parallaxName)
        {
            this.isGameplayLayer = isGameplayLayer;
            this.z = z;
            this.parallaxName = parallaxName;
            gobs = new List<Gob>();
            gobsSort2D = new List<Gob>();
        }

        /// <summary>
        /// Returns a new arena layer with the same specifications but no gobs.
        /// </summary>
        /// <returns>A duplicate arena layer without gobs.</returns>
        public ArenaLayer EmptyCopy()
        {
            return new ArenaLayer(this);
        }

        /// <summary>
        /// Creates a copy of an arena layer excluding its gobs.
        /// </summary>
        ArenaLayer(ArenaLayer other)
        {
            isGameplayLayer = other.isGameplayLayer;
            z = other.z;
            parallaxName = other.parallaxName;
            gobs = new List<Gob>();
            gobsSort2D = new List<Gob>();
        }

        #region Methods for handling gobs

        /// <summary>
        /// Adds a gob to the arena layer.
        /// </summary>
        /// <param name="gob">The gob to add.</param>
        public void AddGob(Gob gob)
        {
            gobs.Add(gob);

            // Find the gob's place in 2D draw order.
            if (gob.DrawMode2D.IsDrawn)
            {
                int index = 0;
                while (index < gobsSort2D.Count
                       && (gob.DepthLayer2D < gobsSort2D[index].DepthLayer2D
                           || (gob.DepthLayer2D == gobsSort2D[index].DepthLayer2D &&
                               gob.DrawMode2D.CompareTo(gobsSort2D[index].DrawMode2D) > 0)))
                    ++index;
                gobsSort2D.Insert(index, gob);
            }
        }

        /// <summary>
        /// Removes a gob from the arena layer.
        /// </summary>
        /// <param name="gob">The gob to remove.</param>
        public void RemoveGob(Gob gob)
        {
            gobs.Remove(gob);
            gobsSort2D.Remove(gob);
        }

        /// <summary>
        /// Performs the specified action on each gob on the arena layer.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each gob.</param>
        public void ForEachGob(Action<Gob> action)
        {
            foreach (Gob gob in gobs)
                action(gob);
        }

        /// <summary>
        /// Performs the specified action on each gob on the arena layer,
        /// enumerating the gobs in their 2D draw order from back to front.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each gob.</param>
        public void ForEachGobSort2D(Action<Gob> action)
        {
            foreach (Gob gob in gobsSort2D)
                action(gob);
        }

        /// <summary>
        /// Removes all gobs from the arena layer.
        /// </summary>
        public void ClearGobs()
        {
            gobs.Clear();
            gobsSort2D.Clear();
        }

        #endregion Methods for handling gobs

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
            if (z >= 1000)
            {
                Log.Write("Warning: Clamping too big arena layer Z coordinate: " + z);
                z = 500;
            }
            if (parallaxName == "")
                parallaxName = null;
        }

        #endregion
    }

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

        /// <summary>
        /// Draws parallaxes to the graphics device's current viewport using a sprite batch.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to use for drawing. <c>Begin</c> is
        /// assumed to have been called, and <c>End</c> is assumed to be called after this
        /// method returns.</param>
        /// <param name="referencePoint">Reference point in game world coordinates for
        /// parallax displacement.</param>
        [Obsolete]
        public void DrawParallaxes(SpriteBatch spriteBatch, Vector2 referencePoint)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            int viewportWidth = AssaultWing.Instance.GraphicsDevice.Viewport.Width;
            int viewportHeight = AssaultWing.Instance.GraphicsDevice.Viewport.Height;
            spriteBatch.Begin();
            data.ForEachArenaLayer(delegate(ArenaLayer layer)
            {
                string parallaxName = layer.ParallaxName;
                float z = layer.Z;
                if (parallaxName == null) return;
                Vector2 pos = new Vector2(-referencePoint.X * 1000 / (1000 - z), referencePoint.Y * 1000 / (1000 - z));
                Vector2 fillPos = new Vector2();
                Texture2D tex = data.GetTexture(parallaxName);
                int mult = (int)Math.Ceiling(pos.X / (float)tex.Width);
                pos.X = pos.X - mult * tex.Width;
                mult = (int)Math.Ceiling(pos.Y / (float)tex.Height);
                pos.Y = pos.Y - mult * tex.Height;

                int loopX = (int)Math.Ceiling((-pos.X + viewportWidth) / tex.Width);
                int loopY = (int)Math.Ceiling((-pos.Y + viewportHeight) / tex.Height);
                fillPos.Y = pos.Y;
                for (int y = 0; y < loopY; y++)
                {
                    fillPos.X = pos.X;
                    for (int x = 0; x < loopX; x++)
                    {
                        spriteBatch.Draw(tex, fillPos, Color.White);
                        fillPos.X += tex.Width;
                    }
                    fillPos.Y += tex.Height;
                }
            });

            spriteBatch.End();
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
