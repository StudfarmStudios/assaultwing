using System;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A reply from a game server to a game client, acknowledging the adding of a new player
    /// to the game and specifying the player's new ID in the network game.
    /// </summary>
    public class PlayerSettingsReply : Message
    {
        protected static MessageType messageType = new MessageType(0x2d, true);

        /// <summary>
        /// The player's old identifier on the client.
        /// </summary>
        public int OldPlayerID { get; set; }

        /// <summary>
        /// The player's new identifier on the server.
        /// </summary>
        public int NewPlayerID { get; set; }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Player update reply structure:
            // byte: old player identifier
            // byte: new player identifier
            writer.Write((byte)OldPlayerID);
            writer.Write((byte)NewPlayerID);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            OldPlayerID = reader.ReadByte();
            NewPlayerID = reader.ReadByte();
        }

        public override string ToString()
        {
            return base.ToString() + " [PlayerID " + OldPlayerID + " -> " + NewPlayerID + "]";
        }
    }
}
