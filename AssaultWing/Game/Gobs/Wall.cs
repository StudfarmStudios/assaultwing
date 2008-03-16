using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A piece of wall.
    /// </summary>
    /// Note that a wall has no position or movement like other gobs have. 
    /// Instead, a wall acts like a polygon. For visual purposes, walls have 
    /// also a third dimension.
    public class Wall : Gob, IThick
    {
        #region Wall Fields

        /// <summary>
        /// The location of the wall's vertices in the game world.
        /// </summary>
        [RuntimeState]
        VertexPositionNormalTexture[] vertexData;

        /// <summary>
        /// The index data where every consequtive index triplet signifies
        /// one triangle. The indices index 'vertexData'.
        /// </summary>
        [RuntimeState]
        short[] indexData;

        // HACK: Extra fields for debugging Graphics3D.RemoveArea
        VertexPositionColor[] wireVertexData;
        short[] wireIndexData;
        VertexPositionColor[] wireVertexData2;

        /// <summary>
        /// The collision polygons of the wall.
        /// </summary>
        [RuntimeState]
        CollisionArea[] polygons;

        /// <summary>
        /// The name of the texture to fill the wall with.
        /// The name indexes the static texture bank in DataEngine.
        /// </summary>
        [TypeParameter]
        string textureName;

        /// <summary>
        /// The effect for drawing the wall.
        /// </summary>
        BasicEffect effect;

        VertexDeclaration vertexDeclaration;

        #endregion // Wall Fields

        #region Wall Properties

        /// <summary>
        /// Names of all textures that this gob type will ever use.
        /// </summary>
        public override List<string> TextureNames
        {
            get
            {
                List<string> names = base.TextureNames;
                names.Add(textureName);
                return names;
            }
        }

        #endregion // Wall Properties

        /// <summary>
        /// Creates an uninitialised piece of wall.
        /// </summary>
        /// This constructor is only for serialisation.
        public Wall() : base() 
        {
            vertexData = new VertexPositionNormalTexture[] {
                new VertexPositionNormalTexture(new Vector3(0,0,0), -Vector3.UnitX, Vector2.Zero),
                new VertexPositionNormalTexture(new Vector3(100,0,0), Vector3.UnitX, Vector2.UnitX),
                new VertexPositionNormalTexture(new Vector3(0,100,0), Vector3.UnitY, Vector2.UnitY),
            };
            polygons = new CollisionArea[] {
                new CollisionArea("General", new Polygon(new Vector2[] {
                    new Vector2(0,0), new Vector2(100,0), new Vector2(0,100),
                }), null),
            };
            indexData = new short[] { 0, 1, 2 };
            textureName = "dummytexture"; // initialised for serialisation
            effect = null;
            vertexDeclaration = null;
        }

        /// <summary>
        /// Creates a piece of wall.
        /// </summary>
        /// <param name="typeName">The type of the wall.</param>
        public Wall(string typeName)
            : base(typeName)
        {
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            this.vertexData = null;
            this.indexData = null;
            this.polygons = null;
            this.effect = new BasicEffect(gfx, null);
            this.vertexDeclaration = new VertexDeclaration(gfx, VertexPositionNormalTexture.VertexElements);
            base.physicsApplyMode = PhysicsApplyMode.None;
        }

        /// <summary>
        /// Creates a piece of wall.
        /// </summary>
        /// The wall is drawn as a series of triangles. The triangles are
        /// specified in 'indexData', where every consequtive index triplet
        /// constitutes one triangle whose vertices are those that the three
        /// indices index in 'vertexData'.
        /// <param name="typeName">The type of the wall.</param>
        /// <param name="vertexData">The list of vertices that make the wall.</param>
        /// <param name="indexData">The list of indices that make triangles out of vertices.</param>
        /// <param name="collPolygon">The collision polygon of the wall.</param>
        [Obsolete]
        public Wall(string typeName, VertexPositionNormalTexture[] vertexData, short[] indexData, Polygon collPolygon)
            : base(typeName, null, Vector2.Zero, Vector2.Zero, 0f)
        {
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            this.vertexData = vertexData;
            this.indexData = indexData;
            this.polygons = new CollisionArea[] {
                new CollisionArea("General", collPolygon, this),
            };
            this.effect = new BasicEffect(gfx, null);
            this.vertexDeclaration = new VertexDeclaration(gfx, VertexPositionNormalTexture.VertexElements);
        }

        #region Methods related to gobs' functionality in the game world

        /// <summary>
        /// A rectangular area in the X-Y-plane that contains the gob in its
        /// current location in the game world.
        /// </summary>
        [Obsolete]
        public override BoundingBox DrawBoundingBox
        {
            get
            {
                // TODO: Proper bounding box for AW2.Game.Gobs.Wall
                return new BoundingBox(new Vector3(Single.MinValue), new Vector3(Single.MaxValue));
            }
        }

        /// <summary>
        /// Draws the gob.
        /// </summary>
        /// Assumes that the sprite batch has been Begun already and will be
        /// Ended later by someone else.
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        /// <param name="spriteBatch">The sprite batch to draw sprites with.</param>
        public override void Draw(Matrix view, Matrix projection, SpriteBatch spriteBatch)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Texture2D texture = data.GetTexture(textureName);
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            gfx.VertexDeclaration = vertexDeclaration;
            effect.World = Matrix.Identity;
            effect.Projection = projection;
            effect.View = view;
            effect.Texture = texture;
            effect.TextureEnabled = true;
            data.PrepareEffect(effect);
            effect.Begin();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                gfx.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(
                    PrimitiveType.TriangleList, vertexData, 0, vertexData.Length, indexData, 0, indexData.Length / 3);
                pass.End();
            }
            effect.End();

            // HACK: Draw wireframe model for debugging Graphics3D.RemoveArea
            return; // anti-HACK :)
            gfx.VertexDeclaration = new VertexDeclaration(gfx, VertexPositionColor.VertexElements);
            BasicEffect eff = new BasicEffect(gfx, null);
            data.PrepareEffect(eff);
            eff.World = Matrix.Identity;
            eff.Projection = projection;
            eff.View = view;
            eff.TextureEnabled = false;
            eff.LightingEnabled = false;
            eff.VertexColorEnabled = true;
            eff.Begin();
            foreach (EffectPass pass in eff.CurrentTechnique.Passes)
            {
                pass.Begin();
                gfx.DrawUserIndexedPrimitives<VertexPositionColor>(
                    PrimitiveType.LineList, wireVertexData, 0, wireVertexData.Length,
                    wireIndexData, 0, wireIndexData.Length / 2);
                pass.End();
            }
            eff.End();

            // HACK: Draw another wireframe debug stuff for Graphics3D.RemoveArea
            if (wireVertexData2 == null) return;
            eff.Begin();
            foreach (EffectPass pass in eff.CurrentTechnique.Passes)
            {
                pass.Begin();
                gfx.DrawUserPrimitives<VertexPositionColor>(
                    PrimitiveType.LineStrip, wireVertexData2, 0, wireVertexData2.Length - 1);
                pass.End();
            }
            eff.End();
        }

        #endregion Methods related to gobs' functionality in the game world

        /// <summary>
        /// Copies the gob's runtime state from another gob.
        /// </summary>
        /// <param name="runtimeState">The gob whose runtime state to imitate.</param>
        protected override void SetRuntimeState(Gob runtimeState)
        {
            base.SetRuntimeState(runtimeState);

            // Gain ownership over our runtime collision areas.
            collisionAreas = polygons;
            for (int i = 0; i < collisionAreas.Length; ++i)
                collisionAreas[i].Owner = (ICollidable)this;

#if DEBUG
            Helpers.Graphics3D.TriangleWinding wind = Helpers.Graphics3D.GetTriangleWinding(vertexData, indexData);
            if (wind != AW2.Helpers.Graphics3D.TriangleWinding.Clockwise)
            {
                Log.Write("Warning: Wall hasn't only clockwise winding -- fixing it now");
                Helpers.Graphics3D.SetTriangleWinding(vertexData, ref indexData, Helpers.Graphics3D.TriangleWinding.Clockwise);
            }
#endif

            // HACK: Create a wireframe model for debugging Graphics3D.RemoveArea
            Helpers.Graphics3D.GetWireframeModelData(vertexData, indexData, Color.HotPink,
                out wireVertexData, out wireIndexData);
        }

        #region ICollidable Members
        // Some members are implemented in class Gob.

        #endregion ICollidable Members

        #region IThick Members

        /// <summary>
        /// Returns the unit normal vector from the thick gob
        /// pointing towards the given location.
        /// </summary>
        /// <param name="pos">The location for the normal to point to.</param>
        /// <returns>The unit normal pointing to the given location.</returns>
        public Vector2 GetNormal(Vector2 pos)
        {
            // TODO: Normal from multiple polygons, perhaps?
            return Helpers.Geometry.GetNormal((Polygon)(polygons[0]).Area, new Helpers.Point(pos));
        }

        /// <summary>
        /// Removes an area from the thick gob. 
        /// </summary>
        /// <param name="area">The area to remove. The polygon must be convex.</param>
        public void MakeHole(Polygon area)
        {
            return; // HACK: RemoveArea isn't ready yet
            Helpers.Graphics3D.RemoveArea(ref vertexData, ref indexData, area);
            if (indexData.Length == 0)
                this.Die();

#if DEBUG
            Helpers.Graphics3D.TriangleWinding wind = Helpers.Graphics3D.GetTriangleWinding(vertexData, indexData);
            if (wind != AW2.Helpers.Graphics3D.TriangleWinding.Clockwise)
                Log.Write("Warning: Wall hasn't only clockwise winding");
#endif

            // Update collision polygon.
#if false
            // HACK: Exceptions pass debug information
            try
            {
                // NON-HACK: This block content
                Polygon poly = Helpers.Math.GetOutline(vertexData, indexData);
                base.originalCollPrimitives = new IGeomPrimitive[] { poly };
                base.collPrimitives = new IGeomPrimitive[base.originalCollPrimitives.Length];
                base.Pos = base.Pos; // initialises base.collPrimitives
            }
            catch (Exception e)
            {
                Vector2[] vertices = (Vector2[])e.Data["debug"];
                wireVertexData2 = new VertexPositionColor[vertices.Length];
                for (int i = 0; i < vertices.Length; ++i)
                    wireVertexData2[i] = new VertexPositionColor(new Vector3(vertices[i], 150), Color.Ivory);
            }
#endif
            // HACK: Update wireframe model for debugging Graphics3D.RemoveArea
            Helpers.Graphics3D.GetWireframeModelData(vertexData, indexData, Color.HotPink,
                out wireVertexData, out wireIndexData);
            Log.Write("Wall has " + vertexData.Length + " vertices and " +
                indexData.Length / 3 + " triangles");
        }

        #endregion IThick Members
    }
}
