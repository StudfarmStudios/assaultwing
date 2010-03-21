using System;
using System.Collections.Generic;

namespace AW2.Helpers
{
    public abstract class StepwiseActionBase
    {
        IEnumerator<object> _enumerator;

        public bool IsRunning { get; private set; }

        public StepwiseActionBase()
        {
            IsRunning = true;
        }

        /// <summary>
        /// Returns true if there was a step to perform.
        /// </summary>
        public bool InvokeStep()
        {
            _enumerator = _enumerator ?? Action().GetEnumerator();
            bool success = _enumerator.MoveNext();
            IsRunning = success;
            return success;
        }

        /// <summary>
        /// The enumerator's return value is discarded.
        /// </summary>
        protected abstract IEnumerable<object> Action();
    }

    public class StepwiseAction : StepwiseActionBase
    {
        IEnumerable<object> _action;

        public StepwiseAction(IEnumerable<object> action)
        {
            _action = action;
        }

        protected override IEnumerable<object> Action()
        {
            return _action;
        }
    }
}
