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
    public class CombatLogOverlay : OverlayComponent
    {
        private const float SHADOW_THICKNESS = 2;
        private const int VISIBLE_LINES = 5;
        private static Curve g_messageFadeoutCurve;
        private Player _player;
        private SpriteFont _chatBoxFont;

        public override Point Dimensions
        {
            get
            {
                var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
                return new Point(gfx.Viewport.Width, (int)(_chatBoxFont.LineSpacing * VISIBLE_LINES + 2 * SHADOW_THICKNESS));
            }
        }

        static CombatLogOverlay()
        {
            g_messageFadeoutCurve = new Curve();
            g_messageFadeoutCurve.Keys.Add(new CurveKey(0, 1));
            g_messageFadeoutCurve.Keys.Add(new CurveKey(2, 1));
            g_messageFadeoutCurve.Keys.Add(new CurveKey(4, 0));
            g_messageFadeoutCurve.ComputeTangents(CurveTangent.Linear);
        }

        /// <param name="player">The player whose chat messages to display.</param>
        public CombatLogOverlay(PlayerViewport viewport)
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
            _player.Messages.NewMessage += HandleNewPlayerMessage;
        }

        public override void LoadContent()
        {
            AssaultWingCore.Instance.GraphicsDeviceService.CheckThread();
            _chatBoxFont = AssaultWingCore.Instance.Content.Load<SpriteFont>("MenuFontBig");
        }

        public override void Dispose()
        {
            _player.Messages.NewMessage -= HandleNewPlayerMessage;
            base.Dispose();
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            var messageY = SHADOW_THICKNESS;
            foreach (var item in _player.Messages.ReversedCombatLog().Take(VISIBLE_LINES))
            {
                float alpha = GetMessageAlpha(item);
                if (alpha == 0) continue;
                var preTextSize = _chatBoxFont.MeasureString(item.Message.PreText);
                var textSize = _chatBoxFont.MeasureString(item.Message.Text);
                var preTextPos = new Vector2((Dimensions.X - textSize.X - preTextSize.X) / 2, messageY);
                var textPos = preTextPos + new Vector2(preTextSize.X, 0);
                ModelRenderer.DrawBorderedText(spriteBatch, _chatBoxFont, item.Message.PreText, preTextPos.Round(), PlayerMessage.PRETEXT_COLOR, alpha, SHADOW_THICKNESS);
                ModelRenderer.DrawBorderedText(spriteBatch, _chatBoxFont, item.Message.Text, textPos.Round(), item.Message.TextColor, alpha, SHADOW_THICKNESS);
                messageY += _chatBoxFont.LineSpacing;
            }
        }

        private float GetMessageAlpha(MessageContainer.Item item)
        {
            return g_messageFadeoutCurve.Evaluate(item.EntryRealTime.SecondsAgoRealTime());
        }

        private void HandleNewPlayerMessage(PlayerMessage message)
        {
            _player.Game.SoundEngine.PlaySound("PlayerMessage");
        }
    }
}
