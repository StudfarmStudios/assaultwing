using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.Helpers;

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
        /// Creates an uninitialised ActionGob.
        /// </summary>
        /// This constructor is only for serialisation.
        public ActionGob()
        {
            actionGobName = "dummyactiongob";
        }

        /// <summary>
        /// Creates a gob of the specified gob type.
        /// </summary>
        public ActionGob(CanonicalString typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// Triggers an predefined action on the ActionGob.
        /// </summary>
        public abstract void Act();

        #region Methods related to serialisation

        /// <summary>
        /// Serialises the gob to a binary writer.
        /// </summary>
        public override void Serialize(Net.NetworkBinaryWriter writer, Net.SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((string)actionGobName, 32, true);
            }
        }

        /// <summary>
        /// Deserialises the gob from a binary reader.
        /// </summary>
        public override void Deserialize(Net.NetworkBinaryReader reader, Net.SerializationModeFlags mode, TimeSpan messageAge)
        {
            base.Deserialize(reader, mode, messageAge);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                actionGobName = reader.ReadString(32);
            }
        }

        #endregion Methods related to serialisation
    }
}
