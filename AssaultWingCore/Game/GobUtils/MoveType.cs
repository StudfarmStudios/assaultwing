using System;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Types of moving for a gob.
    /// </summary>
    public enum MoveType
    {
        /// <summary>
        /// The gob doesn't move.
        /// </summary>
        Static,

        /// <summary>
        /// The gob moves by the laws of physics.
        /// </summary>
        Dynamic,

        /// <summary>
        /// The gob moves along a set route.
        /// </summary>
        Kinematic,
    };
}
