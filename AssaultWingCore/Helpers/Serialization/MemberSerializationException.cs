using System;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Exception during (de)serialisation of a member of a type.
    /// </summary>
    public class MemberSerializationException : Exception
    {
        /// <summary>
        /// The name of the member on which error occurred.
        /// </summary>
        public string MemberName { get; set; }

        /// <summary>
        /// Line number in XML file where error occurred.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Creates a new member serialisation exception.
        /// </summary>
        /// <param name="message">Description of the error.</param>
        /// <param name="memberName">Name of the member on which error occurred.</param>
        public MemberSerializationException(string message, string memberName)
            : base(message)
        {
            MemberName = memberName;
        }

        /// <summary>
        /// Creates a new member serialisation exception.
        /// </summary>
        /// <param name="message">Description of the error.</param>
        /// <param name="memberName">Name of the member on which error occurred.</param>
        /// <param name="innerException">A deeper reason for this exception.</param>
        public MemberSerializationException(string message, string memberName, Exception innerException)
            : base(message, innerException)
        {
            MemberName = memberName;
        }
    }
}
