using System;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// A type whose instances are consistent only under certain conditions
    /// that concern some fields of the instance.
    /// </summary>
    /// Implement this interface for types whose fields must be checked and
    /// possibly corrected after an operation that changes the field values.
    public interface IConsistencyCheckable
    {
        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <seealso cref="Serialization"/>
        void MakeConsistent(Type limitationAttribute);
    }
}
