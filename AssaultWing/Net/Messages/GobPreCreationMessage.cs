namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// of the creation of a gob before gameplay in the arena has started.
    /// </summary>
    [MessageType(0x32, false)]
    public class GobPreCreationMessage : GobCreationMessageBase
    {
    }
}
