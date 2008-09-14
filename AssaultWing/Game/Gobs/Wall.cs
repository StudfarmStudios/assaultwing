using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using Rectangle = AW2.Helpers.Geometric.Rectangle;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A piece of wall.
    /// </summary>
    /// Note that a wall has no position or movement like other gobs have. 
    /// Instead, a wall acts like a polygon. For visual purposes, walls have 
    /// also a third dimension.
    public class Wall : Gob, IConsistencyCheckable
    {
        #region Wall Fields

        /// <summary>
        /// The location of the wall's vertices in the game world.
        /// </summary>
        [RuntimeState]
        protected VertexPositionNormalTexture[] vertexData;

        /// <summary>
        /// The index data where every consequtive index triplet signifies
        /// one triangle. The indices index 'vertexData'.
        /// </summary>
        [RuntimeState]
        protected short[] indexData;

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
        /// The texture to draw the wall's 3D model with.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The effect for drawing the wall.
        /// </summary>
        BasicEffect effect;

        /// <summary>
        /// The effect for drawing the wall as a silhouette.
        /// </summary>
        BasicEffect silhouetteEffect;

        VertexDeclaration vertexDeclaration;

        /// <summary>
        /// The wall's 3D model's bounding box, in world coordinates.
        /// </summary>
        BoundingBox boundingBox;

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
            Set3DModel(new VertexPositionNormalTexture[] 
                {
                    new VertexPositionNormalTexture(new Vector3(0,0,0), -Vector3.UnitX, Vector2.Zero),
                    new VertexPositionNormalTexture(new Vector3(100,0,0), Vector3.UnitX, Vector2.UnitX),
                    new VertexPositionNormalTexture(new Vector3(0,100,0), Vector3.UnitY, Vector2.UnitY),
                },
                new short[] { 0, 1, 2 },
                null, null);
            polygons = new CollisionArea[] {
                new CollisionArea("General", new Polygon(new Vector2[] {
                    new Vector2(0,0), new Vector2(100,0), new Vector2(0,100),
                }), null, 
                CollisionAreaType.PhysicalWall, CollisionAreaType.None, CollisionAreaType.None),
            };
            textureName = "dummytexture";
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
            this.triangleCount = 0;
            this.polygons = null;
            this.effect = new BasicEffect(gfx, null);
            this.silhouetteEffect = new BasicEffect(gfx, null);
            this.vertexDeclaration = new VertexDeclaration(gfx, VertexPositionNormalTexture.VertexElements);
            this.boundingBox = new BoundingBox();
            movable = false;
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
            if (!data.Viewport.Intersects(boundingBox)) return;

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
        }

        /// <summary>
        /// Draws the wall as a silhouette.
        /// </summary>
        /// Assumes that the sprite batch has been Begun already and will be
        /// Ended later by someone else.
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        /// <param name="spriteBatch">The sprite batch to draw sprites with.</param>
        public void DrawSilhouette(Matrix view, Matrix projection, SpriteBatch spriteBatch)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            gfx.VertexDeclaration = vertexDeclaration;
            silhouetteEffect.World = Matrix.Identity;
            silhouetteEffect.Projection = projection;
            silhouetteEffect.View = view;
            silhouetteEffect.Texture = texture;
            silhouetteEffect.VertexColorEnabled = false;
            silhouetteEffect.LightingEnabled = false;
            silhouetteEffect.TextureEnabled = false;
            silhouetteEffect.FogEnabled = false;
            silhouetteEffect.Begin();
            foreach (EffectPass pass in silhouetteEffect.CurrentTechnique.Passes)
            {
                pass.Begin();
                gfx.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(
                    PrimitiveType.TriangleList, vertexData, 0, vertexData.Length, indexData, 0, indexData.Length / 3);
                pass.End();
            }
            silhouetteEffect.End();
        }

        #endregion Methods related to gobs' functionality in the game world

        /// <summary>
        /// Copies the gob's runtime state from another gob.
        /// </summary>
        /// <param name="runtimeState">The gob whose runtime state to imitate.</param>
        protected override void SetRuntimeState(Gob runtimeState)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            base.SetRuntimeState(runtimeState);
            triangleCount = indexData.Length / 3;
            texture = data.GetTexture(textureName);
            boundingBox = BoundingBox.CreateFromPoints(
                Array.ConvertAll<VertexPositionNormalTexture, Vector3>(vertexData,
                delegate(VertexPositionNormalTexture vertex) { return vertex.Position; }));

            // Gain ownership over our runtime collision areas.
            collisionAreas = polygons;
            for (int i = 0; i < collisionAreas.Length; ++i)
                collisionAreas[i].Owner = this;
        }

        /// <summary>
        /// Removes a round area from this wall, i.e. makes a hole.
        /// </summary>
        /// <param name="holePos">Center of the hole, in world coordinates.</param>
        /// <param name="holeRadius">Radius of the hole, in meters.</param>
        public void MakeHole(Vector2 holePos, float holeRadius)
        {
            if (holeRadius <= 0) return;
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Vector2 posInIndexMap = Vector2.Transform(holePos, indexMapTransform);

            // Eat a round hole.
            int indexMapWidth = indexMap.GetLength(1);
            int indexMapHeight = indexMap.GetLength(0);
            AWMathHelper.FillCircle((int)Math.Round(posInIndexMap.X), (int)Math.Round(posInIndexMap.Y),
                (int)Math.Round(holeRadius), delegate(int x, int y)
            {
                if (x < 0 || y < 0 || x >= indexMapWidth || y >= indexMapHeight) return;
                if (indexMap[y, x] == null) return;
                foreach (int index in indexMap[y, x])
                {
                    if (--triangleCovers[index] != 0) continue;

                    // Replace the triangle in the 3D model with a trivial one.
                    indexData[3 * index + 0] = 0;
                    indexData[3 * index + 1] = 0;
                    indexData[3 * index + 2] = 0;

                    // Remove the triangle from physics engine.
                    physics.Unregister(collisionAreas[index]);

                    --triangleCount;
                }
                //indexMap[y, x] = null;
            });

            // Remove the wall gob if all its triangles have been removed.
            if (triangleCount == 0)
                data.RemoveGob(this);
        }
 
        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public new void MakeConsistent(Type limitationAttribute)
        {
            // NOTE: This method is meant to re-implement the interface member
            // IConsistencyCheckable.MakeConsistent(Type) that is already implemented
            // in the base class Gob. According to the C# Language Specification 1.2
            // (and not corrected in the specification version 2.0), adding the 'new'
            // keyword to this re-implementation would make this code
            // 
            //      Wall wall;
            //      ((IConsistencyCheckable)wall).MakeConsistent(type)
            //
            // call Gob.MakeConsistent(Type). However, debugging reveals this is not the
            // case. By leaving out the 'new' keyword, the semantics stays the same, as
            // seen by debugging, but the compiler produces a warning.
            base.MakeConsistent(limitationAttribute);
            if (limitationAttribute == typeof(RuntimeStateAttribute))
                FineTriangles();
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Sets the wall's 3D model. To be called before the wall is Activate()d.
        /// </summary>
        /// <param name="vertexData">Vertex data of the 3D model.</param>
        /// <param name="indexData">Index data of the 3D model as triangle list.</param>
        /// <param name="texture">Texture of the 3D model.</param>
        /// <param name="effect">Effect of the 3D model.</param>
        protected void Set3DModel(VertexPositionNormalTexture[] vertexData, short[] indexData,
            Texture2D texture, BasicEffect effect)
        {
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            this.vertexData = vertexData;
            this.indexData = indexData;
            this.texture = texture;
            this.effect = effect;
            silhouetteEffect = effect == null ? null : (BasicEffect)effect.Clone(gfx);
            FineTriangles();
            triangleCount = this.indexData.Length / 3;
            boundingBox = BoundingBox.CreateFromPoints(
                Array.ConvertAll<VertexPositionNormalTexture, Vector3>(this.vertexData,
                delegate(VertexPositionNormalTexture vertex) { return vertex.Position; }));

            // Create collision areas; one for each triangle in the wall's 3D model
            // and one bounding collision area for making holes in the wall.
            collisionAreas = new CollisionArea[this.indexData.Length / 3 + 1];
            for (int i = 0; i + 2 < this.indexData.Length; i += 3)
            {
                // Create a physical collision area for this triangle.
                Vector3 v1 = this.vertexData[this.indexData[i + 0]].Position;
                Vector3 v2 = this.vertexData[this.indexData[i + 1]].Position;
                Vector3 v3 = this.vertexData[this.indexData[i + 2]].Position;
                IGeomPrimitive triangleArea = new Triangle(
                    new Vector2(v1.X, v1.Y),
                    new Vector2(v2.X, v2.Y),
                    new Vector2(v3.X, v3.Y));
                collisionAreas[i / 3] = new CollisionArea("General", triangleArea, this,
                    CollisionAreaType.PhysicalWall, CollisionAreaType.None, CollisionAreaType.None);
            }

            // Create a bounding volume for the whole wall.
            Vector2 min = new Vector2(float.MaxValue);
            Vector2 max = new Vector2(float.MinValue);
            foreach (VertexPositionNormalTexture vertex in this.vertexData)
            {
                Vector2 vertexPos = new Vector2(vertex.Position.X, vertex.Position.Y);
                min = Vector2.Min(min, vertexPos);
                max = Vector2.Max(max, vertexPos);
            }
            Rectangle boundingArea = new Rectangle(min, max);
            collisionAreas[collisionAreas.Length - 1] = new CollisionArea("Bounding", boundingArea, this,
                CollisionAreaType.WallBounds, CollisionAreaType.None, CollisionAreaType.None);
        }

        #endregion Protected methods

        #region Private methods

        /// <summary>
        /// Fines the wall's 3D model's triangles.
        /// </summary>
        private void FineTriangles()
        {
            VertexPositionNormalTexture[] fineVertexData;
            short[] fineIndexData;
            Graphics3D.FineTriangles(15, vertexData, indexData, out fineVertexData, out fineIndexData);
            indexData = fineIndexData;
            vertexData = fineVertexData;
        }

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
            GraphicsAdapter gfxAdapter = gfx.CreationParameters.Adapter;
            if (!gfxAdapter.CheckDeviceFormat(DeviceType.Hardware, gfx.DisplayMode.Format,
                TextureUsage.None, QueryUsages.None, ResourceType.RenderTarget, SurfaceFormat.Color))
                throw new Exception("Cannot create render target of type SurfaceFormat.Color");
            int targetSize = Math.Min(
                AWMathHelper.FloorPowerTwo(Math.Min(gfxCaps.MaxTextureHeight, gfxCaps.MaxTextureWidth)),
                AWMathHelper.CeilingPowerTwo(Math.Max(indexMap.GetLength(1), indexMap.GetLength(0))));
            RenderTarget2D maskTarget = null;
            while (maskTarget == null)
                try
                {
                    maskTarget = new RenderTarget2D(gfx, targetSize, targetSize, 1, SurfaceFormat.Color);
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
