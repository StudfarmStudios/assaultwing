using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// Gob that triggers an action gob on collision.
    /// </summary>
    [LimitedSerialization]
    public class CollisionTrigger : Gob, IConsistencyCheckable
    {
        /// <summary>
        /// Textual identifier of the action gob to trigger.
        /// </summary>
        [RuntimeState]
        string actionGobName;

        /// <summary>
        /// Use only via property <see cref="ActionGob"/>.
        /// </summary>
        ActionGob actionGob;
        bool actionGobInitialized;

        /// <summary>
        /// The action gob to trigger.
        /// </summary>
        ActionGob ActionGob
        {
            get
            {
                if (!actionGobInitialized)
                {
                    actionGob = (ActionGob)Arena.Gobs.FirstOrDefault(gobb => gobb is ActionGob && ((ActionGob)gobb).ActionGobName == actionGobName);
                    if (actionGob == null) Log.Write("Warning: Trigger gob cannot find ActionGob " + actionGobName);
                    else actionGobInitialized = true;
                }
                return actionGob;
            }
            set
            {
                actionGob = value;
                actionGobInitialized = true;
            }
        }

        /// <summary>
        /// Creates an uninitialised collision trigger.
        /// </summary>
        /// This constructor is only for serialisation.
        public CollisionTrigger()
        {
            actionGobName = "dummyactiongob";
        }

        /// <summary>
        /// Creates a gob of the specified gob type.
        /// </summary>
        /// The gob's serialised fields are initialised according to the gob template 
        /// instance associated with the gob type. This applies also to fields declared
        /// in subclasses, so a subclass constructor only has to initialise its runtime
        /// state fields, not the fields that define its gob type.
        /// <param name="typeName">The type of the gob.</param>
        public CollisionTrigger(CanonicalString typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// Performs collision operations for the case when one of this gob's collision areas
        /// is overlapping one of another gob's collision areas.
        /// </summary>
        /// <param name="myArea">The collision area of this gob.</param>
        /// <param name="theirArea">The collision area of the other gob.</param>
        /// <param name="stuck">If <b>true</b> then the gob is stuck.</param>
        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            base.Collide(myArea, theirArea, stuck);
            if (ActionGob != null) ActionGob.Act();
        }

        /// <summary>
        /// Updates the gob according to physical laws.
        /// </summary>
        public override void Update()
        {
            base.Update();
            if (ActionGob != null && ActionGob.Dead)
                ActionGob = null;
        }

        #region Methods related to serialisation

        /// <summary>
        /// Serialises the gob to a binary writer.
        /// </summary>
        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {

                base.Serialize(writer, mode);
                if ((mode & SerializationModeFlags.ConstantData) != 0)
                {
                    writer.Write((string)actionGobName, 32, true);
                }
            }
        }

        /// <summary>
        /// Deserialises the gob from a binary reader.
        /// </summary>
        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                actionGobName = reader.ReadString(32);
            }
        }

        #endregion Methods related to serialisation
    }
}
