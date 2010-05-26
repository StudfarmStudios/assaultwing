using System;

namespace AW2.Game
{
    public class ArenaLoadException : Exception
    {
        public ArenaLoadException(string message)
            : base(message)
        {
        }

        public ArenaLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
