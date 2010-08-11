using System;

namespace AW2.Net
{
    /// <summary>
    /// An error in Assault Wing networking, which is likely the result of a programming error.
    /// </summary>
    public class NetworkException : Exception
    {
        public NetworkException(string message)
            : base(message)
        {
        }
    }
}
