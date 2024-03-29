using System;
using AW2.Helpers.Serialization;
using AW2.Net.ConnectionUtils;
using AW2.Game;
using AW2.Game.Players;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game instance to another, requesting the update of the settings
    /// of a <see cref="Spectator"/>. This may implicitly request creating the spectator
    /// on the remote game instance.
    /// </summary>
    [MessageType(0x2d, false)]
    public class SpectatorSettingsRequest : StreamMessage
    {
        public enum SubclassType { Undefined, Spectator, Player, BotPlayer };

        public static SubclassType GetSubclassType(Spectator spectator)
        {
            return spectator is Player ? SubclassType.Player :
                spectator is BotPlayer ? SubclassType.BotPlayer :
                SubclassType.Spectator;
        }

        /// <summary>
        /// The actual type of the spectator.
        /// </summary>
        public SubclassType Subclass { get; set; }

        /// <summary>
        /// Has the spectator (who lives at a client) been registered to the server.
        /// Meaningful only when sent from a client to the server.
        /// </summary>
        public bool IsRegisteredToServer { get; set; }

        /// <summary>
        /// The identifier of the spectator to update.
        /// If <see cref="IsRegisteredToServer"/> is true, the identifier is global to the game, as set by the game server.
        /// If <see cref="IsRegisteredToServer"/> is false, the identifier is local to the game client where the spectator lives.
        /// </summary>
        public int SpectatorID { get; set; }

        /// <summary>
        /// If null, the client is not requesting spawn. Otherwise the value is the <see cref="Arena.ID"/>
        /// of the arena into which the client is requesting to be spawned.
        /// Used only in messages from a game client to a game server.
        /// </summary>
        public byte? IsRequestingSpawnForArenaID { get; set; }

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
                    // Spectator settings request structure:
                    // bool: has the spectator been registered to the server
                    // byte?: the ID of the arena the game client wants to spawn to, or null if no spawn is wanted
                    // bool: is the game client ready to play the next arena
                    // byte: spectator identifier
                    // byte: spectator subclass
                    // word: data length N
                    // N bytes: serialised data of the spectator
                    writer.Write((bool)IsRegisteredToServer);
                    writer.Write((byte?)IsRequestingSpawnForArenaID);
                    writer.Write((bool)IsGameClientReadyToStartArena);
                    writer.Write((byte)SpectatorID);
                    writer.Write((byte)Subclass);
                    writer.Write((ushort)StreamedData.Length);
                    writer.Write(StreamedData, 0, StreamedData.Length);
                }
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            IsRegisteredToServer = reader.ReadBoolean();
            IsRequestingSpawnForArenaID = reader.ReadNullableByte();
            IsGameClientReadyToStartArena = reader.ReadBoolean();
            SpectatorID = reader.ReadByte();
            Subclass = (SubclassType)reader.ReadByte();
            if (!Enum.IsDefined(typeof(SubclassType), Subclass)) throw new NetworkException("Invalid value for Subclass, " + Subclass);
            int byteCount = reader.ReadUInt16();
            StreamedData = reader.ReadBytes(byteCount);
        }

        public override string ToString()
        {
            return base.ToString() + " [SpectatorID " + SpectatorID + ", Subclass " + Subclass + "]";
        }
    }
}
