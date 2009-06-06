using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Helpers.Geometric;

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
    /// An area with which a gob can overlap with other gobs' areas,
    /// resulting in a collision.
    /// </summary>
    [LimitedSerialization]
    [System.Diagnostics.DebuggerDisplay("Type:{type} Name:{name} Area:{area}")]
    public class CollisionArea
    {
        /// <summary>
        /// Upper limit for the number of bits in the type representing <see cref="CollisionAreaType"/>.
        /// </summary>
        public const int COLLISION_AREA_TYPE_COUNT = 32;

        [TypeParameter]
        CollisionAreaType type;

        [TypeParameter]
        CollisionAreaType collidesAgainst;

        [TypeParameter]
        CollisionAreaType cannotOverlap;

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
        /// The geometric area for overlap testing, in game world coordinates,
        /// translated according to the hosting gob's location.
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
        /// The geometric area for overlap testing, in hosting gob coordinates.
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
        /// <param name="collidesAgainst">The types of collision areas this area collides against.</param>
        /// <param name="cannotOverlap">The types of collision areas this area collides against and cannot overlap.</param>
        public CollisionArea(string name, IGeomPrimitive area, Gob owner,
            CollisionAreaType type, CollisionAreaType collidesAgainst, CollisionAreaType cannotOverlap)
        {
            this.type = type;
            this.collidesAgainst = collidesAgainst;
            this.cannotOverlap = cannotOverlap;
            this.name = name;
            this.area = area;
            this.transformedArea = null;
            this.oldWorldMatrix = 0 * Matrix.Identity;
            this.owner = owner;
            this.collisionData = null;
        }
    }
}
