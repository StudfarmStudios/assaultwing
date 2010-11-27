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

        public int PlayerID { get; set; }
        public Color Color { get; set; }
        public string Text { get; set; }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            // Player message (request) message structure:
            // int: player ID
            // Color: message color
            // variable_length_string message: text
            writer.Write((int)PlayerID);
            writer.Write((Color)Color);
            writer.Write((string)Text);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            PlayerID = reader.ReadInt32();
            Color = reader.ReadColor();
            Text = reader.ReadString();
        }

        public override string ToString()
        {
            return base.ToString() + " [PlayerID " + PlayerID + ", Message '" + Text + "']";
        }
    }
}
