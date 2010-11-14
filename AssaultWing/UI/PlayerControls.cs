using System;

namespace AW2.UI
{
    /// <summary>
    /// A player's in-game controls.
    /// </summary>
    public struct PlayerControls
    {
        public static readonly int CONTROL_COUNT = Enum.GetValues(typeof(PlayerControlType)).Length;

        /// <summary>
        /// Thrusts the player's ship forward, or moves up in a menu.
        /// </summary>
        public Control Thrust;

        /// <summary>
        /// Turns the player's ship counter-clockwise, or moves left in a menu.
        /// </summary>
        public Control Left;

        /// <summary>
        /// Turns the player's ship clockwise, or moves right in a menu.
        /// </summary>
        public Control Right;

        /// <summary>
        /// Uses the player's ship modification, or moves down in a menu.
        /// </summary>
        public Control Down;

        /// <summary>
        /// Fires the player's ship's primary weapon, or performs a selected action in a menu.
        /// </summary>
        public Control Fire1;

        /// <summary>
        /// Fires the player's ship's secondary weapon, or performs an additional action in a menu.
        /// </summary>
        public Control Fire2;

        /// <summary>
        /// Uses the player's ship's extra function, or performs yet an additional action in a menu.
        /// </summary>
        public Control Extra;

        /// <summary>
        /// Returns the specified control.
        /// </summary>
        /// <param name="type">The type of control.</param>
        /// <returns>The control.</returns>
        public Control this[PlayerControlType type]
        {
            get
            {
                switch (type)
                {
                    case PlayerControlType.Thrust: return Thrust;
                    case PlayerControlType.Left: return Left;
                    case PlayerControlType.Right: return Right;
                    case PlayerControlType.Down: return Down;
                    case PlayerControlType.Fire1: return Fire1;
                    case PlayerControlType.Fire2: return Fire2;
                    case PlayerControlType.Extra: return Extra;
                    default: throw new ArgumentException("Unknown control type " + Enum.GetName(typeof(PlayerControlType), type));
                }
            }
        }

        public ControlState[] GetStates()
        {
            return new[]
            {
                Thrust.State,
                Left.State,
                Right.State,
                Fire1.State,
                Fire2.State,
                Extra.State,
            };
        }
    }
}
