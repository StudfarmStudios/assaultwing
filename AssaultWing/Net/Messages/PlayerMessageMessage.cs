using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A network message about a textual message to a player's chat box overlay.
    /// </summary>
    public class PlayerMessageMessage : GameplayMessage
    {
        protected static MessageType messageType = new MessageType(0x2b, false);

        /// <summary>
        /// Receiving player identifier. From client to server, this can be -1
        /// to mean broadcast to all players.
        /// </summary>
        public int PlayerID { get; set; }
        public bool AllPlayers { get { return PlayerID == -1; } }
        public Color Color { get; set; }
        public string Text { get; set; }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            checked
            {
                // Player message (request) message structure:
                // short: player ID
                // Color: message color
                // variable_length_string message: text
                writer.Write((short)PlayerID);
                writer.Write((Color)Color);
                writer.Write((string)Text);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            PlayerID = reader.ReadInt16();
            Color = reader.ReadColor();
            Text = reader.ReadString();
        }

        public override string ToString()
        {
            var recipientText = AllPlayers ? "All players" : "PlayerID " + PlayerID;
            return base.ToString() + " [" + recipientText + ", Message '" + Text + "']";
        }
    }
}
