using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game;
using AW2.Game.Collisions;
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
        private enum StateType
        {
            /// <summary>
            /// Nothing has been done yet. The message is either uninitialized or just deserialized.
            /// </summary>
            Initial,

            /// <summary>
            /// Some gobs have been added and more gobs may be added.
            /// </summary>
            AddingGobs,

            /// <summary>
            /// CollisionEvents have been set. The message can be serialized.
            /// </summary>
            CollisionEventsSet,

            /// <summary>
            /// Gobs have been read. No more gobs can be read but collision events may be read.
            /// </summary>
            GobsRead,

            /// <summary>
            /// Both gobs and collision events have been read. Nothing may be done any more.
            /// </summary>
            CollisionEventsRead,
        }

        private StateType _state = StateType.Initial;
        private int _collisionEventCount;
        private List<CollisionEvent.SerializationData> _collisionEventInitDatas = new List<CollisionEvent.SerializationData>();
        private List<int> _gobIds = new List<int>();

        public bool HasContent { get { return _collisionEventCount > 0 || _gobIds.Any(); } }
        public override MessageSendType SendType { get { return MessageSendType.UDP; } }

        public void SetCollisionEvents(List<CollisionEvent> collisionEvents, SerializationModeFlags serializationMode)
        {
            if (_state != StateType.Initial && _state != StateType.AddingGobs) throw new InvalidOperationException("Cannot set collision events in state " + _state);
            _state = StateType.CollisionEventsSet;
            _collisionEventCount = collisionEvents.Count;
            foreach (var collisionEvent in collisionEvents) Write(collisionEvent.GetSerializationData(), serializationMode);
        }

        public IEnumerable<CollisionEvent> ReadCollisionEvents(Func<int, Gob> gobFinder, SerializationModeFlags serializationMode, int framesAgo)
        {
            if (_state != StateType.GobsRead) throw new InvalidOperationException("Cannot read collision events in state " + _state);
            _state = StateType.CollisionEventsRead;
            for (int i = 0; i < _collisionEventCount; i++)
            {
                var collisionEventData = new CollisionEvent.SerializationData();
                Read(collisionEventData, serializationMode, framesAgo);
                yield return new CollisionEvent(collisionEventData, gobFinder);
            }
        }

        public void AddGob(int gobId, INetworkSerializable gob, SerializationModeFlags serializationMode)
        {
            // Note: Data in GobUpdateMessage may get lost on client because of this case:
            // Client doesn't have the gob at the moment the update message is received.
            // Because the gob doesn't exist, the client doesn't know the type of the gob and
            // therefore has no way of knowing how many bytes in the message are for that one gob.
            // The only solution is to skip the remaining message, losing many gob updates.
            // This is why GobUpdateMessage sends only varying data.
            if (_state != StateType.Initial && _state != StateType.AddingGobs) throw new InvalidOperationException("Cannot add a gob in state " + _state);
            _state = StateType.AddingGobs;
            _gobIds.Add(gobId);
            Write(gob, serializationMode);
        }

        /// <summary>
        /// Reads gob contents from the update message.
        /// </summary>
        public void ReadGobs(Func<int, INetworkSerializable> gobFinder, SerializationModeFlags serializationMode, int framesAgo)
        {
            if (_state != StateType.Initial) throw new InvalidOperationException("Cannot read gobs in state " + _state);
            _state = StateType.GobsRead;
            for (int i = 0; i < _gobIds.Count; ++i)
            {
                var gob = gobFinder(_gobIds[i]);
                if (gob == null) break;
                Read(gob, serializationMode, framesAgo);
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
                // byte: number of gobs to update, K
                // byte: number of collision events, N
                // ushort: total byte count of gob data and collision event data
                // K shorts: identifiers of the gobs
                // repeat K times:
                //   ??? bytes: serialised data of a gob (content known only by the Gob subclass in question)
                // repeat N times:
                //   ??? bytes: collision event
                byte[] writeBytes = StreamedData;
#if NETWORK_PROFILING
                using (new NetworkProfilingScope("GobUpdateMessageHeader"))
#endif
                checked
                {
                    writer.Write((byte)_gobIds.Count);
                    writer.Write((byte)_collisionEventCount);
                    writer.Write((ushort)writeBytes.Length);
                    foreach (var gobId in _gobIds) writer.Write((short)gobId);
                }
                writer.Write(writeBytes, 0, writeBytes.Length);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            var gobCount = reader.ReadByte();
            _collisionEventCount = reader.ReadByte();
            var totalByteCount = reader.ReadUInt16();
            _gobIds.Clear();
            for (int i = 0; i < gobCount; i++) _gobIds.Add(reader.ReadInt16());
            StreamedData = reader.ReadBytes(totalByteCount);
        }

        public override string ToString()
        {
            return base.ToString() + " [" + _gobIds.Count + " gobs]";
        }
    }
}
