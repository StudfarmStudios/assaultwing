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
    public class Wall : Gob, IThick, IHoleable, IConsistencyCheckable
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

        /// <summary>
        /// Handles to the 3D model's triangles. For <b>PhysicsEngine</b>.
        /// </summary>
        /// Index n corresponds to the triangle that is defined by <b>indexData</b>
        /// indices 3n, 3n+1 and 3n+2.
        object[] wallTriangleHandles;

        /// <summary>
        /// Polygon representations of the 3D model's triangles. Any element in the array
        /// may be <b>null</b>, meaning that the triangle has been removed.
        /// </summary>
        /// Index n corresponds to the triangle that is defined by <b>indexData</b>
        /// indices 3n, 3n+1 and 3n+2.
        Polygon?[] wallTrianglePolygons;

        /// <summary>
        /// Triangle index map of the wall's 3D model in the X-Y plane.
        /// </summary>
        /// If indexMap[y,x] == null then no triangle covers index map point (x,y).
        /// Otherwise indexMap[y,x] is an array of indices n such that 
        /// the triangle that is defined by the 3D model's index map elements 
        /// 3n, 3n+1 and 3n+2 covers the index map point (x,y).
        /// The index map has its own coordinate system that can be obtained from
        /// the 3D model's coordinate system by <b>indexMapTransform</b>.
        int[,][] indexMap;

        /// <summary>
        /// Transformation matrix from wall's 3D model's coordinates to index map coordinates.
        /// </summary>
        Matrix indexMapTransform;

        /// <summary>
        /// Triangle cover counts of the wall's 3D model.
        /// </summary>
        /// Index n corresponds to the triangle defined by the 3D model's
        /// index list members 3n, 3n+1 and 3n+2. A positive cover count signifies
        /// the number of index map points covered by the triangle that still 
        /// need to be deleted before the triangle is erased from the 3D model.
        /// A negative cover count marks a deleted triangle.
        int[] triangleCovers;

        /// <summary>
        /// The number of triangles in the wall's 3D model not yet removed.
        /// </summary>
        int triangleCount;

        // HACK: Extra fields for debugging Graphics3D.RemoveArea
        VertexPositionColor[] wireVertexData;
        short[] wireIndexData;
        VertexPositionColor[] wireVertexData2;

        /// <summary>
        /// The collision polygons of the wall.
        /// </summary>
        [RuntimeState]
        CollisionArea[] polygons; // TODO: Remove Wall.polygons

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
            triangleCount = indexData.Length / 3;
            wallTriangleHandles = new object[triangleCount];
            wallTrianglePolygons = new Polygon?[triangleCount];
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
            this.wallTriangleHandles = null;
            this.wallTrianglePolygons = null;
            this.triangleCount = 0;
            this.polygons = null;
            this.effect = new BasicEffect(gfx, null);
            this.vertexDeclaration = new VertexDeclaration(gfx, VertexPositionNormalTexture.VertexElements);
            base.physicsApplyMode = PhysicsApplyMode.None;
        }

        #region Methods related to gobs' functionality in the game world

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            InitializeIndexMap();
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

#if false
            // HACK: Draw wireframe model for debugging Graphics3D.RemoveArea
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
#endif
        }

        #endregion Methods related to gobs' functionality in the game world

        /// <summary>
        /// Copies the gob's runtime state from another gob.
        /// </summary>
        /// <param name="runtimeState">The gob whose runtime state to imitate.</param>
        protected override void SetRuntimeState(Gob runtimeState)
        {
            base.SetRuntimeState(runtimeState);
            triangleCount = indexData.Length / 3;
            wallTriangleHandles = new object[triangleCount];
            wallTrianglePolygons = new Polygon?[triangleCount];

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
            Helpers.Point posPoint = new Helpers.Point(pos);
            // TODO: Check only closest triangles; utilise PhysicsEngineImpl.wallTriangles
            List<Polygon> checkTriangles = new List<Polygon>(triangleCount);
            foreach (Polygon? triangle in wallTrianglePolygons)
                if (triangle.HasValue)
                    checkTriangles.Add(triangle.Value);
            return Helpers.Geometry.GetNormal(checkTriangles, posPoint);
        }

        #endregion IThick Members

        #region IHoleable Members

        /// <summary>
        /// Index data of the entity's 3D model.
        /// </summary>
        public short[] IndexData { get { return indexData; } }
        
        /// <summary>
        /// Vertex data of the entity's 3D model.
        /// </summary>
        public VertexPositionNormalTexture[] VertexData { get { return vertexData; } }

        /// <summary>
        /// Handles for all triangles in the entity's 3D model.
        /// Used internally by <b>PhysicsEngine</b>.
        /// </summary>
        public object[] WallTriangleHandles { get { return wallTriangleHandles; } }

        /// <summary>
        /// Polygons for all triangles in the entity's 3D model. Any element in the array
        /// may be <b>null</b>, meaning that the triangle has been removed.
        /// </summary>
        public Polygon?[] WallTrianglePolygons { get { return wallTrianglePolygons; } }

        /// <summary>
        /// Removes an area from the gob. 
        /// </summary>
        /// <param name="holePos">Center of the area to remove, in world coordinates.</param>
        public void MakeHole(Vector2 holePos)
        {
            float holeRadius = 10; // HACK: hole size and shape
            Vector2 posInWall = holePos - this.Pos;
            Vector2 posInIndexMap = Vector2.Transform(posInWall, indexMapTransform);

            // Eat a square hole.
            int minX = (int)Math.Round(posInIndexMap.X - holeRadius);
            int maxX = (int)Math.Round(posInIndexMap.X + holeRadius) + 1;
            int minY = (int)Math.Round(posInIndexMap.Y - holeRadius);
            int maxY = (int)Math.Round(posInIndexMap.Y + holeRadius) + 1;
            minX = Math.Max(minX, 0);
            maxX = Math.Min(maxX, indexMap.GetLength(1));
            minY = Math.Max(minY, 0);
            maxY = Math.Min(maxY, indexMap.GetLength(0));
            for (int y = minY; y < maxY; ++y)
                for (int x = minX; x < maxX; ++x)
                {
                    if (indexMap[y, x] == null) continue;
                    foreach (int index in indexMap[y, x])
                    {
                        if (--triangleCovers[index] != 0) continue;

                        // Replace the triangle in the 3D model with a trivial one.
                        indexData[3 * index + 0] = 0;
                        indexData[3 * index + 1] = 0;
                        indexData[3 * index + 2] = 0;

                        // Remove the triangle from physics engine.
                        physics.RemoveWallTriangle(wallTriangleHandles[index]);
                        wallTriangleHandles[index] = null;
                        wallTrianglePolygons[index] = null;
                        --triangleCount;
                    }
                    //indexMap[y, x] = null;
                }

            // Remove the wall gob if all its triangles have been removed.
            if (triangleCount == 0)
            {
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                data.RemoveGob(this);
            }
        }

        #endregion

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
            if (limitationAttribute == typeof(RuntimeStateAttribute))
            {
                VertexPositionNormalTexture[] fineVertexData;
                short[] fineIndexData;
                Graphics3D.FineTriangles(15, vertexData, indexData, out fineVertexData, out fineIndexData);
                indexData = fineIndexData;
                vertexData = fineVertexData;
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Initialises the wall's index map from the wall's 3D model.
        /// </summary>
        private void InitializeIndexMap()
        {
            // Find out model dimensions.
            Vector2 modelMin = new Vector2(float.MaxValue);
            Vector2 modelMax = new Vector2(float.MinValue);
            foreach (VertexPositionNormalTexture vert in vertexData)
            {
                Vector2 vertV2 = new Vector2(vert.Position.X, vert.Position.Y);
                modelMin = Vector2.Min(modelMin, vertV2);
                modelMax = Vector2.Max(modelMax, vertV2);
            }
            Vector2 modelDim = new Vector2(modelMax.X - modelMin.X, modelMax.Y - modelMin.Y);

            // Create an index map for the model.
            // The mask is initialised by a render of the 3D model by the graphics card.
            indexMap = new int[(int)Math.Ceiling(modelDim.Y) + 1, (int)Math.Ceiling(modelDim.X) + 1][];
            indexMapTransform = Matrix.CreateTranslation(-modelMin.X, -modelMin.Y, 0);

            // Create colour-coded vertices for each triangle.
            VertexPositionColor[] colouredVertexData = new VertexPositionColor[indexData.Length];
            for (int indexI = 0; indexI < indexData.Length; ++indexI)
            {
                VertexPositionNormalTexture originalVertex = vertexData[indexData[indexI]];
                Color color = new Color((byte)((indexI / 3) % 256), (byte)((indexI / 3 / 256) % 256), (byte)((indexI / 3 / 256 / 256) % 256));
                colouredVertexData[indexI] = new VertexPositionColor(originalVertex.Position, color);
            }

            // Draw the colour-coded triangles on our own render target for
            // index map initialisation. Render target will be a square with
            // size ('targetSize') a power of two to meet the demands of some
            // graphics devices. If the model dimensions are larger than 
            // 'targetSize', we will have to render the coloured triangles in pieces.
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            GraphicsDeviceCapabilities gfxCaps = gfx.GraphicsDeviceCapabilities;
            int targetSize = Math.Min(
                AWMathHelper.FloorPowerTwo(Math.Min(gfxCaps.MaxTextureHeight, gfxCaps.MaxTextureWidth)),
                AWMathHelper.CeilingPowerTwo(Math.Max(indexMap.GetLength(1), indexMap.GetLength(0))));
            RenderTarget2D maskTarget = null;
            while (maskTarget == null)
                try
                {
                    maskTarget = new RenderTarget2D(gfx, targetSize, targetSize, 1, gfx.DisplayMode.Format);
                }
                catch (Exception e)
                {
                    if (e is OutOfVideoMemoryException)
                        targetSize /= 2;
                    else
                        throw new Exception("Cannot create render target for index map creation", e);
                }

            // Set up graphics device.
            VertexDeclaration oldVertexDeclaration = gfx.VertexDeclaration;
            DepthStencilBuffer oldDepthStencilBuffer = gfx.DepthStencilBuffer;
            gfx.VertexDeclaration = new VertexDeclaration(gfx, VertexPositionColor.VertexElements);
            gfx.DepthStencilBuffer = null;

            // Set up an effect.
            BasicEffect maskEff = new BasicEffect(gfx, null);
            maskEff.VertexColorEnabled = true;
            maskEff.LightingEnabled = false;
            maskEff.TextureEnabled = false;
            maskEff.View = Matrix.CreateLookAt(new Vector3(0, 0, 500), Vector3.Zero, Vector3.Up);
            maskEff.Projection = Matrix.CreateOrthographicOffCenter(0, targetSize - 1,
                0, targetSize - 1, 10, 1000);
            maskEff.World = indexMapTransform;

            // Draw the coloured triangles in as many parts as necessary to cover 
            // the whole model with one unit in world coordinates corresponding to
            // one pixel width in the render target.
            for (int startY = 0; startY < indexMap.GetLength(0); startY += targetSize)
                for (int startX = 0; startX < indexMap.GetLength(1); startX += targetSize)
                {
                    // Move view to current start coordinates.
                    maskEff.View = Matrix.CreateLookAt(new Vector3(startX, startY, 500), new Vector3(startX, startY, 0), Vector3.Up);

                    // Set and clear our own render target.
                    gfx.SetRenderTarget(0, maskTarget);
                    gfx.Clear(ClearOptions.Target, Color.White, 0, 0);

                    // Draw the coloured triangles.
                    maskEff.Begin();
                    foreach (EffectPass pass in maskEff.CurrentTechnique.Passes)
                    {
                        pass.Begin();
                        gfx.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList,
                            colouredVertexData, 0, colouredVertexData.Length / 3);
                        pass.End();
                    }
                    maskEff.End();

                    // Restore render target so what we can extract drawn pixels.
                    gfx.SetRenderTarget(0, null);

                    // Figure out mask data from the render target.
                    Texture2D maskTexture = maskTarget.GetTexture();
                    Color[] maskData = new Color[targetSize * targetSize];
                    maskTexture.GetData<Color>(maskData);
                    for (int y = 0; y < targetSize; ++y)
                        for (int x = 0; x < targetSize; ++x)
                        {
                            Color color = maskData[x + y * maskTexture.Width];
                            if (color == Color.White) continue;
                            int maskValue = color.R + color.G * 256 + color.B * 256 * 256;
                            indexMap[startY + targetSize - 1 - y, startX + x] = new int[] { maskValue };
                        }
                }

            // Restore graphics device's old settings.
            gfx.VertexDeclaration = oldVertexDeclaration;
            gfx.DepthStencilBuffer = oldDepthStencilBuffer;
            maskTarget.Dispose();

            // Initialise triangle cover counts.
            triangleCovers = new int[indexData.Length / 3];
            foreach (int[] indices in indexMap)
                if (indices != null)
                    foreach (int index in indices)
                        ++triangleCovers[index];

            // If some triangle isn't mentioned in the index map, force it there.
            for (int i = 0; i < triangleCovers.Length; ++i)
                if (triangleCovers[i] == 0)
                {
                    Vector3 vert0 = vertexData[indexData[3 * i + 0]].Position;
                    Vector3 vert1 = vertexData[indexData[3 * i + 1]].Position;
                    Vector3 vert2 = vertexData[indexData[3 * i + 2]].Position;
                    Vector3 triangleCenter = (vert0 + vert1 + vert2) / 3;
                    Vector3 centerInIndexMap = Vector3.Transform(triangleCenter, indexMapTransform);
                    int centerInIndexMapX = (int)(Math.Round(centerInIndexMap.X) + 0.1);
                    int centerInIndexMapY = (int)(Math.Round(centerInIndexMap.Y) + 0.1);
                    int[] oldIndices = indexMap[centerInIndexMapY, centerInIndexMapX];
                    int[] newIndices = null;
                    if (oldIndices != null)
                    {
                        newIndices = new int[oldIndices.Length + 1];
                        Array.Copy(oldIndices, newIndices, oldIndices.Length);
                        newIndices[oldIndices.Length] = i;
                    }
                    else
                        newIndices = new int[] { i };
                    indexMap[centerInIndexMapY, centerInIndexMapX] = newIndices;
                    ++triangleCovers[i];
                }
        }

        #endregion Private methods
    }
}
