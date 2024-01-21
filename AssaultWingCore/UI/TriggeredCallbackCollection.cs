using System;
using System.Collections.Generic;

namespace AW2.UI
{
    /// <summary>
    /// A collection of callbacks, each triggered by a control.
    /// There is an additional callback, common for all the other callbacks,
    /// that is triggered when any of the other callbacks is triggered.
    /// </summary>
    public class TriggeredCallbackCollection
    {
        /// <summary>
        /// Called after any callback in the collection is triggered.
        /// </summary>
        public Action TriggeredCallback { get; set; }

        /// <summary>
        /// The callbacks
        /// </summary>
        public IList<TriggeredCallback> Callbacks { get; private set; }

        /// <summary>
        /// Creates a collection of triggered callbacks.
        /// </summary>
        public TriggeredCallbackCollection()
        {
            Callbacks = new List<TriggeredCallback>();
        }

        /// <summary>
        /// Checks the callbacks and triggers them if necessary.
        /// </summary>
        public void Update()
        {
            bool triggered = false;
            foreach (var callback in Callbacks)
                triggered |= callback.Update();
            if (triggered && TriggeredCallback != null)
                TriggeredCallback();
        }
    }
}
