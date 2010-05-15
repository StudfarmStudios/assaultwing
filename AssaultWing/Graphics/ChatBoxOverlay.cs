using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;

namespace AW2.Graphics
{
    /// <summary>
    /// Overlay graphics component displaying a box with a player's chat and gameplay messages.
    /// The messages are stored in <c>DataEngine</c> from where this component reads them.
    /// </summary>
    public class ChatBoxOverlay : OverlayComponent
    {
        private const int VISIBLE_LINES = 5;
        private Player _player;
        private SpriteFont _chatBoxFont;

        public override Point Dimensions
        {
            get { return new Point(500, _chatBoxFont.LineSpacing * VISIBLE_LINES); }
        }

        /// <param name="player">The player whose chat messages to display.</param>
        public ChatBoxOverlay(Player player)
            : base(HorizontalAlignment.Center, VerticalAlignment.Center)
        {
            CustomAlignment = new Vector2(0, 200);
            _player = player;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            // Chat messages
            var messagePos = Vector2.Zero;
            for (int i = 0, messageI = _player.Messages.Count - 1; i < VISIBLE_LINES && messageI >= 0; ++i, --messageI, messagePos += new Vector2(0, _chatBoxFont.LineSpacing))
                spriteBatch.DrawString(_chatBoxFont, _player.Messages[messageI], messagePos, new Color(1f, 1f, 1f, 1f - i / (float)VISIBLE_LINES));
        }

        public override void LoadContent()
        {
            _chatBoxFont = AssaultWing.Instance.Content.Load<SpriteFont>("ConsoleFont");
        }

        public override void UnloadContent()
        {
            // Our textures and fonts are disposed by the graphics engine.
        }
    }
}
