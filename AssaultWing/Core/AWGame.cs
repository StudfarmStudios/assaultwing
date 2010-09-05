using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Graphics;

namespace AW2.Core
{
    /// <summary>
    /// Replacement for <see cref="Microsoft.Xna.Framework.Game"/>.
    /// </summary>
    public class AWGame : IDisposable
    {
        [Obsolete("Use GraphicsDeviceService.Instance.GraphicsDevice instead")]
        public GraphicsDevice GraphicsDevice { get { return GraphicsDeviceService.Instance.GraphicsDevice; } }
        public AWContentManager Content { get; private set; }
        public GameServiceContainer Services { get; private set; }
        public List<AWGameComponent> Components { get; private set; }
        public TimeSpan TargetElapsedTime { get; set; }

        public event EventHandler Exiting;

        public AWGame()
        {
            Services = new GameServiceContainer();
            Components = new List<AWGameComponent>();
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60);
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
            Services.AddService(typeof(IGraphicsDeviceService), GraphicsDeviceService.Instance);
            Content = new AWContentManager(Services);
            foreach (var component in Components) component.Initialize();
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
        public virtual void Update(GameTime gameTime)
        {
            Components.Sort((a, b) => a.UpdateOrder.CompareTo(b.UpdateOrder));
            foreach (var component in Components)
                if (component.Enabled) component.Update();
        }

        /// <summary>
        /// Called when the game determines it is time to draw a frame.
        /// </summary>
        public virtual void Draw()
        {
            Components.Sort((a, b) => a.UpdateOrder.CompareTo(b.UpdateOrder));
            foreach (var component in Components)
                if (component.Visible) component.Draw();
        }

        /// <summary>
        /// Called after the game loop has stopped running before exiting.
        /// </summary>
        public virtual void EndRun()
        {
            throw new NotImplementedException();
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
