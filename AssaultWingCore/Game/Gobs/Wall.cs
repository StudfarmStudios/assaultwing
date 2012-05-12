//#define VERY_SMALL_TRIANGLES_ARE_COLLIDABLE // #define this only if large areas of walls become fly-through
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game.Collisions;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;
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

        [RuntimeState]
        private bool _destructible;

        /// <summary>
        /// The location of the wall's vertices in the game world.
        /// </summary>
        private VertexPositionNormalTexture[] _vertexData;

        /// <summary>
        /// The index data where every consecutive index triplet signifies
        /// one triangle. The indices index 'vertexData'.
        /// </summary>
        protected short[] _indexData;

        private WallIndexMap _indexMap;
        private List<int> _removedTriangleIndices;
        private List<int> _removedTriangleIndicesToSerialize;
        private List<int> _removedTriangleIndicesOfAllTime;

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

        public static int WallActivatedCounter { get; set; }

        private Matrix WorldToIndexMapTransform
        {
            get
            {
                return Matrix.CreateTranslation(-Pos.X, -Pos.Y, 0)
                    * _indexMap.WallToIndexMapTransform;
            }
        }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Wall()
        {
            Movable = false;
            _destructible = true;
            Set3DModel(new[] 
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
            _removedTriangleIndicesToSerialize = new List<int>();
            _removedTriangleIndicesOfAllTime = new List<int>();
        }

        #region Methods related to gobs' functionality in the game world

        public override void Activate()
        {
            base.Activate();
            if (Arena.IsForPlaying)
            {
                var binReader = new System.IO.BinaryReader(Arena.Bin[StaticID]);
                var gobVertices = _vertexData.Select(AWMathHelper.ProjectXY).ToArray();
                var worldMatrix = Matrix.CreateRotationZ(Rotation); // FIXME: Use WorldMatrix or nothing
                Vector2.Transform(gobVertices, ref worldMatrix, gobVertices);
                var boundingBox = GetBoundingBox(gobVertices);
                _indexMap = new WallIndexMap(_removedTriangleIndices.Add, boundingBox, binReader);
                binReader.Close();
                CreateCollisionAreas();
            }
            WallActivatedCounter++;
        }

        public override void Update()
        {
            base.Update();
            foreach (int index in _removedTriangleIndices) RemoveTriangle(index);
            _removedTriangleIndices.Clear();
            if (TriangleCount == 0) Die();
        }

        public override void Draw3D(Matrix view, Matrix projection, Player viewer)
        {
            if (!Arena.IsForPlaying)
            {
                base.Draw3D(view, projection, viewer);
                return;
            }
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
            Effect.World = WorldMatrix;
            Effect.Projection = projection;
            Effect.View = view;
            Effect.Texture = Texture;
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
        /// Assumes that the SpriteBatch has been Begun already and will be
        /// Ended later by someone else.
        /// </summary>
        public void DrawSilhouette(Matrix view, Matrix projection, SpriteBatch spriteBatch)
        {
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
            var silhouetteEffect = Game.GraphicsEngine.GameContent.WallSilhouetteEffect;
            silhouetteEffect.World = WorldMatrix;
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
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                // HACK to reduce network traffic
                var reducedMode = (mode & SerializationModeFlags.ConstantDataFromServer) != 0
                    ? SerializationModeFlags.AllFromServer
                    : SerializationModeFlags.None;
                base.Serialize(writer, reducedMode);
                checked
                {
                    if (mode != SerializationModeFlags.None)
                    {
                        var indices = (mode & SerializationModeFlags.ConstantDataFromServer) != 0
                            ? _removedTriangleIndicesOfAllTime
                            : _removedTriangleIndicesToSerialize;
                        writer.Write((short)indices.Count());
                        foreach (short index in indices) writer.Write((short)index);
                        if ((mode & SerializationModeFlags.VaryingDataFromServer) != 0) _removedTriangleIndicesToSerialize.Clear();
                    }
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            // HACK to reduce network traffic
            var reducedMode = (mode & SerializationModeFlags.ConstantDataFromServer) != 0
                ? SerializationModeFlags.AllFromServer
                : SerializationModeFlags.None;
            base.Deserialize(reader, reducedMode, framesAgo);
            if (mode != SerializationModeFlags.None)
            {
                int indexCount = reader.ReadInt16();
                _removedTriangleIndices.Capacity += indexCount;
                for (int i = 0; i < indexCount; i++) _removedTriangleIndices.Add(reader.ReadInt16());
            }
        }

        public WallIndexMap CreateIndexMap()
        {
            var transformedVertexPositions = new Vector2[_vertexData.Length];
            var transformation = Matrix.CreateRotationZ(Rotation);
            Vector2.Transform(_vertexData.Select(AWMathHelper.ProjectXY).ToArray(),
                ref transformation, transformedVertexPositions);
            var indexMap = new WallIndexMap(_removedTriangleIndices.Add, GetBoundingBox(transformedVertexPositions),
                transformedVertexPositions, _indexData);
#if VERY_SMALL_TRIANGLES_ARE_COLLIDABLE
            indexMap.ForceVerySmallTrianglesIntoIndexMap(_vertexData, _indexData);
#endif
            return indexMap;
        }

        /// <summary>
        /// Removes a round area from this wall, i.e. makes a hole.
        /// Returns the number of pixels removed
        /// </summary>
        /// <param name="holePos">Center of the hole, in world coordinates.</param>
        /// <param name="holeRadius">Radius of the hole, in meters.</param>
        public int MakeHole(Vector2 holePos, float holeRadius)
        {
            if (!_destructible || holeRadius <= 0) return 0;
            if (Game.NetworkMode == NetworkMode.Client) return 0;
            var posInIndexMap = Vector2.Transform(holePos, WorldToIndexMapTransform).Round();
            var removeCount = 0;
            AWMathHelper.FillCircle((int)posInIndexMap.X, (int)posInIndexMap.Y, (int)Math.Round(holeRadius),
                (x, y) => { if (_indexMap.Remove(x, y)) removeCount++; });
            if (Game.NetworkMode == NetworkMode.Server && _removedTriangleIndices.Any())
            {
                _removedTriangleIndicesToSerialize.AddRange(_removedTriangleIndices);
                _removedTriangleIndicesOfAllTime.AddRange(_removedTriangleIndices);
                ForcedNetworkUpdate = true;
            }
            return removeCount;
        }

        /// <summary>
        /// Removes some triangles from the wall's 3D model.
        /// </summary>
        public void MakeHole(IList<int> triangleIndices)
        {
            foreach (int index in triangleIndices) RemoveTriangle(index);
        }

        #region Protected methods

        protected override BoundingSphere CreateDrawBounds()
        {
            return Arena.IsForPlaying
                ? BoundingSphere.CreateFromPoints(_vertexData.Select(v => v.Position))
                : base.CreateDrawBounds();
        }

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

        private void RemoveTriangle(int index)
        {
            if (_collisionAreas[index] == null) return; // triangle already removed earlier this frame
            _collisionAreas[index].Destroy();
            _collisionAreas[index] = null;
            --TriangleCount;

            // Replace the triangle in the 3D model with a trivial one.
            _indexData[3 * index + 0] = 0;
            _indexData[3 * index + 1] = 0;
            _indexData[3 * index + 2] = 0;
        }

        private void CreateCollisionAreas()
        {
#if !VERY_SMALL_TRIANGLES_ARE_COLLIDABLE
            var verySmallTriangles = _indexMap.GetVerySmallTriangles(); // sorted in increasing order
#else
            var verySmallTriangles = new List<int>();
#endif
            TriangleCount = _indexData.Length / 3 - verySmallTriangles.Count();
            _collisionAreas = new CollisionArea[_indexData.Length / 3];
            var smallTriangleEnumerator = verySmallTriangles.GetEnumerator();
            var smallTrianglesRemaining = smallTriangleEnumerator.MoveNext();
            for (int i = 0; i + 2 < _indexData.Length; i += 3)
            {
                if (smallTrianglesRemaining && smallTriangleEnumerator.Current == i / 3)
                {
                    smallTrianglesRemaining = smallTriangleEnumerator.MoveNext();
                    continue;
                }
                var v1 = _vertexData[_indexData[i + 0]];
                var v2 = _vertexData[_indexData[i + 1]];
                var v3 = _vertexData[_indexData[i + 2]];
                var triangleArea = new Triangle(v1.ProjectXY(), v2.ProjectXY(), v3.ProjectXY());
                _collisionAreas[i / 3] = new CollisionArea("General", triangleArea, this, CollisionAreaType.Static, CollisionMaterialType.Rough);
            }
        }

        private Rectangle GetBoundingBox(IEnumerable<Vector2> vertexPositions)
        {
            var min = vertexPositions.Aggregate((v1, v2) => Vector2.Min(v1, v2));
            var max = vertexPositions.Aggregate((v1, v2) => Vector2.Max(v1, v2));
            var boundingArea = new Rectangle(min, max);
            return boundingArea;
        }

        #endregion Private methods
    }
}
