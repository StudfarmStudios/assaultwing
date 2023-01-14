using System;

namespace AW2.Net
{
    /// <summary>
    /// Exception denoting an error related to messages.
    /// </summary>
    /// <seealso cref="Message"/>
    public class MessageException : Exception
    {
        /// <param name="explanation">Explanation of the occurred error.</param>
        public MessageException(string explanation)
            : base(explanation)
        {
        }

        /// <param name="explanation">Explanation of the occurred error.</param>
        /// <param name="innerException">Inner exception</param>
        public MessageException(string explanation, Exception innerException)
            : base(explanation, innerException)
        {
        }
    }
}
