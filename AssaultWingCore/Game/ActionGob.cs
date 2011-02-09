using System;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game
{
    /// <summary>
    /// Gob with a triggerable action.
    /// </summary>
    [LimitedSerialization]
    public abstract class ActionGob : Gob
    {
        [RuntimeState]
        string actionGobName;

        /// <summary>
        /// Textual identifier of the action gob.
        /// </summary>
        public string ActionGobName { get { return actionGobName; } }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public ActionGob()
        {
            actionGobName = "dummyactiongob";
        }

        public ActionGob(CanonicalString typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// Triggers an predefined action on the ActionGob.
        /// </summary>
        public abstract void Act();

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((string)actionGobName, 32, true);
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                actionGobName = reader.ReadString(32);
            }
        }
    }
}
