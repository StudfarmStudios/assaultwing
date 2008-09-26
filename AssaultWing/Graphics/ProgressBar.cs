using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;

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
    class ProgressBar : OverlayComponent
    {
        /// <summary>
        /// Delegate type for a long-running task that returns exceptions
        /// instead of throwing them.
        /// </summary>
        /// <returns><c>null</c> on success or any exception 
        /// if one was thrown during the task.</returns>
        delegate Exception CatchingAsyncCallback();

        /// <summary>
        /// Delegate type for a long-running task to be run in a separate thread.
        /// </summary>
        public delegate void AsyncCallback();

        Texture2D backgroundTexture, barTexture, flowTexture;

        object @lock;
        int subtaskCount, subtaskCompletedCount;

        /// <summary>
        /// The task to run.
        /// </summary>
        CatchingAsyncCallback task;

        /// <summary>
        /// Status of the task.
        /// </summary>
        IAsyncResult taskResult;

        /// <summary>
        /// The task to run and whose progress to measure.
        /// </summary>
        public AsyncCallback Task
        {
            set
            {
                if (taskResult != null)
                    throw new InvalidOperationException("Cannot change background task while it's running");
                task = delegate()
                {
#if !DEBUG
                    try
                    {
#endif
                        value();
                        return null;
#if !DEBUG
                    }
                    catch (Exception e)
                    {
                        return e;
                    }
#endif
                };
            }
        }

        /// <summary>
        /// Is the background task set and running.
        /// </summary>
        public bool TaskRunning { get { return taskResult != null; } }

        /// <summary>
        /// Has the task finished running.
        /// </summary>
        /// This will turn <c>true</c> only after <c>StartTask</c> has been called.
        /// When this is <c>true</c>, call <c>FinishTask</c>.
        public bool TaskCompleted { get { lock (@lock) return taskResult != null && taskResult.IsCompleted; } }

        /// <summary>
        /// An estimated state of progress of the task; 0 is freshly started; 1 is finished.
        /// </summary>
        /// The state of progress is based on other components' reports on completed subtasks,
        /// thus this measure may be quite inaccurate. If you only need to know whether the task 
        /// has finished or not, use <c>TaskCompleted</c>.
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
        /// After calling this method, poll regularly for <c>TaskCompleted</c>.
        /// When it turns <c>true</c>, call <c>FinishTask</c>.
        public void StartTask()
        {
            if (taskResult != null)
                throw new InvalidOperationException("Cannot start background task that is already running");
            subtaskCompletedCount = 0;
            taskResult = task.BeginInvoke(null, null);
        }

        /// <summary>
        /// Blocks until the task finishes, ties up loose ends and then
        /// forgets all about the task.
        /// </summary>
        /// There will be no blocking if <c>TaskCompleted == true</c>.
        public void FinishTask()
        {
            if (taskResult == null)
                throw new InvalidOperationException("There is no background task to finish");
            Exception exception = task.EndInvoke(taskResult);
            if (exception != null)
                throw new Exception("Exception thrown during background task", exception);
            taskResult = null;
            task = null;
            subtaskCount = subtaskCompletedCount = 0;
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
            float flowSpeed = 15; // flow speed in pixels per second
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

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public override void LoadContent()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            backgroundTexture = data.GetTexture(TextureName.ProgressBarBackground);
            barTexture = data.GetTexture(TextureName.ProgressBarFill);
            flowTexture = data.GetTexture(TextureName.ProgressBarFlow);
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public override void UnloadContent()
        {
            // Our textures are disposed by the graphics engine.
        }
    }
}
