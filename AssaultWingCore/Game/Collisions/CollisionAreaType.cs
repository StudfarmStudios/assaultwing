namespace AW2.Game.Collisions
{
    public enum CollisionAreaType
    {
        /// <summary>
        /// Receptor area of gobs that can collect a bonus.
        /// </summary>
        BonusCollect,

        /// <summary>
        /// Physical area of common gobs.
        /// </summary>
        Common,

        /// <summary>
        /// Receptor of targets that can be damaged.
        /// </summary>
        Damage,

        /// <summary>
        /// Doesn't collide with anything.
        /// </summary>
        Disabled,

        /// <summary>
        /// Receptor of targets of a flow of medium.
        /// </summary>
        Flow,

        /// <summary>
        /// Receptor of targets of a mine.
        /// </summary>
        MineMagnet,

        /// <summary>
        /// Physical area of a mine.
        /// </summary>
        MinePhysical,

        /// <summary>
        /// Receptor of friendly mines for spreading out.
        /// </summary>
        MineSpread,

        /// <summary>
        /// Physical area of a shot.
        /// </summary>
        Shot,

        /// <summary>
        /// Physical area of a gob that doesn't move.
        /// </summary>
        Static,
    }
}
