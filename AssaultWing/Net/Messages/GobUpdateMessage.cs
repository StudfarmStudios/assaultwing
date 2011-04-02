using System;
using System.Collections.Generic;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client updating
    /// the <see cref="SerializationModeFlags.VaryingData"/> state of a gob.
    /// </summary>
    [MessageType(0x24, false)]
    public class GobUpdateMessage : GameplayMessage
    {
        private List<int> _gobIds = new List<int>();

        public override MessageSendType SendType { get { return MessageSendType.UDP; } }

        public void AddGob(int gobId, INetworkSerializable gob)
        {
            // Note: Data in GobUpdateMessage may get lost on client because of this case:
            // Client doesn't have the gob at the moment the update message is received.
            // Because the gob doesn't exist, the client doesn't know the type of the gob and
            // therefore has no way of knowing how many bytes in the message are for that one gob.
            // The only solution is to skip the remaining message, losing many gob updates.
            // This is why GobUpdateMessage does not send SerializationModeFlags.ConstantData.
            _gobIds.Add(gobId);
            Write(gob, SerializationModeFlags.VaryingData);
        }

        /// <summary>
        /// Reads gob contents from the update message.
        /// </summary>
        /// <param name="gobFinder">A method returning a gob for its identifier.</param>
        /// <param name="framesAgo">How long time ago was the message current.</param>
        public void ReadGobs(Func<int, INetworkSerializable> gobFinder, int framesAgo)
        {
            for (int i = 0; i < _gobIds.Count; ++i)
            {
                var gob = gobFinder(_gobIds[i]);
                if (gob == null) break;
                Read(gob, SerializationModeFlags.VaryingData, framesAgo);
            }
        }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
            base.SerializeBody(writer);
            // Gob update (request) message structure:
            // int: number of gobs to update, K
            // K ints: identifiers of the gobs
            // ushort: total byte count of gob data
            // repeat K times:
            //   ??? bytes: serialised data of a gob (content known only by the Gob subclass in question)
            byte[] writeBytes = StreamedData;
#if NETWORK_PROFILING
            using (new NetworkProfilingScope("GobUpdateMessageHeader"))
#endif
                checked
                {
                    writer.Write((int)_gobIds.Count);
                    foreach (int gobId in _gobIds)
                        writer.Write((int)gobId);
                    writer.Write((ushort)writeBytes.Length);
                }
            writer.Write(writeBytes, 0, writeBytes.Length);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            int gobCount = reader.ReadInt32();
            _gobIds.Clear();
            for (int i = 0; i < gobCount; ++i)
                _gobIds.Add(reader.ReadInt32());
            var totalByteCount = reader.ReadUInt16();
            StreamedData = reader.ReadBytes(totalByteCount);
        }

        public override string ToString()
        {
            return base.ToString() + " [" + _gobIds.Count + " gobs]";
        }
    }
}
