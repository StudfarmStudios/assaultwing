using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Game.Players;
using AW2.Helpers;

namespace AW2.Graphics.OverlayComponents
{
    /// <summary>
    /// Overlay graphics component displaying a box with a player's chat and gameplay messages.
    /// The messages are stored in <c>DataEngine</c> from where this component reads them.
    /// </summary>
    public class CombatLogOverlay : OverlayComponent
    {
        private const float SHADOW_THICKNESS = 2;
        private const int VISIBLE_LINES = 5;
        private static Curve g_messageFadeoutCurve;
        private Player _player;
        private SpriteFont _chatBoxFont;
        private MessageBeeper _messageBeeper;

        public override Point Dimensions
        {
            get
            {
                var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
                return new Point(gfx.Viewport.Width, (int)(_chatBoxFont.LineSpacing * VISIBLE_LINES + 2 * SHADOW_THICKNESS));
            }
        }

        private IEnumerable<MessageContainer.Item> Messages { get { return _player.Messages.ReversedCombatLog(); } }

        static CombatLogOverlay()
        {
            g_messageFadeoutCurve = new Curve();
            g_messageFadeoutCurve.Keys.Add(new CurveKey(0, 1));
            g_messageFadeoutCurve.Keys.Add(new CurveKey(3.5f, 1));
            g_messageFadeoutCurve.Keys.Add(new CurveKey(5.2f, 0));
            g_messageFadeoutCurve.ComputeTangents(CurveTangent.Linear);
        }

        /// <param name="player">The player whose chat messages to display.</param>
        public CombatLogOverlay(PlayerViewport viewport)
            : base(viewport, HorizontalAlignment.Center, VerticalAlignment.Center)
        {
            CustomAlignment = () => new Vector2(0, 300);
            _player = viewport.Owner;
            _messageBeeper = new MessageBeeper(_player.Game, "playerMessage", () => Messages.FirstOrDefault());
        }

        public override void LoadContent()
        {
            _chatBoxFont = AssaultWingCore.Instance.Content.Load<SpriteFont>("MenuFontBig");
        }

        public override void Update()
        {
            base.Update();
            _messageBeeper.BeepOnNewMessage();
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            var messageY = SHADOW_THICKNESS;
            foreach (var item in Messages.Take(VISIBLE_LINES))
            {
                float alpha = GetMessageAlpha(item);
                if (alpha == 0) continue;
                var preTextSize = _chatBoxFont.MeasureString(item.Message.PreText);
                var textSize = _chatBoxFont.MeasureString(item.Message.Text);
                var preTextPos = new Vector2((Dimensions.X - textSize.X - preTextSize.X) / 2, messageY);
                var textPos = preTextPos + new Vector2(preTextSize.X, 0);
                ModelRenderer.DrawBorderedText(spriteBatch, _chatBoxFont, item.Message.PreText, Vector2.Round(preTextPos), PlayerMessage.PRETEXT_COLOR, alpha, SHADOW_THICKNESS);
                ModelRenderer.DrawBorderedText(spriteBatch, _chatBoxFont, item.Message.Text, Vector2.Round(textPos), item.Message.TextColor, alpha, SHADOW_THICKNESS);
                messageY += _chatBoxFont.LineSpacing;
            }
        }

        private float GetMessageAlpha(MessageContainer.Item item)
        {
            return g_messageFadeoutCurve.Evaluate(item.EntryRealTime.SecondsAgoRealTime());
        }
    }
}
