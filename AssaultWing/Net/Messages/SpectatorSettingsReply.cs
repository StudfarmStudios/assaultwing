using System;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A reply from a game server to a game client, acknowledging the adding of a new spectator
    /// to the game and specifying the spectator's new ID in the network game.
    /// </summary>
    [MessageType(0x2d, true)]
    public class SpectatorSettingsReply : Message
    {
        /// <summary>
        /// Local identifier of the spectator on the game client.
        /// </summary>
        public int SpectatorLocalID { get; set; }

        /// <summary>
        /// New (non-local) identifier of the spectator.
        /// </summary>
        public int SpectatorID { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                // Spectator update reply structure:
                // byte: local spectator identifier
                // byte: non-local spectator identifier
                writer.Write((byte)SpectatorLocalID);
                writer.Write((byte)SpectatorID);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            SpectatorLocalID = reader.ReadByte();
            SpectatorID = reader.ReadByte();
        }

        public override string ToString()
        {
            return base.ToString() + " [local " + SpectatorLocalID + " -> global " + SpectatorID + "]";
        }
    }
}
