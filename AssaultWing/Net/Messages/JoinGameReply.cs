using System.Collections.Generic;
using System.Linq;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client replying
    /// to a request to join the server.
    /// </summary>
    public class JoinGameReply : Message
    {
        /// <summary>
        /// Marks the change of an int-type identifier.
        /// </summary>
        public struct IdChange
        {
            /// <summary>
            /// The old value of the identifier.
            /// </summary>
            public int oldId;

            /// <summary>
            /// The new value of the identifier.
            /// </summary>
            public int newId;
        }

        /// <summary>
        /// Changes to the identifiers of the local players of the game client
        /// who requested joining the game.
        /// </summary>
        public IdChange[] PlayerIdChanges { get; set; }

        /// <summary>
        /// The list of canonical strings on the game server.
        /// </summary>
        public IList<string> CanonicalStrings { get; set; }

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x20, true);

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// <param name="writer">Writer of serialised data.</param>
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Join game reply message structure:
            // byte: number of players, N
            // repeat N
            //   int: old player ID
            //   int: new player ID
            // int: number of canonical strings, K
            // repeat K - 1 (all but the zero-indexed canonical string)
            //   32 byte string: string value
            writer.Write((byte)PlayerIdChanges.Length);
            foreach (IdChange change in PlayerIdChanges)
            {
                writer.Write((int)change.oldId);
                writer.Write((int)change.newId);
            }
            writer.Write((int)CanonicalStrings.Count);
            foreach (var canonical in CanonicalStrings.Skip(1))
                writer.Write((string)canonical, 32, true);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            int idChangeCount = reader.ReadByte();
            PlayerIdChanges = new IdChange[idChangeCount];
            for (int i = 0; i < idChangeCount; ++i)
            {
                PlayerIdChanges[i].oldId = reader.ReadInt32();
                PlayerIdChanges[i].newId = reader.ReadInt32();
            }
            int canonicalStringCount = reader.ReadInt32();
            CanonicalStrings = new string[canonicalStringCount];
            CanonicalStrings[0] = null;
            for (int i = 1; i < canonicalStringCount; ++i)
                CanonicalStrings[i] = reader.ReadString(32);
        }

        /// <summary>
        /// Returns a String that represents the current Object. 
        /// </summary>
        public override string ToString()
        {
            return base.ToString() + " " + PlayerIdChanges.Length + " players";
        }
    }
}
