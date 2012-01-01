namespace AW2.UI
{
    /// <summary>
    /// The type of a player's control.
    /// </summary>
    public enum PlayerControlType
    {
        /// <summary>
        /// Thrusts the player's ship forward, or moves up in a menu.
        /// </summary>
        Thrust,

        /// <summary>
        /// Turns the player's ship counter-clockwise, or moves left in a menu.
        /// </summary>
        Left,

        /// <summary>
        /// Turns the player's ship clockwise, or moves right in a menu.
        /// </summary>
        Right,

        /// <summary>
        /// Fires the player's ship's primary weapon, or performs a selected action in a menu.
        /// </summary>
        Fire1,

        /// <summary>
        /// Fires the player's ship's secondary weapon, or performs an additional action in a menu.
        /// </summary>
        Fire2,

        /// <summary>
        /// Uses the player's ship's extra function, or performs yet an additional action in a menu.
        /// </summary>
        Extra,
    };
}
