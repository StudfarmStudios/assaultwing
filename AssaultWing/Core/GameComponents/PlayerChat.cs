using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.UI;
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
        private const int SCROLL_MARKER_POSITION_MIN = 25;
        private const int SCROLL_MARKER_POSITION_MAX = 208;

        private AssaultWing _game;
        private Control _chatSendControl, _escapeControl, _scrollUpControl, _scrollDownControl;
        private EditableText _message;
        private SpriteBatch _spriteBatch;
        private MessageBeeper _messageBeeper;

        private static Curve g_cursorBlinkCurve;
        private static Curve g_scrollArrowBlinkCurve;
        private static char[] g_pretextSplitChars = new[] { '>' };

        private Texture2D _chatBackgroundTexture;
        private Texture2D _typeLineBackgroundTexture, _typeLineCursorTexture;
        private Texture2D _scrollArrowUpTexture, _scrollArrowDownTexture, _scrollArrowGlowTexture, _scrollTrackTexture, _scrollMarkerTexture;

        private TimeSpan _scrollArrowGlowStartTime;
        private TimeSpan _cursorBlinkStartTime;
        private int _scrollPosition;

        private Viewport Viewport { get { return Game.GraphicsDeviceService.GraphicsDevice.Viewport; } }
        private Vector2 TopLeftCorner { get { return new Vector2(Viewport.Width - ChatAreaWidth, Viewport.Height - ChatAreaHeight); } }
        private Vector2 TypingPos { get { return TopLeftCorner + new Vector2(12, ChatAreaHeight - 20).Round(); } }
        private float ChatAreaWidth { get { return _chatBackgroundTexture.Width; } }
        private float ChatAreaHeight { get { return IsTyping ? _chatBackgroundTexture.Height : _chatBackgroundTexture.Height - 172; } }
        private float ChatTextWidth { get { return ChatAreaWidth - 22; } }
        private int VisibleLines { get { return (int)ChatAreaHeight / ChatFont.LineSpacing - (IsTyping ? 2 : 0); } }
        private SpriteFont ChatFont { get { return Game.GraphicsEngine.GameContent.ChatFont; } }
        private float CursorAlpha { get { return g_cursorBlinkCurve.Evaluate((float)(_game.GameTime.TotalRealTime - _cursorBlinkStartTime).TotalSeconds); } }
        private Color TypingColor { get { return Color.White; } }
        private Color ChatBackgroundColor { get { return IsTyping ? Color.White : Color.Multiply(Color.White, 0.7f); } }
        private Color ArrowUpColor { get { return CanScrollUp ? Color.White : Color.Multiply(Color.White, 0.2f); } }
        private Color ArrowDownColor { get { return CanScrollDown ? Color.White : Color.Multiply(Color.White, 0.2f); } }
        private Player ChatPlayer { get { return Game.DataEngine.ChatPlayer; } }
        private IEnumerable<MessageContainer.Item> Messages { get { return ChatPlayer.Messages.ReversedChat(); } }
        private ReadOnlyCollection<WrappedTextList.Line> MessageLines { get { return _game.DataEngine.ChatHistory[ChatTextWidth]; } }

        private bool IsTyping { get { return _message != null; } }
        private bool IsScrollable { get { return MessageLines.Count > VisibleLines; } }
        private bool CanScrollUp { get { return _scrollPosition < MessageLines.Count - VisibleLines; } }
        private bool CanScrollDown { get { return _scrollPosition > 0; } }

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
            _scrollUpControl = new KeyboardKey(Keys.Up);
            _scrollDownControl = new KeyboardKey(Keys.Down);
            _cursorBlinkStartTime = _game.GameTime.TotalRealTime;
            _scrollArrowGlowStartTime = _game.GameTime.TotalRealTime;
            _messageBeeper = new MessageBeeper(game, "PlayerMessage", () => Messages.FirstOrDefault());
        }

        public override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(Game.GraphicsDeviceService.GraphicsDevice);
            _chatBackgroundTexture = Game.Content.Load<Texture2D>("gui_chat_bg");
            _typeLineCursorTexture = Game.Content.Load<Texture2D>("gui_chat_typeline_cursor");
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
                if (_scrollUpControl.Force > 0) ScrollUp();
                if (_scrollDownControl.Force > 0) ScrollDown();
                if (_escapeControl.Pulse) StopWritingMessage();
                else if (_chatSendControl.Pulse && ChatPlayer != null)
                {
                    _game.SendMessageToAllPlayers(_message.Content, ChatPlayer);
                    StopWritingMessage();
                }
            }
            else
            {
                if (_game.ChatStartControl.Pulse) StartWritingMessage();
            }
            _messageBeeper.BeepOnNewMessage();
        }

        public override void Draw()
        {
            _spriteBatch.Begin();
            _spriteBatch.Draw(_chatBackgroundTexture, TopLeftCorner, ChatBackgroundColor);
            DrawTypingBox();
            var textPos = TopLeftCorner + new Vector2(11, 7);
            foreach (var line in MessageLines.GetRange(Math.Max(0, MessageLines.Count - VisibleLines - _scrollPosition), Math.Min(VisibleLines, MessageLines.Count)))
            {
                if (line.ContainsPretext)
                {
                    var splitIndex = line.Text.IndexOf('>');
                    if (splitIndex < 0) throw new ApplicationException("Pretext char not found");
                    var pretext = line.Text.Substring(0, splitIndex + 1);
                    var properText = line.Text.Substring(splitIndex + 1);
                    ModelRenderer.DrawBorderedText(_spriteBatch, ChatFont, pretext, textPos.Round(), Color.White, 1, 1);
                    var properPos = textPos + new Vector2(ChatFont.MeasureString(pretext).X, 0);
                    ModelRenderer.DrawBorderedText(_spriteBatch, ChatFont, properText, properPos.Round(), line.Color, 1, 1);
                }
                else
                    ModelRenderer.DrawBorderedText(_spriteBatch, ChatFont, line.Text, textPos.Round(), line.Color, 1, 1);
                textPos.Y += ChatFont.LineSpacing;
            }
            _spriteBatch.End();
        }

        protected override void EnabledOrVisibleChanged()
        {
            if ((!Enabled || !Visible) && IsTyping) StopWritingMessage();
        }

        private void DrawTypingBox()
        {
            if (!IsTyping) return;
            var typeLinePos = TopLeftCorner + new Vector2((_chatBackgroundTexture.Width - _typeLineBackgroundTexture.Width) / 2, _chatBackgroundTexture.Height - _typeLineBackgroundTexture.Height - 2);
            var scrollPos = TopLeftCorner + new Vector2(_chatBackgroundTexture.Width - 20, 0);
            var arrowGlowColor = Color.Multiply(Color.White, g_scrollArrowBlinkCurve.Evaluate((float)(_game.GameTime.TotalRealTime - _scrollArrowGlowStartTime).TotalSeconds));

            if (CanScrollUp) _spriteBatch.Draw(_scrollArrowGlowTexture, scrollPos + new Vector2(-12, 1), arrowGlowColor);
            _spriteBatch.Draw(_scrollArrowUpTexture, scrollPos + new Vector2(0, 13), ArrowUpColor);

            if (CanScrollDown) _spriteBatch.Draw(_scrollArrowGlowTexture, scrollPos + new Vector2(-12, 216), arrowGlowColor);
            _spriteBatch.Draw(_scrollArrowDownTexture, scrollPos + new Vector2(0, 230), ArrowDownColor);

            _spriteBatch.Draw(_typeLineBackgroundTexture, typeLinePos.Round(), null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            _spriteBatch.Draw(_scrollTrackTexture, scrollPos + new Vector2(0, 24), Color.White);

            if (IsScrollable) _spriteBatch.Draw(_scrollMarkerTexture, scrollPos + new Vector2(-4, GetScrollMarkerYPosition()), Color.White);

            // Draw typeline text
            var chatName = ChatPlayer != null ? ChatPlayer.Name : "???";
            var text = string.Format("{0}>{1}", chatName, _message.Content);
            var cursorColor = Color.Multiply(Color.White, CursorAlpha);
            _spriteBatch.DrawString(ChatFont, text, TypingPos, TypingColor);
            _spriteBatch.Draw(_typeLineCursorTexture, TypingPos + new Vector2(ChatFont.MeasureString(text).X + 2, -2), cursorColor);
        }

        private void StartWritingMessage()
        {
            if (_message != null) throw new InvalidOperationException("Already writing a message");
            IEnumerable<Control> exclusiveControls = new[] { _chatSendControl, _escapeControl, _scrollUpControl, _scrollDownControl };
            _game.UIEngine.PushExclusiveControls(exclusiveControls);
            _message = new EditableText("", 1000, new CharacterSet(ChatFont.Characters), _game, () => { }) { IsActive = true };
        }

        private void StopWritingMessage()
        {
            _message.Dispose();
            _message = null;
            _scrollPosition = 0;
            _game.UIEngine.PopExclusiveControls();
        }

        private void ScrollUp()
        {
            if (CanScrollUp) _scrollPosition++;
        }

        private void ScrollDown()
        {
            if (CanScrollDown) _scrollPosition--;
        }

        private float GetScrollMarkerYPosition()
        {
            var lineToPixelRatio = (SCROLL_MARKER_POSITION_MAX - SCROLL_MARKER_POSITION_MIN) / (float)(MessageLines.Count - VisibleLines);
            var scrollAmountInPixels = _scrollPosition * lineToPixelRatio;
            return MathHelper.Clamp(SCROLL_MARKER_POSITION_MAX - scrollAmountInPixels, SCROLL_MARKER_POSITION_MIN, SCROLL_MARKER_POSITION_MAX);
        }
    }
}
