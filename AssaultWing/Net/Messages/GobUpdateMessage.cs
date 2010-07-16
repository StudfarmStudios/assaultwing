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
        private List<int> _gobIds = new List<int>();
        private List<ushort> _byteCounts = new List<ushort>();
        private MessageSendType _sendType = MessageSendType.UDP;

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x24, false);

        public override MessageSendType SendType { get { return MessageSendType.TCP; /* UNDONE !!! _sendType */; } }

        /// <summary>
        /// Adds a gob to the update message.
        /// </summary>
        /// <param name="gobId">Identifier of the gob.</param>
        /// <param name="gob">The gob.</param>
        /// <param name="mode">What to serialise of the gob.</param>
        public void AddGob(int gobId, INetworkSerializable gob, SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0) _sendType = MessageSendType.TCP;
            _gobIds.Add(gobId);
            ushort byteCount = checked((ushort)Write(gob, mode));
            _byteCounts.Add(byteCount);
        }

        /// <summary>
        /// Reads gob contents from the update message.
        /// </summary>
        /// <param name="gobFinder">A method returning a gob for its identifier.
        /// If it returns <c>null</c>, the corresponding serialised data is
        /// skipped.</param>
        /// <param name="mode">What to deserialise of the gobs.</param>
        /// <param name="messageAge">How long time ago was the message current.</param>
        public void ReadGobs(Func<int, INetworkSerializable> gobFinder, SerializationModeFlags mode, TimeSpan messageAge)
        {
            for (int i = 0; i < _gobIds.Count; ++i)
            {
                var gob = gobFinder(_gobIds[i]);
                if (gob != null)
                    Read(gob, mode, messageAge);
                else
                    Skip(_byteCounts[i]);
            }
        }

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
            writer.Write((int)_gobIds.Count);
            foreach (int gobId in _gobIds)
                writer.Write((int)gobId);
            foreach (ushort byteCount in _byteCounts)
                writer.Write((ushort)byteCount);
            writer.Write(writeBytes, 0, writeBytes.Length);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            int gobCount = reader.ReadInt32();
            _gobIds.Clear();
            _byteCounts.Clear();
            for (int i = 0; i < gobCount; ++i)
                _gobIds.Add(reader.ReadInt32());
            int totalByteCount = 0;
            for (int i = 0; i < gobCount; ++i)
            {
                ushort byteCount = reader.ReadUInt16();
                _byteCounts.Add(byteCount);
                totalByteCount += byteCount;
            }
            StreamedData = reader.ReadBytes(totalByteCount);
        }

        public override string ToString()
        {
            return base.ToString() + " [" + _gobIds.Count + " gobs]";
        }
    }
}
