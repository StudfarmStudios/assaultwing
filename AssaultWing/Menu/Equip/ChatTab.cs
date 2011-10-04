using System;
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
        private Control _sendControl;
        private EditableText _message;

        private static Curve g_cursorBlinkCurve;
        private TimeSpan _cursorBlinkStartTime;

        public override Texture2D TabTexture { get { return Content.TabChatTexture; } }
        public override string HelpText { get { return "Enter sends message, " + BasicHelpText; } }
        public MessageContainer Messages
        {
            get
            {
                if (ChatPlayer == null) return new MessageContainer();
                return ChatPlayer.Messages;
            }
        }

        private Player ChatPlayer { get { return MenuEngine.Game.DataEngine.Players.FirstOrDefault(plr => !plr.IsRemote); } }
        private SpriteFont Font { get { return Content.FontChat; } }
        private Vector2 TypingPos { get { return StatusPanePos + new Vector2(30, Content.StatusPaneTexture.Height - 44); } }

        public ChatTab(EquipMenuComponent menuComponent)
            : base(menuComponent)
        {
            _sendControl = new KeyboardKey(Keys.Enter);
            _message = new EditableText("", 40, new CharacterSet(Content.FontChat.Characters), MenuEngine.Game, () => { });
            // FIXME !!! Memory leak: _message will never be garbage collected because it referenced by the Window.KeyPress event

            g_cursorBlinkCurve = new Curve();
            g_cursorBlinkCurve.Keys.Add(new CurveKey(0, 1));
            g_cursorBlinkCurve.Keys.Add(new CurveKey(0.5f, 0));
            g_cursorBlinkCurve.Keys.Add(new CurveKey(1, 1));
            g_cursorBlinkCurve.PreLoop = CurveLoopType.Cycle;
            g_cursorBlinkCurve.PostLoop = CurveLoopType.Cycle;

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
        }

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            DrawLargeStatusBackground(view, spriteBatch);
            DrawPlayerListDisplay(view, spriteBatch, drawCursor: false);
            DrawChatMessages(view, spriteBatch, TypingPos, Content.StatusPaneTexture.Height - 70);
            DrawChatTextInputBox(view, spriteBatch);
        }

        public void DrawChatMessages(Vector2 view, SpriteBatch spriteBatch, Vector2 lowerLeftCorner, int height)
        {
            Font.LineSpacing = 15;
            var visibleLines = height / Font.LineSpacing;
            var lineDelta = new Vector2(0, Font.LineSpacing);
            var preTextPos = lowerLeftCorner - view - lineDelta;
            foreach (var item in Messages.ReversedChat().Take(visibleLines))
            {
                var preTextSize = Font.MeasureString(item.Message.PreText);
                var textPos = preTextPos + new Vector2(preTextSize.X, 0);

                if (preTextSize.X > 2)
                    textPos += new Vector2(4, 0);

                ModelRenderer.DrawBorderedText(spriteBatch, Font, item.Message.PreText, preTextPos.Round(), PlayerMessage.PRETEXT_COLOR, 1, 1);
                ModelRenderer.DrawBorderedText(spriteBatch, Font, item.Message.Text, textPos.Round(), item.Message.TextColor, 1, 1);
                preTextPos -= lineDelta;
            }
        }

        private void DrawChatTextInputBox(Vector2 view, SpriteBatch spriteBatch)
        {
            if (ChatPlayer == null) return;
            var text = string.Format("{0}>{1}", ChatPlayer.Name, _message.Content);
            ModelRenderer.DrawBorderedText(spriteBatch, Font, text, (TypingPos - view).Round(), Color.White, 1, 1);
            Color cursorColor = Color.FromNonPremultiplied(new Vector4(1, 1, 1, g_cursorBlinkCurve.Evaluate((float)(MenuEngine.Game.GameTime.TotalRealTime - _cursorBlinkStartTime).TotalSeconds)));
            spriteBatch.Draw(Content.TypingCursor, (TypingPos - view).Round() + new Vector2(Font.MeasureString(text).X + 2, -2), cursorColor);
        }
    }
}
