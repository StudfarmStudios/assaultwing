using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using AW2.Helpers;
using AW2.UI;

namespace AW2.Graphics
{
    /// <summary>
    /// Game intro graphics implementation.
    /// </summary>
    public class IntroEngine : DrawableGameComponent
    {
        Control _skipControl;
        Video _awIntroVideo;
        VideoPlayer _videoPlayer;
        SpriteBatch _spriteBatch;

        public IntroEngine(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            _skipControl = new KeyboardKey(Microsoft.Xna.Framework.Input.Keys.Escape);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            _awIntroVideo = Game.Content.Load<Video>("aw_intro");
            _videoPlayer = new VideoPlayer();
            _spriteBatch = new SpriteBatch(Game.GraphicsDevice);
        }

        protected override void UnloadContent()
        {
            if (_spriteBatch != null)
            {
                _spriteBatch.Dispose();
                _spriteBatch = null;
            }
            if (_videoPlayer != null)
            {
                _videoPlayer.Dispose();
                _videoPlayer = null;
            }
            base.UnloadContent();
        }

        public override void Update(GameTime gameTime)
        {
            if (_videoPlayer.State != MediaState.Playing) _videoPlayer.Play(_awIntroVideo);
            if (_skipControl.Pulse) IntroFinished();
            if (_videoPlayer.PlayPosition == _videoPlayer.Video.Duration) IntroFinished();
        }

        public override void Draw(GameTime gameTime)
        {
            var gfx = AssaultWing.Instance.GraphicsDevice;
            gfx.Clear(Color.Black);
            var videoFrame = _videoPlayer.GetTexture();
            _spriteBatch.Begin();
            int width = videoFrame.Width;
            int height = videoFrame.Height;
            var titleSafeArea = gfx.Viewport.TitleSafeArea;
            titleSafeArea.Clamp(ref width, ref height);
            var destinationRect = new Rectangle((titleSafeArea.Width - width) / 2, (titleSafeArea.Height - height) / 2, width, height);
            _spriteBatch.Draw(videoFrame, destinationRect, Color.White);
            _spriteBatch.End();
        }

        private void IntroFinished()
        {
            _videoPlayer.Stop();
            AssaultWing.Instance.ShowMenu();
        }
    }
}
