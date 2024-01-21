using System;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// An entity that supports serialisation for sending over a network
    /// to a remote game instance.
    /// </summary>
    public interface INetworkSerializable
    {
        /// <summary>
        /// Serialises the gob to a binary writer.
        /// </summary>
        /// Subclasses should call the base implementation
        /// before performing their own serialisation.
        /// <param name="writer">The writer where to write the serialised data.</param>
        /// <param name="mode">Which parts of the gob to serialise.</param>
        void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode);

        /// <summary>
        /// Deserialises the gob from a binary writer.
        /// </summary>
        /// Subclasses should call the base implementation
        /// before performing their own deserialisation.
        /// <param name="reader">The reader where to read the serialised data.</param>
        /// <param name="mode">Which parts of the gob to deserialise.</param>
        /// <param name="framesAgo">How long time ago was the data current.</param>
        void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo);
    }
}
