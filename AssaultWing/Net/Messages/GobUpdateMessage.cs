using System;
using System.Collections.Generic;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client updating
    /// the state of a gob.
    /// </summary>
    public class GobUpdateMessage : GameplayMessage
    {
        List<int> gobIds = new List<int>();
        List<ushort> byteCounts = new List<ushort>();

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x24, false);

        /// <summary>
        /// Adds a gob to the update message.
        /// </summary>
        /// <param name="gobId">Identifier of the gob.</param>
        /// <param name="gob">The gob.</param>
        /// <param name="mode">What to serialise of the gob.</param>
        public void AddGob(int gobId, INetworkSerializable gob, SerializationModeFlags mode)
        {
            gobIds.Add(gobId);
            ushort byteCount = checked((ushort)Write(gob, mode));
            byteCounts.Add(byteCount);
        }

        /// <summary>
        /// Reads gob contents from the update message.
        /// </summary>
        /// <param name="gobFinder">A method returning a gob for its identifier.
        /// If it returns <c>null</c>, the corresponding serialised data is
        /// skipped.</param>
        /// <param name="mode">What to deserialise of the gobs.</param>
        public void ReadGobs(Func<int, INetworkSerializable> gobFinder, SerializationModeFlags mode)
        {
            for (int i = 0; i < gobIds.Count; ++i)
            {
                INetworkSerializable gob = gobFinder(gobIds[i]);
                if (gob != null)
                    Read(gob, mode);
                else
                    Skip(byteCounts[i]);
            }
        }

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// <param name="writer">Writer of serialised data.</param>
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            // Gob update (request) message structure:
            // int: number of gobs to update, K
            // K ints: identifiers of the gobs
            // K ushorts: byte count of gob data, M(k)
            // repeat K times:
            //   M(k) bytes: serialised data of a gob (content known only by the Gob subclass in question)
            byte[] writeBytes = StreamedData;
            writer.Write((int)gobIds.Count);
            foreach (int gobId in gobIds)
                writer.Write((int)gobId);
            foreach (ushort byteCount in byteCounts)
                writer.Write((ushort)byteCount);
            writer.Write(writeBytes, 0, writeBytes.Length);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            int gobCount = reader.ReadInt32();
            gobIds.Clear();
            byteCounts.Clear();
            for (int i = 0; i < gobCount; ++i)
                gobIds.Add(reader.ReadInt32());
            int totalByteCount = 0;
            for (int i = 0; i < gobCount; ++i)
            {
                ushort byteCount = reader.ReadUInt16();
                byteCounts.Add(byteCount);
                totalByteCount += byteCount;
            }
            StreamedData = reader.ReadBytes(totalByteCount);
        }

        /// <summary>
        /// Returns a String that represents the current Object. 
        /// </summary>
        public override string ToString()
        {
            return base.ToString() + " [" + gobIds.Count + " gobs]";
        }
    }
}
