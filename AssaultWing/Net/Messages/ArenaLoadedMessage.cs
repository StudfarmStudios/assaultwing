using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game client to the game server acknowledging
    /// that the arena that is to be played next has been loaded and the
    /// game client is ready to start gameplay on the game server's mark.
    /// </summary>
    public class ArenaLoadedMessage : GameplayMessage
    {
        protected static MessageType messageType = new MessageType(0x33, false);

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            // Arena loaded message structure:
            // empty
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
