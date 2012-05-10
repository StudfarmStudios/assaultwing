using System;
using System.Linq;
using Microsoft.Xna.Framework;
using FarseerPhysics.Dynamics;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;

namespace AW2.Game.Collisions
{
    /// <summary>
    /// An area with which a gob can overlap with other gobs' areas,
    /// resulting in a collision.
    /// </summary>
    [LimitedSerialization]
    [System.Diagnostics.DebuggerDisplay("Type:{Type} Name:{Name} AreaGob:{AreaGob}")]
    public class CollisionArea : INetworkSerializable
    {
        [TypeParameter]
        private CollisionAreaType _type;

        [TypeParameter]
        private CollisionMaterialType _collisionMaterial;

        [TypeParameter]
        private string _name;

        /// <summary>
        /// Area in gob coordinates, not world coordinates.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private IGeomPrimitive _area;

        [ExcludeFromDeepCopy]
        private Gob _owner;

        /// <summary>
        /// Collision area name; either "General" for general collision
        /// checking (including physical collisions), or something else
        /// for a receptor area that can react to other gobs' general
        /// areas.
        /// </summary>
        public string Name { get { return _name; } }

        /// <summary>
        /// The type of the collision area.
        /// </summary>
        public CollisionAreaType Type { get { return _type; } }

        /// <summary>
        /// Elasticity factor of the collision area. Zero means no collision bounce.
        /// One means fully elastic collision.
        /// </summary>
        public float Elasticity { get { return CollisionMaterial.Get(_collisionMaterial).Elasticity; } }

        /// <summary>
        /// Friction factor of the collision area. Zero means that movement along the
        /// collision surface is not slowed by friction.
        /// </summary>
        public float Friction { get { return CollisionMaterial.Get(_collisionMaterial).Friction; } }

        /// <summary>
        /// Multiplier for collision damage.
        /// </summary>
        public float Damage { get { return CollisionMaterial.Get(_collisionMaterial).Damage; } }

        /// <summary>
        /// The geometric area for overlap testing, in hosting gob coordinates if the gob is movable,
        /// in world coordinates if the gob is unmovable.
        /// </summary>
        public IGeomPrimitive AreaGob { get { return _area; } set { _area = value; } }

        /// <summary>
        /// The gob whose collision area this is.
        /// </summary>
        public Gob Owner { get { return _owner; } set { _owner = value; } }

        public Fixture Fixture { get; set; }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public CollisionArea()
        {
            _type = CollisionAreaType.Common;
            _name = "dummyarea";
            _collisionMaterial = CollisionMaterialType.Regular;
            _area = new Circle(Vector2.Zero, 10);
        }

        public CollisionArea(string name, IGeomPrimitive area, Gob owner, CollisionAreaType type, CollisionMaterialType collisionMaterial)
        {
            _type = type;
            _collisionMaterial = collisionMaterial;
            _name = name;
            _area = area;
            _owner = owner;
            if (owner != null && owner.IsRegistered) Initialize();
        }

        public void Initialize(float scale = 1)
        {
            var gob = Owner;
            var areaArea = AreaGob;
            if (scale != 1) areaArea = areaArea.Transform(Matrix.CreateScale(scale));
            var fixture = gob.Body.CreateFixture(areaArea.GetShape(), this);
            fixture.Friction = Friction;
            fixture.Restitution = Elasticity;
            fixture.IsSensor = !Type.IsPhysical();
            fixture.CollisionCategories = Type.Category();
            fixture.CollidesWith = Type.CollidesWith();
            Fixture = fixture;
        }

        public void Destroy()
        {
            Fixture.Dispose();
        }

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
                {
                    writer.Write((int)_type);
                    writer.Write((string)_name);
                    writer.Write((byte)_collisionMaterial);
                    _area.Serialize(writer, SerializationModeFlags.AllFromServer);
                }
            }
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
            {
                _type = (CollisionAreaType)reader.ReadInt32();
                _name = reader.ReadString();
                _collisionMaterial = (CollisionMaterialType)reader.ReadByte();
                _area.Deserialize(reader, SerializationModeFlags.AllFromServer, framesAgo);
            }
        }
    }
}
