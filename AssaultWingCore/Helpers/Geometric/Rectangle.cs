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
    /// An axis-aligned rectangle in two-dimensional space.
    /// </summary>
    public class Rectangle : IGeomPrimitive
    {
#if TRUSTED_VISIBILITY_BREACH
        [SerializedName("min")]
        public Vector2 Min;
        [SerializedName("max")]
        public Vector2 Max;
#else
        private Vector2 _min, _max;
#endif

#if !TRUSTED_VISIBILITY_BREACH
        /// <summary>
        /// The bottom left corner of the rectangle.
        /// </summary>
        public Vector2 Min { get { return _min; } }

        /// <summary>
        /// The top right corner of the rectangle.
        /// </summary>
        public Vector2 Max { get { return _max; } }
#endif

        /// <summary>
        /// The width and height of the rectangle.
        /// </summary>
        public Vector2 Dimensions
        {
            get
            {
#if TRUSTED_VISIBILITY_BREACH
                return Max - Min;
#else
                return _max - _min;
#endif
            }
        }

        public Vector2 Center { get { return (_min + _max) / 2; } }

        /// <summary>
        /// Creates a zero-sized rectangle at the origin.
        /// </summary>
        public Rectangle()
        {
        }

        /// <summary>
        /// Creates a rectangle.
        /// </summary>
        /// <param name="min">The bottom left corner of the rectangle.</param>
        /// <param name="max">The top right corner of the rectangle.</param>
        public Rectangle(Vector2 min, Vector2 max)
        {
            if (min.X > max.X || min.Y > max.Y)
                throw new ArgumentException("Min is not less than or equal to max");
#if TRUSTED_VISIBILITY_BREACH
            Min = min;
            Max = max;
#else
            _min = min;
            _max = max;
#endif
        }

        /// <summary>
        /// Creates a rectangle.
        /// </summary>
        /// <param name="minX">The left coordinate of the rectangle.</param>
        /// <param name="minY">The bottom coordinate of the rectangle.</param>
        /// <param name="maxX">The right coordinate of the rectangle.</param>
        /// <param name="maxY">The top coordinate of the rectangle.</param>
        public Rectangle(float minX, float minY, float maxX, float maxY)
        {
            if (minX > maxX || minY > maxY)
                throw new ArgumentException("Min is not less than or equal to max");
#if TRUSTED_VISIBILITY_BREACH
            Min = new Vector2(minX, minY);
            Max = new Vector2(maxX, maxY);
#else
            _min = new Vector2(minX, minY);
            _max = new Vector2(maxX, maxY);
#endif
        }

        /// <summary>
        /// Returns a string representation of the rectangle.
        /// </summary>
        public override string ToString()
        {
            return "{" + Min + " - " + Max + "}";
        }

        #region IGeomPrimitive Members

        public Rectangle BoundingBox { get { return this; } }

        public IGeomPrimitive Transform(Matrix transformation)
        {
#if TRUSTED_VISIBILITY_BREACH
            var _min = Min;
            var _max = Max;
#endif
            var p1 = Vector2.Transform(_min, transformation);
            var p2 = Vector2.Transform(new Vector2(_min.X, _max.Y), transformation);
            var p3 = Vector2.Transform(_max, transformation);
            var p4 = Vector2.Transform(new Vector2(_max.X, _min.Y), transformation);
            return new Polygon(new Vector2[] { p1, p2, p3, p4 });
        }

        public float DistanceTo(Vector2 point)
        {
            return Geometry.Distance(new Point(point), this);
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
                    var _min = Min;
                    var _max = Max;
#endif
                    writer.Write((float)_min.X);
                    writer.Write((float)_min.Y);
                    writer.Write((float)_max.X);
                    writer.Write((float)_max.Y);
                }
            }
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
            {
#if TRUSTED_VISIBILITY_BREACH
                Vector2 _min, _max;
#endif
                _min = new Vector2 { X = reader.ReadSingle(), Y = reader.ReadSingle() };
                _max = new Vector2 { X = reader.ReadSingle(), Y = reader.ReadSingle() };
#if TRUSTED_VISIBILITY_BREACH
                Min = _min;
                Max = _max;
#endif
            }
        }

        #endregion
    }
}
