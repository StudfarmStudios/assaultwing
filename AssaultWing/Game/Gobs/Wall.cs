#define VERY_SMALL_TRIANGLES_ARE_COLLIDABLE // TODO: #undefine
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.GobUtils;
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

        private WallIndexMap _indexMap;

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
        private static BasicEffect g_maskEff; // !!! remove eventually when index maps are created in ArenaEditor

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
            g_maskEff = g_maskEff ?? WallIndexMap.CreateIndexMapEffect(gfx);
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
                if (AssaultWing.Instance.NetworkMode != NetworkMode.Client) Prepare3DModel();
                _indexMap = CreateIndexMap();
#if !VERY_SMALL_TRIANGLES_ARE_COLLIDABLE
                RemoveVerySmallTrianglesFromCollisionAreas();
#endif
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

        public WallIndexMap CreateIndexMap()
        {
            var indexMap = new WallIndexMap(RemoveTriangle, g_maskEff, _vertexData, _indexData, GetBoundingBox());
#if VERY_SMALL_TRIANGLES_ARE_COLLIDABLE
            indexMap.ForceVerySmallTrianglesIntoIndexMap(_vertexData, _indexData);
#endif
            return indexMap;
        }

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
            Vector2 posInIndexMap = Vector2.Transform(holePos, _indexMap.WallToIndexMapTransform).Round();
            int indexMapWidth = _indexMap.Width;
            int indexMapHeight = _indexMap.Height;
            var removeIndices = new List<int>();
            AWMathHelper.FillCircle((int)posInIndexMap.X, (int)posInIndexMap.Y, (int)Math.Round(holeRadius), (x, y) =>
            {
                if (x < 0 || y < 0 || x >= indexMapWidth || y >= indexMapHeight) return;
                _indexMap.Remove(x, y);
            });

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
            foreach (int index in triangleIndices) RemoveTriangle(index);
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

        private void RemoveVerySmallTrianglesFromCollisionAreas()
        {
            foreach (int index in _indexMap.GetVerySmallTriangles()) collisionAreas[index] = null;
            TriangleCount -= _indexMap.GetVerySmallTriangles().Count();
        }

        private void RemoveTriangle(int index)
        {
            // Replace the triangle in the 3D model with a trivial one.
            _indexData[3 * index + 0] = 0;
            _indexData[3 * index + 1] = 0;
            _indexData[3 * index + 2] = 0;

            Arena.Unregister(collisionAreas[index]);
            --TriangleCount;
        }

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
            _silhouetteEffect = Effect == null ? null : (BasicEffect)Effect.Clone(AssaultWing.Instance.GraphicsDevice);
            FineTriangles();
            TriangleCount = _indexData.Length / 3;
            CreateCollisionAreas();
        }

        private void CreateCollisionAreas()
        {
            // Create one collision area for each triangle in the wall's 3D model.
            collisionAreas = new CollisionArea[_indexData.Length / 3 + 1];
            for (int i = 0; i + 2 < _indexData.Length; i += 3)
            {
                // Create a physical collision area for this triangle.
                var v1 = _vertexData[_indexData[i + 0]].Position;
                var v2 = _vertexData[_indexData[i + 1]].Position;
                var v3 = _vertexData[_indexData[i + 2]].Position;
                var triangleArea = new Triangle(
                    new Vector2(v1.X, v1.Y),
                    new Vector2(v2.X, v2.Y),
                    new Vector2(v3.X, v3.Y));
                collisionAreas[i / 3] = new CollisionArea("General", triangleArea, this,
                    CollisionAreaType.PhysicalWall, CollisionAreaType.None, CollisionAreaType.None, CollisionMaterialType.Rough);
            }

            // Create a collision bounding volume for the whole wall.
            collisionAreas[collisionAreas.Length - 1] = new CollisionArea("Bounding", GetBoundingBox(), this,
                CollisionAreaType.WallBounds, CollisionAreaType.None, CollisionAreaType.None, CollisionMaterialType.Rough);
        }

        private Rectangle GetBoundingBox()
        {
            var positions = _vertexData.Select(vertex => new Vector2(vertex.Position.X, vertex.Position.Y));
            var min = positions.Aggregate((v1, v2) => Vector2.Min(v1, v2));
            var max = positions.Aggregate((v1, v2) => Vector2.Max(v1, v2));
            var boundingArea = new Rectangle(min, max);
            return boundingArea;
        }

        #endregion Private methods
    }
}
