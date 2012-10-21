using System;
using AW2.Game.Players;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client updating the state of a spectator or a team.
    /// </summary>
    [MessageType(0x28, false)]
    public class SpectatorOrTeamUpdateMessage : StreamMessage
    {
        public enum ClassType { Spectator, Team }

        /// <summary>
        /// Identifier of the spectator or team to update.
        /// </summary>
        public int SpectatorOrTeamID { get; private set; }

        /// <summary>
        /// Is this message is about a team or about a spectator.
        /// </summary>
        public ClassType Class { get; private set; }

        /// <summary>
        /// Only for deserialization.
        /// </summary>
        public SpectatorOrTeamUpdateMessage()
        {
        }

        public SpectatorOrTeamUpdateMessage(Spectator spectator, SerializationModeFlags serializationMode)
        {
            Class = ClassType.Spectator;
            SpectatorOrTeamID = spectator.ID;
            Write(spectator, serializationMode);
        }

        public SpectatorOrTeamUpdateMessage(Team team, SerializationModeFlags serializationMode)
        {
#warning NEVER CALLED!!!
            Class = ClassType.Team;
            SpectatorOrTeamID = team.ID;
            Write(team, serializationMode);
        }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(null))
#endif
            checked
            {
                // Spectator or team update (request) message structure:
                // byte (ClassType): class identifier
                // byte: spectator or team identifier
                // ushort: data length N
                // N bytes: serialised data of the spectator or team
                byte[] writeBytes = StreamedData;
#if NETWORK_PROFILING
                using (new NetworkProfilingScope("SpectatorOrTeamUpdateMessageHeader"))
#endif
                {
                    writer.Write((byte)Class);
                    writer.Write((byte)SpectatorOrTeamID);
                    writer.Write((ushort)writeBytes.Length);
                }
                writer.Write(writeBytes, 0, writeBytes.Length);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            Class = (ClassType)reader.ReadByte();
            SpectatorOrTeamID = reader.ReadByte();
            int byteCount = reader.ReadUInt16();
            StreamedData = reader.ReadBytes(byteCount);
            if (!Enum.IsDefined(typeof(ClassType), Class)) throw new NetworkException("Invalid value for Class, " + Class);
        }

        public override string ToString()
        {
            return base.ToString() + " [SpectatorOrTeamID " + SpectatorOrTeamID + ", Class " + Class + "]";
        }
    }
}
