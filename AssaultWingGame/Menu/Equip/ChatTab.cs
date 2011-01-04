﻿using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.UI;

namespace AW2.Menu.Equip
{
    public class ChatTab : EquipMenuTab
    {
        private Control _sendControl;
        private EditableText _message;

        public override Texture2D TabTexture { get { return Content.TabChatTexture; } }
        public ChatContainer Messages { get { return MenuEngine.Game.DataEngine.Players.First(p => !p.IsRemote).Messages; } }

        private SpriteFont Font { get { return Content.FontSmall; } }
        private Vector2 TypingPos { get { return StatusPanePos + new Vector2(30, Content.StatusPaneTexture.Height - 44); } }

        public ChatTab(EquipMenuComponent menuComponent)
            : base(menuComponent)
        {
            _sendControl = new KeyboardKey(Keys.Enter);
            _message = new EditableText("", 40, EditableText.Keysets.All);
        }

        public override void Update()
        {
            _message.Update(() => { });
            if (_sendControl.Pulse)
            {
                MenuEngine.Game.SendMessageToAllPlayers(_message.Content, MenuEngine.Game.DataEngine.Players.First(plr => !plr.IsRemote));
                _message.Clear();
            }
        }

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            DrawLargeStatusBackground(view, spriteBatch);
            DrawPlayerListDisplay(view, spriteBatch);
            DrawChatMessages(view, spriteBatch);
            DrawChatTextInputBox(view, spriteBatch);
        }

        private void DrawChatMessages(Vector2 view, SpriteBatch spriteBatch)
        {
            var visibleLines = (Content.StatusPaneTexture.Height - 70 ) / Font.LineSpacing;
            var lineDelta = new Vector2(0, Font.LineSpacing);
            var preTextPos = TypingPos - view - lineDelta;
            foreach (var item in Messages.Reversed().Take(visibleLines))
            {
                var preTextSize = Font.MeasureString(item.Message.PreText);
                var textPos = preTextPos + new Vector2(preTextSize.X, 0);
                spriteBatch.DrawString(Font, item.Message.PreText, preTextPos, Player.PRETEXT_COLOR);
                spriteBatch.DrawString(Font, item.Message.Text, textPos, item.Message.TextColor);
                preTextPos -= lineDelta;
            }
        }

        private void DrawChatTextInputBox(Vector2 view, SpriteBatch spriteBatch)
        {
            var text = string.Format("{0}>{1}<", MenuEngine.Game.DataEngine.Players.First(plr => !plr.IsRemote).Name, _message.Content);
            var x = _message.CaretPosition;
            spriteBatch.DrawString(Font, text, TypingPos - view, Color.White);
        }
    }
}
