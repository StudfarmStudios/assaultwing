namespace AW2.Net
{
    /// <summary>
    /// Flags for message headers.
    /// </summary>
    public enum MessageHeaderFlags : byte
    {
        /// <summary>
        /// The message is a reply to a previous message.
        /// </summary>
        Reply = 0x80,

        /// <summary>
        /// The message is to be sent to several recipients.
        /// </summary>
        Multicast = 0x40,
    }
}
