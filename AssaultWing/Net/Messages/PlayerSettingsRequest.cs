using System;
using AW2.Helpers.Serialization;
using AW2.Net.ConnectionUtils;

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

        /// <summary>
        /// As <see cref="GameClientStatus.IsPlayingArena"/>.
        /// Used only in messages from a game client to a game server.
        /// </summary>
        public bool IsGameClientPlayingArena { get; set; }

        /// <summary>
        /// Is the client in menus, ready to start playing an arena.
        /// Used only in messages from a game client to a game server.
        /// </summary>
        public bool IsGameClientReadyToStartArena { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                checked
                {
                    // Player settings request structure:
                    // bool: has the player been registered to the server
                    // bool: is the game client playing the current arena
                    // bool: is the game client ready to play the next arena
                    // byte: player identifier
                    // word: data length N
                    // N bytes: serialised data of the player
                    writer.Write((bool)IsRegisteredToServer);
                    writer.Write((bool)IsGameClientPlayingArena);
                    writer.Write((bool)IsGameClientReadyToStartArena);
                    writer.Write((byte)PlayerID);
                    writer.Write((ushort)StreamedData.Length);
                    writer.Write(StreamedData, 0, StreamedData.Length);
                }
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            IsRegisteredToServer = reader.ReadBoolean();
            IsGameClientPlayingArena = reader.ReadBoolean();
            IsGameClientReadyToStartArena = reader.ReadBoolean();
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
