using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Menu;
using AW2.UI;

namespace AW2.Core.OverlayComponents
{
    /// <summary>
    /// Content and action data for an overlay dialog displaying custom data.
    /// </summary>
    public class CustomOverlayDialogData : OverlayDialogData
    {
        private string _text;

        /// <summary>
        /// Creates contents for an overlay dialog displaying arena over.
        /// </summary>
        /// <param name="text">The text to display in the dialog.</param>
        /// <param name="actions">The actions to allow in the dialog.</param>
        public CustomOverlayDialogData(MenuEngineImpl menu, string text, params TriggeredCallback[] actions)
            : base(menu, actions)
        {
            _text = text;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
            var font = Menu.MenuContent.FontBig;
            var textCenter = new Vector2(gfx.Viewport.Width, gfx.Viewport.Height) / 2;
            var textSize = font.MeasureString(_text);
            spriteBatch.DrawString(font, _text, Vector2.Round(textCenter - textSize / 2), Color.White);
        }
    }
}
