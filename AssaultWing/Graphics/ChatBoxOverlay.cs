using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Graphics
{
    /// <summary>
    /// Overlay graphics component displaying a box with a player's chat and gameplay messages.
    /// The messages are stored in <c>DataEngine</c> from where this component reads them.
    /// </summary>
    public class ChatBoxOverlay : OverlayComponent
    {
        private const int VISIBLE_LINES = 5;
        private static Curve g_messageFadeoutCurve;
        private Player _player;
        private SpriteFont _chatBoxFont;

        public override Point Dimensions
        {
            get { return new Point(600, _chatBoxFont.LineSpacing * VISIBLE_LINES); }
        }

        static ChatBoxOverlay()
        {
            g_messageFadeoutCurve = new Curve();
            g_messageFadeoutCurve.Keys.Add(new CurveKey(0, 1));
            g_messageFadeoutCurve.Keys.Add(new CurveKey(2, 1));
            g_messageFadeoutCurve.Keys.Add(new CurveKey(4, 0));
            g_messageFadeoutCurve.ComputeTangents(CurveTangent.Linear);
        }

        /// <param name="player">The player whose chat messages to display.</param>
        public ChatBoxOverlay(Player player)
            : base(HorizontalAlignment.Center, VerticalAlignment.Center)
        {
            CustomAlignment = new Vector2(0, 300);
            _player = player;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            // Chat messages
            var messagePos = Vector2.Zero;
            for (int i = 0, messageI = _player.Messages.Count - 1; i < VISIBLE_LINES && messageI >= 0; ++i, --messageI, messagePos += new Vector2(0, _chatBoxFont.LineSpacing))
            {
                float alpha = g_messageFadeoutCurve.Evaluate(_player.Messages[messageI].GameTime.SecondsAgoGameTime());
                if (alpha == 0) continue;
                messagePos = new Vector2((Dimensions.X - _chatBoxFont.MeasureString(_player.Messages[messageI].Text).X) / 2, messagePos.Y);
                _player.Messages[messageI].TextColor.A = new Color(1f, 1f, 1f, alpha).A;
                spriteBatch.DrawString(_chatBoxFont, _player.Messages[messageI].Text, messagePos, _player.Messages[messageI].TextColor);
            }
        }

        public override void LoadContent()
        {
            _chatBoxFont = AssaultWing.Instance.Content.Load<SpriteFont>("MenuFontBig");
        }

        public override void UnloadContent()
        {
            // Our textures and fonts are disposed by the graphics engine.
        }
    }
}
