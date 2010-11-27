//#define VERY_SMALL_TRIANGLES_ARE_COLLIDABLE // #define this only if large areas of walls become fly-through
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;
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
        private List<int> _removedTriangleIndices;

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
                    ? _drawBounds
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

        public Wall(CanonicalString typeName)
            : base(typeName)
        {
            _removedTriangleIndices = new List<int>();
            _movable = false;
        }

        #region Methods related to gobs' functionality in the game world

        public override void Activate()
        {
            base.Activate();
            if (Arena.IsForPlaying)
            {
                Prepare3DModel();
                var binReader = new System.IO.BinaryReader(Arena.Bin[StaticID]);
                var boundingBox = CollisionAreas.Single(area => area.Name == "Bounding").Area.BoundingBox;
                _indexMap = new WallIndexMap(RemoveTriangle, boundingBox, binReader);
                binReader.Close();
#if !VERY_SMALL_TRIANGLES_ARE_COLLIDABLE
                RemoveVerySmallTrianglesFromCollisionAreas();
#endif
                _drawBounds = BoundingSphere.CreateFromPoints(_vertexData.Select(v => v.Position));
            }
            Game.DataEngine.ProgressBar.SubtaskCompleted();
        }

        public override void Draw(Matrix view, Matrix projection)
        {
            if (!Arena.IsForPlaying)
            {
                base.Draw(view, projection);
                return;
            }
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
            Effect.World = Matrix.Identity;
            Effect.Projection = projection;
            Effect.View = view;
            Effect.Texture = Texture;
            Effect.TextureEnabled = true;
            Arena.PrepareEffect(Effect);
            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gfx.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(
                    PrimitiveType.TriangleList, _vertexData, 0, _vertexData.Length, _indexData, 0, _indexData.Length / 3);
            }
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
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
            var silhouetteEffect = Game.GraphicsEngine.GameContent.WallSilhouetteEffect;
            silhouetteEffect.Projection = projection;
            silhouetteEffect.View = view;
            silhouetteEffect.Texture = Texture;
            foreach (var pass in silhouetteEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gfx.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(
                    PrimitiveType.TriangleList, _vertexData, 0, _vertexData.Length, _indexData, 0, _indexData.Length / 3);
            }
        }

        #endregion Methods related to gobs' functionality in the game world

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            // HACK to reduce network traffic
            var reducedMode = (mode & SerializationModeFlags.ConstantData) != 0
                ? SerializationModeFlags.All
                : SerializationModeFlags.None;
            base.Serialize(writer, reducedMode);
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            // HACK to reduce network traffic
            var reducedMode = (mode & SerializationModeFlags.ConstantData) != 0
                ? SerializationModeFlags.All
                : SerializationModeFlags.None;
            base.Deserialize(reader, reducedMode, framesAgo);
        }

        public WallIndexMap CreateIndexMap()
        {
            var indexMap = new WallIndexMap(RemoveTriangle, GetBoundingBox(), _vertexData, _indexData);
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
            if (Game.NetworkMode == NetworkMode.Client) return;

            // Eat a round hole.
            Vector2 posInIndexMap = Vector2.Transform(holePos, _indexMap.WallToIndexMapTransform).Round();
            _removedTriangleIndices.Clear();
            AWMathHelper.FillCircle((int)posInIndexMap.X, (int)posInIndexMap.Y, (int)Math.Round(holeRadius), _indexMap.Remove);

            if (Game.NetworkMode == NetworkMode.Server && _removedTriangleIndices.Any())
            {
                var message = new WallHoleMessage { GobID = ID, TriangleIndices = _removedTriangleIndices.ToList() };
                Game.NetworkEngine.SendToGameClients(message);
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
            foreach (int index in _indexMap.GetVerySmallTriangles()) _collisionAreas[index] = null;
            TriangleCount -= _indexMap.GetVerySmallTriangles().Count();
        }

        private void RemoveTriangle(int index)
        {
            // Replace the triangle in the 3D model with a trivial one.
            _indexData[3 * index + 0] = 0;
            _indexData[3 * index + 1] = 0;
            _indexData[3 * index + 2] = 0;

            _removedTriangleIndices.Add(index);
            Arena.Unregister(_collisionAreas[index]);
            --TriangleCount;
        }

        /// <summary>
        /// Prepares the wall's 3D model for use in gameplay.
        /// </summary>
        private void Prepare3DModel()
        {
            TriangleCount = _indexData.Length / 3;
            CreateCollisionAreas();
        }

        private void CreateCollisionAreas()
        {
            // Create one collision area for each triangle in the wall's 3D model.
            _collisionAreas = new CollisionArea[_indexData.Length / 3 + 1];
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
                _collisionAreas[i / 3] = new CollisionArea("General", triangleArea, this,
                    CollisionAreaType.PhysicalWall, CollisionAreaType.None, CollisionAreaType.None, CollisionMaterialType.Rough);
            }

            // Create a collision bounding volume for the whole wall.
            _collisionAreas[_collisionAreas.Length - 1] = new CollisionArea("Bounding", GetBoundingBox(), this,
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
