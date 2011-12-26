#if !DEBUG
#define TRUSTED_VISIBILITY_BREACH // makes code faster at the cost of naughty class design
#endif
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Helpers.Serialization;

namespace AW2.Helpers.Geometric
{
    /// <summary>
    /// A triangle in two-dimensional space. The triangle is formed by three
    /// corner points that are ordered in clockwise order.
    /// A triangle can also be degenerate, i.e. all the corner points
    /// lie on the same line.
    /// </summary>
    [LimitedSerialization]
    public class Triangle : IGeomPrimitive, IConsistencyCheckable
    {
#if TRUSTED_VISIBILITY_BREACH
        /// <summary>First vertex</summary>
        [TypeParameter, SerializedName("p1")]
        public Vector2 P1;

        /// <summary>Second vertex</summary>
        [TypeParameter, SerializedName("p2")]
        public Vector2 P2;

        /// <summary>Third vertex</summary>
        [TypeParameter, SerializedName("p3")]
        public Vector2 P3;
#else
        [TypeParameter]
        private Vector2 _p1, _p2, _p3;
#endif
        // Unit normals of each face.
        [TypeParameter]
        private Vector2 _n12, _n13, _n23;

        /// <summary>
        /// A rectangle containing the triangle.
        /// </summary>
        private Rectangle _boundingBox;

#if !TRUSTED_VISIBILITY_BREACH
        /// <summary>
        /// The first corner point.
        /// </summary>
        public Vector2 P1 { get { return _p1; } }

        /// <summary>
        /// The second corner point.
        /// </summary>
        public Vector2 P2 { get { return _p2; } }

        /// <summary>
        /// The third corner point.
        /// </summary>
        public Vector2 P3 { get { return _p3; } }
#endif

        /// <summary>
        /// The unit normal pointing away from the triangle at the edge
        /// defined by P1 and P2.
        /// </summary>
        public Vector2 Normal12 { get { return _n12; } }

        /// <summary>
        /// The unit normal pointing away from the triangle at the edge
        /// defined by P1 and P3.
        /// </summary>
        public Vector2 Normal13 { get { return _n13; } }

        /// <summary>
        /// The unit normal pointing away from the triangle at the edge
        /// defined by P2 and P3.
        /// </summary>
        public Vector2 Normal23 { get { return _n23; } }

        /// <summary>
        /// Creates a triangle. The corner points will be reordered
        /// to clockwise order.
        /// </summary>
        /// <param name="p1">The first corner point.</param>
        /// <param name="p2">The second corner point.</param>
        /// <param name="p3">The third corner point.</param>
        public Triangle(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            // Assign p1 as given, but possibly swap p2 and p3 to enforce
            // clockwise order of corner points.
            Vector2 e12LeftNormal = new Vector2(p1.Y - p2.Y, p2.X - p1.X);
            Vector2 e23 = p3 - p2;
            float dot = Vector2.Dot(e12LeftNormal, e23);
            if (dot > 0)
            {
#if TRUSTED_VISIBILITY_BREACH
                P1 = p1;
                P2 = p3;
                P3 = p2;
#else
                _p1 = p1;
                _p2 = p3;
                _p3 = p2;
#endif
                _n13 = -e12LeftNormal;
                _n12 = new Vector2(p1.Y - p3.Y, p3.X - p1.X);
                _n23 = new Vector2(p3.Y - p2.Y, p2.X - p3.X);
            }
            else
            {
#if TRUSTED_VISIBILITY_BREACH
                P1 = p1;
                P2 = p2;
                P3 = p3;
#else
                _p1 = p1;
                _p2 = p2;
                _p3 = p3;
#endif
                _n12 = e12LeftNormal;
                _n13 = new Vector2(p3.Y - p1.Y, p1.X - p3.X);
                _n23 = new Vector2(p2.Y - p3.Y, p3.X - p2.X);
            }
            _n12.Normalize();
            _n13.Normalize();
            _n23.Normalize();
            UpdateBoundingBox();
        }

        public override string ToString()
        {
            return "{" + P1 + ", " + P2 + ", " + P3 + "}";
        }

        void UpdateBoundingBox()
        {
#if TRUSTED_VISIBILITY_BREACH
            _boundingBox = new Geometric.Rectangle(
                Math.Min(P1.X, Math.Min(P2.X, P3.X)),
                Math.Min(P1.Y, Math.Min(P2.Y, P3.Y)),
                Math.Max(P1.X, Math.Max(P2.X, P3.X)),
                Math.Max(P1.Y, Math.Max(P2.Y, P3.Y)));
#else
            _boundingBox = new Geometric.Rectangle(
                Math.Min(_p1.X, Math.Min(_p2.X, _p3.X)),
                Math.Min(_p1.Y, Math.Min(_p2.Y, _p3.Y)),
                Math.Max(_p1.X, Math.Max(_p2.X, _p3.X)),
                Math.Max(_p1.Y, Math.Max(_p2.Y, _p3.Y)));
#endif
        }

        #region IGeomPrimitive Members

        public Rectangle BoundingBox { get { return _boundingBox; } }

        public IGeomPrimitive Transform(Matrix transformation)
        {
#if TRUSTED_VISIBILITY_BREACH
            return new Triangle(Vector2.Transform(P1, transformation),
                Vector2.Transform(P2, transformation),
                Vector2.Transform(P3, transformation));
#else
            return new Triangle(Vector2.Transform(_p1, transformation),
                Vector2.Transform(_p2, transformation),
                Vector2.Transform(_p3, transformation));
#endif
        }

        public float DistanceTo(Vector2 point)
        {
            return Geometry.Distance(new Point(point), this);
        }

        #endregion

        #region IConsistencyCheckable Members

        public void MakeConsistent(Type limitationAttribute)
        {
            UpdateBoundingBox();
        }

        #endregion

        #region INetworkSerializable Members

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
                {
#if TRUSTED_VISIBILITY_BREACH
                    var _p1 = P1;
                    var _p2 = P2;
                    var _p3 = P3;
#endif
                    writer.Write((float)_p1.X);
                    writer.Write((float)_p1.Y);
                    writer.Write((float)_p2.X);
                    writer.Write((float)_p2.Y);
                    writer.Write((float)_p3.X);
                    writer.Write((float)_p3.Y);
                }
            }
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
            {
#if TRUSTED_VISIBILITY_BREACH
                Vector2 _p1, _p2, _p3;
#endif
                _p1 = new Vector2 { X = reader.ReadSingle(), Y = reader.ReadSingle() };
                _p2 = new Vector2 { X = reader.ReadSingle(), Y = reader.ReadSingle() };
                _p3 = new Vector2 { X = reader.ReadSingle(), Y = reader.ReadSingle() };
#if TRUSTED_VISIBILITY_BREACH
                P1 = _p1;
                P2 = _p2;
                P3 = _p3;
#endif
            }
        }

        #endregion
    }
}
