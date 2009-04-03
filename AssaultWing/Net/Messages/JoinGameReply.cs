﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            // byte number of players N
            // repeat N
            //   int old player ID
            //   int new player ID
            writer.Write((byte)PlayerIdChanges.Length);
            foreach (IdChange change in PlayerIdChanges)
            {
                writer.Write((int)change.oldId);
                writer.Write((int)change.newId);
            }
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            int count = reader.ReadByte();
            PlayerIdChanges = new IdChange[count];
            for (int i = 0; i < count; ++i)
            {
                PlayerIdChanges[i].oldId = reader.ReadInt32();
                PlayerIdChanges[i].newId = reader.ReadInt32();
            }
        }

        /// <summary>
        /// Returns a String that represents the current Object. 
        /// </summary>
        public override string ToString()
        {
            return base.ToString() + PlayerIdChanges.Length + " players";
        }
    }
}
