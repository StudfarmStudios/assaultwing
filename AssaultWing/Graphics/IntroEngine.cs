using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using AW2.Core;
using AW2.Helpers;
using AW2.UI;

namespace AW2.Graphics
{
    /// <summary>
    /// Game intro graphics implementation.
    /// </summary>
    public class IntroEngine : AWGameComponent
    {
        private Control _skipControl;
        private AWVideo _introVideo;
        private SpriteBatch _spriteBatch;

        public IntroEngine()
        {
        }

        public void BeginIntro()
        {
            _introVideo.Play();
        }

        private void EndIntro()
        {
            _introVideo.Stop();
            AssaultWingCore.Instance.ShowMenu();
        }

        public override void Initialize()
        {
            base.Initialize();
            _skipControl = new KeyboardKey(Microsoft.Xna.Framework.Input.Keys.Escape);
            _introVideo = new AWVideo("aw_intro");
        }

        public override void LoadContent()
        {
            base.LoadContent();
            _spriteBatch = new SpriteBatch(GraphicsDeviceService.Instance.GraphicsDevice);
        }

        public override void UnloadContent()
        {
            if (_spriteBatch != null)
            {
                _spriteBatch.Dispose();
                _spriteBatch = null;
            }
            _introVideo.Dispose();
            base.UnloadContent();
        }

        public override void Update()
        {
            if (_skipControl.Pulse) EndIntro();
            if (_introVideo.IsFinished) EndIntro();
        }

        public override void Draw()
        {
            var gfx = AssaultWingCore.Instance.GraphicsDevice;
            gfx.Clear(Color.Black);
            var videoFrame = _introVideo.GetTexture();
            if (videoFrame != null)
            {
                _spriteBatch.Begin();
                int width = videoFrame.Width;
                int height = videoFrame.Height;
                var titleSafeArea = gfx.Viewport.TitleSafeArea;
                titleSafeArea.Clamp(ref width, ref height);
                var destinationRect = new Rectangle((titleSafeArea.Width - width) / 2, (titleSafeArea.Height - height) / 2, width, height);
                _spriteBatch.Draw(videoFrame, destinationRect, Color.White);
                _spriteBatch.End();
            }
        }
    }
}
