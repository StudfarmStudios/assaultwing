namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// of the creation of a gob before gameplay in the arena has started.
    /// </summary>
    public class GobPreCreationMessage : GobCreationMessageBase
    {
        protected static MessageType messageType = new MessageType(0x32, false);
    }
}
