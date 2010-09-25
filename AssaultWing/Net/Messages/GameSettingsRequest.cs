using System;
using System.Collections.Generic;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client, requesting the update of the settings of the game session.
    /// </summary>
    public class GameSettingsRequest : Message
    {
        protected static MessageType messageType = new MessageType(0x30, false);

        /// <summary>
        /// The list of arenas to play.
        /// </summary>
        public IList<string> ArenaPlaylist { get; set; }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            checked
            {
                // Game settings request structure:
                // int: number of arenas in playlist, N
                // N variable-length strings: arena names
                writer.Write((int)ArenaPlaylist.Count);
                foreach (var arenaName in ArenaPlaylist) writer.Write((string)arenaName);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            int count = reader.ReadInt32();
            var playlist = new List<string>(count);
            for (int i = 0; i < count; ++i) playlist.Add(reader.ReadString());
            ArenaPlaylist = playlist;
        }

        public override string ToString()
        {
            return base.ToString() + " [" + ArenaPlaylist.Count + " arenas]";
        }
    }
}
