#if !DEBUG
#define TRUSTED_VISIBILITY_BREACH // makes code faster at the cost of naughty class design
#endif
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using FarseerPhysics.Collision.Shapes;
using AW2.Helpers.Serialization;

namespace AW2.Helpers.Geometric
{
    /// <summary>
    /// A point in two-dimensional space.
    /// </summary>
    [LimitedSerialization]
    public class Point : IGeomPrimitive
    {
        public float Density { get; set; }

#if TRUSTED_VISIBILITY_BREACH
        [TypeParameter, RuntimeState, SerializedName("location")]
        public Vector2 Location;
#else
        [TypeParameter, RuntimeState]
        private Vector2 _location;

        public Vector2 Location { get { return _location; } set { _location = value; } }
#endif

        /// <summary>
        /// Creates a point at the origin.
        /// </summary>
        public Point()
        {
            Density = 1;
#if TRUSTED_VISIBILITY_BREACH
            Location = Vector2.Zero;
#else
            _location = Vector2.Zero;
#endif
        }

        public Point(Vector2 location)
        {
            Density = 1;
#if TRUSTED_VISIBILITY_BREACH
            Location = location;
#else
            _location = location;
#endif
        }

        public override string ToString()
        {
            return Location.ToString();
        }

        #region IGeomPrimitive Members

        public Rectangle BoundingBox
        {
#if TRUSTED_VISIBILITY_BREACH
            get { return new Rectangle(Location, Location); } 
#else
            get { return new Rectangle(_location, _location); }
#endif
        }

        public IGeomPrimitive Transform(Matrix transformation)
        {
#if TRUSTED_VISIBILITY_BREACH
            return new Point(Vector2.Transform(Location, transformation));
#else
            return new Point(Vector2.Transform(_location, transformation));
#endif
        }

        public float DistanceTo(Vector2 point)
        {
#if TRUSTED_VISIBILITY_BREACH
            return Vector2.Distance(Location, point);
#else
            return Vector2.Distance(_location, point);
#endif
        }

        public Shape GetShape()
        {
            return new CircleShape(0.05f.ToFarseer(), Density) { Position = Location.ToFarseer() };
        }

        #endregion IGeomPrimitive Members

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
                    var _location = Location;
#endif
                    writer.Write((float)_location.X);
                    writer.Write((float)_location.Y);
                }
            }
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
            {
#if TRUSTED_VISIBILITY_BREACH
                var
#endif
                _location = new Vector2 { X = reader.ReadSingle(), Y = reader.ReadSingle() };
#if TRUSTED_VISIBILITY_BREACH
                Location = _location;
#endif
            }
        }

        #endregion
    }
}
