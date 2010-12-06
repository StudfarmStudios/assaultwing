using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Graphics;
using AW2.Helpers;

namespace AW2.Core
{
    /// <summary>
    /// Replacement for <see cref="Microsoft.Xna.Framework.Game"/>.
    /// </summary>
    public class AWGame : IDisposable
    {
        public GraphicsDeviceService GraphicsDeviceService { get; private set; }
        public AWContentManager Content { get; private set; }
        public GameServiceContainer Services { get; private set; }
        public AWGameComponentCollection Components { get; private set; }
        public TimeSpan TargetElapsedTime { get { return TimeSpan.FromSeconds(1f / TargetFPS); } }
        public int TargetFPS { get; set; }

        public event EventHandler Exiting;

        public AWGame(GraphicsDeviceService graphicsDeviceService)
        {
            GraphicsDeviceService = graphicsDeviceService;
            Services = new GameServiceContainer();
            Services.AddService(typeof(IGraphicsDeviceService), graphicsDeviceService);
            Components = new AWGameComponentCollection();
            TargetFPS = 60;
        }

        public void Dispose()
        {
            foreach (var component in Components) component.Dispose();
        }

        /// <summary>
        /// Called after the Game and GraphicsDevice are created, but before LoadContent.
        /// </summary>
        public virtual void Initialize()
        {
            Content = new AWContentManager(Services);
            foreach (var component in Components)
            {
                Log.Write("Initializing " + component.GetType().Name);
                component.Initialize();
            }
        }

        /// <summary>
        /// Called when unmanaged content is to be loaded such as after graphics device reset.
        /// </summary>
        public virtual void LoadContent()
        {
            foreach (var component in Components) component.LoadContent();
        }

        /// <summary>
        /// Called when unmanaged content is to be disposed such as before graphics device reset.
        /// It is very important to dispose all disposable graphics content here. Failure to do so
        /// may cause unexpected exceptions at graphics device reset.
        /// </summary>
        public virtual void UnloadContent()
        {
            foreach (var component in Components) component.UnloadContent();
        }

        /// <summary>
        /// Called after all components are initialized but before the first update in the game loop.
        /// </summary>
        public virtual void BeginRun()
        {
            foreach (var component in Components) component.LoadContent();
        }

        /// <summary>
        /// Called when the game has determined that game logic needs to be processed.
        /// </summary>
        public virtual void Update(AWGameTime gameTime)
        {
            foreach (var component in Components)
                if (component.Enabled) component.Update();
        }

        /// <summary>
        /// Called when the game determines it is time to draw a frame.
        /// </summary>
        public virtual void Draw()
        {
            foreach (var component in Components)
                if (component.Visible) component.Draw();
        }

        /// <summary>
        /// Called after the game loop has stopped running before exiting.
        /// </summary>
        public virtual void EndRun()
        {
        }

        /// <summary>
        /// Raises an Exiting event.
        /// </summary>
        protected virtual void OnExiting(object sender, EventArgs args)
        {
            if (Exiting != null) Exiting(sender, args);
            throw new NotImplementedException();
        }
    }
}
