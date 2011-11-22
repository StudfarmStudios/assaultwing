using System;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Specifies which parts of an entity to serialise over a network.
    /// </summary>
    [Flags]
    public enum SerializationModeFlags
    {
        /// <summary>
        /// Serialise nothing.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Serialise data that is constant after initialisation.
        /// </summary>
        ConstantDataFromServer = 0x01,

        /// <summary>
        /// Serialise data that varies even after initialisation.
        /// </summary>
        VaryingDataFromServer = 0x02,

        /// <summary>
        /// Serialise all data.
        /// </summary>
        AllFromServer = ConstantDataFromServer | VaryingDataFromServer,
    }
}
