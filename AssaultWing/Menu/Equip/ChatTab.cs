using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Helpers;
using AW2.UI;
using AW2.Graphics;

namespace AW2.Menu.Equip
{
    public class ChatTab : EquipMenuTab
    {
        private const float CHAT_TEXT_WIDTH = 565;

        private Control _sendControl;
        private EditableText _message;

        private static Curve g_cursorBlinkCurve;
        private TimeSpan _cursorBlinkStartTime;
        private MessageBeeper _messageBeeper;

        public override Texture2D TabTexture { get { return Content.TabChatTexture; } }
        public override string HelpText { get { return "Enter sends message, " + BasicHelpText; } }
        public IEnumerable<MessageContainer.Item> Messages
        {
            get
            {
                if (ChatPlayer == null) return Enumerable.Empty<MessageContainer.Item>();
                return ChatPlayer.Messages.ReversedChat();
            }
        }
        private ReadOnlyCollection<WrappedTextList.Line> MessageLines { get { return MenuEngine.Game.DataEngine.ChatHistory[CHAT_TEXT_WIDTH]; } }

        private Player ChatPlayer { get { return MenuEngine.Game.DataEngine.Players.FirstOrDefault(plr => plr.IsLocal); } }
        private SpriteFont Font { get { return Content.FontChat; } }
        private Vector2 TypingPos { get { return StatusPanePos + new Vector2(30, Content.StatusPaneTexture.Height - 47); } }
        private Vector2 ChatHistoryPos { get { return StatusPanePos + new Vector2(30, 32); } }
        private Color CursorColor { get { return Color.Multiply(Color.White, g_cursorBlinkCurve.Evaluate((float)(MenuEngine.Game.GameTime.TotalRealTime - _cursorBlinkStartTime).TotalSeconds)); } }

        static ChatTab()
        {
            g_cursorBlinkCurve = new Curve();
            g_cursorBlinkCurve.Keys.Add(new CurveKey(0, 1));
            g_cursorBlinkCurve.Keys.Add(new CurveKey(0.5f, 0));
            g_cursorBlinkCurve.Keys.Add(new CurveKey(1, 1));
            g_cursorBlinkCurve.PreLoop = CurveLoopType.Cycle;
            g_cursorBlinkCurve.PostLoop = CurveLoopType.Cycle;
        }

        public ChatTab(EquipMenuComponent menuComponent)
            : base(menuComponent)
        {
            _sendControl = new KeyboardKey(Keys.Enter);
            _message = new EditableText("", 1000, new CharacterSet(Content.FontChat.Characters), MenuEngine.Game, () => { });
            _messageBeeper = new MessageBeeper(MenuEngine.Game, "PlayerMessage", () => Messages.FirstOrDefault());
            _cursorBlinkStartTime = MenuEngine.Game.GameTime.TotalRealTime;
        }

        public override void Update()
        {
            _message.ActivateTemporarily();
            if (_sendControl.Pulse && ChatPlayer != null)
            {
                MenuEngine.Game.SendMessageToAllPlayers(_message.Content, ChatPlayer);
                _message.Clear();
            }
            _messageBeeper.BeepOnNewMessage();
        }

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            DrawLargeStatusBackground(view, spriteBatch);
            DrawPlayerListDisplay(view, spriteBatch, drawCursor: false);
            DrawChatMessages(view, spriteBatch, ChatHistoryPos, Content.StatusPaneTexture.Height - 70);
            DrawChatTextInputBox(view, spriteBatch);
        }

        public void DrawChatMessages(Vector2 view, SpriteBatch spriteBatch, Vector2 topLeftCorner, int height)
        {
            var visibleLines = height / Font.LineSpacing;
            var messageLines = MessageLines.GetRange(MessageLines.Count - visibleLines, visibleLines);
            ModelRenderer.DrawChatLines(spriteBatch, Font, messageLines, topLeftCorner - view);
        }

        private void DrawChatTextInputBox(Vector2 view, SpriteBatch spriteBatch)
        {
            if (ChatPlayer == null) return;
            var text = GetVisibleInputString(ChatPlayer.Name + "> ", _message.Content);
            ModelRenderer.DrawBorderedText(spriteBatch, Font, text, (TypingPos - view).Round(), Color.White, 1, 1);
            spriteBatch.Draw(Content.TypingCursor, (TypingPos - view).Round() + new Vector2(Font.MeasureString(text).X + 2, -2), CursorColor);
        }

        private string GetVisibleInputString(string pretext, string text)
        {
            var visibleTextWidth = CHAT_TEXT_WIDTH - Font.MeasureString(pretext).X - Content.TypingCursor.Width;
            var visibleText = MiscHelper.FindMaxAcceptedOrMin<string>(0, text.Length,
                get: len => text.Substring(text.Length - len, len),
                accept: substr => Font.MeasureString(substr).X <= visibleTextWidth);
            return string.Format("{0}{1}", pretext, visibleText);
        }
    }
}
