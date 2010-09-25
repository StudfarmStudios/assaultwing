using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using AW2.Helpers.Serialization;

namespace AW2.Game
{
    /// <summary>
    /// Container of indexed chunks of serialized data.
    /// </summary>
    public class ArenaBin
    {
        private static readonly byte[] HEADER_BYTES = System.Text.Encoding.ASCII.GetBytes("AWBIN100");
        private Dictionary<int, byte[]> _data;

        public Stream this[int index] { get { return new MemoryStream(_data[index], false); } }

        public ArenaBin()
        {
            _data = new Dictionary<int, byte[]>();
        }

        public void Clear()
        {
            _data.Clear();
        }

        public void Add(int index, IAWSerializable value)
        {
            var buffer = new MemoryStream();
            var bufferWriter = new BinaryWriter(buffer);
            value.Serialize(bufferWriter);
            bufferWriter.Close();
            _data.Add(index, buffer.ToArray());
        }

        public void Save(string path)
        {
            // Create file and write header without compression
            var file = File.Open(path, FileMode.Create);
            var rawWriter = new BinaryWriter(file);
            rawWriter.Write(HEADER_BYTES);
            rawWriter.Close();

            // Append contents to the file with compression
            file = File.Open(path, FileMode.Append);
            var packWriter = new BinaryWriter(new DeflateStream(file, CompressionMode.Compress));
            packWriter.Write((int)_data.Count);
            foreach (var pair in _data)
            {
                packWriter.Write((int)pair.Key);
                packWriter.Write((int)pair.Value.Length);
                packWriter.Write((byte[])pair.Value);
            }
            packWriter.Close();
        }

        public void Load(string path)
        {
            Clear();
            try
            {
                var file = File.OpenRead(path);

                // Check header
                var rawReader = new BinaryReader(file);
                var header = rawReader.ReadBytes(HEADER_BYTES.Length);
                if (!header.SequenceEqual(HEADER_BYTES)) throw new ArenaLoadException("Arena bin has invalid header (" + path + ")");

                // Load contents
                var packReader = new BinaryReader(new DeflateStream(file, CompressionMode.Decompress));
                int elementCount = packReader.ReadInt32();
                for (int i = 0; i < elementCount; i++)
                {
                    int index = packReader.ReadInt32();
                    int length = packReader.ReadInt32();
                    var data = packReader.ReadBytes(length);
                    _data.Add(index, data);
                }

                file.Close();
            }
            catch (Exception e)
            {
                if (e is ArenaLoadException) throw;
                throw new ArenaLoadException("An error occurred while loading arena bin (" + path + ")", e);
            }
        }
    }
}
