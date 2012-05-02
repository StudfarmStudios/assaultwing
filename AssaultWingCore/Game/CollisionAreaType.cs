using System;

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
        #region Elementary types

        /// <summary>
        /// A non-physical collision area that collides against other collision areas.
        /// </summary>
        Receptor = 0x0001,

        /// <summary>
        /// A non-physical collision area that collides against gob locations.
        /// </summary>
        Force = 0x0002,

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

        #endregion

        #region Combined types

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
        /// The group of physical collision areas of all gob types that are have a concrete body that
        /// must avoid overlap with other concrete bodies, i.e. whose overlap consistency is compromisable.
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
                          CollisionAreaType.PhysicalOtherMovable,

        #endregion
    }
}
