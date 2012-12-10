using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Graphics;
using AW2.Graphics.Content;
using AW2.Helpers;

namespace AW2.Core
{
    /// <summary>
    /// Replacement for <see cref="Microsoft.Xna.Framework.Game"/>.
    /// </summary>
    public class AWGame : IDisposable
    {
        public const int TargetFPS = 60;
        public static readonly TimeSpan TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / TargetFPS);
        private static readonly TimeSpan FastFrameDrawMaxDuration = TimeSpan.FromMilliseconds(2);

        public GraphicsDeviceService GraphicsDeviceService { get; private set; }
        public RenderTarget2D DefaultRenderTarget { get; private set; }
        public AWContentManager Content { get; private set; }
        public GameServiceContainer Services { get; private set; }
        public AWGameComponentCollection Components { get; private set; }
        public int FramesDrawnLastSecond { get; private set; }
        public bool FrameDrawIsFast { get { return _frameDrawTimes.Average <= FastFrameDrawMaxDuration; } }

        /// <summary>
        /// The current game time.
        /// </summary>
        public AWGameTime GameTime { get; private set; }

        private bool _takeScreenShot;
        private AutoRenderTarget2D _screenshotRenderTarget;
        private AWTimer _framerateTimer;
        private RunningSequenceTimeSpan _frameDrawTimes;
        private Stopwatch _frameDrawStopwatch;

        public AWGame(GraphicsDeviceService graphicsDeviceService)
        {
            GraphicsDeviceService = graphicsDeviceService;
            Services = new GameServiceContainer();
            if (graphicsDeviceService != null) Services.AddService(typeof(IGraphicsDeviceService), graphicsDeviceService);
            Components = new AWGameComponentCollection();
            GameTime = new AWGameTime();
            _framerateTimer = new AWTimer(() => GameTime.TotalRealTime, TimeSpan.FromSeconds(1)) { SkipPastIntervals = true };
            _frameDrawTimes = new RunningSequenceTimeSpan(TimeSpan.FromSeconds(1));
            _frameDrawStopwatch = new Stopwatch();
        }

        public void TakeScreenShot()
        {
            _takeScreenShot = true;
        }

        public void Dispose()
        {
            foreach (var item in Components) item.Dispose();
        }

        public virtual void Initialize()
        {
            Content = new AWContentManager(Services);
            foreach (var item in Components)
            {
                Log.Write("Initializing " + item.GetType().Name);
                item.Initialize();
            }
        }

        /// <summary>
        /// Called when unmanaged content is to be disposed such as at shutdown.
        /// </summary>
        public virtual void UnloadContent()
        {
            if (_screenshotRenderTarget != null) _screenshotRenderTarget.Dispose();
            _screenshotRenderTarget = null;
            foreach (var item in Components) item.UnloadContent();
            Content.Unload();
        }

        /// <summary>
        /// Called after all components are initialized but before the first update in the game loop.
        /// </summary>
        public virtual void BeginRun()
        {
            foreach (var component in Components)
            {
                Log.Write("Loading content for " + component.GetType().Name);
                component.LoadContent();
            }
        }

        /// <summary>
        /// Called when the game has determined that game logic needs to be processed.
        /// </summary>
        public void Update(AWGameTime gameTime)
        {
            GameTime = gameTime;
            foreach (var item in Components)
                if (item.Enabled) item.Update();
            UpdateImpl();
        }

        /// <summary>
        /// Subclasses may process additional game logic by overriding this method.
        /// </summary>
        protected virtual void UpdateImpl() { }

        /// <summary>
        /// Called when the game determines it is time to draw a frame.
        /// </summary>
        public virtual void Draw()
        {
            if (_takeScreenShot) RenderToFile(DrawImpl);
            _takeScreenShot = false;
            DrawImpl();
        }

        /// <summary>
        /// Called after the game loop has stopped running before exiting.
        /// </summary>
        public virtual void EndRun()
        {
        }

        private string GetScreenshotPath()
        {
            var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Assault Wing");
            System.IO.Directory.CreateDirectory(dir);
            var filename = string.Format("AW {0:yyyy-MM-dd HH-mm-ss}.jpg", DateTime.Now);
            return System.IO.Path.Combine(dir, filename);
        }

        private void RenderToFile(Action render)
        {
            var gfx = GraphicsDeviceService.GraphicsDevice;
            var pp = gfx.PresentationParameters;
            var oldViewport = gfx.Viewport;
            if (_screenshotRenderTarget == null) _screenshotRenderTarget = new AutoRenderTarget2D(
                GraphicsDeviceService.GraphicsDevice, () => new AutoRenderTarget2D.CreationData
                {
                    Width = GraphicsDeviceService.GraphicsDevice.Viewport.Width,
                    Height = GraphicsDeviceService.GraphicsDevice.Viewport.Height,
                    DepthStencilState = GraphicsDeviceService.GraphicsDevice.DepthStencilState,
                });
            _screenshotRenderTarget.SetAsRenderTarget();
            DefaultRenderTarget = _screenshotRenderTarget.GetTexture();
            render();
            gfx.SetRenderTarget(DefaultRenderTarget = null);
            gfx.Viewport = oldViewport;
            var screenshot = _screenshotRenderTarget.GetTexture();
            using (var stream = System.IO.File.OpenWrite(GetScreenshotPath()))
                screenshot.SaveAsJpeg(stream, screenshot.Width, screenshot.Height);
        }

        private void DrawImpl()
        {
            _frameDrawStopwatch.Restart();
            foreach (var item in Components)
                if (item.Visible)
                    item.Draw();
            _frameDrawStopwatch.Stop();
            _frameDrawTimes.Add(_frameDrawStopwatch.Elapsed, GameTime.TotalRealTime);
            if (_framerateTimer.IsElapsed)
            {
                _frameDrawTimes.Prune(GameTime.TotalRealTime);
                FramesDrawnLastSecond = _frameDrawTimes.Count;
            }
        }
    }
}
