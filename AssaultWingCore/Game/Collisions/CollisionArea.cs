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
        private CollisionAreaType _collidesAgainst;

        [TypeParameter]
        private CollisionAreaType _cannotOverlap;

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
        /// The types of collision areas this collision area collides against.
        /// </summary>
        public CollisionAreaType CollidesAgainst { get { return _collidesAgainst; } }

        /// <summary>
        /// The types of collision areas this collision area collides physically against,
        /// i.e. types of collision areas that this collision area cannot overlap.
        /// </summary>
        public CollisionAreaType CannotOverlap { get { return _cannotOverlap; } }

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

        /// <summary>
        /// If true, the collision area represents physical collisions.
        /// </summary>
        public bool IsPhysical { get { return CannotOverlap != CollisionAreaType.None; } }

        public Fixture Fixture { get; set; }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public CollisionArea()
        {
            _type = CollisionAreaType.None;
            _collidesAgainst = CollisionAreaType.None;
            _cannotOverlap = CollisionAreaType.None;
            _name = "dummyarea";
            _collisionMaterial = CollisionMaterialType.Regular;
            _area = new Circle(Vector2.Zero, 10);
        }

        /// <param name="name">Collision area name.</param>
        /// <param name="area">The geometric area.</param>
        /// <param name="owner">The gob whose collision area this is.</param>
        /// <param name="type">The type of the collision area.</param>
        /// <param name="collisionMaterial">Material of the collision area.</param>
        /// <param name="collidesAgainst">The types of collision areas this area collides against.</param>
        /// <param name="cannotOverlap">The types of collision areas this area collides against and cannot overlap.</param>
        public CollisionArea(string name, IGeomPrimitive area, Gob owner,
            CollisionAreaType type, CollisionAreaType collidesAgainst, CollisionAreaType cannotOverlap,
            CollisionMaterialType collisionMaterial)
        {
            _type = type;
            _collidesAgainst = collidesAgainst;
            _cannotOverlap = cannotOverlap;
            _collisionMaterial = collisionMaterial;
            _name = name;
            _area = area;
            _owner = owner;
        }

        public void Disable()
        {
            Fixture.CollidesWith = Category.None;
            Fixture.CollisionCategories = Category.None;
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
                    writer.Write((int)_collidesAgainst);
                    writer.Write((int)_cannotOverlap);
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
                _collidesAgainst = (CollisionAreaType)reader.ReadInt32();
                _cannotOverlap = (CollisionAreaType)reader.ReadInt32();
                _name = reader.ReadString();
                _collisionMaterial = (CollisionMaterialType)reader.ReadByte();
                _area.Deserialize(reader, SerializationModeFlags.AllFromServer, framesAgo);
            }
        }
    }
}
