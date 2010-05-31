﻿using System;
using System.Collections.Generic;
using AW2.UI;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game client to a game server containing
    /// the state of the controls of a player at the client.
    /// </summary>
    public class PlayerControlsMessage : GameplayMessage
    {
        ControlState[] controlStates = new ControlState[Enum.GetValues(typeof(PlayerControlType)).Length];

        /// <summary>
        /// Identifier of the player the message is about.
        /// </summary>
        public int PlayerId { get; set; }

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x22, false);

        /// <summary>
        /// Returns the state of a control of the player.
        /// </summary>
        /// <param name="controlType">Type of the control whose state to return.</param>
        /// <returns>The state of a control of the player.</returns>
        public ControlState GetControlState(PlayerControlType controlType)
        {
            return controlStates[(int)controlType];
        }

        /// <summary>
        /// The states of all the controls of the player.
        /// </summary>
        public IList<ControlState> ControlStates { get { return controlStates; } }

        /// <summary>
        /// Sets the state of a control of the player.
        /// </summary>
        /// <param name="controlType">Type of the control whose state to return.</param>
        /// <param name="state">State of the control.</param>
        public void SetControlState(PlayerControlType controlType, ControlState state)
        {
            controlStates[(int)controlType] = state;
        }

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// <param name="writer">Writer of serialised data.</param>
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            // Player controls (request) message structure:
            // int player ID
            // repeat over PlayerControlType
            //   4 bytes = float: force of the control
            //   1 byte  = bool:  pulse of the control
            writer.Write((int)PlayerId);
            foreach (ControlState state in controlStates)
            {
                writer.Write((float)state.Force);
                writer.Write((bool)state.Pulse);
            }
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            PlayerId = reader.ReadInt32();
            for (int i = 0; i < controlStates.Length; ++i)
            {
                float force = reader.ReadSingle();
                bool pulse = reader.ReadBoolean();
                controlStates[i] = new ControlState(force, pulse);
            }
        }

        /// <summary>
        /// Returns a String that represents the current Object. 
        /// </summary>
        public override string ToString()
        {
            return base.ToString() + " [PlayerId " + PlayerId + "]";
        }
    }
}
