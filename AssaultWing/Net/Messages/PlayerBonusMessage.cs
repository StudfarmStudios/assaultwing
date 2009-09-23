using System;
using AW2.Game;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// of one or more player bonuses.
    /// </summary>
    public class PlayerBonusMessage : GameplayMessage
    {
        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x2b, false);

        /// <summary>
        /// The types of bonuses to notify about.
        /// </summary>
        public PlayerBonusTypes BonusTypes { get; set; }

        /// <summary>
        /// Time of expiry of the bonus or bonuses in game time.
        /// </summary>
        public TimeSpan ExpiryTime { get; set; }

        /// <summary>
        /// The identifier of the player whose bonuses to notify about.
        /// </summary>
        public int PlayerId { get; set; }

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            // Player bonus (request) message structure:
            // int: player ID
            // PlayerBonusTypes as ushort: BonusTypes
            // long: ExpiryTime
            byte[] writeBytes = StreamedData;
            writer.Write((int)PlayerId);
            writer.Write((ushort)BonusTypes);
            writer.Write((long)ExpiryTime.Ticks);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            PlayerId = reader.ReadInt32();
            BonusTypes = (PlayerBonusTypes)reader.ReadUInt16();
            ExpiryTime = TimeSpan.FromTicks(reader.ReadInt64());
        }

        /// <summary>
        /// Returns a String that represents the current Object. 
        /// </summary>
        public override string ToString()
        {
            return base.ToString() + " [Player " + PlayerId + " Bonuses " + BonusTypes + "]";
        }
    }
}
