using System;
using AW2.Helpers.Serialization;

namespace AW2.Net
{
    /// <summary>
    /// Message about the state of an ongoing game.
    /// </summary>
    /// A gameplay message carries a piece of information about the state of
    /// an ongoing game session at a particular point in game time. Some
    /// gameplay messages may become irrelevant as they grow old.
    public abstract class GameplayMessage : StreamMessage
    {
        /// <summary>
        /// The game frame at which the message was current.
        /// </summary>
        public int FrameNumber { get; set; }

        /// <summary>
        /// Creates a gameplay message, initialising its timestamp to the
        /// current time of gameplay.
        /// </summary>
        public GameplayMessage()
        {
            FrameNumber = AssaultWingCore.Instance.DataEngine.ArenaFrameCount;
        }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            writer.Write((int)FrameNumber);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            FrameNumber = reader.ReadInt32();
        }
    }
}
