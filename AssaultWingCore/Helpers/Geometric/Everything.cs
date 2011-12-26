using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Helpers.Serialization;

namespace AW2.Helpers.Geometric
{
    /// <summary>
    /// The whole two-dimensional space.
    /// </summary>
    public class Everything : IGeomPrimitive
    {
        #region IGeomPrimitive Members

        public Rectangle BoundingBox
        {
            get
            {
                return new Rectangle(float.MinValue, float.MinValue, 
                                     float.MaxValue, float.MaxValue);
            }
        }

        public IGeomPrimitive Transform(Matrix transformation)
        {
            // It's not a _copy_ but it shouldn't matter because this
            // object has no state.
            return this;
        }

        public float DistanceTo(Vector2 point)
        {
            return 0;
        }

        #endregion

        #region INetworkSerializable Members

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode) { }
        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo) { }

        #endregion
    }
}
