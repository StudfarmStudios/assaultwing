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
        public ActionGob(string typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// Triggers an predefined action on the ActionGob.
        /// </summary>
        public abstract void Act();
    }
}
