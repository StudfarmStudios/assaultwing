using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A network message about a textual message to a player's chat box overlay.
    /// </summary>
    public class PlayerMessageMessage : GameplayMessage
    {
        protected static MessageType messageType = new MessageType(0x2b, false);

        public int PlayerId { get; set; }
        public string Text { get; set; }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            // Player message (request) message structure:
            // int player ID
            // variable_length_string message text
            writer.Write((int)PlayerId);
            writer.Write((string)Text);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            PlayerId = reader.ReadInt32();
            Text = reader.ReadString();
        }

        public override string ToString()
        {
            return base.ToString() + " [PlayerId " + PlayerId + ", Message '" + Text + "']";
        }
    }
}
