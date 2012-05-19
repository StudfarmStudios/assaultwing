using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Graphics.Content;
using AW2.Helpers;
using AW2.Graphics;

namespace AW2.Core
{
    /// <summary>
    /// Replacement for <see cref="Microsoft.Xna.Framework.Game"/>.
    /// </summary>
    public class AWGame : IDisposable
    {
        public GraphicsDeviceService GraphicsDeviceService { get; private set; }
        public RenderTarget2D DefaultRenderTarget { get; private set; }
        public AWContentManager Content { get; private set; }
        public GameServiceContainer Services { get; private set; }
        public AWGameComponentCollection Components { get; private set; }
        public TimeSpan TargetElapsedTime { get { return TimeSpan.FromSeconds(1f / TargetFPS); } }
        public int TargetFPS { get; set; }

        /// <summary>
        /// The current game time.
        /// </summary>
        public AWGameTime GameTime { get; private set; }

        private bool _takeScreenShot;
        private AutoRenderTarget2D _screenshotRenderTarget;

        public AWGame(GraphicsDeviceService graphicsDeviceService)
        {
            GraphicsDeviceService = graphicsDeviceService;
            Services = new GameServiceContainer();
            if (graphicsDeviceService != null) Services.AddService(typeof(IGraphicsDeviceService), graphicsDeviceService);
            Components = new AWGameComponentCollection();
            TargetFPS = 60;
            GameTime = new AWGameTime();
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
            var filename = string.Format("AW {0:yyyy-MM-dd HH-mm-ss}.png", DateTime.Now);
            return System.IO.Path.Combine(dir, filename);
        }

        private void RenderToFile(Action render)
        {
            var gfx = GraphicsDeviceService.GraphicsDevice;
            var pp = gfx.PresentationParameters;
            var oldViewport = gfx.Viewport;
            if (_screenshotRenderTarget ==null) _screenshotRenderTarget = new AutoRenderTarget2D(GraphicsDeviceService.GraphicsDevice, () => new AutoRenderTarget2D.CreationData
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
            foreach (var item in Components)
                if (item.Visible)
                    item.Draw();
        }
    }
}
