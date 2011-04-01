using System;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A reply from a game server to a game client, acknowledging the adding of a new player
    /// to the game and specifying the player's new ID in the network game.
    /// </summary>
    [MessageType(0x2d, true)]
    public class PlayerSettingsReply : Message
    {
        /// <summary>
        /// Local identifier of the player on the game client.
        /// </summary>
        public int PlayerLocalID { get; set; }

        /// <summary>
        /// New (non-local) identifier of the player.
        /// </summary>
        public int PlayerID { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                // Player update reply structure:
                // byte: local player identifier
                // byte: non-local player identifier
                writer.Write((byte)PlayerLocalID);
                writer.Write((byte)PlayerID);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            PlayerLocalID = reader.ReadByte();
            PlayerID = reader.ReadByte();
        }

        public override string ToString()
        {
            return base.ToString() + " [local " + PlayerLocalID + " -> global " + PlayerID + "]";
        }
    }
}
