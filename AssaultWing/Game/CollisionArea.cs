using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Net;

namespace AW2.Game
{
    /// <summary>
    /// Type of collision area.
    /// </summary>
    /// <remarks>Some of the values denote groups of types of collision areas. Such
    /// values are not to be used to denote a single collision area's type. They
    /// are for marking a range of collision area types e.g. in specifying which
    /// types of collision areas to collide against.</remarks>
    [Flags]
    public enum CollisionAreaType
    {
        /// <summary>
        /// A non-physical collision area that collides against other collision areas.
        /// </summary>
        Receptor = 0x0001,

        /// <summary>
        /// A non-physical collision area that collides against gob locations.
        /// </summary>
        Force = 0x0002,

        /// <summary>
        /// A bounding volume for a piece of wall.
        /// </summary>
        WallBounds = 0x0004,

        /// <summary>
        /// The physical collision area of a ship.
        /// </summary>
        PhysicalShip = 0x0010,

        /// <summary>
        /// The physical collision area of a shot.
        /// </summary>
        PhysicalShot = 0x0020,

        /// <summary>
        /// The physical collision area of a piece of wall.
        /// </summary>
        PhysicalWall = 0x0040,
        
        /// <summary>
        /// The physical collision area of a blob of water.
        /// </summary>
        PhysicalWater = 0x0080,

        /// <summary>
        /// The physical collision area of a cloud of gas.
        /// </summary>
        PhysicalGas = 0x0100,

        /// <summary>
        /// The physical collision area of a nonspecific gob type that is neither damageable nor movable.
        /// </summary>
        PhysicalOtherUndamageableUnmovable = 0x1000,
        
        /// <summary>
        /// The physical collision area of a nonspecific gob type that is damageable but not movable.
        /// </summary>
        PhysicalOtherDamageableUnmovable = 0x2000,

        /// <summary>
        /// The physical collision area of a nonspecific gob type that is not damageable but is movable.
        /// </summary>
        PhysicalOtherUndamageableMovable = 0x4000,
        
        /// <summary>
        /// The physical collision area of a nonspecific gob type that is both damageable and movable.
        /// </summary>
        PhysicalOtherDamageableMovable = 0x8000,

        
        /// <summary>
        /// The (empty) group of no collision area types.
        /// </summary>
        None = 0,

        /// <summary>
        /// The group of physical collision areas of all gob types.
        /// </summary>
        Physical = CollisionAreaType.PhysicalShip |
                   CollisionAreaType.PhysicalShot |
                   CollisionAreaType.PhysicalWall |
                   CollisionAreaType.PhysicalWater |
                   CollisionAreaType.PhysicalGas |
                   CollisionAreaType.PhysicalOther,

        /// <summary>
        /// The group of physical collision areas of nonspecific gob types.
        /// </summary>
        PhysicalOther = CollisionAreaType.PhysicalOtherUndamageableUnmovable |
                        CollisionAreaType.PhysicalOtherDamageableUnmovable |
                        CollisionAreaType.PhysicalOtherUndamageableMovable |
                        CollisionAreaType.PhysicalOtherDamageableMovable,
        
        /// <summary>
        /// The group of physical collision areas of nonspecific gob types that are movable.
        /// </summary>
        PhysicalOtherMovable = CollisionAreaType.PhysicalOtherUndamageableMovable |
                               CollisionAreaType.PhysicalOtherDamageableMovable,
        
        /// <summary>
        /// The group of physical collision areas of nonspecific gob types that are damageable.
        /// </summary>
        PhysicalOtherDamageable = CollisionAreaType.PhysicalOtherDamageableUnmovable |
                                  CollisionAreaType.PhysicalOtherDamageableMovable,
        
        /// <summary>
        /// The group of physical collision areas of all gob types that are have a concrete body that must avoid overlap with other concrete bodies, i.e.
        /// whose overlap consistency is compromisable.
        /// </summary>
        PhysicalConsistencyCompromisable = CollisionAreaType.PhysicalShip |
                                           CollisionAreaType.PhysicalWall |
                                           CollisionAreaType.PhysicalOther,
        
        /// <summary>
        /// The group of physical collision areas of all gob types that are damageable.
        /// </summary>
        PhysicalDamageable = CollisionAreaType.PhysicalShip |
                             CollisionAreaType.PhysicalOtherDamageable,
        
        /// <summary>
        /// The group of physical collision areas of all gob types that are movable.
        /// </summary>
        PhysicalMovable = CollisionAreaType.PhysicalShip |
                          CollisionAreaType.PhysicalShot |
                          CollisionAreaType.PhysicalWater |
                          CollisionAreaType.PhysicalGas |
                          CollisionAreaType.PhysicalOtherMovable,
    }

    /// <summary>
    /// Type of material of a collision area.
    /// </summary>
    /// The collision material determines the behaviour of a physical collision
    /// area in a physical collision. A material consists of elasticity, friction
    /// and damage factor.
    public enum CollisionMaterialType
    {
        /// <summary>
        /// Quite elastic, with moderate friction, normal damage
        /// </summary>
        Regular,

        /// <summary>
        /// Rather inelastic, with strong friction, normal damage
        /// </summary>
        Rough,

        /// <summary>
        /// Excessively elastic, with moderate friction, normal damage
        /// </summary>
        Bouncy,

        /// <summary>
        /// Very inelastic, with high friction, no damage
        /// </summary>
        Sticky,
    }

    /// <summary>
    /// An area with which a gob can overlap with other gobs' areas,
    /// resulting in a collision.
    /// </summary>
    [LimitedSerialization]
    [System.Diagnostics.DebuggerDisplay("Type:{type} Name:{name} Area:{area}")]
    public class CollisionArea : INetworkSerializable
    {
        struct CollisionMaterial
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

        static CollisionMaterial[] collisionMaterials;

        [TypeParameter]
        CollisionAreaType type;

        [TypeParameter]
        CollisionAreaType collidesAgainst;

        [TypeParameter]
        CollisionAreaType cannotOverlap;

        [TypeParameter]
        CollisionMaterialType collisionMaterial;

        [TypeParameter]
        string name;

        /// <summary>
        /// Area in gob coordinates, not world coordinates.
        /// </summary>
        [TypeParameter, ShallowCopy]
        IGeomPrimitive area;

        /// <summary>
        /// Area in world coordinates, transformed by <b>oldWorldMatrix</b>.
        /// </summary>
        IGeomPrimitive transformedArea;
        Matrix oldWorldMatrix;

        Gob owner;
        
        object collisionData;

        /// <summary>
        /// Collision area name; either "General" for general collision
        /// checking (including physical collisions), or something else
        /// for a receptor area that can react to other gobs' general
        /// areas.
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// The type of the collision area.
        /// </summary>
        public CollisionAreaType Type { get { return type; } }

        /// <summary>
        /// The types of collision areas this collision area collides against.
        /// </summary>
        public CollisionAreaType CollidesAgainst { get { return collidesAgainst; } }

        /// <summary>
        /// The types of collision areas this collision area collides physically against,
        /// i.e. types of collision areas that this collision area cannot overlap.
        /// </summary>
        public CollisionAreaType CannotOverlap { get { return cannotOverlap; } }

        /// <summary>
        /// Elasticity factor of the collision area. Zero means no collision bounce.
        /// One means fully elastic collision.
        /// </summary>
        public float Elasticity { get { return collisionMaterials[(int)collisionMaterial].Elasticity; } }

        /// <summary>
        /// Friction factor of the collision area. Zero means that movement along the
        /// collision surface is not slowed by friction.
        /// </summary>
        public float Friction { get { return collisionMaterials[(int)collisionMaterial].Friction; } }

        /// <summary>
        /// Multiplier for collision damage.
        /// </summary>
        public float Damage { get { return collisionMaterials[(int)collisionMaterial].Damage; } }

        /// <summary>
        /// The geometric area for overlap testing, in game world coordinates,
        /// transformed according to the hosting gob's world matrix.
        /// </summary>
        public IGeomPrimitive Area
        {
            get
            {
                if (!owner.Movable)
                    return area;
                if (!owner.WorldMatrix.Equals(oldWorldMatrix))
                {
                    oldWorldMatrix = owner.WorldMatrix;
                    transformedArea = area.Transform(oldWorldMatrix);
                }
                return transformedArea;
            }
        }

        /// <summary>
        /// The geometric area for overlap testing, in hosting gob coordinates if the gob is movable,
        /// in world coordinates if the gob is unmovable.
        /// </summary>
        public IGeomPrimitive AreaGob { get { return area; } set { area = value; } }

        /// <summary>
        /// The gob whose collision area this is.
        /// </summary>
        public Gob Owner { get { return owner; } set { owner = value; } }

        /// <summary>
        /// Data storage for PhysicsEngine.
        /// </summary>
        public object CollisionData { get { return collisionData; } set { collisionData = value; } }

        static CollisionArea()
        {
            collisionMaterials = new CollisionMaterial[Enum.GetValues(typeof(CollisionMaterialType)).Length];
            for (int i = 0; i < collisionMaterials.Length; ++i) collisionMaterials[i].Elasticity = -1;

            collisionMaterials[(int)CollisionMaterialType.Regular] = new CollisionMaterial
            {
                Elasticity = 0.9f,
                Friction = 0.5f,
                Damage = 1.0f,
            };
            collisionMaterials[(int)CollisionMaterialType.Rough] = new CollisionMaterial
            {
                Elasticity = 0.2f,
                Friction = 0.7f,
                Damage = 1.0f,
            };
            collisionMaterials[(int)CollisionMaterialType.Bouncy] = new CollisionMaterial
            {
                Elasticity = 2.0f,
                Friction = 0.1f,
                Damage = 1.0f,
            };
            collisionMaterials[(int)CollisionMaterialType.Sticky] = new CollisionMaterial
            {
                Elasticity = 0.01f,
                Friction = 4.0f,
                Damage = 0.0f,
            };

            if (collisionMaterials.Any(mat => mat.Elasticity == -1))
                throw new Exception("Invalid number of collision materials defined");
        }

        /// <summary>
        /// Creates an uninitialised collision area. 
        /// This constructor is only for (de)serialisation.
        /// </summary>
        public CollisionArea()
        {
            type = CollisionAreaType.None;
            collidesAgainst = CollisionAreaType.None;
            cannotOverlap = CollisionAreaType.None;
            name = "dummyarea";
            collisionMaterial = CollisionMaterialType.Regular;
            area = new Everything();
            transformedArea = null;
            oldWorldMatrix = 0 * Matrix.Identity;
            owner = null;
            collisionData = null;
        }

        /// <summary>
        /// Creates a new collision area.
        /// </summary>
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
            this.type = type;
            this.collidesAgainst = collidesAgainst;
            this.cannotOverlap = cannotOverlap;
            this.collisionMaterial = collisionMaterial;
            this.name = name;
            this.area = area;
            this.transformedArea = null;
            this.oldWorldMatrix = 0 * Matrix.Identity;
            this.owner = owner;
            this.collisionData = null;
        }

        #region INetworkSerializable Members

        /// <summary>
        /// Serialises the object to a binary writer.
        /// </summary>
        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((int)type);
                writer.Write((int)collidesAgainst);
                writer.Write((int)cannotOverlap);
                writer.Write((string)name, 32, true);
                writer.Write((byte)collisionMaterial);
                area.Serialize(writer, SerializationModeFlags.All);
            }
        }

        /// <summary>
        /// Deserialises the object from a binary writer.
        /// </summary>
        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode)
        {
            type = (CollisionAreaType)reader.ReadInt32();
            collidesAgainst = (CollisionAreaType)reader.ReadInt32();
            cannotOverlap = (CollisionAreaType)reader.ReadInt32();
            name = reader.ReadString(32);
            collisionMaterial = (CollisionMaterialType)reader.ReadByte();
            area.Deserialize(reader, SerializationModeFlags.All);
        }

        #endregion
    }
}
