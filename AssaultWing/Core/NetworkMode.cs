namespace AW2.Core
{
    /// <summary>
    /// Mode of network operation.
    /// </summary>
    public enum NetworkMode
    {
        /// <summary>
        /// Acting as a standalone game session, no networking involved.
        /// </summary>
        Standalone,

        /// <summary>
        /// Acting as a client in a game session.
        /// </summary>
        Client,

        /// <summary>
        /// Acting as a server in a game session.
        /// </summary>
        Server,
    }
}
