using System;
using System.Collections.Generic;
using AW2.Helpers.Serialization;
using AW2.UI;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game client to a game server containing
    /// the state of the controls of a player at the client.
    /// </summary>
    [MessageType(0x22, false)]
    public class PlayerControlsMessage : GameplayMessage
    {
        /// <summary>
        /// Identifier of the player the message is about.
        /// </summary>
        public int PlayerID { get; set; }

        /// <summary>
        /// The states of all the controls of the player.
        /// </summary>
        public IList<ControlState> ControlStates { get; private set; }

        public ControlState GetControlState(PlayerControlType controlType)
        {
            return ControlStates[(int)controlType];
        }

        public void SetControlState(PlayerControlType controlType, ControlState state)
        {
            ControlStates[(int)controlType] = state;
        }

        public PlayerControlsMessage()
        {
            ControlStates = new ControlState[PlayerControls.CONTROL_COUNT];
        }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            checked
            {
                // Player controls (request) message structure:
                // byte: player ID
                // loop PlayerControls.CONTROL_COUNT times
                //   byte: highest bit = control pulse; other bits = control force
                writer.Write((byte)PlayerID);
                foreach (var state in ControlStates)
                {
                    var force7Bit = (byte)(state.Force * 127);
                    var pulseHighBit = state.Pulse ? (byte)0x80 : (byte)0x00;
                    var value = (byte)(force7Bit | pulseHighBit);
                    writer.Write((byte)value);
                }
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            PlayerID = reader.ReadByte();
            for (int i = 0; i < PlayerControls.CONTROL_COUNT; ++i)
            {
                var value = reader.ReadByte();
                var force = (value & 0x7f) / 127f;
                var pulse = (value & 0x80) != 0;
                ControlStates[i] = new ControlState(force, pulse);
            }
        }

        public override string ToString()
        {
            return base.ToString() + " [PlayerID " + PlayerID + "]";
        }
    }
}
