using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Menu;

namespace AW2.Core.OverlayComponents
{
    /// <summary>
    /// Progress bar, visualising the progress of a long-running thread.
    /// </summary>
    /// <remarks>
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
    /// </remarks>
    public class ProgressBar : OverlayComponent
    {
        private object _lock;
        private int _subtaskCount;
        private Func<int> _subtaskCompletedCount;

        public MenuEngineImpl Menu { get; private set; }
        public AssaultWing Game { get { return Menu.Game; } }
        public bool IsFinished { get; private set; }
        public override Point Dimensions { get { return new Point(BackgroundTexture.Width, BackgroundTexture.Height); } }
        private Texture2D BackgroundTexture { get { return Menu.MenuContent.ProgressBarBackgroundTexture; } }

        public ProgressBar(MenuEngineImpl menu)
            : base(null, HorizontalAlignment.Center, VerticalAlignment.Center)
        {
            Menu = menu;
            _lock = new object();
            IsFinished = true;
        }

        public void Start(int subtaskCount, Func<int> subtaskCompletedCount)
        {
            if (subtaskCount < 0) throw new ArgumentException("Subtask count must be non-negative, not " + subtaskCount);
            _subtaskCompletedCount = subtaskCompletedCount;
            lock (_lock)
            {
                IsFinished = false;
                _subtaskCount = subtaskCount;
            }
        }

        public void SkipRemainingSubtasks()
        {
            lock (_lock)
            {
                IsFinished = true;
            }
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            if (_subtaskCompletedCount() == _subtaskCount) IsFinished = true; // TODO !!! Move to some kind of Update method.

            var barTexture = Menu.MenuContent.ProgressBarBarTexture;
            var flowTexture = Menu.MenuContent.ProgressBarFlowTexture;
            spriteBatch.Draw(BackgroundTexture, Vector2.Zero, Color.White);

            // Draw fill bar.
            var barPos = new Vector2(3, 3);
            var barRectangle = new Rectangle(0, 0, (int)(barTexture.Width * GetTaskProgress()), barTexture.Height);
            spriteBatch.Draw(barTexture, barPos, barRectangle, Color.White);

            // Draw flow pattern.
            float flowSpeed = 45; // flow speed in pixels per second
            float flowPassTime = flowTexture.Width / flowSpeed; // how many seconds it takes to flow one texture width
            int flowDisplacement = (int)(flowTexture.Width * (Game.GameTime.TotalRealTime.TotalSeconds % flowPassTime) / flowPassTime);
            var flowPos = barPos;
            var flowRectangle = new Rectangle(
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

        private float GetTaskProgress()
        {
            lock (_lock)
            {
                if (IsFinished) return 1;
                if (_subtaskCompletedCount() >= _subtaskCount) return 1;
                return (float)_subtaskCompletedCount() / _subtaskCount;
            }
        }
    }
}
