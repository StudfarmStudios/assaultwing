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
    [MessageType(0x2b, false)]
    public class PlayerMessageMessage : GameplayMessage
    {
        /// <summary>
        /// Receiving player identifier. From client to server, this can be -1
        /// to mean broadcast to all players.
        /// </summary>
        public int PlayerID { get; set; }
        public PlayerMessage Message { get; set; }
        public bool AllPlayers { get { return PlayerID == -1; } }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                base.SerializeBody(writer);
                checked
                {
                    // Player message (request) message structure:
                    // short: player ID
                    // word: data length, N
                    // N bytes: serialised message data
                    Write(Message, SerializationModeFlags.AllFromServer);
                    writer.Write((short)PlayerID);
                    writer.Write((ushort)StreamedData.Length);
                    writer.Write(StreamedData, 0, StreamedData.Length);
                }
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            PlayerID = reader.ReadInt16();
            int byteCount = reader.ReadUInt16();
            StreamedData = reader.ReadBytes(byteCount);
            Message = new PlayerMessage();
            Read(Message, SerializationModeFlags.AllFromServer, 0);
        }

        public override string ToString()
        {
            var recipientText = AllPlayers ? "All players" : "PlayerID " + PlayerID;
            return base.ToString() + " [" + recipientText + ", Message '" + Message + "']";
        }
    }
}
