using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Graphics;
using AW2.Helpers;

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
        private int _subtaskCount, _subtaskCompletedCount;

        public AssaultWing Game { get { return (AssaultWing)Viewport.Game; } }
        public bool IsFinished { get; private set; }
        public override Point Dimensions { get { return new Point(BackgroundTexture.Width, BackgroundTexture.Height); } }
        private Texture2D BackgroundTexture { get { return Game.MenuEngine.MenuContent.ProgressBarBackgroundTexture; } }

        public ProgressBar()
            : base(null, HorizontalAlignment.Center, VerticalAlignment.Center)
        {
            _lock = new object();
            IsFinished = true;
        }

        public void Start(int subtaskCount)
        {
            if (subtaskCount <= 0) throw new ArgumentException("Subtask count must be positive, not " + subtaskCount);
            lock (_lock)
            {
                IsFinished = false;
                _subtaskCount = subtaskCount;
                _subtaskCompletedCount = 0;
            }
        }

        public void SubtaskCompleted()
        {
            lock (_lock)
            {
                if (IsFinished) throw new InvalidOperationException("Cannot complete subtask when task is already finished");
                _subtaskCompletedCount++;
                if (_subtaskCompletedCount == _subtaskCount) IsFinished = true;
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
            var barTexture = Game.MenuEngine.MenuContent.ProgressBarBarTexture;
            var flowTexture = Game.MenuEngine.MenuContent.ProgressBarFlowTexture;
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
                if (_subtaskCount == 0) throw new InvalidOperationException("No task set yet");
                if (IsFinished) return 1;
                if (_subtaskCompletedCount >= _subtaskCount) return 1;
                return (float)_subtaskCompletedCount / _subtaskCount;
            }
        }
    }
}
