using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using AW2.Helpers.Serialization;

namespace AW2.Helpers.Geometric
{
    /// <summary>
    /// A polygon in two-dimensional space.
    /// </summary>
    /// A polygon is always simple, i.e., its edge doesn't intersect itself.
    [LimitedSerialization]
    public class Polygon : IGeomPrimitive, IEquatable<Polygon>, IConsistencyCheckable
    {
        /// <summary>
        /// A strip of faces of a polygon.
        /// </summary>
        public struct FaceStrip
        {
            /// <summary>
            /// Index of the first vertex in the strip.
            /// </summary>
            public int StartIndex;

            /// <summary>
            /// Index of the last vertex in the strip, (inclusive end).
            /// If the strip ends the polygon, then <b>endIndex</b> equals
            /// vertex count + 1 which denotes index 0.
            /// </summary>
            public int EndIndex;

            /// <summary>
            /// Tight, axis-aligned bounding box for the face strip.
            /// </summary>
            public Rectangle BoundingBox;

            /// <summary>
            /// Creates a face strip for a polygon.
            /// </summary>
            /// <param name="startIndex">Index of the first vertex in the strip.</param>
            /// <param name="endIndex">Index of the first vertex not in the strip, (exclusive end).</param>
            /// <param name="boundingBox">Bounding box for the face strip.</param>
            public FaceStrip(int startIndex, int endIndex, Rectangle boundingBox)
            {
                StartIndex = startIndex;
                EndIndex = endIndex;
                BoundingBox = boundingBox;
            }
        }

        /// <summary>
        /// Maximum number of faces in one face strip.
        /// </summary>
        private static readonly int faceStripSize = 20;

        /// <summary>
        /// The vertices of the polygon. Each vertex is listed only once and in order.
        /// </summary>
        [TypeParameter, RuntimeState]
        private Vector2[] _vertices;

        /// <summary>
        /// A rectangle containing all the vertices.
        /// </summary>
        private Rectangle _boundingBox;

        /// <summary>
        /// The polygon's faces separated into strips, for optimisation purposes.
        /// May be <b>null</b>.
        /// </summary>
        private FaceStrip[] _faceStrips;

        /// <summary>
        /// Returns the vertices of the polygon.
        /// </summary>
        /// In order to preserve the simplicity of the polygon, the 
        /// vertices should not be modified. Rather, create a new polygon.
        public Vector2[] Vertices { get { return _vertices; } }

        /// <summary>
        /// Grouping of the polygon's faces into small strips.
        /// May be null.
        /// </summary>
        /// Face strips can be used for optimisation purposes.
        public FaceStrip[] FaceStrips { get { return _faceStrips; } }

        /// <summary>
        /// Creates an uninitialised polygon.
        /// </summary>
        public Polygon()
        {
            _vertices = null;
        }

        /// <summary>
        /// Creates a simple polygon.
        /// </summary>
        /// <param name="vertices">The vertices of the polygon in order.</param>
        public Polygon(Vector2[] vertices)
        {
            // Sanity check.
            if (vertices.Length < 3)
                throw new Exception("At least 3 vertices needed for a polygon");

            // Make a shallow copy so that outside code cannot alter our data 
            // without us knowing about it. Vector2 is a struct so a deep copy
            // is not needed.
            _vertices = (Vector2[])vertices.Clone();

            _boundingBox = new Rectangle();
            _faceStrips = null;
            UpdateBoundingBox();
            UpdateFaceStrips();
        }

        /// <summary>
        /// Returns a string that represents this polygon.
        /// </summary>
        public override string ToString()
        {
            return "{" + String.Join(", ", _vertices.Select(v => v.ToString()).ToArray()) + "}";
        }

        /// <summary>
        /// Updates <see cref="_boundingBox"/>>
        /// </summary>
        private void UpdateBoundingBox()
        {
            Vector2 min = new Vector2(Single.MaxValue);
            Vector2 max = new Vector2(Single.MinValue);
            foreach (Vector2 v in _vertices)
            {
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
            _boundingBox = new Rectangle(min, max);
        }

        /// <summary>
        /// Updates <see cref="_faceStrips"/>
        /// </summary>
        private void UpdateFaceStrips()
        {
            _faceStrips = null;

            // Small polygons won't benefit from extra structures.
            if (_vertices.Length < faceStripSize * 2)
                return;

            // Divide faces to maximal strips with no brilliant logic.
            // This is a place for a clever algorithm. A good split into face strips
            // is one where the total area of bounding boxes is small.
            List<FaceStrip> faceStripList = new List<FaceStrip>();
            int startIndex = 0;
            while (startIndex < _vertices.Length)
            {
                int endIndex = Math.Min(startIndex + faceStripSize, _vertices.Length);
                Vector2 min = _vertices[startIndex];
                Vector2 max = _vertices[startIndex];
                for (int i = startIndex + 1; i <= endIndex; ++i)
                {
                    int realI = i % _vertices.Length;
                    min = Vector2.Min(min, _vertices[realI]);
                    max = Vector2.Max(max, _vertices[realI]);
                }
                faceStripList.Add(new FaceStrip(startIndex, endIndex,
                    new Rectangle(min, max)));
                startIndex = endIndex;
            }
            _faceStrips = faceStripList.ToArray();
        }

        #region IGeomPrimitive Members

        public Rectangle BoundingBox { get { return _boundingBox; } }

        public IGeomPrimitive Transform(Matrix transformation)
        {
            Polygon poly = new Polygon(_vertices); // vertices are cloned
            Vector2.Transform(poly._vertices, ref transformation, poly._vertices);
            poly.UpdateBoundingBox();
            poly.UpdateFaceStrips();
            return poly;
        }

        public float DistanceTo(Vector2 point)
        {
            return Geometry.Distance(new Point(point), this);
        }

        public Shape GetShape()
        {
            return new PolygonShape(AWMathHelper.CreateVertices(Vertices), 1);
        }

        #endregion IGeomPrimitive Members

        #region IEquatable<Polygon> Members

        public bool Equals(Polygon other)
        {
            if (this.Vertices.Length != other.Vertices.Length)
                return false;

            // Find our first vertex in the other polygon.
            int firstOtherI = -1;
            for (int otherI = 0; otherI < other.Vertices.Length; ++otherI)
            {
                if (other.Vertices[otherI].Equals(this.Vertices[0]))
                {
                    firstOtherI = otherI;
                    break;
                }
            }
            if (firstOtherI == -1)
                return false;

            // Figure out winding in the other polygon.
            int otherInc = 0;
            if (other.Vertices[(firstOtherI + 1) % other.Vertices.Length].Equals(this.Vertices[1]))
                otherInc = 1;
            else if (other.Vertices[(firstOtherI - 1 + other.Vertices.Length) % other.Vertices.Length].Equals(this.Vertices[1]))
                otherInc = -1;
            else
                return false;

            // Compare the remaining vertices.
            for (int i = 1; i < Vertices.Length; ++i)
                if (!other.Vertices[(firstOtherI + i * otherInc + other.Vertices.Length) % other.Vertices.Length].Equals(this.Vertices[i]))
                    return false;
            return true;
        }

        #endregion

        #region IConsistencyCheckable Members

        public void MakeConsistent(Type limitationAttribute)
        {
            UpdateBoundingBox();
            UpdateFaceStrips();
        }

        #endregion

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
                {
                    writer.Write((ushort)_vertices.Length);
                    foreach (var vertex in _vertices) writer.Write((Vector2)vertex);
                }
            }
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
            {
                int vertexCount = reader.ReadUInt16();
                _vertices = new Vector2[vertexCount];
                for (int i = 0; i < vertexCount; i++) _vertices[i] = reader.ReadVector2();
                UpdateBoundingBox();
                UpdateFaceStrips();
            }
        }
    }
}
