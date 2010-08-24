using System;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Net;

namespace AW2.Game
{
    /// <summary>
    /// An area with which a gob can overlap with other gobs' areas,
    /// resulting in a collision.
    /// </summary>
    [LimitedSerialization]
    [System.Diagnostics.DebuggerDisplay("Type:{type} Name:{name} Area:{area}")]
    public class CollisionArea : INetworkSerializable
    {
        private struct CollisionMaterial
        {
            /// <summary>
            /// Elasticity factor of the collision area. Zero means no collision bounce.
            /// One means fully elastic collision.
            /// </summary>
            /// The elasticity factors of both colliding collision areas affect the final elasticity
            /// of the collision. Avoid using zero; instead, use a very small number.
            /// Use a number above one to regain fully elastic collisions even
            /// when countered by inelastic gobs.
            public float Elasticity;

            /// <summary>
            /// Friction factor of the collision area. Zero means that movement along the
            /// collision surface is not slowed by friction.
            /// </summary>
            /// The friction factors of both colliding collision areas affect the final friction
            /// of the collision. It's a good idea to use values that are closer to
            /// zero than one.
            public float Friction;

            /// <summary>
            /// Multiplier for collision damage.
            /// </summary>
            public float Damage;
        }

        /// <summary>
        /// Upper limit for the number of bits in the type representing <see cref="CollisionAreaType"/>.
        /// </summary>
        public const int COLLISION_AREA_TYPE_COUNT = 32;

        private static CollisionMaterial[] g_collisionMaterials;

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

        /// <summary>
        /// Area in world coordinates, transformed by <b>oldWorldMatrix</b>.
        /// </summary>
        private IGeomPrimitive _transformedArea;
        private Matrix _oldWorldMatrix;

        private Gob _owner;

        private object _collisionData;

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
        public float Elasticity { get { return g_collisionMaterials[(int)_collisionMaterial].Elasticity; } }

        /// <summary>
        /// Friction factor of the collision area. Zero means that movement along the
        /// collision surface is not slowed by friction.
        /// </summary>
        public float Friction { get { return g_collisionMaterials[(int)_collisionMaterial].Friction; } }

        /// <summary>
        /// Multiplier for collision damage.
        /// </summary>
        public float Damage { get { return g_collisionMaterials[(int)_collisionMaterial].Damage; } }

        /// <summary>
        /// The geometric area for overlap testing, in game world coordinates,
        /// transformed according to the hosting gob's world matrix.
        /// </summary>
        public IGeomPrimitive Area
        {
            get
            {
                if (!_owner.Movable)
                    return _area;
                if (!_owner.WorldMatrix.Equals(_oldWorldMatrix))
                {
                    _oldWorldMatrix = _owner.WorldMatrix;
                    _transformedArea = _area.Transform(_oldWorldMatrix);
                }
                return _transformedArea;
            }
        }

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
        /// Data storage for PhysicsEngine.
        /// </summary>
        public object CollisionData { get { return _collisionData; } set { _collisionData = value; } }

        static CollisionArea()
        {
            g_collisionMaterials = new CollisionMaterial[Enum.GetValues(typeof(CollisionMaterialType)).Length];
            for (int i = 0; i < g_collisionMaterials.Length; ++i) g_collisionMaterials[i].Elasticity = -1;

            g_collisionMaterials[(int)CollisionMaterialType.Regular] = new CollisionMaterial
            {
                Elasticity = 0.9f,
                Friction = 0.5f,
                Damage = 1.0f,
            };
            g_collisionMaterials[(int)CollisionMaterialType.Rough] = new CollisionMaterial
            {
                Elasticity = 0.2f,
                Friction = 0.7f,
                Damage = 1.0f,
            };
            g_collisionMaterials[(int)CollisionMaterialType.Bouncy] = new CollisionMaterial
            {
                Elasticity = 2.0f,
                Friction = 0.1f,
                Damage = 1.0f,
            };
            g_collisionMaterials[(int)CollisionMaterialType.Sticky] = new CollisionMaterial
            {
                Elasticity = 0.01f,
                Friction = 4.0f,
                Damage = 0.0f,
            };

            if (g_collisionMaterials.Any(mat => mat.Elasticity == -1))
                throw new ApplicationException("Invalid number of collision materials defined");
        }

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
            _area = new Everything();
            _transformedArea = null;
            _oldWorldMatrix = 0 * Matrix.Identity;
            _owner = null;
            _collisionData = null;
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
            _transformedArea = null;
            _oldWorldMatrix = 0 * Matrix.Identity;
            _owner = owner;
            _collisionData = null;
        }

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((int)_type);
                writer.Write((int)_collidesAgainst);
                writer.Write((int)_cannotOverlap);
                writer.Write((string)_name, 32, true);
                writer.Write((byte)_collisionMaterial);
                _area.Serialize(writer, SerializationModeFlags.All);
            }
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                _type = (CollisionAreaType)reader.ReadInt32();
                _collidesAgainst = (CollisionAreaType)reader.ReadInt32();
                _cannotOverlap = (CollisionAreaType)reader.ReadInt32();
                _name = reader.ReadString(32);
                _collisionMaterial = (CollisionMaterialType)reader.ReadByte();
                _area.Deserialize(reader, SerializationModeFlags.All, framesAgo);
            }
        }
    }
}
