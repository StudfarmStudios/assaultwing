using AW2.Helpers;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client updating
    /// the damage level of a gob.
    /// </summary>
    public class GobDamageMessage : GameplayMessage
    {
        protected static MessageType messageType = new MessageType(0x26, false);

        /// <summary>
        /// Identifier of the gob to update.
        /// </summary>
        public int GobID { get; set; }

        /// <summary>
        /// New damage level of the gob.
        /// </summary>
        public float DamageLevel { get; set; }

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// <param name="writer">Writer of serialised data.</param>
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            // Gob damage (request) message structure:
            // int: gob identifier
            // float: gob damage level
            writer.Write((int)GobID);
            writer.Write((Half)DamageLevel);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            GobID = reader.ReadInt32();
            DamageLevel = reader.ReadHalf();
        }

        public override string ToString()
        {
            return base.ToString() + " [GobID " + GobID + ", damage " + DamageLevel + "]";
        }
    }
}
