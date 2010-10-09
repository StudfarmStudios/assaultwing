namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// of the creation of a gob while gameplay is in progress.
    /// </summary>
    public class GobCreationMessage : GobCreationMessageBase
    {
        protected static MessageType messageType = new MessageType(0x23, false);
    }
}
