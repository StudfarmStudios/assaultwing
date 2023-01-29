using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// that playing has finished for the current arena.
    /// </summary>
    [MessageType(0x27, false)]
    public class ArenaFinishMessage : GameplayMessage
    {
        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
            base.SerializeBody(writer);
            // Arena finish (request) message structure:
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
