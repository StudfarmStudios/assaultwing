using System;
using System.Collections.Generic;
using System.Text;
using AW2.UI;

namespace AW2.Events
{
    /// <summary>
    /// Event signalling of a player's in-game control.
    /// </summary>
    class PlayerControlEvent : Event
    {
        string playerName;
        PlayerControlType controlType;
        float force;
        bool pulse;

        /// <summary>
        /// The name of the player whose control we are signalling about.
        /// </summary>
        public string PlayerName { get { return playerName; } set { playerName = value; } }

        /// <summary>
        /// The type of the player's control.
        /// </summary>
        public PlayerControlType ControlType { get { return controlType; } set { controlType = value; } }

        /// <summary>
        /// The current force amount of the control.
        /// </summary>
        public float Force { get { return force; } set { force = value; } }

        /// <summary>
        /// The current pulse state of the control.
        /// </summary>
        public bool Pulse { get { return pulse; } set { pulse = value; } }

        /// <summary>
        /// Creates a new player control event.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <param name="controlType">The type of the player's control.</param>
        /// <param name="force">The current force amount of the control.</param>
        /// <param name="pulse">The current pulse state of the control.</param>
        public PlayerControlEvent(string playerName, PlayerControlType controlType, float force, bool pulse)
        {
            this.playerName = playerName;
            this.controlType = controlType;
            this.force = force;
            this.pulse = pulse;
        }
    }
}
