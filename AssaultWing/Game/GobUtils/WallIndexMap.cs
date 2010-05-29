using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AWRectangle = AW2.Helpers.Geometric.Rectangle;
using IndexMapData = AW2.Game.GobUtils.WallIndexMap.ArrayPlusArrayOfArraysData;
using TriInt = System.Int16;

namespace AW2.Game.GobUtils
{
    public class WallIndexMap : IAWSerializable
    {
        public delegate void RemoveTriangleDelegate(int index);

        /// <summary>
        /// Interface for different data models of wall index data. Each model has its
        /// benefits, hence the possibility to easily switch between them.
        /// </summary>
        internal abstract class IData : IEnumerable<TriInt>
        {
            public abstract int Width { get; protected set; }
            public abstract int Height { get; protected set; }
            public int[] TriangleCovers { get; protected set; }

            public abstract void Add(int x, int y, int index);
            public abstract IEnumerable<TriInt> Get(int x, int y);

            public void ComputeTriangleCovers(int triangleCount)
            {
                TriangleCovers = new int[triangleCount];
                for (int y = 0; y < Height; ++y)
                    for (int x = 0; x < Width; ++x)
                        foreach (TriInt index in Get(x, y))
                            ++TriangleCovers[index];
            }

            protected static int[] ReadTriangleCovers(System.IO.BinaryReader reader)
            {
                var triangleCount = reader.ReadInt16();
                var triangleCovers = new int[triangleCount];
                for (int i = 0; i < triangleCount; ++i)
                    triangleCovers[i] = reader.ReadInt16();
                return triangleCovers;
            }

            protected void SetData(int[,][] data)
            {
                if (data.GetLength(1) != Width || data.GetLength(0) != Height) throw new ArgumentException("Data array has wrong dimensions");
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        if (data[y, x] != null)
                            foreach (int index in data[y, x])
                                Add(x, y, checked((TriInt)index));
            }

            protected static void ListAppend(ref TriInt[] list, TriInt index)
            {
                TriInt[] newList = null;
                if (list != null)
                {
                    newList = new TriInt[list.Length + 1];
                    Array.Copy(list, newList, list.Length);
                    newList[list.Length] = index;
                }
                else
                    newList = new TriInt[] { index };
                list = newList;
            }

            public IEnumerator<TriInt> GetEnumerator()
            {
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        foreach (TriInt index in Get(x, y)) yield return index;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// Simple implementation.
        /// </summary>
        internal class ArrayOfArraysData : IData
        {
            private static TriInt[] g_emptyArray = new TriInt[0];

            /// <summary>
            /// Triangle index map of the wall's 3D model in the X-Y plane.
            /// </summary>
            /// If _indexMap[y,x] == null then no triangle covers index map point (x,y).
            /// Otherwise _indexMap[y,x] is an array of indices n such that 
            /// the triangle that is defined by the 3D model's index map elements 
            /// 3n, 3n+1 and 3n+2 covers the index map point (x,y).
            /// The index map has its own coordinate system that can be obtained from
            /// the 3D model's coordinate system by <see cref="IndexMapTransform"/>.
            private TriInt[,][] _indexMap;

            public override int Width { get; protected set; }
            public override int Height { get; protected set; }

            public ArrayOfArraysData(int width, int height)
            {
                Initialize(width, height);
            }

            public ArrayOfArraysData(System.IO.BinaryReader reader)
            {
                // Note: 'reader.ReadInt16()' below must match chosen type 'TriInt'
                if (sizeof(TriInt) != 2) throw new ApplicationException("Programmer error: ReadInt16() doesn't match index map element size (" + sizeof(TriInt) + ")");
                var headerBytes = reader.ReadBytes(HEADER_BYTES.Length);
                if (!headerBytes.SequenceEqual(HEADER_BYTES)) throw new ApplicationException("Unexpected serialized data");
                int width = reader.ReadInt16();
                int height = reader.ReadInt16();
                Initialize(width, height);
                TriangleCovers = ReadTriangleCovers(reader);
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                    {
                        int count = reader.ReadByte();
                        TriInt[] values = null;
                        if (count > 0)
                        {
                            values = new TriInt[count];
                            for (int i = 0; i < count; ++i) values[i] = reader.ReadInt16();
                        }
                        _indexMap[y, x] = values;
                    }
            }

            public override void Add(int x, int y, int index)
            {
                ListAppend(ref _indexMap[y, x], checked((TriInt)index));
            }

            public override IEnumerable<TriInt> Get(int x, int y)
            {
                if (_indexMap[y, x] == null) return g_emptyArray;
                return _indexMap[y, x];
            }

            private void Initialize(int width, int height)
            {
                Width = width;
                Height = height;
                _indexMap = new TriInt[height, width][];
            }
        }

        /// <summary>
        /// Seems to use less memory than <see cref="ArrayOfArraysData"/>.
        /// </summary>
        internal class ArrayPlusArrayOfArraysData : IData
        {
            private const TriInt NO_TRIANGLE = -1;

            /// <summary>
            /// Indexed as [y, x]. Contains one triangle index
            /// or NO_TRIANGLE if no triangles overlap the pixel.
            /// </summary>
            private TriInt[,] _baseIndexMap;

            /// <summary>
            /// Indexed as [y, x]. Contains the remaining triangle indices
            /// or null if less than two triangles overlap the pixel.
            /// </summary>
            private TriInt[,][] _extraIndexMap;

            public override int Width { get; protected set; }
            public override int Height { get; protected set; }

            public ArrayPlusArrayOfArraysData(int width, int height)
            {
                Initialize(width, height, true);
            }

            public ArrayPlusArrayOfArraysData(System.IO.BinaryReader reader)
            {
                // Note: 'reader.ReadInt16()' below must match chosen type 'TriInt'
                if (sizeof(TriInt) != 2) throw new ApplicationException("Programmer error: ReadInt16() doesn't match index map element size (" + sizeof(TriInt) + ")");
                var headerBytes = reader.ReadBytes(HEADER_BYTES.Length);
                if (!headerBytes.SequenceEqual(HEADER_BYTES)) throw new ApplicationException("Unexpected serialized data");
                int width = reader.ReadInt16();
                int height = reader.ReadInt16();
                Initialize(width, height, false);
                TriangleCovers = ReadTriangleCovers(reader);
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                    {
                        int count = reader.ReadByte();
                        switch (count)
                        {
                            case 0:
                                _baseIndexMap[y, x] = NO_TRIANGLE;
                                break;
                            case 1:
                                _baseIndexMap[y, x] = reader.ReadInt16();
                                break;
                            default:
                                var extraValues = new TriInt[count - 1];
                                _baseIndexMap[y, x] = reader.ReadInt16();
                                for (int i = 0; i < count - 1; ++i) extraValues[i] = reader.ReadInt16();
                                _extraIndexMap[y, x] = extraValues;
                                break;
                        }
                    }
            }

            public override void Add(int x, int y, int index)
            {
                if (_baseIndexMap[y, x] == NO_TRIANGLE)
                    _baseIndexMap[y, x] = checked((TriInt)index);
                else
                    ListAppend(ref _extraIndexMap[y, x], checked((TriInt)index));
            }

            public override IEnumerable<TriInt> Get(int x, int y)
            {
                var baseIndex = _baseIndexMap[y, x];
                if (baseIndex == NO_TRIANGLE) yield break;
                yield return baseIndex;
                var extraIndices = _extraIndexMap[y, x];
                if (extraIndices == null) yield break;
                foreach (var index in extraIndices) yield return index;
            }

            private void Initialize(int width, int height, bool emptyData)
            {
                Width = width;
                Height = height;
                _baseIndexMap = new TriInt[height, width];
                _extraIndexMap = new TriInt[height, width][];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        _baseIndexMap[y, x] = NO_TRIANGLE;
            }
        }

        private static readonly byte[] HEADER_BYTES = System.Text.Encoding.ASCII.GetBytes("AW10");
        private static BasicEffect g_indexMapEffect;
        private IData _data;

        /// <summary>
        /// Triangle cover counts of the wall's 3D model.
        /// </summary>
        /// Index n corresponds to the triangle defined by the 3D model's
        /// index list members 3n, 3n+1 and 3n+2. A positive cover count signifies
        /// the number of index map points covered by the triangle that still 
        /// need to be deleted before the triangle is erased from the 3D model.
        /// A negative cover count marks a deleted triangle.
        private int[] _triangleCovers;

        public int Width { get { return _data.Width; } }
        public int Height { get { return _data.Height; } }

        /// <summary>
        /// Transformation matrix from wall's 3D model's coordinates to index map coordinates.
        /// </summary>
        public Matrix WallToIndexMapTransform { get; private set; }

        private RemoveTriangleDelegate _removeTriangle;

        private static BasicEffect GetIndexMapEffect(GraphicsDevice gfx)
        {
            if (g_indexMapEffect == null || g_indexMapEffect.IsDisposed)
            {
                g_indexMapEffect = new BasicEffect(gfx, null);
                g_indexMapEffect.VertexColorEnabled = true;
                g_indexMapEffect.LightingEnabled = false;
                g_indexMapEffect.TextureEnabled = false;
                g_indexMapEffect.View = Matrix.CreateLookAt(new Vector3(0, 0, 1000), Vector3.Zero, Vector3.Up);
            }
            return g_indexMapEffect;
        }

        public WallIndexMap(RemoveTriangleDelegate removeTriangle, AWRectangle boundingBox, VertexPositionNormalTexture[] vertexData, short[] indexData)
            : this(removeTriangle, boundingBox)
        {
            Initialize(vertexData, indexData, boundingBox);
        }

        public WallIndexMap(RemoveTriangleDelegate removeTriangle, AWRectangle boundingBox, System.IO.BinaryReader reader)
            : this(removeTriangle, boundingBox)
        {
            _data = new IndexMapData(reader);
            _triangleCovers = _data.TriangleCovers;
        }

        private WallIndexMap(RemoveTriangleDelegate removeTriangle, AWRectangle boundingBox)
        {
            _removeTriangle = removeTriangle;
            var minCorner = boundingBox.Min;
            WallToIndexMapTransform = Matrix.CreateTranslation(-minCorner.X, -minCorner.Y, 0);
        }

        public void Remove(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            foreach (TriInt index in _data.Get(x, y))
                if (--_triangleCovers[index] == 0) _removeTriangle(index);
        }

        /// <summary>
        /// Returns the indices of triangles that are too small to show up in the index map.
        /// Triangle index * 3 = the index of the first vertex of the triangle in the 3D model index data.
        /// </summary>
        public IEnumerable<int> GetVerySmallTriangles()
        {
            for (int index = 0; index < _triangleCovers.Length; ++index)
                if (_triangleCovers[index] == 0) yield return index;
        }

        public void ForceVerySmallTrianglesIntoIndexMap(VertexPositionNormalTexture[] vertexData, short[] indexData)
        {
            if (!GetVerySmallTriangles().Any()) return;
            foreach (int index in GetVerySmallTriangles())
            {
                var vert0 = vertexData[indexData[3 * index + 0]].Position;
                var vert1 = vertexData[indexData[3 * index + 1]].Position;
                var vert2 = vertexData[indexData[3 * index + 2]].Position;
                var triangleCenter = (vert0 + vert1 + vert2) / 3;
                var centerInIndexMap = Vector3.Transform(triangleCenter, WallToIndexMapTransform);
                int centerInIndexMapX = (int)(Math.Round(centerInIndexMap.X) + 0.1);
                int centerInIndexMapY = (int)(Math.Round(centerInIndexMap.Y) + 0.1);
                _data.Add(centerInIndexMapX, centerInIndexMapY, index);
            }
            _data.ComputeTriangleCovers(indexData.Length / 3);
            _triangleCovers = _data.TriangleCovers;
        }

        public void Serialize(System.IO.BinaryWriter writer)
        {
            checked
            {
                writer.Write(HEADER_BYTES);
                writer.Write((short)Width);
                writer.Write((short)Height);
                writer.Write((short)_data.TriangleCovers.Length);
                foreach (int value in _triangleCovers) writer.Write((short)value);
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                    {
                        var values = _data.Get(x, y);
                        writer.Write((byte)values.Count());
                        foreach (TriInt value in values) writer.Write(value);
                    }
            }
        }

        private static Point PointFromVertex(VertexPositionNormalTexture vertex, Vector2 origin)
        {
            return new Point((int)(vertex.Position.X - origin.X), (int)(vertex.Position.Y - origin.Y));
        }

        private void Initialize(VertexPositionNormalTexture[] vertexData, short[] indexData, AWRectangle boundingArea)
        {
            var modelDim = boundingArea.Dimensions;
            _data = new IndexMapData((int)Math.Ceiling(modelDim.X) + 1, (int)Math.Ceiling(modelDim.Y) + 1);
            for (int indexI = 0; indexI < indexData.Length; indexI += 3)
            {
                var point1 = PointFromVertex(vertexData[indexData[indexI + 0]], boundingArea.Min);
                var point2 = PointFromVertex(vertexData[indexData[indexI + 1]], boundingArea.Min);
                var point3 = PointFromVertex(vertexData[indexData[indexI + 2]], boundingArea.Min);
                AWMathHelper.FillTriangle(point1, point2, point3, (x, y) => _data.Add(x, y, indexI / 3));
            }
            _data.ComputeTriangleCovers(indexData.Length / 3);
            _triangleCovers = _data.TriangleCovers;
        }
    }
}
