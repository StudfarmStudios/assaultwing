using System;
using System.Collections.Generic;
using AW2.Game;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// of the creation of a gob while gameplay is in progress.
    /// </summary>
    [MessageType(0x23, false)]
    public class GobCreationMessage : GameplayMessage
    {
        /// <summary>
        /// Type names of the gobs to create.
        /// </summary>
        private List<CanonicalString> _gobTypeNames = new List<CanonicalString>();

        /// <summary>
        /// Indices of the arena layers the gobs live in.
        /// </summary>
        private List<int> _layerIndices = new List<int>();

        public byte ArenaID { get; set; }
        public int GobCount { get { return _gobTypeNames.Count; } }

        public void AddGob(Gob gob)
        {
            _gobTypeNames.Add(gob.TypeName);
            _layerIndices.Add(gob.Arena.Layers.IndexOf(gob.Layer));
            Write(gob, SerializationModeFlags.All);
        }

        public void ReadGobs(int framesAgo, Func<CanonicalString, int, Gob> createGob, Action<Gob> initGob)
        {
            for (int i = 0; i < _gobTypeNames.Count; ++i)
            {
                var gob = createGob(_gobTypeNames[i], _layerIndices[i]);
                if (gob == null) continue;
                Read(gob, SerializationModeFlags.All, framesAgo);
                initGob(gob);
            }
        }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                base.SerializeBody(writer);
                checked
                {
                    // Gob creation (request) message structure:
                    // byte: arena identifier
                    // short: number of gobs to create, N
                    // N bytes: arena layer indices
                    // N ints: canonical forms of gob type names
                    // ushort: byte count of all gob data, K
                    // K bytes: serialised data of all gobs
                    var writeBytes = StreamedData;
                    if (_gobTypeNames.Count != _layerIndices.Count) throw new MessageException("_gobTypeNames.Count != _layerIndices.Count");
                    writer.Write((byte)ArenaID);
                    writer.Write((short)_gobTypeNames.Count);
                    foreach (byte layerIndex in _layerIndices) writer.Write((byte)layerIndex);
                    foreach (var typeName in _gobTypeNames) writer.Write((CanonicalString)typeName);
                    writer.Write((ushort)writeBytes.Length);
                    writer.Write(writeBytes, 0, writeBytes.Length);
                }
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            ArenaID = reader.ReadByte();
            int gobCount = reader.ReadInt16();
            _layerIndices = new List<int>(gobCount);
            _gobTypeNames = new List<CanonicalString>(gobCount);
            for (int i = 0; i < gobCount; ++i) _layerIndices.Add(reader.ReadByte());
            for (int i = 0; i < gobCount; ++i) _gobTypeNames.Add(reader.ReadCanonicalString());
            int byteCount = reader.ReadUInt16();
            StreamedData = reader.ReadBytes(byteCount);
        }

        public override string ToString()
        {
            return base.ToString() + " [" + _gobTypeNames.Count + " gobs, " + StreamedData.Length + " bytes]";
        }
    }
}
