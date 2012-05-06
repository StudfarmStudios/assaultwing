using System;
using System.Collections.Generic;
using AW2.Game;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game instance to another updating the
    /// <see cref="SerializationModeFlags.VaryingDataFromServer"/> or
    /// <see cref="SerializationModeFlags.VaryingDataFromClient"/> state of a gob.
    /// </summary>
    [MessageType(0x24, false)]
    public class GobUpdateMessage : GameplayMessage
    {
        private List<int> _gobIds = new List<int>();

        public override MessageSendType SendType { get { return MessageSendType.UDP; } }
        public List<Arena.CollisionEvent> CollisionEvents { get; set; }

        public void AddGob(int gobId, INetworkSerializable gob, SerializationModeFlags serializationMode)
        {
            // Note: Data in GobUpdateMessage may get lost on client because of this case:
            // Client doesn't have the gob at the moment the update message is received.
            // Because the gob doesn't exist, the client doesn't know the type of the gob and
            // therefore has no way of knowing how many bytes in the message are for that one gob.
            // The only solution is to skip the remaining message, losing many gob updates.
            // This is why GobUpdateMessage sends only varying data.
            _gobIds.Add(gobId);
            Write(gob, serializationMode);
        }

        /// <summary>
        /// Reads gob contents from the update message.
        /// </summary>
        /// <param name="gobFinder">A method returning a gob for its identifier.</param>
        /// <param name="framesAgo">How long time ago was the message current.</param>
        public void ReadGobs(Func<int, INetworkSerializable> gobFinder, int framesAgo, SerializationModeFlags serializationMode)
        {
            var gobTypes = new System.Text.StringBuilder(); // DEBUG for a rare EndOfStreamException
            try
            {
                for (int i = 0; i < _gobIds.Count; ++i)
                {
                    var gob = gobFinder(_gobIds[i]);
                    if (gob == null) break;
                    gobTypes.Append(gob.GetType().Name);
                    if (gob is Gob) gobTypes.AppendFormat(" [{0}]", ((Gob)gob).TypeName);
                    gobTypes.Append(", ");
                    Read(gob, serializationMode, framesAgo);
                }
            }
            catch (Exception)
            {
                AW2.Helpers.Log.Write("Exception during GobUpdateMessage.ReadGobs. Gob types were " + gobTypes);
                throw;
            }
        }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(null))
#endif
            {
                base.SerializeBody(writer);
                // Gob update (request) message structure:
                // byte: number of collision events, N
                // repeat N times: collision event
                // byte: number of gobs to update, K
                // K shorts: identifiers of the gobs
                // ushort: total byte count of gob data
                // repeat K times:
                //   ??? bytes: serialised data of a gob (content known only by the Gob subclass in question)
                byte[] writeBytes = StreamedData;
#if NETWORK_PROFILING
                using (new NetworkProfilingScope("GobUpdateMessageHeader"))
#endif
                    checked
                    {
                        writer.Write((byte)CollisionEvents.Count);
                        foreach (var collisionEvent in CollisionEvents) collisionEvent.Serialize(writer);
                        writer.Write((byte)_gobIds.Count);
                        foreach (var gobId in _gobIds)
                            writer.Write((short)gobId);
                        writer.Write((ushort)writeBytes.Length);
                    }

                writer.Write(writeBytes, 0, writeBytes.Length);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            var collisionEventCount = reader.ReadByte();
            CollisionEvents = new List<Arena.CollisionEvent>(collisionEventCount);
            for (int i = 0; i < collisionEventCount; i++) CollisionEvents.Add(Arena.CollisionEvent.Deserialize(reader));
            var gobCount = reader.ReadByte();
            _gobIds.Clear();
            for (int i = 0; i < gobCount; i++)
                _gobIds.Add(reader.ReadInt16());
            var totalByteCount = reader.ReadUInt16();
            StreamedData = reader.ReadBytes(totalByteCount);
        }

        public override string ToString()
        {
            return base.ToString() + " [" + _gobIds.Count + " gobs]";
        }
    }
}
