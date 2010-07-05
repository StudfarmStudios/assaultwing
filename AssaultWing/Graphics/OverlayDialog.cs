using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Graphics.OverlayComponents;
using AW2.UI;

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
        private OverlayDialogData _dialogData;
        private SpriteBatch _spriteBatch;
        private Texture2D _dialogTexture;

        public OverlayDialog(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
        }

        #region Public interface

        /// <summary>
        /// The dialog's actions and visual contents.
        /// </summary>
        public OverlayDialogData Data { set { _dialogData = value; } }

        #endregion Public interface

        #region Overridden methods from DrawableGameComponent

        public override void Update(GameTime gameTime)
        {
            // Check our controls and react to them.
            _dialogData.Update();

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

        protected override void LoadContent()
        {
            _dialogTexture = AssaultWing.Instance.Content.Load<Texture2D>("ingame_dialog");
            _spriteBatch = new SpriteBatch(this.GraphicsDevice);
        }

        protected override void UnloadContent()
        {
            if (_spriteBatch != null)
            {
                _spriteBatch.Dispose();
                _spriteBatch = null;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            // Set viewport exactly to the dialog's area.
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            Rectangle screen = AssaultWing.Instance.ClientBounds;
            Viewport newViewport = gfx.Viewport;
            newViewport.X = (screen.Width - _dialogTexture.Width) / 2;
            newViewport.Y = (screen.Height - _dialogTexture.Height) / 2;
            newViewport.Width = _dialogTexture.Width;
            newViewport.Height = _dialogTexture.Height;
            gfx.Viewport = newViewport;

            // Draw contents.
            _spriteBatch.Begin();
            _spriteBatch.Draw(_dialogTexture, Vector2.Zero, Color.White);
            _spriteBatch.End();
            _dialogData.Draw(_spriteBatch);
        }

        #endregion Overridden methods from DrawableGameComponent
    }
}
