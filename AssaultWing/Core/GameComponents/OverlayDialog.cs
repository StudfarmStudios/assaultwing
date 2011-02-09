using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core.OverlayDialogs;
using AW2.Graphics.OverlayComponents;

namespace AW2.Core.GameComponents
{
    /// <summary>
    /// Game overlay dialog. It displays text on top of the game display.
    /// </summary>
    /// Call <c>Prepare(OverlayDialogData)</c> to set up the dialog before making it visible.
    /// This sets up the visual contents of the dialog and sets which controls perform
    /// which actions.
    /// <seealso cref="OverlayDialogData"/>
    public class OverlayDialog : AWGameComponent
    {
        private AssaultWing _game;
        private SpriteBatch _spriteBatch;
        private Texture2D _dialogTexture;

        public OverlayDialog(AssaultWing game, int updateOrder)
            : base(game, updateOrder)
        {
            _game = game;
            Data = new Queue<OverlayDialogData>();
        }

        /// <summary>
        /// The dialog's actions and visual contents.
        /// </summary>
        public Queue<OverlayDialogData> Data { get; private set; }

        public override void Update()
        {
            Data.First().Update();
            base.Update();
        }

        public override void LoadContent()
        {
            _dialogTexture = Game.Content.Load<Texture2D>("ingame_dialog");
            _spriteBatch = new SpriteBatch(Game.GraphicsDeviceService.GraphicsDevice);
        }

        public override void UnloadContent()
        {
            if (_spriteBatch != null)
            {
                _spriteBatch.Dispose();
                _spriteBatch = null;
            }
        }

        public override void Draw()
        {
            // Set viewport exactly to the dialog's area.
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
            var screen = Game.GraphicsDeviceService.GraphicsDevice.Viewport;
            var oldViewport = gfx.Viewport;
            var newViewport = gfx.Viewport;
            newViewport.X = (screen.Width - _dialogTexture.Width) / 2;
            newViewport.Y = (screen.Height - _dialogTexture.Height) / 2;
            newViewport.Width = _dialogTexture.Width;
            newViewport.Height = _dialogTexture.Height;
            gfx.Viewport = newViewport;

            // Draw contents.
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _spriteBatch.Draw(_dialogTexture, Vector2.Zero, Color.White);
            _spriteBatch.End();
            Data.First().Draw(_spriteBatch);

            gfx.Viewport = oldViewport;
        }
    }
}
