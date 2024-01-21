using System;
using System.Text;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework;
using AW2.Game.Players;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Reads primitive types in binary from a network stream 
    /// and supports reading strings in a specific encoding.
    /// Takes care of byte order.
    /// </summary>
    public class NetworkBinaryReader : BinaryReader
    {
        public NetworkBinaryReader(Stream input)
            : base(input, Encoding.UTF8)
        {
        }

        public override ushort ReadUInt16()
        {
            return unchecked((ushort)IPAddress.NetworkToHostOrder(base.ReadInt16()));
        }

        public override int ReadInt32()
        {
            return IPAddress.NetworkToHostOrder(base.ReadInt32());
        }

        public override float ReadSingle()
        {
            return Converter.IntToFloat(ReadInt32());
        }

        public float ReadAngle8()
        {
            return ReadByte() * MathHelper.TwoPi / (byte.MaxValue + 1);
        }

        public byte? ReadNullableByte()
        {
            return ReadBoolean() ? ReadByte() : (byte?)null;
        }

        /// <summary>
        /// Reads a 16-bit floating point value.
        /// </summary>
        public Half ReadHalf()
        {
            short bits = ReadInt16();
            return Converter.ShortToHalf(bits);
        }

        /// <summary>
        /// Reads a length-prefixed string.
        /// </summary>
        public override string ReadString()
        {
            int length = ReadUInt16();
            var chars = ReadChars(length);
            return new string(chars);
        }

        public CanonicalString ReadCanonicalString()
        {
            return (CanonicalString)ReadInt16();
        }

        public Vector2 ReadVector2()
        {
            return new Vector2
            {
                X = ReadSingle(),
                Y = ReadSingle()
            };
        }

        public Vector3 ReadVector3()
        {
            return new Vector3
            {
                X = ReadSingle(),
                Y = ReadSingle(),
                Z = ReadSingle()
            };
        }

        public TimeSpan ReadTimeSpan()
        {
            return new TimeSpan(ReadInt64());
        }

        public Color ReadColor()
        {
            return new Color { PackedValue = ReadUInt32() };
        }

        public LazyProxy<int, Team> ReadTeamID(Func<int, Team> findTeam)
        {
            int teamID = ReadSByte();
            var proxy = new LazyProxy<int, Team>(findTeam);
            proxy.SetData(teamID);
            return proxy;
        }

        public LazyProxy<int, Spectator> ReadSpectatorID(Func<int, Spectator> findSpectator)
        {
            int spectatorID = ReadSByte();
            var proxy = new LazyProxy<int, Spectator>(findSpectator);
            proxy.SetData(spectatorID);
            return proxy;
        }

        public Vector2 ReadVector2Normalized16(float minNormalized, float maxNormalized)
        {
            var scale = (maxNormalized - minNormalized) / ushort.MaxValue;
            return new Vector2
            {
                X = minNormalized + ReadUInt16() * scale,
                Y = minNormalized + ReadUInt16() * scale
            };
        }

        public Vector2 ReadVector2Normalized8(float minNormalized, float maxNormalized)
        {
            var scale = (maxNormalized - minNormalized) / byte.MaxValue;
            return new Vector2
            {
                X = minNormalized + ReadByte() * scale,
                Y = minNormalized + ReadByte() * scale
            };
        }

        /// <summary>
        /// Reads a Vector2 value given in half precision.
        /// </summary>
        public Vector2 ReadHalfVector2()
        {
            return new Vector2
            {
                X = ReadHalf(),
                Y = ReadHalf()
            };
        }

        /// <summary>
        /// Reads a Vector3 value given in half precision.
        /// </summary>
        public Vector3 ReadHalfVector3()
        {
            return new Vector3
            {
                X = ReadHalf(),
                Y = ReadHalf(),
                Z = ReadHalf()
            };
        }
    }
}
