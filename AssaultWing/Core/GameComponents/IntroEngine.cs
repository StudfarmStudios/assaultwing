using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using AW2.Graphics;
using AW2.Helpers;
using AW2.UI;

namespace AW2.Core.GameComponents
{
    /// <summary>
    /// Game intro sequence implementation.
    /// </summary>
    public class IntroEngine : AWGameComponent
    {
        public enum ModeType { NotStarted, Playing, Finished };
        public ModeType Mode { get; private set; }
        private Control _skipControl;
        private AWVideo _introVideo;
        private SpriteBatch _spriteBatch;

        public new AssaultWing Game { get { return (AssaultWing)base.Game; } }

        public IntroEngine(AssaultWing game, int updateOrder)
            : base(game, updateOrder)
        {
        }

        private void BeginIntro()
        {
            Log.Write("Starting intro");
            try
            {
                _introVideo.Play();
                Mode = ModeType.Playing;
            }
            catch (InvalidOperationException)
            {
                // Several people have got System.InvalidOperationException: An unexpected error has occurred
                // thrown from Microsoft.Xna.Framework.Media.VideoPlayer.Resume() of XNA 4.0. Cause unknown.
                Log.Write("Error starting the intro video. Skipping.");
                EndIntro();
            }
        }

        private void EndIntro()
        {
            if (Mode == ModeType.Playing) _introVideo.Stop();
            Mode = ModeType.Finished;
        }

        public override void Initialize()
        {
            base.Initialize();
            _skipControl = new KeyboardKey(Microsoft.Xna.Framework.Input.Keys.Escape);
        }

        public override void LoadContent()
        {
            base.LoadContent();
            _spriteBatch = new SpriteBatch(Game.GraphicsDeviceService.GraphicsDevice);
            _introVideo = new AWVideo(Game.Content.Load<Video>("aw_intro"), Game.Settings.Sound.SoundVolume);
        }

        public override void UnloadContent()
        {
            if (_spriteBatch != null) _spriteBatch.Dispose();
            _spriteBatch = null;
        }

        public override void Update()
        {
            if (_skipControl.Pulse || (_introVideo != null && _introVideo.IsFinished)) EndIntro();
        }

        public override void Draw()
        {
            if (Mode == ModeType.NotStarted) BeginIntro();
            Game.GraphicsDeviceService.GraphicsDevice.Clear(Color.Black);
            var videoFrame = _introVideo.GetTexture();
            if (videoFrame != null)
            {
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
                int width = videoFrame.Width;
                int height = videoFrame.Height;
                var titleSafeArea = Game.GraphicsDeviceService.GraphicsDevice.Viewport.TitleSafeArea;
                titleSafeArea.Stretch(ref width, ref height);
                var destinationRect = new Rectangle((titleSafeArea.Width - width) / 2, (titleSafeArea.Height - height) / 2, width, height);
                _spriteBatch.Draw(videoFrame, destinationRect, Color.White);
                _spriteBatch.End();
            }
        }
    }
}
