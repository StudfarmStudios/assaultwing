using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core.OverlayComponents;

namespace AW2.Core.GameComponents
{
    /// <summary>
    /// Game overlay dialog. It displays text on top of the game display.
    /// </summary>
    /// <seealso cref="OverlayDialogData"/>
    public class OverlayDialog : AWGameComponent
    {
        private SpriteBatch _spriteBatch;
        private Texture2D _dialogTexture;
        private Queue<OverlayDialogData> _data;

        public new AssaultWing Game { get; private set; }

        public OverlayDialog(AssaultWing game, int updateOrder)
            : base(game, updateOrder)
        {
            Game = game;
            _data = new Queue<OverlayDialogData>();
        }

        public void Show(OverlayDialogData dialogData)
        {
            _data.Enqueue(dialogData);
            if (!Enabled)
            {
                Game.UIEngine.PushExclusiveControls(dialogData.Controls);
                Game.SoundEngine.PlaySound("EscPause");
            }
            Enabled = Visible = true;
        }

        public void Dismiss()
        {
            _data.Dequeue();
            Game.UIEngine.PopExclusiveControls();
            if (_data.Any())
            {
                Game.UIEngine.PushExclusiveControls(_data.First().Controls);
                Game.SoundEngine.PlaySound("EscPause");
            }
            else
            {
                Enabled = Visible = false;
            }
        }

        public override void Update()
        {
            _data.First().Update();
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
            _data.First().Draw(_spriteBatch);

            gfx.Viewport = oldViewport;
        }
    }
}
