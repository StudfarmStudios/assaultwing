using System;
using System.Collections.Generic;
using AW2.Game;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client updating
    /// the <see cref="SerializationModeFlags.VaryingDataFromServer"/> state of a gob.
    /// </summary>
    [MessageType(0x24, false)]
    public class GobUpdateMessage : GameplayMessage
    {
        private List<int> _gobIds = new List<int>();

        public override MessageSendType SendType { get { return MessageSendType.UDP; } }
        public List<Arena.CollisionEvent> CollisionEvents { get; set; }

        public void AddGob(int gobId, INetworkSerializable gob)
        {
            // Note: Data in GobUpdateMessage may get lost on client because of this case:
            // Client doesn't have the gob at the moment the update message is received.
            // Because the gob doesn't exist, the client doesn't know the type of the gob and
            // therefore has no way of knowing how many bytes in the message are for that one gob.
            // The only solution is to skip the remaining message, losing many gob updates.
            // This is why GobUpdateMessage does not send SerializationModeFlags.ConstantData.
            _gobIds.Add(gobId);
            Write(gob, SerializationModeFlags.VaryingDataFromServer);
        }

        /// <summary>
        /// Reads gob contents from the update message.
        /// </summary>
        /// <param name="gobFinder">A method returning a gob for its identifier.</param>
        /// <param name="framesAgo">How long time ago was the message current.</param>
        public void ReadGobs(Func<int, INetworkSerializable> gobFinder, int framesAgo)
        {
            var gobTypes = new System.Text.StringBuilder(); // debugging a rare EndOfStreamException
            try
            {
                for (int i = 0; i < _gobIds.Count; ++i)
                {
                    var gob = gobFinder(_gobIds[i]);
                    if (gob == null) break;
                    gobTypes.Append(gob.GetType().Name);
                    if (gob is Gob) gobTypes.AppendFormat(" [{0}]", ((Gob)gob).TypeName);
                    gobTypes.Append(", ");
                    Read(gob, SerializationModeFlags.VaryingDataFromServer, framesAgo);
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
                // repeat N times:
                //   short: gob 1 ID
                //   short: gob 2 ID
                //   byte: mixed data
                //     bits 0..1: area 1 ID
                //     bits 2..3: area 2 ID
                //     bits 4..5: physical collision sound effects to play (Arena.CollisionSoundTypes)
                //     bit 6: collision while stuck
                //     bit 7: collide both ways (if not, then only gob 1 to gob 2)
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
                        foreach (var collisionEvent in CollisionEvents)
                        {
                            writer.Write((short)collisionEvent.Gob1ID);
                            writer.Write((short)collisionEvent.Gob2ID);
                            if (collisionEvent.Area1ID > 0x03 || collisionEvent.Area2ID > 0x03)
                                throw new ApplicationException("Too large collision area identifier: " + collisionEvent.Area1ID + " or " + collisionEvent.Area2ID);
                            var mixedData = (byte)(collisionEvent.Area1ID & 0x03);
                            mixedData |= (byte)((collisionEvent.Area2ID & 0x03) << 2);
                            mixedData |= (byte)(((byte)collisionEvent.Sound & 0x03) << 4);
                            if (collisionEvent.Stuck) mixedData |= 0x40;
                            if (collisionEvent.CollideBothWays) mixedData |= 0x80;
                            writer.Write((byte)mixedData);
                        }
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
            for (int i = 0; i < collisionEventCount; i++)
            {
                var gob1ID = reader.ReadInt16();
                var gob2ID = reader.ReadInt16();
                var mixedData = reader.ReadByte();
                var area1ID = mixedData & 0x03;
                var area2ID = (mixedData >> 2) & 0x03;
                var soundsToPlay = (Arena.CollisionSoundTypes)((mixedData >> 4) & 0x03);
                var stuck = (mixedData & 0x40) != 0;
                var collideBothWays = (mixedData & 0x80) != 0;
                CollisionEvents.Add(new Arena.CollisionEvent(gob1ID, gob2ID, area1ID, area2ID, stuck, collideBothWays, soundsToPlay));
            }
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
