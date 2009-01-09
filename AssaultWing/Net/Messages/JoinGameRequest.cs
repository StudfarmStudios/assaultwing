using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AW2.Net.Messages
{
    /// <summary>
    /// Information about a player who wants to join a game.
    /// </summary>
    public struct PlayerInfo
    {
        /// <summary>
        /// The player's identifier on the game instance he lives on.
        /// </summary>
        public int id;

        /// <summary>
        /// The player's name.
        /// </summary>
        public string name;

        /// <summary>
        /// The player's ship type.
        /// </summary>
        public string shipTypeName;

        /// <summary>
        /// The player's primary weapon type.
        /// </summary>
        public string weapon1TypeName;

        /// <summary>
        /// The player's secondary weapon type.
        /// </summary>
        public string weapon2TypeName;

        /// <summary>
        /// Creates a new player info based on a player.
        /// </summary>
        /// <param name="player">The player.</param>
        public PlayerInfo(AW2.Game.Player player)
        {
            id = player.Id;
            name = player.Name;
            shipTypeName = player.ShipName;
            weapon1TypeName = player.Weapon1Name;
            weapon2TypeName = player.Weapon2Name;
        }
    }

    /// <summary>
    /// A message from a game client to a game server requesting
    /// joining to the game the server is hosting.
    /// </summary>
    public class JoinGameRequest : Message
    {
        /// <summary>
        /// Information about the players that want to join the game.
        /// </summary>
        public List<PlayerInfo> PlayerInfos { get; set; }

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x20, false);

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// <param name="writer">Writer of serialised data.</param>
        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
            // Join game request message structure:
            // byte number of players N
            // repeat N
            //   int player ID
            //   32-byte-string player name
            //   32-byte-string player ship type
            //   32-byte-string player weapon1 type
            //   32-byte-string player weapon2 type
            writer.Write((byte)PlayerInfos.Count);
            foreach (PlayerInfo info in PlayerInfos)
            {
                writer.Write((int)info.id);
                writer.Write((string)info.name, 32, false);
                writer.Write((string)info.shipTypeName, 32, false);
                writer.Write((string)info.weapon1TypeName, 32, false);
                writer.Write((string)info.weapon2TypeName, 32, false);
            }
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            int count = reader.ReadByte();
            PlayerInfos = new List<PlayerInfo>(count);
            for (int i = 0; i < count; ++i)
            {
                PlayerInfo info;
                info.id = reader.ReadInt32();
                info.name = reader.ReadString(32);
                info.shipTypeName = reader.ReadString(32);
                info.weapon1TypeName = reader.ReadString(32);
                info.weapon2TypeName = reader.ReadString(32);
                PlayerInfos.Add(info);
            }
        }
    }
}
