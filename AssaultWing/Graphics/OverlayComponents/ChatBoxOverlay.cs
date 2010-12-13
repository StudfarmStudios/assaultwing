using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Graphics.OverlayComponents
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
        public ChatBoxOverlay(PlayerViewport viewport)
            : base(viewport, HorizontalAlignment.Center, VerticalAlignment.Center)
        {
            CustomAlignment = new Vector2(0, 300);
            _player = viewport.Player;
            // WARNING !!! Attaching this handler to Player.NewMessage may cause a memory leak
            // in the following situation: Screen is resized which triggers recreation of viewports,
            // including ChatBoxOverlay. Someone forgets to call Dispose on the AWViewport and
            // consequently Dispose is not called on ChatBoxOverlay. Then the old ChatBoxOverlay
            // instance will not be garbage collected because Player.NewMessage is still holding
            // a reference to it. This does not happen as of 2010-12-12 but future code changes
            // may introduce a memory leak. A good permanent fix would be to use the weak event
            // pattern from WPF in Player.NewMessage.
            _player.NewMessage += HandleNewPlayerMessage;
        }

        public override void LoadContent()
        {
            base.LoadContent();
            _chatBoxFont = AssaultWingCore.Instance.Content.Load<SpriteFont>("MenuFontBig");
        }

        public override void Dispose()
        {
            _player.NewMessage -= HandleNewPlayerMessage;
            base.Dispose();
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            var messagePos = Vector2.Zero;
            for (int i = 0, messageI = _player.Messages.Count - 1; i < VISIBLE_LINES && messageI >= 0; ++i, --messageI, messagePos += new Vector2(0, _chatBoxFont.LineSpacing))
            {
                float alpha = GetMessageAlpha(messageI);
                if (alpha == 0) continue;
                messagePos = new Vector2((Dimensions.X - _chatBoxFont.MeasureString(_player.Messages[messageI].Text).X) / 2, messagePos.Y);
                var color = Color.Multiply(_player.Messages[messageI].TextColor, alpha);
                spriteBatch.DrawString(_chatBoxFont, _player.Messages[messageI].Text, messagePos, color);
            }
        }

        private float GetMessageAlpha(int messageIndex)
        {
            return g_messageFadeoutCurve.Evaluate(_player.Messages[messageIndex].GameTime.SecondsAgoGameTime());
        }

        private void HandleNewPlayerMessage(Player.Message message)
        {
            _player.Game.SoundEngine.PlaySound("PlayerMessage");
        }
    }
}
