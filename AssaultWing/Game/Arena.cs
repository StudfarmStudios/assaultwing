using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using Microsoft.Xna.Framework.Graphics;
using AW2.Graphics;

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
        /// Names of the textures to use as parallaxes.
        /// </summary>
        [TypeParameter]
        string[] parallaxNames;

        /// <summary>
        /// Distance of each parallax level in inverse coordinates.
        /// </summary>
        /// Distance 0.0 means the level moves along with the game level.
        /// Distance 1.0 means the level is infinitely far and doesn't move.
        [TypeParameter]
        float[] parallaxZ;

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
        // TODO: Take away 3D model material properties from arena fields. Also remove from XML.
        [TypeParameter]
        float alpha;

        [TypeParameter]
        Vector3 ambientLightColor;

        [TypeParameter]
        Vector3 diffuseColor;

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
        Vector3 emissiveColor;

        [TypeParameter]
        Vector3 fogColor;

        [TypeParameter]
        bool fogEnabled;

        [TypeParameter]
        float fogEnd;

        [TypeParameter]
        float fogStart;

        [TypeParameter]
        bool lightingEnabled;

        [TypeParameter]
        Vector3 specularColor;

        [TypeParameter]
        float specularPower;

        #endregion

        /// <summary>
        /// The gobs the arena contains by default.
        /// </summary>
        /// This field will be serialised so that the gobs have their runtime state
        /// (positions, movements, etc.) serialised and not their type parameters.
        [TypeParameter]
        [LimitationSwitch(typeof(TypeParameterAttribute), typeof(RuntimeStateAttribute))]
        List<Gob> gobs;

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
        /// The gobs the arena contains when it is activated.
        /// </summary>
        public List<Gob> Gobs { get { return gobs; } }

        /// <summary>
        /// The parallax textures (names of the textures) the arena contains.
        /// </summary>
        public string[] ParallaxNames { get { return parallaxNames; } }

        /// <summary>
        /// The parallax distances (the z coordinates) of the texture layers.
        /// </summary>
        public float[] ParallaxZ { get { return parallaxZ; } }


        #endregion // Arena properties

        /// <summary>
        /// Creates an uninitialised arena.
        /// </summary>
        /// This constructor is only for serialisation.
        public Arena()
        {
            this.name = "dummyarena";
            this.dimensions = new Vector2(4000, 4000);
            this.gobs = new List<Gob>();
            this.gobs.Add(Gob.CreateGob("dummygobtype"));
            this.parallaxNames = new string[] { "dummytexture" };
            this.parallaxZ = new float[] { 0.5f };
            this.alpha = 1.0f;
            this.ambientLightColor = Vector3.Zero;
            this.diffuseColor = Vector3.Zero;
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
            this.emissiveColor = Vector3.Zero;
            this.fogColor = Vector3.Zero;
            this.fogEnabled = false;
            this.fogEnd = 1.0f;
            this.fogStart = 0.0f;
            this.lightingEnabled = true;
            this.specularColor = Vector3.Zero;
            this.specularPower = 1.0f;
        }

        /// <summary>
        /// Sets lighting for the effect.
        /// </summary>
        /// <param name="effect">The effect to modify.</param>
        public void PrepareEffect(BasicEffect effect)
        {

            //effect.Alpha = alpha;
            //effect.AmbientLightColor = ambientLightColor;
            //effect.DiffuseColor = diffuseColor;
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
            //effect.EmissiveColor = emissiveColor;
            effect.FogColor = fogColor;
            effect.FogEnabled = fogEnabled;
            effect.FogEnd = fogEnd;
            effect.FogStart = fogStart;
            effect.LightingEnabled = lightingEnabled;
            //effect.SpecularColor = specularColor;
            //effect.SpecularPower = specularPower;
        }

        /// <summary>
        /// Draws parallaxes to a viewport using a sprite batch.
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="viewport"></param>
        public void DrawParallaxes(SpriteBatch spriteBatch, AWViewport viewport)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));

            spriteBatch.Begin();
            for (int i = 0; i < parallaxNames.Length; i++)
            {
                Vector2 pos = new Vector2(-((PlayerViewport)viewport).WorldAreaMin.X * (1f - parallaxZ[i]), ((PlayerViewport)viewport).WorldAreaMin.Y * (1f - parallaxZ[i]));
                Vector2 fillPos = new Vector2();
                Texture2D tex = data.GetTexture(parallaxNames[i]);
                int mult = (int)Math.Ceiling(pos.X / (float)tex.Width);
                pos.X = pos.X - mult * tex.Width;
                mult = (int)Math.Ceiling(pos.Y / (float)tex.Height);
                pos.Y = pos.Y - mult * tex.Height;

                int loopX = (int)Math.Ceiling((-pos.X + viewport.InternalViewport.Width )/ tex.Width);
                int loopY = (int)Math.Ceiling((-pos.Y + viewport.InternalViewport.Height) / tex.Height);
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
            }

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
            if (parallaxNames.Length != parallaxZ.Length)
            {
                Log.Write("Warning: Different amount of parallax names and parallax Z's. Cropping.");
                int len = Math.Min(parallaxNames.Length, parallaxZ.Length);
                Array.ConstrainedCopy(parallaxNames, 0, parallaxNames, 0, len);
                Array.ConstrainedCopy(parallaxZ, 0, parallaxZ, 0, len);
            }
            dimensions = Vector2.Max(dimensions, new Vector2(500));
            
            alpha = MathHelper.Clamp(alpha, 0, 1);
            ambientLightColor = Vector3.Clamp(ambientLightColor, Vector3.Zero, Vector3.One);
            diffuseColor = Vector3.Clamp(diffuseColor, Vector3.Zero, Vector3.One);
            light0DiffuseColor = Vector3.Clamp(light0DiffuseColor, Vector3.Zero, Vector3.One);
            light0Direction.Normalize();
            light0SpecularColor = Vector3.Clamp(light0SpecularColor, Vector3.Zero, Vector3.One);
            light1DiffuseColor = Vector3.Clamp(light1DiffuseColor, Vector3.Zero, Vector3.One);
            light1Direction.Normalize();
            light1SpecularColor = Vector3.Clamp(light1SpecularColor, Vector3.Zero, Vector3.One);
            light2DiffuseColor = Vector3.Clamp(light2DiffuseColor, Vector3.Zero, Vector3.One);
            light2Direction.Normalize();
            light2SpecularColor = Vector3.Clamp(light2SpecularColor, Vector3.Zero, Vector3.One);
            emissiveColor = Vector3.Clamp(emissiveColor, Vector3.Zero, Vector3.One);
            fogColor = Vector3.Clamp(fogColor, Vector3.Zero, Vector3.One);
            fogEnd = MathHelper.Max(fogEnd, 0);
            fogStart = MathHelper.Max(fogStart, 0);
            specularColor = Vector3.Clamp(specularColor, Vector3.Zero, Vector3.One);
            specularPower = MathHelper.Max(specularPower, 0);
        }

        #endregion
    }
}
