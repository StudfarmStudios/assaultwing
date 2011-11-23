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
        /// Serialise data that is constant after initialisation. Serialisation is done from server to client.
        /// </summary>
        ConstantDataFromServer = 0x01,

        /// <summary>
        /// Serialise data that varies even after initialisation. Serialisation is done from server to client.
        /// </summary>
        VaryingDataFromServer = 0x02,

        /// <summary>
        /// Serialise data that varies even after initialisation. Serialisation is done from client to server.
        /// </summary>
        VaryingDataFromClient = 0x04,

        /// <summary>
        /// Serialise all data from server to client.
        /// </summary>
        AllFromServer = ConstantDataFromServer | VaryingDataFromServer,
    }
}
