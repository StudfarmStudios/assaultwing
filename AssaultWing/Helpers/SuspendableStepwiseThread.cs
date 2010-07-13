using System;

namespace AW2.Helpers
{
    public class SuspendableStepwiseThread : SuspendableThread
    {
        private Action<Exception> _exceptionHandler;
        private StepwiseActionBase _threadAction;

        public SuspendableStepwiseThread(string name, Action<Exception> exceptionHandler)
            : base(name)
        {
            _exceptionHandler = exceptionHandler;
        }

        public void SetAction(StepwiseActionBase threadAction)
        {
            if (_threadAction != null) throw new InvalidOperationException("SuspendableStepwiseThread already has an action to perform");
            _threadAction = threadAction;
        }

        protected override void OnDoWork()
        {
            if (_threadAction == null) throw new InvalidOperationException("SuspendableStepwiseThread must have an action to perform before starting");
            try
            {
                while (!HasTerminateRequest())
                {
                    bool awokenByTerminate = SuspendIfNeeded();
                    if (awokenByTerminate) return;
                    _threadAction.InvokeStep();
                }
            }
            catch (Exception e)
            {
                _exceptionHandler(e);
            }
        }
    }
}
