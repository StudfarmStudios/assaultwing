using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AW2.Graphics.Content
{
    /// <summary>
    /// Reads an XNB file.
    /// See http://blogs.msdn.com/b/shawnhar/archive/2011/07/07/xnb-file-format-documentation.aspx .
    /// </summary>
    public class XNBReader
    {
        public static T Read<T>(string filename)
        {
            using (var reader = new BinaryReader(new FileStream(filename, FileMode.Open)))
            {
                CheckVersion(reader);
                var typeReaders = ReadTypeReaders(reader);
                var sharedResourceCount = reader.Read7BitEncodedInt();
                var obj = (T)TypeReaders.ReadObject(reader, typeReaders);
                var sharedObjects = new object[sharedResourceCount];
                try
                {
                    for (int i = 0; i < sharedResourceCount; i++)
                        sharedObjects[i] = TypeReaders.ReadObject(reader, typeReaders);
                }
                catch (TypeReaders.ReaderMissingException)
                {
                    // Some reader is not implemented. It's probably not needed. Just skip the remaining shared objects.
                }
                TypeReaders.InsertSharedReferences(obj, sharedObjects);
                return obj;
            }
        }

        private static void CheckVersion(BinaryReader reader)
        {
            reader.AssertBytes("Not an XNB file", (byte)'X', (byte)'N', (byte)'B');
            reader.AssertBytes("Not a Windows XNB file", (byte)'w');
            reader.AssertBytes("Not an XNA 4.0 XNB file", 5);
            reader.AssertBytes("Not an unpacked Reach profile XNB file", 0);
            var bytes = reader.ReadUInt32();
            if (bytes != reader.BaseStream.Length) throw new InvalidDataException("Length mismatch (" + bytes + " expected but is " + reader.BaseStream.Length + ")");
        }

        private static List<string> ReadTypeReaders(BinaryReader reader)
        {
            var typeReaders = new List<string>();
            var typeReaderCount = reader.Read7BitEncodedInt();
            for (int i = 0; i < typeReaderCount; i++)
            {
                var typeReaderFullName = reader.ReadString();
                var typeReaderShortName = new Regex(@"\b([a-z0-9_]+)(,|$)", RegexOptions.IgnoreCase).Match(typeReaderFullName).Groups[1].Value;
                var typeReaderVersion = reader.ReadInt32();
                if (typeReaderVersion != 0) throw new InvalidDataException("Unexpected type reader version " + typeReaderVersion);
                typeReaders.Add(typeReaderShortName);
            }
            return typeReaders;
        }
    }
}
