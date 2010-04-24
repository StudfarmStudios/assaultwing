using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using AW2.Game;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// that the game session starts. The message contains information
    /// about the player setup of the game session.
    /// </summary>
    /// To initialise a message for sending, set <see cref="ArenaPlaylist"/>
    /// and call <see cref="SerializePlayer"/> for each player.
    /// 
    /// When receiving a message, get <see cref="ArenaPlaylist"/> and
    /// call <see cref="DeserializePlayers"/> with a delegate that 
    /// returns for a player ID the Player instance where to deserialise
    /// that player's data.
    public class StartGameMessage : StreamMessage
    {
        public delegate Player ChoosePlayerDelegate(int playerID);

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x21, false);

        private List<int> PlayerIDs { get; set; }

        /// <summary>
        /// Names of arenas to play in the game session.
        /// </summary>
        public IList<string> ArenaPlaylist { get; set; }

        /// <summary>
        /// Creates an uninitialised start game message.
        /// </summary>
        public StartGameMessage()
        {
            ArenaPlaylist = new List<string>();
            PlayerIDs = new List<int>();
        }

        public void SerializePlayer(Player player)
        {
            PlayerIDs.Add(player.Id);
            Write(player, SerializationModeFlags.All);
        }

        public void DeserializePlayers(ChoosePlayerDelegate choosePlayer)
        {
            foreach (int playerID in PlayerIDs)
            {
                var player = choosePlayer(playerID);
                Read(player, SerializationModeFlags.All, TimeSpan.Zero);
            }
        }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Start game (request) message structure:
            // int: total number of players in the game, N
            // int: total number of arenas in the game, M
            // word: length of serialised data of all players, L
            // N ints: player IDs of N players
            // L bytes: serialised data of N players (content known only by Player)
            // 32 * M bytes: names of M arenas
            byte[] writeBytes = StreamedData;
            writer.Write((int)PlayerIDs.Count);
            writer.Write((int)ArenaPlaylist.Count);
            writer.Write(checked((ushort)writeBytes.Length));
            foreach (int playerID in PlayerIDs) writer.Write((int)playerID);
            writer.Write(writeBytes, 0, writeBytes.Length);
            foreach (string name in ArenaPlaylist) writer.Write((string)name, 32, true);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            int playerCount = reader.ReadInt32();
            int arenaCount = reader.ReadInt32();
            int byteCount = reader.ReadUInt16();
            for (int i = 0; i < playerCount; i++) PlayerIDs.Add(reader.ReadInt32());
            StreamedData = reader.ReadBytes(byteCount);
            ArenaPlaylist.Clear();
            for (int i = 0; i < arenaCount; ++i) ArenaPlaylist.Add(reader.ReadString(32));
        }

        public override string ToString()
        {
            return base.ToString() + string.Format(" PlayerIDs: {0}, ArenaPlaylist: {1}", PlayerIDs, ArenaPlaylist);
        }
    }
}
