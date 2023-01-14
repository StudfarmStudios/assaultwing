using System;

namespace AW2.Net
{
    /// <summary>
    /// An error with a <see cref="AW2.Net.Connections.Connection"/>.
    /// Such errors may occur because of problems with the physical network,
    /// but not because of a programming error.
    /// </summary>
    public class ConnectionException : Exception
    {
        public ConnectionException(string message)
            : base(message)
        {
        }
    }
}
