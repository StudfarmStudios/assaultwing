using System;
using Microsoft.Xna.Framework;
using AW2.Helpers.Serialization;

namespace AW2.Helpers.Geometric
{
    /// <summary>
    /// A circle in two-dimensional space.
    /// </summary>
    [LimitedSerialization]
    public class Circle : IGeomPrimitive, IConsistencyCheckable
    {
        [TypeParameter]
        private Vector2 _center;
        [TypeParameter]
        private float _radius;
        private Rectangle _boundingBox;

        public Vector2 Center { get { return _center; } }
        public float Radius { get { return _radius; } }

        /// <summary>
        /// Creates a zero-radius circle at the origin.
        /// </summary>
        public Circle()
        {
            _center = Vector2.Zero;
            _radius = 0;
            UpdateBoundingBox();
        }

        public Circle(Vector2 center, float radius)
        {
            _center = center;
            _radius = radius;
            UpdateBoundingBox();
        }

        void UpdateBoundingBox()
        {
            _boundingBox = new Rectangle(
                _center.X - _radius, _center.Y - _radius,
                _center.X + _radius, _center.Y + _radius);
        }

        #region IGeomPrimitive Members

        public Rectangle BoundingBox { get { return _boundingBox; } }

        public IGeomPrimitive Transform(Matrix transformation)
        {
            Vector2 newCenter = Vector2.Transform(_center, transformation);
            Vector2 newRadiusVector = Vector2.TransformNormal(_radius * Vector2.UnitX, transformation);
            return new Circle(newCenter, newRadiusVector.Length());
        }

        public float DistanceTo(Vector2 point)
        {
            float distance = Vector2.Distance(_center, point) - _radius;
            return Math.Max(distance, 0);
        }

        #endregion IGeomPrimitive Members

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
                    writer.Write((float)_center.X);
                    writer.Write((float)_center.Y);
                    writer.Write((float)_radius);
                }
            }
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
            {
                _center = new Vector2 { X = reader.ReadSingle(), Y = reader.ReadSingle() };
                _radius = reader.ReadSingle();
            }
        }

        #endregion
    }
}
