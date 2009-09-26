using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client about
    /// making a hole in an arena wall.
    /// </summary>
    public class WallHoleMessage : GameplayMessage
    {
        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x2c, false);

        /// <summary>
        /// Gob identifier of the wall.
        /// </summary>
        public int GobId { get; set; }

        /// <summary>
        /// Indices of triangles to remove.
        /// </summary>
        public IList<int> TriangleIndices { get; set; }

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            // Wall hole (request) message structure:
            // int: gob ID of the wall
            // int: number of triangles to remove, N
            // N ints: indices of triangles to remove
            writer.Write((int)GobId);
            writer.Write((int)TriangleIndices.Count());
            foreach (int index in TriangleIndices)
                writer.Write((int)index);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            GobId = reader.ReadInt32();
            int indexCount = reader.ReadInt32();
            TriangleIndices = new List<int>(indexCount);
            for (int i = 0; i < indexCount; ++i)
                TriangleIndices.Add(reader.ReadInt32());
        }

        /// <summary>
        /// Returns a String that represents the current Object. 
        /// </summary>
        public override string ToString()
        {
            return base.ToString() + " [Gob ID: " + GobId + ", " + TriangleIndices.Count() + " triangles ]";
        }
    }
}
