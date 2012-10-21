using System;
using AW2.Helpers.Serialization;
using AW2.Game;
using AW2.Game.Players;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game instance to another, requesting the update of the settings
    /// of a <see cref="AW2.Game.Players.Team"/>. This may implicitly request creating the team
    /// on the remote game instance.
    /// </summary>
    [MessageType(0x32, false)]
    public class TeamSettingsMessage : StreamMessage
    {
        /// <summary>
        /// The identifier of the team to update.
        /// </summary>
        public int TeamID { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                // Team settings message (request) structure:
                // byte: team identifier
                // word: data length N
                // N bytes: serialised data of the team
                writer.Write((byte)TeamID);
                writer.Write((ushort)StreamedData.Length);
                writer.Write(StreamedData, 0, StreamedData.Length);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            TeamID = reader.ReadByte();
            int byteCount = reader.ReadUInt16();
            StreamedData = reader.ReadBytes(byteCount);
        }

        public override string ToString()
        {
            return base.ToString() + " [TeamID " + TeamID + "]";
        }
    }
}
