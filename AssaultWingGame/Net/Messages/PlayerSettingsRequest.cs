using System;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game instance to another, requesting the update of the settings of a player.
    /// This may implicitly request creating the player on the remote game instance.
    /// </summary>
    [MessageType(0x2d, false)]
    public class PlayerSettingsRequest : StreamMessage
    {
        /// <summary>
        /// Has the player (who lives at a client) been registered to the server.
        /// Meaningful only when sent from a client to the server.
        /// </summary>
        public bool IsRegisteredToServer { get; set; }

        /// <summary>
        /// If <see cref="IsRegisteredToServer"/> is true, local identifier of the player to update.
        /// If <see cref="IsRegisteredToServer"/> is false, global identifier of the player to update
        /// </summary>
        public int PlayerID { get; set; }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            checked
            {
                // Player settings request structure:
                // bool: has the player been registered to the server
                // byte: player identifier
                // word: data length N
                // N bytes: serialised data of the player
                byte[] writeBytes = StreamedData;
                writer.Write((bool)IsRegisteredToServer);
                writer.Write((byte)PlayerID);
                writer.Write((ushort)writeBytes.Length);
                writer.Write(writeBytes, 0, writeBytes.Length);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            IsRegisteredToServer = reader.ReadBoolean();
            PlayerID = reader.ReadByte();
            int byteCount = reader.ReadUInt16();
            StreamedData = reader.ReadBytes(byteCount);
        }

        public override string ToString()
        {
            return base.ToString() + " [PlayerID " + PlayerID + "]";
        }
    }
}
