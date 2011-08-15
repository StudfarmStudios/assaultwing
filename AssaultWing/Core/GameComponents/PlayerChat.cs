using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.UI;
using AW2.Net.Messages;
using AW2.Game;
using AW2.Helpers;
using AW2.Graphics;

namespace AW2.Core.GameComponents
{
    /// <summary>
    /// In-game chat functionality.
    /// </summary>
    public class PlayerChat : AWGameComponent
    {
        private AssaultWing _game;
        private Control _chatSendControl, _escapeControl;
        private EditableText _message;
        private SpriteBatch _spriteBatch;
        private SpriteFont _typingFont;

        private Color TypingColor { get { return Color.White; } }
        private bool IsTyping { get { return _message != null; } }
        private Player ChatPlayer { get { return Game.DataEngine.Players.First(plr => !plr.IsRemote); } }

        private static Curve g_cursorBlinkCurve;
        private TimeSpan _cursorBlinkStartTime;
        private Texture2D _typeLineCursorTexture;
       // private IEnumerable<Control> _additionalExclusiveKeys;

        private Texture2D _chatBackgroundTexture;
        private Texture2D _typeLineBackgroundTexture;
        private Texture2D _scrollArrowUpTexture;
        private Texture2D _scrollArrowDownTexture;
        private Texture2D _scrollArrowGlowTexture;
        private Texture2D _scrollTrackTexture;
        private Texture2D _scrollMarkerTexture;

        private static Curve g_scrollArrowBlinkCurve;
        private TimeSpan _scrollArrowGlowStartTime;

        public PlayerChat(AssaultWing game, int updateOrder)
            : base(game, updateOrder)
        {
            g_cursorBlinkCurve = new Curve();
            g_cursorBlinkCurve.Keys.Add(new CurveKey(0, 1));
            g_cursorBlinkCurve.Keys.Add(new CurveKey(0.5f, 0));
            g_cursorBlinkCurve.Keys.Add(new CurveKey(1, 1));
            g_cursorBlinkCurve.PreLoop = CurveLoopType.Cycle;
            g_cursorBlinkCurve.PostLoop = CurveLoopType.Cycle;

            g_scrollArrowBlinkCurve = new Curve();
            g_scrollArrowBlinkCurve.Keys.Add(new CurveKey(0, 1));
            g_scrollArrowBlinkCurve.Keys.Add(new CurveKey(0.75f, 0));
            g_scrollArrowBlinkCurve.Keys.Add(new CurveKey(1.5f, 1));
            g_scrollArrowBlinkCurve.PreLoop = CurveLoopType.Cycle;
            g_scrollArrowBlinkCurve.PostLoop = CurveLoopType.Cycle;

            _game = game;
            _chatSendControl = new KeyboardKey(Keys.Enter);
            _escapeControl = new KeyboardKey(Keys.Escape);
            _cursorBlinkStartTime = _game.GameTime.TotalRealTime;
            _scrollArrowGlowStartTime = _game.GameTime.TotalRealTime;
        }

        /*
        public void SetAdditionalExclusiveKeys(IEnumerable<Control> keys)
        {
            _additionalExclusiveKeys = keys;
        }*/

        public override void LoadContent()
        {
            Game.GraphicsDeviceService.CheckThread();
            _spriteBatch = new SpriteBatch(Game.GraphicsDeviceService.GraphicsDevice);
            _typingFont = Game.Content.Load<SpriteFont>("ChatFont");
            _typeLineCursorTexture = Game.Content.Load<Texture2D>("gui_chat_typeline_cursor");
            _chatBackgroundTexture = Game.Content.Load<Texture2D>("gui_chat_bg");
            _typeLineBackgroundTexture = Game.Content.Load<Texture2D>("gui_chat_typeline_bg");

            _scrollArrowUpTexture = Game.Content.Load<Texture2D>("gui_chat_scroll_arrow_up");
            _scrollArrowDownTexture = Game.Content.Load<Texture2D>("gui_chat_scroll_arrow_down");
            _scrollTrackTexture = Game.Content.Load<Texture2D>("gui_chat_scroll_track");
            _scrollArrowGlowTexture = Game.Content.Load<Texture2D>("gui_chat_scroll_arrow_glow");
            _scrollMarkerTexture = Game.Content.Load<Texture2D>("gui_chat_scroll_marker");
        }

        public override void Update()
        {
            if (IsTyping)
            {
                if (_chatSendControl.Pulse && ChatPlayer != null)
                {
                    _game.SendMessageToAllPlayers(_message.Content, ChatPlayer);
                    StopWritingMessage();
                }
                else if (_escapeControl.Pulse) StopWritingMessage();
            }
            else
            {
                if (_game.ChatStartControl.Pulse) StartWritingMessage();
            }
        }

        public override void Draw()
        {
            Game.GraphicsDeviceService.CheckThread();
            _typingFont.LineSpacing = 15;

            Color backgroundColor = Color.White;

            if (!IsTyping)
                backgroundColor = Color.FromNonPremultiplied(new Vector4(1, 1, 1, 0.7f));

            _spriteBatch.Begin();

            var viewport = Game.GraphicsDeviceService.GraphicsDevice.Viewport;
            var chatBoxPos = new Vector2(viewport.Width - _chatBackgroundTexture.Width, viewport.Height - _chatBackgroundTexture.Height);

            if (!IsTyping)
                chatBoxPos = new Vector2(viewport.Width - _chatBackgroundTexture.Width, viewport.Height - 100);

            _spriteBatch.Draw(_chatBackgroundTexture, chatBoxPos, backgroundColor);

            if (IsTyping)
            {
                Vector2 typeLinePos = chatBoxPos + new Vector2((_chatBackgroundTexture.Width - _typeLineBackgroundTexture.Width) / 2, _chatBackgroundTexture.Height - _typeLineBackgroundTexture.Height - 2);
                _spriteBatch.Draw(_typeLineBackgroundTexture, typeLinePos.Round(), Color.White);

                Vector2 scrollPos = chatBoxPos + new Vector2(_chatBackgroundTexture.Width - 20, 0);
                Color arrowGlowColor = Color.FromNonPremultiplied(new Vector4(1, 1, 1, g_scrollArrowBlinkCurve.Evaluate((float)(_game.GameTime.TotalRealTime - _scrollArrowGlowStartTime).TotalSeconds)));
                _spriteBatch.Draw(_scrollArrowGlowTexture, scrollPos + new Vector2(-12, 1), arrowGlowColor);
                _spriteBatch.Draw(_scrollArrowGlowTexture, scrollPos + new Vector2(-12, 216), arrowGlowColor);
                _spriteBatch.Draw(_scrollArrowUpTexture, scrollPos + new Vector2(0, 13), Color.White);
                _spriteBatch.Draw(_scrollArrowDownTexture, scrollPos + new Vector2(0, 230), Color.White);
                _spriteBatch.Draw(_scrollTrackTexture, scrollPos + new Vector2(0, 24), Color.White);
                _spriteBatch.Draw(_scrollMarkerTexture, scrollPos + new Vector2(-4, 100), Color.White);
            }

            var messageY = 7;
            var messageLines = IsTyping ? 16 : 6;

            foreach (var item in ChatPlayer.Messages.Reversed().Take(messageLines).Reverse())
            {
                var preTextSize = _typingFont.MeasureString(item.Message.PreText);
                var textSize = _typingFont.MeasureString(item.Message.Text);
                var preTextPos = chatBoxPos + new Vector2(11, messageY);
                var textPos = preTextPos + new Vector2(preTextSize.X, 0);

                if (preTextSize.X > 2)
                    textPos += new Vector2(4, 0);

                ModelRenderer.DrawBorderedText(_spriteBatch, _typingFont, item.Message.PreText, preTextPos.Round(), PlayerMessage.PRETEXT_COLOR, 1, 1);
                ModelRenderer.DrawBorderedText(_spriteBatch, _typingFont, item.Message.Text, textPos.Round(), item.Message.TextColor, 1, 1);
                //_spriteBatch.DrawString(_typingFont, item.Message.PreText, preTextPos.Round(), PlayerMessage.PRETEXT_COLOR);
                //_spriteBatch.DrawString(_typingFont, item.Message.Text, textPos.Round(), item.Message.TextColor);
                messageY += _typingFont.LineSpacing;
            }


            if (IsTyping)
            {
                var chatName = ChatPlayer != null ? ChatPlayer.Name : "???";
                var text = string.Format("{0}>{1}", chatName, _message.Content);

                Color cursorColor = Color.FromNonPremultiplied(new Vector4(1, 1, 1, g_cursorBlinkCurve.Evaluate((float)(_game.GameTime.TotalRealTime - _cursorBlinkStartTime).TotalSeconds)));

                _spriteBatch.DrawString(_typingFont, text, GetTypingPos(text).Round(), TypingColor);
                _spriteBatch.Draw(_typeLineCursorTexture, GetTypingPos(text).Round() + new Vector2(_typingFont.MeasureString(text).X + 2, -2), cursorColor);
            }

            _spriteBatch.End();
        }

        protected override void EnabledOrVisibleChanged()
        {
            if ((!Enabled || !Visible) && IsTyping) StopWritingMessage();
        }

        private Vector2 GetTypingPos(string text)
        {
            var viewport = Game.GraphicsDeviceService.GraphicsDevice.Viewport;
            var textSize = _typingFont.MeasureString(text);
            //var textY = Math.Min(viewport.Height / 2 + 300 - _typingFont.LineSpacing * 3, viewport.Height - textSize.Y);
            var textY = viewport.Height - 20;
            var textX = viewport.Width - 471;
           // return new Vector2(viewport.Width / 2, textY) - textSize / 2;
            return new Vector2(textX, textY);
        }

        private void StartWritingMessage()
        {
            if (_message != null) throw new InvalidOperationException("Already writing a message");
            IEnumerable<Control> exclusiveControls = new[] { _chatSendControl, _escapeControl };
            //if (_additionalExclusiveKeys != null)
             //   exclusiveControls = exclusiveControls.Concat(_additionalExclusiveKeys);
            _game.UIEngine.PushExclusiveControls(exclusiveControls);
            _message = new EditableText("", 40, _game, () => { }) { IsActive = true };
        }

        private void StopWritingMessage()
        {
            _message.Dispose();
            _message = null;
            _game.UIEngine.PopExclusiveControls();
        }
    }
}
