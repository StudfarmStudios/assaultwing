using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.UI;
using AW2.Events;
using AW2.Game;

namespace AW2.Graphics
{
    /// <summary>
    /// Game overlay dialog. It displays text on top of the game display.
    /// </summary>
    /// Call <c>Prepare(OverlayDialogData)</c> to set up the dialog before making it visible.
    /// This sets up the visual contents of the dialog and sets which controls perform
    /// which actions.
    /// <seealso cref="OverlayDialogData"/>
    public class OverlayDialog : Microsoft.Xna.Framework.DrawableGameComponent
    {
        OverlayDialogData dialogData;
        SpriteBatch spriteBatch;
        Texture2D dialogTexture;

        /// <summary>
        /// Creates an overlay dialog.
        /// </summary>
        /// <param name="game">The game instance to attach the dialog to.</param>
        public OverlayDialog(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
        }

        #region Public interface

        /// <summary>
        /// The dialog's actions and visual contents.
        /// </summary>
        public OverlayDialogData Data { set { dialogData = value; } }

        #endregion Public interface

        #region Overridden methods from DrawableGameComponent

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // Check our controls and react to them.
            dialogData.Update();

#if DEBUG
            // Check for cheat codes.
            KeyboardState keys = Keyboard.GetState();
            if (keys.IsKeyDown(Keys.K) && keys.IsKeyDown(Keys.P))
            {
                // K + P = kill players
                foreach (var player in AssaultWing.Instance.DataEngine.Spectators)
                    if (player is Player && ((Player)player).Ship != null)
                        ((Player)player).Ship.Die(new DeathCause());
            }

            if (keys.IsKeyDown(Keys.L) && keys.IsKeyDown(Keys.E))
            {
                if (!AssaultWing.Instance.DataEngine.ProgressBar.TaskRunning)
                    AssaultWing.Instance.FinishArena();
            }
#endif

            base.Update(gameTime);
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        protected override void LoadContent()
        {
            dialogTexture = AssaultWing.Instance.Content.Load<Texture2D>("ingame_dialog");
            spriteBatch = new SpriteBatch(this.GraphicsDevice);
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        protected override void UnloadContent()
        {
            spriteBatch.Dispose();
            // Our textures and fonts are disposed by GraphicsEngine.
        }

        /// <summary>
        /// Called when the DrawableGameComponent needs to be drawn. Override this method
        /// with component-specific drawing code.
        /// </summary>
        /// <param name="gameTime">Time passed since the last call to Microsoft.Xna.Framework.DrawableGameComponent.Draw(Microsoft.Xna.Framework.GameTime).</param>
        public override void Draw(GameTime gameTime)
        {
            // Set viewport exactly to the dialog's area.
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            Rectangle screen = AssaultWing.Instance.ClientBounds;
            Viewport newViewport = gfx.Viewport;
            newViewport.X = (screen.Width - dialogTexture.Width) / 2;
            newViewport.Y = (screen.Height - dialogTexture.Height) / 2;
            newViewport.Width = dialogTexture.Width;
            newViewport.Height = dialogTexture.Height;
            gfx.Viewport = newViewport;

            // Draw contents.
            spriteBatch.Begin();
            spriteBatch.Draw(dialogTexture, Vector2.Zero, Color.White);
            spriteBatch.End();
            dialogData.Draw(spriteBatch);
        }

        #endregion Overridden methods from DrawableGameComponent
    }
}