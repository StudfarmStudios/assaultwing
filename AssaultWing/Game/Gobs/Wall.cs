using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Net.Messages;
using Rectangle = AW2.Helpers.Geometric.Rectangle;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A piece of wall.
    /// </summary>
    /// Note that a wall has no position or movement like other gobs have. 
    /// Instead, a wall acts like a polygon. For visual purposes, walls have 
    /// also a third dimension.
    public class Wall : Gob
    {
        #region Wall Fields

        /// <summary>
        /// The location of the wall's vertices in the game world.
        /// </summary>
        protected VertexPositionNormalTexture[] _vertexData;

        /// <summary>
        /// The index data where every consequtive index triplet signifies
        /// one triangle. The indices index 'vertexData'.
        /// </summary>
        protected short[] _indexData;

        /// <summary>
        /// Triangle index map of the wall's 3D model in the X-Y plane.
        /// </summary>
        /// If indexMap[y,x] == null then no triangle covers index map point (x,y).
        /// Otherwise indexMap[y,x] is an array of indices n such that 
        /// the triangle that is defined by the 3D model's index map elements 
        /// 3n, 3n+1 and 3n+2 covers the index map point (x,y).
        /// The index map has its own coordinate system that can be obtained from
        /// the 3D model's coordinate system by <b>indexMapTransform</b>.
        private int[,][] _indexMap;

        /// <summary>
        /// Transformation matrix from wall's 3D model's coordinates to index map coordinates.
        /// </summary>
        private Matrix _indexMapTransform;

        /// <summary>
        /// Triangle cover counts of the wall's 3D model.
        /// </summary>
        /// Index n corresponds to the triangle defined by the 3D model's
        /// index list members 3n, 3n+1 and 3n+2. A positive cover count signifies
        /// the number of index map points covered by the triangle that still 
        /// need to be deleted before the triangle is erased from the 3D model.
        /// A negative cover count marks a deleted triangle.
        private int[] _triangleCovers;

        /// <summary>
        /// The number of triangles in the wall's 3D model not yet removed.
        /// </summary>
        protected int TriangleCount { get; set; }

        /// <summary>
        /// The texture to draw the wall's 3D model with.
        /// </summary>
        protected Texture2D Texture { get; set; }

        /// <summary>
        /// The effect for drawing the wall.
        /// </summary>
        protected BasicEffect Effect { get; set; }

        /// <summary>
        /// The effect for drawing the wall as a silhouette.
        /// </summary>
        private BasicEffect _silhouetteEffect;

        /// <summary>
        /// The default effect for drawing the wall.
        /// </summary>
        private static BasicEffect g_defaultEffect;

        /// <summary>
        /// The default effect for drawing the wall as a silhouette.
        /// </summary>
        private static BasicEffect g_defaultSilhouetteEffect;

        /// <summary>
        /// Effect for drawing data for index maps.
        /// </summary>
        private static BasicEffect g_maskEff;

        private VertexDeclaration _vertexDeclaration;

        #endregion // Wall Fields

        #region Properties

        /// <summary>
        /// Returns the world matrix of the gob, i.e., the translation from
        /// game object coordinates to game world coordinates.
        /// </summary>
        public override Matrix WorldMatrix
        {
            get
            {
                return Arena.IsForPlaying
                    ? Matrix.Identity
                    : base.WorldMatrix;
            }
        }

        /// <summary>
        /// Bounding volume of the visuals of the gob, in world coordinates.
        /// </summary>
        public override BoundingSphere DrawBounds
        {
            get
            {
                return Arena.IsForPlaying
                    ? drawBounds
                    : base.DrawBounds;
            }
        }

        #endregion Properties

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Wall()
        {
            Set3DModel(new VertexPositionNormalTexture[] 
                {
                    new VertexPositionNormalTexture(new Vector3(0,0,0), -Vector3.UnitX, Vector2.Zero),
                    new VertexPositionNormalTexture(new Vector3(100,0,0), Vector3.UnitX, Vector2.UnitX),
                    new VertexPositionNormalTexture(new Vector3(0,100,0), Vector3.UnitY, Vector2.UnitY),
                },
                new short[] { 0, 1, 2 },
                null, null);
        }

        /// <summary>
        /// Creates a piece of wall.
        /// </summary>
        /// <param name="typeName">The type of the wall.</param>
        public Wall(CanonicalString typeName)
            : base(typeName)
        {
            movable = false;
        }

        #region Methods related to gobs' functionality in the game world

        public override void LoadContent()
        {
            var gfx = AssaultWing.Instance.GraphicsDevice;
            g_defaultEffect = g_defaultEffect ?? new BasicEffect(gfx, null);
            g_defaultSilhouetteEffect = g_defaultSilhouetteEffect ?? (BasicEffect)g_defaultEffect.Clone(gfx);
            g_maskEff = g_maskEff ?? (BasicEffect)g_defaultEffect.Clone(gfx);
            _silhouetteEffect = g_defaultSilhouetteEffect;
            _vertexDeclaration = _vertexDeclaration ?? new VertexDeclaration(gfx, VertexPositionNormalTexture.VertexElements);
            base.LoadContent();
        }

        public override void UnloadContent()
        {
            // Must not dispose 'defaultSilhouetteEffect' because others may be using it.
            // Must not dispose 'silhouetteEffect' because it may refer to 'defaultSilhouetteEffect'.
            // 'texture' will be disposed by the graphics engine.
            // 'effect' is managed by other objects
            _silhouetteEffect = null;
            if (g_defaultEffect != null)
            {
                g_defaultEffect.Dispose();
                g_defaultEffect = null;
            }
            if (g_maskEff != null)
            {
                g_maskEff.Dispose();
                g_maskEff = null;
            }
            if (_vertexDeclaration != null)
            {
                _vertexDeclaration.Dispose();
                _vertexDeclaration = null;
            }
            base.UnloadContent();
        }

        public override void Activate()
        {
            base.Activate();
            if (Arena.IsForPlaying)
            {
                if (AssaultWing.Instance.NetworkMode != NetworkMode.Client)
                    Prepare3DModel();
                InitializeIndexMap();
                drawBounds = BoundingSphere.CreateFromPoints(_vertexData.Select(v => v.Position));
            }
            AssaultWing.Instance.DataEngine.ProgressBar.SubtaskCompleted();
        }

        public override void Draw(Matrix view, Matrix projection)
        {
            if (!Arena.IsForPlaying)
            {
                base.Draw(view, projection);
                return;
            }
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            gfx.VertexDeclaration = _vertexDeclaration;
            Effect.World = Matrix.Identity;
            Effect.Projection = projection;
            Effect.View = view;
            Effect.Texture = Texture;
            Effect.TextureEnabled = true;
            Arena.PrepareEffect(Effect);
            Effect.Begin();
            foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                gfx.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(
                    PrimitiveType.TriangleList, _vertexData, 0, _vertexData.Length, _indexData, 0, _indexData.Length / 3);
                pass.End();
            }
            Effect.End();
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
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            gfx.VertexDeclaration = _vertexDeclaration;
            _silhouetteEffect.World = Matrix.Identity;
            _silhouetteEffect.Projection = projection;
            _silhouetteEffect.View = view;
            _silhouetteEffect.Texture = Texture;
            _silhouetteEffect.VertexColorEnabled = false;
            _silhouetteEffect.LightingEnabled = false;
            _silhouetteEffect.TextureEnabled = false;
            _silhouetteEffect.FogEnabled = false;
            _silhouetteEffect.Begin();
            foreach (EffectPass pass in _silhouetteEffect.CurrentTechnique.Passes)
            {
                pass.Begin();
                gfx.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(
                    PrimitiveType.TriangleList, _vertexData, 0, _vertexData.Length, _indexData, 0, _indexData.Length / 3);
                pass.End();
            }
            _silhouetteEffect.End();
        }

        #endregion Methods related to gobs' functionality in the game world

        /// <summary>
        /// Removes a round area from this wall, i.e. makes a hole.
        /// </summary>
        /// <param name="holePos">Center of the hole, in world coordinates.</param>
        /// <param name="holeRadius">Radius of the hole, in meters.</param>
        public void MakeHole(Vector2 holePos, float holeRadius)
        {
            if (holeRadius <= 0) return;
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client) return;

            // Eat a round hole.
            Vector2 posInIndexMap = Vector2.Transform(holePos, _indexMapTransform);
            int indexMapWidth = _indexMap.GetLength(1);
            int indexMapHeight = _indexMap.GetLength(0);
            var removeIndices = new List<int>();
            AWMathHelper.FillCircle((int)Math.Round(posInIndexMap.X), (int)Math.Round(posInIndexMap.Y),
                (int)Math.Round(holeRadius), (x, y) =>
            {
                if (x < 0 || y < 0 || x >= indexMapWidth || y >= indexMapHeight) return;
                if (_indexMap[y, x] == null) return;
                foreach (int index in _indexMap[y, x])
                    if (--_triangleCovers[index] == 0)
                        removeIndices.Add(index);
            });
            MakeHole(removeIndices);

            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server && removeIndices.Any())
            {
                var message = new WallHoleMessage { GobId = Id, TriangleIndices = removeIndices };
                AssaultWing.Instance.NetworkEngine.GameClientConnections.Send(message);
            }

            // Remove the wall gob if all its triangles have been removed.
            if (TriangleCount == 0)
                Die(new DeathCause());
        }

        /// <summary>
        /// Removes some triangles from the wall's 3D model.
        /// </summary>
        public void MakeHole(IList<int> triangleIndices)
        {
            foreach (int index in triangleIndices)
            {
                // Replace the triangle in the 3D model with a trivial one.
                _indexData[3 * index + 0] = 0;
                _indexData[3 * index + 1] = 0;
                _indexData[3 * index + 2] = 0;

                Arena.Unregister(collisionAreas[index]);
            }
            TriangleCount -= triangleIndices.Count();
        }

        #region Methods related to serialisation

        public override void Serialize(Net.NetworkBinaryWriter writer, Net.SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((int)_vertexData.Length);
                foreach (var vertex in _vertexData)
                    writer.WriteHalf((VertexPositionNormalTexture)vertex);
                writer.Write((int)_indexData.Length);
                foreach (var index in _indexData)
                    writer.Write((short)index);
            }
        }

        public override void Deserialize(Net.NetworkBinaryReader reader, Net.SerializationModeFlags mode, TimeSpan messageAge)
        {
            base.Deserialize(reader, mode, messageAge);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                int vertexDataLength = reader.ReadInt32();
                _vertexData = new VertexPositionNormalTexture[vertexDataLength];
                for (int i = 0; i < vertexDataLength; ++i)
                    _vertexData[i] = reader.ReadHalfVertexPositionTextureNormal();
                int indexDataLength = reader.ReadInt32();
                _indexData = new short[indexDataLength];
                for (int i = 0; i < indexDataLength; ++i)
                    _indexData[i] = reader.ReadInt16();
                CreateCollisionAreas();
            }
        }

        #endregion Methods related to serialisation

        #region Protected methods

        /// <summary>
        /// Sets the wall's 3D model. To be called before the wall is Activate()d.
        /// </summary>
        protected void Set3DModel(VertexPositionNormalTexture[] vertexData, short[] indexData, Texture2D texture, BasicEffect effect)
        {
            _vertexData = vertexData;
            _indexData = indexData;
            Texture = texture;
            Effect = effect;
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
            Graphics3D.FineTriangles(50, _vertexData, _indexData, out fineVertexData, out fineIndexData);
            _indexData = fineIndexData;
            _vertexData = fineVertexData;
        }

        /// <summary>
        /// Prepares the wall's 3D model for use in gameplay.
        /// </summary>
        private void Prepare3DModel()
        {
            var gfx = AssaultWing.Instance.GraphicsDevice;
            _silhouetteEffect = Effect == null ? null : (BasicEffect)Effect.Clone(gfx);
            FineTriangles();
            TriangleCount = this._indexData.Length / 3;
            CreateCollisionAreas();
        }

        private void CreateCollisionAreas()
        {
            // Create one collision area for each triangle in the wall's 3D model.
            collisionAreas = new CollisionArea[this._indexData.Length / 3 + 1];
            for (int i = 0; i + 2 < this._indexData.Length; i += 3)
            {
                // Create a physical collision area for this triangle.
                Vector3 v1 = this._vertexData[this._indexData[i + 0]].Position;
                Vector3 v2 = this._vertexData[this._indexData[i + 1]].Position;
                Vector3 v3 = this._vertexData[this._indexData[i + 2]].Position;
                IGeomPrimitive triangleArea = new Triangle(
                    new Vector2(v1.X, v1.Y),
                    new Vector2(v2.X, v2.Y),
                    new Vector2(v3.X, v3.Y));
                collisionAreas[i / 3] = new CollisionArea("General", triangleArea, this,
                    CollisionAreaType.PhysicalWall, CollisionAreaType.None, CollisionAreaType.None, CollisionMaterialType.Rough);
            }

            // Create a collision bounding volume for the whole wall.
            var positions = _vertexData.Select(vertex => new Vector2(vertex.Position.X, vertex.Position.Y));
            var min = positions.Aggregate((v1, v2) => Vector2.Min(v1, v2));
            var max = positions.Aggregate((v1, v2) => Vector2.Max(v1, v2));
            var boundingArea = new Rectangle(min, max);
            collisionAreas[collisionAreas.Length - 1] = new CollisionArea("Bounding", boundingArea, this,
                CollisionAreaType.WallBounds, CollisionAreaType.None, CollisionAreaType.None, CollisionMaterialType.Rough);
        }

        /// <summary>
        /// Initialises the wall's index map from the wall's 3D model.
        /// </summary>
        private void InitializeIndexMap()
        {
            var boundingArea = collisionAreas.First(area => area.Name == "Bounding").Area.BoundingBox;
            var modelMin = boundingArea.Min;
            var modelDim = boundingArea.Dimensions;

            // Create an index map for the model.
            // The mask is initialised by a render of the 3D model by the graphics card.
            _indexMap = new int[(int)Math.Ceiling(modelDim.Y) + 1, (int)Math.Ceiling(modelDim.X) + 1][];
            _indexMapTransform = Matrix.CreateTranslation(-modelMin.X, -modelMin.Y, 0);

            // Create colour-coded vertices for each triangle.
            VertexPositionColor[] colouredVertexData = new VertexPositionColor[_indexData.Length];
            for (int indexI = 0; indexI < _indexData.Length; ++indexI)
            {
                VertexPositionNormalTexture originalVertex = _vertexData[_indexData[indexI]];
                Color color = new Color((byte)((indexI / 3) % 256), (byte)((indexI / 3 / 256) % 256), (byte)((indexI / 3 / 256 / 256) % 256));
                colouredVertexData[indexI] = new VertexPositionColor(originalVertex.Position, color);
            }

            // Draw the colour-coded triangles on our own render target for
            // index map initialisation. Render target will be a square with
            // size ('targetSize') a power of two to meet the demands of some
            // graphics devices. If the model dimensions are larger than 
            // 'targetSize', we will have to render the coloured triangles in pieces.

            // This method is run usually in a background thread -- during arena initialisation.
            // Therefore we have to tell the main draw routines to let us use the device in peace.
            // We break out of the lock regularly to allow others use the device, too.
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            RenderTarget2D maskTarget = null;
            int targetSize = -1;
            lock (gfx) CreateMaskTarget(out maskTarget, out targetSize);

            // Set up the effect.
            g_maskEff.VertexColorEnabled = true;
            g_maskEff.LightingEnabled = false;
            g_maskEff.TextureEnabled = false;
            g_maskEff.View = Matrix.CreateLookAt(new Vector3(0, 0, 1000), Vector3.Zero, Vector3.Up);
            g_maskEff.Projection = Matrix.CreateOrthographicOffCenter(0, targetSize - 1,
                0, targetSize - 1, 10, 1000);
            g_maskEff.World = _indexMapTransform;

            // Draw the coloured triangles in as many parts as necessary to cover 
            // the whole model with one unit in world coordinates corresponding to
            // one pixel width in the render target.
            for (int startY = 0; startY < _indexMap.GetLength(0); startY += targetSize)
                for (int startX = 0; startX < _indexMap.GetLength(1); )
                    try
                    {
                        lock (gfx) ComputeIndexMapFragment(colouredVertexData, maskTarget, targetSize, startY, startX);
                        startX += targetSize;
                        System.Threading.Thread.Sleep(0);
                    }
                    // Some exceptions may be thrown if the graphics card is reset e.g.
                    // by a window resize. Just retry.
                    catch (NullReferenceException) { }
                    catch (InvalidOperationException) { }

            // Initialise triangle cover counts.
            _triangleCovers = new int[_indexData.Length / 3];
            foreach (int[] indices in _indexMap)
                if (indices != null)
                    foreach (int index in indices)
                        ++_triangleCovers[index];

            // If some triangle isn't mentioned in the index map, force it there.
            for (int i = 0; i < _triangleCovers.Length; ++i)
                if (_triangleCovers[i] == 0)
                {
                    Vector3 vert0 = _vertexData[_indexData[3 * i + 0]].Position;
                    Vector3 vert1 = _vertexData[_indexData[3 * i + 1]].Position;
                    Vector3 vert2 = _vertexData[_indexData[3 * i + 2]].Position;
                    Vector3 triangleCenter = (vert0 + vert1 + vert2) / 3;
                    Vector3 centerInIndexMap = Vector3.Transform(triangleCenter, _indexMapTransform);
                    int centerInIndexMapX = (int)(Math.Round(centerInIndexMap.X) + 0.1);
                    int centerInIndexMapY = (int)(Math.Round(centerInIndexMap.Y) + 0.1);
                    int[] oldIndices = _indexMap[centerInIndexMapY, centerInIndexMapX];
                    int[] newIndices = null;
                    if (oldIndices != null)
                    {
                        newIndices = new int[oldIndices.Length + 1];
                        Array.Copy(oldIndices, newIndices, oldIndices.Length);
                        newIndices[oldIndices.Length] = i;
                    }
                    else
                        newIndices = new int[] { i };
                    _indexMap[centerInIndexMapY, centerInIndexMapX] = newIndices;
                    ++_triangleCovers[i];
                }
        }

        private void CreateMaskTarget(out RenderTarget2D maskTarget, out int targetSize)
        {
            var gfx = AssaultWing.Instance.GraphicsDevice;
            var gfxCaps = gfx.GraphicsDeviceCapabilities;
            var gfxAdapter = gfx.CreationParameters.Adapter;
            if (!gfxAdapter.CheckDeviceFormat(DeviceType.Hardware, gfx.DisplayMode.Format,
                TextureUsage.None, QueryUsages.None, ResourceType.RenderTarget, SurfaceFormat.Color))
                throw new ApplicationException("Cannot create render target of type SurfaceFormat.Color");
            targetSize = Math.Min(
                AWMathHelper.FloorPowerTwo(Math.Min(gfxCaps.MaxTextureHeight, gfxCaps.MaxTextureWidth)),
                AWMathHelper.CeilingPowerTwo(Math.Max(_indexMap.GetLength(1), _indexMap.GetLength(0))));
            maskTarget = null;
            while (maskTarget == null)
                try
                {
                    maskTarget = new RenderTarget2D(gfx, targetSize, targetSize, 1, SurfaceFormat.Color);
                }
                catch (OutOfVideoMemoryException)
                {
                    targetSize /= 2;
                }
                catch (Exception e)
                {
                    throw new ApplicationException("Cannot create render target for index map creation", e);
                }
        }

        private void ComputeIndexMapFragment(VertexPositionColor[] colouredVertexData,
            RenderTarget2D maskTarget, int targetSize, int startY, int startX)
        {
            var gfx = AssaultWing.Instance.GraphicsDevice;

            // Set up graphics device.
            var oldVertexDeclaration = gfx.VertexDeclaration;
            var oldDepthStencilBuffer = gfx.DepthStencilBuffer;
            gfx.VertexDeclaration = new VertexDeclaration(gfx, VertexPositionColor.VertexElements);
            gfx.DepthStencilBuffer = null;

            // Move view to current start coordinates.
            g_maskEff.View = Matrix.CreateLookAt(new Vector3(startX, startY, 1000), new Vector3(startX, startY, 0), Vector3.Up);

            // Set and clear our own render target.
            gfx.SetRenderTarget(0, maskTarget);
            gfx.Clear(ClearOptions.Target, Color.White, 0, 0);

            // Draw the coloured triangles.
            g_maskEff.Begin();
            foreach (EffectPass pass in g_maskEff.CurrentTechnique.Passes)
            {
                pass.Begin();
                gfx.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList,
                    colouredVertexData, 0, colouredVertexData.Length / 3);
                pass.End();
            }
            g_maskEff.End();

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
                    int indexMapY = startY + targetSize - 1 - y;
                    int indexMapX = startX + x;
                    if (indexMapY >= _indexMap.GetLength(0) || indexMapX >= _indexMap.GetLength(1))
                        throw new IndexOutOfRangeException(string.Format("Index map overflow (x={0}, y={1}), color={2}", indexMapX, indexMapY, color));
                    int maskValue = color.R + color.G * 256 + color.B * 256 * 256;
                    _indexMap[indexMapY, indexMapX] = new int[] { maskValue };
                }

            // Restore graphics device's old settings.
            gfx.VertexDeclaration = oldVertexDeclaration;
            gfx.DepthStencilBuffer = oldDepthStencilBuffer;
            maskTarget.Dispose();
        }

        #endregion Private methods
    }
}
