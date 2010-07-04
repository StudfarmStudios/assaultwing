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
        class TaskStatus
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

        Texture2D backgroundTexture, barTexture, flowTexture;

        object @lock;
        int subtaskCount, subtaskCompletedCount;

        /// <summary>
        /// The task to run.
        /// </summary>
        Action task;

        /// <summary>
        /// Info about the running task or <c>null</c> if the task is not running.
        /// </summary>
        WorkItem taskWorkItem;

        /// <summary>
        /// The task to run and whose progress to measure.
        /// </summary>
        public Action Task
        {
            set
            {
                if (taskWorkItem != null)
                    throw new InvalidOperationException("Cannot change background task while it's running");
                task = value;
            }
        }

        /// <summary>
        /// Is the background task set and running.
        /// </summary>
        public bool TaskRunning { get { return taskWorkItem != null; } }

        /// <summary>
        /// Has the task finished running.
        /// </summary>
        /// This will turn <c>true</c> only after <see cref="StartTask"/> has been called.
        /// When this is <c>true</c>, call <see cref="FinishTask"/>.
        public bool TaskCompleted
        {
            get
            {
                lock (@lock)
                {
                    if (taskWorkItem == null) return false;
                    TaskStatus status = (TaskStatus)taskWorkItem.State;
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
                lock (@lock)
                {
                    if (subtaskCount <= 0) return 1;
                    if (subtaskCompletedCount >= subtaskCount) return 1;
                    return (float)subtaskCompletedCount / subtaskCount;
                }
            }
        }

        /// <summary>
        /// Creates a progress bar.
        /// </summary>
        public ProgressBar()
            : base(HorizontalAlignment.Center, VerticalAlignment.Center)
        {
            @lock = new object();
        }

        /// <summary>
        /// Sets the number of subtasks that will complete during the task.
        /// </summary>
        /// <param name="count">The number of subtasks that will complete.</param>
        public void SetSubtaskCount(int count)
        {
            lock (@lock) subtaskCount = count;
        }

        /// <summary>
        /// Registers one subtask as completed.
        /// </summary>
        public void SubtaskCompleted()
        {
            lock (@lock) ++subtaskCompletedCount;
        }

        /// <summary>
        /// Starts running the task.
        /// </summary>
        /// After calling this method, poll regularly for <see cref="TaskCompleted"/>.
        /// When it turns <c>true</c>, call <see cref="FinishTask"/>.
        public void StartTask()
        {
            if (taskWorkItem != null)
                throw new InvalidOperationException("Cannot start a background task when it is already running");
            subtaskCompletedCount = 0;
            taskWorkItem = AbortableThreadPool.QueueUserWorkItem(RunTask, new TaskStatus());
        }

        /// <summary>
        /// Blocks until the task finishes, ties up loose ends and then
        /// forgets all about the task.
        /// </summary>
        /// There will be no blocking if <see cref="TaskCompleted"/> is <c>true</c>.
        public void FinishTask()
        {
            if (taskWorkItem == null)
                throw new InvalidOperationException("There is no background task to finish");
            TaskStatus status = (TaskStatus)taskWorkItem.State;

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
            if (taskWorkItem == null)
                throw new InvalidOperationException("There is no background task to abort");
            AbortableThreadPool.Abort(taskWorkItem, true);
            ResetTask();
        }

        /// <summary>
        /// The dimensions of the component in pixels.
        /// </summary>
        /// The return value field <c>Point.X</c> is the width of the component,
        /// and the field <c>Point.Y</c> is the height of the component,
        public override Point Dimensions
        {
            get { return new Point(backgroundTexture.Width, backgroundTexture.Height); }
        }

        /// <summary>
        /// Draws the overlay graphics component using the guarantee that the
        /// graphics device's viewport is set to the exact area needed by the component.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to use. <c>Begin</c> is assumed
        /// to have been called and <c>End</c> is assumed to be called after this
        /// method returns.</param>
        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            // Draw background.
            spriteBatch.Draw(backgroundTexture, Vector2.Zero, Color.White);

            // Draw fill bar.
            Vector2 barPos = new Vector2(3, 3);
            Rectangle barRectangle = new Rectangle(0, 0, (int)(barTexture.Width * TaskProgress), barTexture.Height);
            spriteBatch.Draw(barTexture, barPos, barRectangle, Color.White);

            // Draw flow pattern.
            float flowSpeed = 45; // flow speed in pixels per second
            float flowPassTime = flowTexture.Width / flowSpeed; // how many seconds it takes to flow one texture width
            int flowDisplacement = (int)(flowTexture.Width * (AssaultWing.Instance.GameTime.TotalRealTime.TotalSeconds % flowPassTime) / flowPassTime);
            Vector2 flowPos = barPos;
            Rectangle flowRectangle = new Rectangle(
                flowTexture.Width - flowDisplacement,
                0,
                Math.Min(flowDisplacement, barRectangle.Width),
                flowTexture.Height);
            spriteBatch.Draw(flowTexture, flowPos, flowRectangle, Color.White);
            flowPos += new Vector2(flowDisplacement, 0);
            while (flowPos.X + flowTexture.Width <= barPos.X + barRectangle.Width)
            {
                spriteBatch.Draw(flowTexture, flowPos, Color.White);
                flowPos += new Vector2(flowTexture.Width, 0);
            }
            flowRectangle.X = 0;
            flowRectangle.Width = (int)(barPos.X + barRectangle.Width - flowPos.X);
            spriteBatch.Draw(flowTexture, flowPos, flowRectangle, Color.White);
        }

        public override void LoadContent()
        {
            var content = AssaultWing.Instance.Content;
            backgroundTexture = content.Load<Texture2D>("menu_progressbar_bg");
            barTexture = content.Load<Texture2D>("menu_progressbar_fill");
            flowTexture = content.Load<Texture2D>("menu_progressbar_advancer");
        }

        private void ResetTask()
        {
            taskWorkItem = null;
            task = null;
            subtaskCount = subtaskCompletedCount = 0;
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
                task();
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
