using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;
using System;


namespace AW2.Graphics.OverlayComponents
{
    class ChatLogOverlay : OverlayComponent
    {
        // Textures
        private Texture2D _chatBackgroundTexture;
        private Texture2D _typeLineBackgroundTexture;
        private Texture2D _scrollArrowUpTexture;
        private Texture2D _scrollArrowDownTexture;
        private Texture2D _scrollArrowGlowTexture;
        private Texture2D _scrollTrackTexture;
        private Texture2D _scrollMarkerTexture;

        private SpriteFont _textFont;
       
        private static Curve g_scrollArrowBlinkCurve;
        
        private TimeSpan _scrollArrowGlowStartTime;
        private Player _player;

        public override Point Dimensions
        {
            get { return new Point(_chatBackgroundTexture.Width, _chatBackgroundTexture.Height); }
        }

        public ChatLogOverlay(PlayerViewport viewport)
            : base(viewport, HorizontalAlignment.Right, VerticalAlignment.Bottom)
        {
            _player = viewport.Player;
            
            _scrollArrowGlowStartTime = _player.Game.GameTime.TotalRealTime;
        }

        static ChatLogOverlay()
        {
            g_scrollArrowBlinkCurve = new Curve();
            g_scrollArrowBlinkCurve.Keys.Add(new CurveKey(0, 1));
            g_scrollArrowBlinkCurve.Keys.Add(new CurveKey(0.75f, 0));
            g_scrollArrowBlinkCurve.Keys.Add(new CurveKey(1.5f, 1));
            g_scrollArrowBlinkCurve.PreLoop = CurveLoopType.Cycle;
            g_scrollArrowBlinkCurve.PostLoop = CurveLoopType.Cycle;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            _textFont.LineSpacing = 15;

            spriteBatch.Draw(_chatBackgroundTexture, Vector2.Zero, Color.White);
            spriteBatch.DrawString(_textFont, "16 Lines of text\nKokeillaanpa vähän kirjottaa tällä\njotkut kirjaimet VoI oLLa RuMia", Vector2.Zero + new Vector2(11, 7), Color.White);
            
            Vector2 typeLinePos = new Vector2((_chatBackgroundTexture.Width - _typeLineBackgroundTexture.Width) / 2, _chatBackgroundTexture.Height - _typeLineBackgroundTexture.Height - 2);
            spriteBatch.Draw(_typeLineBackgroundTexture, typeLinePos.Round(), Color.White);

            Vector2 scrollPos = new Vector2(_chatBackgroundTexture.Width - 20, 0);
            Color arrowGlowColor = Color.FromNonPremultiplied(new Vector4(1, 1, 1, g_scrollArrowBlinkCurve.Evaluate((float)(_player.Game.GameTime.TotalRealTime - _scrollArrowGlowStartTime).TotalSeconds)));
            spriteBatch.Draw(_scrollArrowGlowTexture, scrollPos + new Vector2(-12, 1), arrowGlowColor);
            spriteBatch.Draw(_scrollArrowGlowTexture, scrollPos + new Vector2(-12, typeLinePos.Y - 31), arrowGlowColor);
            spriteBatch.Draw(_scrollArrowUpTexture, scrollPos + new Vector2(0, 13), Color.White);
            spriteBatch.Draw(_scrollArrowDownTexture, scrollPos + new Vector2(0, typeLinePos.Y - 17), Color.White);
            spriteBatch.Draw(_scrollTrackTexture, scrollPos + new Vector2(0, 24), Color.White);
            spriteBatch.Draw(_scrollMarkerTexture, scrollPos + new Vector2(-4, 100), Color.White);

        }

        public override void LoadContent()
        {
            AssaultWingCore.Instance.GraphicsDeviceService.CheckThread();
            var content = AssaultWingCore.Instance.Content;

            _chatBackgroundTexture = content.Load<Texture2D>("gui_chat_bg");
            _typeLineBackgroundTexture = content.Load<Texture2D>("gui_chat_typeline_bg");

            _scrollArrowUpTexture = content.Load<Texture2D>("gui_chat_scroll_arrow_up");
            _scrollArrowDownTexture = content.Load<Texture2D>("gui_chat_scroll_arrow_down");
            _scrollTrackTexture = content.Load<Texture2D>("gui_chat_scroll_track");
            _scrollArrowGlowTexture = content.Load<Texture2D>("gui_chat_scroll_arrow_glow");
            _scrollMarkerTexture = content.Load<Texture2D>("gui_chat_scroll_marker");

            _textFont = content.Load<SpriteFont>("ChatFont");
        }
    }
}
