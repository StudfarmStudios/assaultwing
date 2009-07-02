using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.Helpers;

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
        /// The action gob to trigger.
        /// </summary>
        public ActionGob ActionGob { get; private set; }

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
        public CollisionTrigger(string typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            ActionGob = (ActionGob)Arena.Gobs.FirstOrDefault(gob => gob is ActionGob && ((ActionGob)gob).ActionGobName == actionGobName);
            if (ActionGob == null) Log.Write("Warning: Trigger gob cannot find ActionGob " + actionGobName);
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
            if (ActionGob.Dead)
                ActionGob = null;
        }
    }
}
