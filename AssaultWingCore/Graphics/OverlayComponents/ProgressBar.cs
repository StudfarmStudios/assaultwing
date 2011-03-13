using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Helpers;

namespace AW2.Graphics.OverlayComponents
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
        private Texture2D _backgroundTexture, _barTexture, _flowTexture;
        private object _lock;
        private int _subtaskCount, _subtaskCompletedCount;

        /// <summary>
        /// An estimated state of progress of the task; 0 is freshly started; 1 is finished.
        /// </summary>
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

        public void SetSubtaskCount(int count)
        {
            lock (_lock)
            {
                _subtaskCount = count;
                _subtaskCompletedCount = 0;
            }
        }

        public void SubtaskCompleted()
        {
            lock (_lock) _subtaskCompletedCount++;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_backgroundTexture, Vector2.Zero, Color.White);

            // Draw fill bar.
            var barPos = new Vector2(3, 3);
            var barRectangle = new Rectangle(0, 0, (int)(_barTexture.Width * TaskProgress), _barTexture.Height);
            spriteBatch.Draw(_barTexture, barPos, barRectangle, Color.White);

            // Draw flow pattern.
            float flowSpeed = 45; // flow speed in pixels per second
            float flowPassTime = _flowTexture.Width / flowSpeed; // how many seconds it takes to flow one texture width
            int flowDisplacement = (int)(_flowTexture.Width * (AssaultWingCore.Instance.GameTime.TotalRealTime.TotalSeconds % flowPassTime) / flowPassTime);
            var flowPos = barPos;
            var flowRectangle = new Rectangle(
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
            var content = AssaultWingCore.Instance.Content;
            _backgroundTexture = content.Load<Texture2D>("menu_progressbar_bg");
            _barTexture = content.Load<Texture2D>("menu_progressbar_fill");
            _flowTexture = content.Load<Texture2D>("menu_progressbar_advancer");
        }
    }
}
