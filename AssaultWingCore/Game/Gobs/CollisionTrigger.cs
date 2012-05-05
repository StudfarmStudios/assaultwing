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
    public class CollisionTrigger : Gob
    {
        /// <summary>
        /// Textual identifier of the action gob to trigger.
        /// </summary>
        [RuntimeState]
        private string _actionGobName;

        /// <summary>
        /// Use only via property <see cref="ActionGob"/>.
        /// </summary>
        private ActionGob _actionGob;
        private bool _actionGobInitialized;

        /// <summary>
        /// The action gob to trigger.
        /// </summary>
        private ActionGob ActionGob
        {
            get
            {
                if (!_actionGobInitialized)
                {
                    _actionGob = (ActionGob)Arena.Gobs.FirstOrDefault(gobb => gobb is ActionGob && ((ActionGob)gobb).ActionGobName == _actionGobName);
                    if (_actionGob == null) Log.Write("Warning: Trigger gob cannot find ActionGob " + _actionGobName);
                    else _actionGobInitialized = true;
                }
                return _actionGob;
            }
            set
            {
                _actionGob = value;
                _actionGobInitialized = true;
            }
        }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public CollisionTrigger()
        {
            _actionGobName = "dummyactiongob";
        }

        public CollisionTrigger(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override bool CollideIrreversible(CollisionArea myArea, CollisionArea theirArea)
        {
            if (ActionGob != null) ActionGob.Act();
            return true;
        }

        public override void Update()
        {
            base.Update();
            if (ActionGob != null && ActionGob.Dead)
                ActionGob = null;
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                base.Serialize(writer, mode);
                if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
                {
                    writer.Write((string)_actionGobName);
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
            {
                _actionGobName = reader.ReadString();
            }
        }
    }
}
