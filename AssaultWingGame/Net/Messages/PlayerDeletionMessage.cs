using System;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client signalling the removal of a remote player.
    /// </summary>
    [MessageType(0x2e, false)]
    public class PlayerDeletionMessage : Message
    {
        /// <summary>
        /// Identifier of the player to delete.
        /// </summary>
        public int PlayerID { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
            checked
            {
                // Player deletion (request) message structure:
                // int: player identifier
                writer.Write((byte)PlayerID);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            PlayerID = reader.ReadByte();
        }

        public override string ToString()
        {
            return base.ToString() + " [PlayerID " + PlayerID + "]";
        }
    }
}
