using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// that the game session starts.
    /// </summary>
    public class StartGameMessage : Message
    {
        protected static MessageType messageType = new MessageType(0x21, false);

        /// <summary>
        /// Names of arenas to play in the game session.
        /// </summary>
        public IList<string> ArenaPlaylist { get; set; }

        public StartGameMessage()
        {
            ArenaPlaylist = new List<string>();
        }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Start game (request) message structure:
            // int: total number of arenas in the game, M
            // 32 * M bytes: names of M arenas
            writer.Write((int)ArenaPlaylist.Count);
            foreach (string name in ArenaPlaylist) writer.Write((string)name, 32, true);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            int arenaCount = reader.ReadInt32();
            ArenaPlaylist.Clear();
            for (int i = 0; i < arenaCount; ++i) ArenaPlaylist.Add(reader.ReadString(32));
        }

        public override string ToString()
        {
            return base.ToString() + string.Format(" [ArenaPlaylist: {1}]", string.Join(", ", ArenaPlaylist.ToArray()));
        }
    }
}
