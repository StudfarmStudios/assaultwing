using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Graphics
{
    /// <summary>
    /// Progress bar, visualising the progress of a long-running thread.
    /// </summary>
    /// The progress bar is given a task to run and whose progress to measure.
    /// The progress bar then creates a background thread for the task, runs it
    /// and shows progress of the task in its visual output. The progress bar
    /// can be polled about the completion of the task after which the results
    /// of the task can be obtained.
    /// 
    /// The progress bar estimates the progress of the running task.
    /// Before starting the task, set the number of subtasks that will complete
    /// during the task. Then each subtask should report of its completion.
    /// 
    /// This class is thread safe enough to be safely operated from both 
    /// the main thread and the running background task.
    public class ProgressBar : OverlayComponent
    {
        /// <summary>
        /// Status of a task run in a background thread.
        /// </summary>
        private class TaskStatus
        {
            /// <summary>
            /// The exception that was thrown in the task, or <c>null</c> if none thrown.
            /// </summary>
            public Exception Exception { get; set; }

            /// <summary>
            /// Has the task completed.
            /// </summary>
            public bool IsCompleted { get; set; }
        }

        private Texture2D _backgroundTexture, _barTexture, _flowTexture;

        private object _lock;
        private int _subtaskCount, _subtaskCompletedCount;

        /// <summary>
        /// The task to run.
        /// </summary>
        private Action _task;

        /// <summary>
        /// Info about the running task or <c>null</c> if the task is not running.
        /// </summary>
        private WorkItem _taskWorkItem;

        /// <summary>
        /// The task to run and whose progress to measure.
        /// </summary>
        public Action Task
        {
            set
            {
                if (_taskWorkItem != null)
                    throw new InvalidOperationException("Cannot change background task while it's running");
                _task = value;
            }
        }

        /// <summary>
        /// Is the background task set and running.
        /// </summary>
        public bool TaskRunning { get { return _taskWorkItem != null; } }

        /// <summary>
        /// Has the task finished running.
        /// </summary>
        /// This will turn <c>true</c> only after <see cref="StartTask"/> has been called.
        /// When this is <c>true</c>, call <see cref="FinishTask"/>.
        public bool TaskCompleted
        {
            get
            {
                lock (_lock)
                {
                    if (_taskWorkItem == null) return false;
                    TaskStatus status = (TaskStatus)_taskWorkItem.State;
                    return status.IsCompleted;
                }
            }
        }

        /// <summary>
        /// An estimated state of progress of the task; 0 is freshly started; 1 is finished.
        /// </summary>
        /// The state of progress is based on other components' reports on completed subtasks,
        /// thus this measure may be quite inaccurate. If you only need to know whether the task 
        /// has finished or not, use <see cref="TaskCompleted"/>.
        public float TaskProgress
        {
            get
            {
                lock (_lock)
                {
                    if (_subtaskCount <= 0) return 1;
                    if (_subtaskCompletedCount >= _subtaskCount) return 1;
                    return (float)_subtaskCompletedCount / _subtaskCount;
                }
            }
        }

        public override Point Dimensions
        {
            get { return new Point(_backgroundTexture.Width, _backgroundTexture.Height); }
        }

        public ProgressBar()
            : base(null, HorizontalAlignment.Center, VerticalAlignment.Center)
        {
            _lock = new object();
        }

        /// <summary>
        /// Sets the number of subtasks that will complete during the task.
        /// </summary>
        /// <param name="count">The number of subtasks that will complete.</param>
        public void SetSubtaskCount(int count)
        {
            lock (_lock) _subtaskCount = count;
        }

        /// <summary>
        /// Registers one subtask as completed.
        /// </summary>
        public void SubtaskCompleted()
        {
            lock (_lock) ++_subtaskCompletedCount;
        }

        /// <summary>
        /// Starts running the task.
        /// </summary>
        /// After calling this method, poll regularly for <see cref="TaskCompleted"/>.
        /// When it turns <c>true</c>, call <see cref="FinishTask"/>.
        public void StartTask()
        {
            if (_taskWorkItem != null)
                throw new InvalidOperationException("Cannot start a background task when it is already running");
            _subtaskCompletedCount = 0;
            _taskWorkItem = AbortableThreadPool.QueueUserWorkItem(RunTask, new TaskStatus());
        }

        /// <summary>
        /// Blocks until the task finishes, ties up loose ends and then
        /// forgets all about the task.
        /// </summary>
        /// There will be no blocking if <see cref="TaskCompleted"/> is <c>true</c>.
        public void FinishTask()
        {
            if (_taskWorkItem == null)
                throw new InvalidOperationException("There is no background task to finish");
            TaskStatus status = (TaskStatus)_taskWorkItem.State;

            // Block until the task completes.
            while (!status.IsCompleted) System.Threading.Thread.Sleep(0);

            if (status.Exception != null)
                throw new Exception("Exception thrown during background task", status.Exception);
            ResetTask();
        }

        /// <summary>
        /// Aborts the task.
        /// </summary>
        public void AbortTask()
        {
            if (_taskWorkItem == null)
                throw new InvalidOperationException("There is no background task to abort");
            AbortableThreadPool.Abort(_taskWorkItem, true);
            ResetTask();
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            // Draw background.
            spriteBatch.Draw(_backgroundTexture, Vector2.Zero, Color.White);

            // Draw fill bar.
            Vector2 barPos = new Vector2(3, 3);
            Rectangle barRectangle = new Rectangle(0, 0, (int)(_barTexture.Width * TaskProgress), _barTexture.Height);
            spriteBatch.Draw(_barTexture, barPos, barRectangle, Color.White);

            // Draw flow pattern.
            float flowSpeed = 45; // flow speed in pixels per second
            float flowPassTime = _flowTexture.Width / flowSpeed; // how many seconds it takes to flow one texture width
            int flowDisplacement = (int)(_flowTexture.Width * (AssaultWing.Instance.GameTime.TotalRealTime.TotalSeconds % flowPassTime) / flowPassTime);
            Vector2 flowPos = barPos;
            Rectangle flowRectangle = new Rectangle(
                _flowTexture.Width - flowDisplacement,
                0,
                Math.Min(flowDisplacement, barRectangle.Width),
                _flowTexture.Height);
            spriteBatch.Draw(_flowTexture, flowPos, flowRectangle, Color.White);
            flowPos += new Vector2(flowDisplacement, 0);
            while (flowPos.X + _flowTexture.Width <= barPos.X + barRectangle.Width)
            {
                spriteBatch.Draw(_flowTexture, flowPos, Color.White);
                flowPos += new Vector2(_flowTexture.Width, 0);
            }
            flowRectangle.X = 0;
            flowRectangle.Width = (int)(barPos.X + barRectangle.Width - flowPos.X);
            spriteBatch.Draw(_flowTexture, flowPos, flowRectangle, Color.White);
        }

        public override void LoadContent()
        {
            var content = AssaultWing.Instance.Content;
            _backgroundTexture = content.Load<Texture2D>("menu_progressbar_bg");
            _barTexture = content.Load<Texture2D>("menu_progressbar_fill");
            _flowTexture = content.Load<Texture2D>("menu_progressbar_advancer");
        }

        private void ResetTask()
        {
            _taskWorkItem = null;
            _task = null;
            _subtaskCount = _subtaskCompletedCount = 0;
        }

        /// <summary>
        /// Runs the task, handling exceptions. This method is to be run in a background thread.
        /// </summary>
        /// <param name="state">A <see cref="TaskStatus"/> instance.</param>
        private void RunTask(object state)
        {
            TaskStatus status = (TaskStatus)state;
            try
            {
                _task();
            }
#if !DEBUG
            catch (Exception e)
            {
                status.Exception = e;
            }
#endif
            finally
            {
                status.IsCompleted = true;
            }
        }
    }
}
