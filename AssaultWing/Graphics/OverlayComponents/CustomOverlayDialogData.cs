using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.UI;

namespace AW2.Graphics.OverlayComponents
{
    /// <summary>
    /// Content and action data for an overlay dialog displaying custom data.
    /// </summary>
    public class CustomOverlayDialogData : OverlayDialogData
    {
        private string _text;
        private SpriteFont _font;

        /// <summary>
        /// Creates contents for an overlay dialog displaying arena over.
        /// </summary>
        /// <param name="text">The text to display in the dialog.</param>
        /// <param name="actions">The actions to allow in the dialog.</param>
        public CustomOverlayDialogData(string text, params TriggeredCallback[] actions)
            : base(actions)
        {
            _text = text;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
            var textCenter = new Vector2(gfx.Viewport.Width, gfx.Viewport.Height) / 2;
            var textSize = _font.MeasureString(_text);
            spriteBatch.DrawString(_font, _text, AWMathHelper.Round(textCenter - textSize / 2), Color.White);
        }

        public override void LoadContent()
        {
            _font = AssaultWingCore.Instance.Content.Load<SpriteFont>("MenuFontBig");
        }
    }
}
