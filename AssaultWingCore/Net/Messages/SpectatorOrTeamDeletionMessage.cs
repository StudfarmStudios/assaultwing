using System;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client signalling the removal of a remote player.
    /// </summary>
    [MessageType(0x2e, false)]
    public class SpectatorOrTeamDeletionMessage : Message
    {
        /// <summary>
        /// Identifier of the spectator or team to delete.
        /// </summary>
        public int SpectatorOrTeamID { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                checked
                {
                    // Spectator or team deletion (request) message structure:
                    // sbyte: spectator or team identifier
                    writer.Write((sbyte)SpectatorOrTeamID);
                }
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            SpectatorOrTeamID = reader.ReadSByte();
        }

        public override string ToString()
        {
            return base.ToString() + " [ID " + SpectatorOrTeamID + "]";
        }
    }
}
