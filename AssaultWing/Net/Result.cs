using System;
using System.Collections.Generic;
using System.Text;

namespace AW2.Net
{
    /// <summary>
    /// The result of an operation whose return value is of a type.
    /// Each result carries the identifier of the operation for identification purposes.
    /// </summary>
    /// <typeparam name="T">The type of the return value of the operation.</typeparam>
    public class Result<T>
    {
        T value;
        string id;
        Exception error;

        /// <summary>
        /// Was the operation successful.
        /// </summary>
        public bool Successful { get { return error == null; } }

        /// <summary>
        /// The return value of the operation. The default value if the operation failed.
        /// </summary>
        public T Value { get { return value; } }

        /// <summary>
        /// The identifier of the operation.
        /// </summary>
        public string Id { get { return id; } }

        /// <summary>
        /// Error information on the operation. <c>null</c> if the operation succeeded.
        /// </summary>
        public Exception Error { get { return error; } }

        /// <summary>
        /// Creates a result of a successful operation.
        /// </summary>
        /// <param name="value">The return value of the operation.</param>
        /// <param name="id">Identifier of the operation.</param>
        public Result(T value, string id)
        {
            this.value = value;
            this.id = id;
            this.error = null;
        }

        /// <summary>
        /// Creates a result of a failed operation.
        /// </summary>
        /// <param name="error">Information on the error.</param>
        /// <param name="id">Identifier of the operation.</param>
        public Result(Exception error, string id)
        {
            this.value = default(T);
            this.id = id;
            this.error = error;
        }
    }
}
