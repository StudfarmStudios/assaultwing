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

namespace AW2.Core.GameComponents
{
    /// <summary>
    /// In-game chat functionality.
    /// </summary>
    public class PlayerChat : AWGameComponent
    {
        private AssaultWing _game;
        private Control _chatControl, _escapeControl;
        private EditableText _message;
        private SpriteBatch _spriteBatch;
        private SpriteFont _typingFont;

        private Color TypingColor { get { return Color.White; } }
        private bool IsTyping { get { return _message != null; } }
        private Player ChatPlayer { get { return Game.DataEngine.Players.First(plr => !plr.IsRemote); } }

        public PlayerChat(AssaultWing game, int updateOrder)
            : base(game, updateOrder)
        {
            _game = game;
            _chatControl = new KeyboardKey(Keys.Enter);
            _escapeControl = new KeyboardKey(Keys.Escape);
        }

        public override void LoadContent()
        {
            Game.GraphicsDeviceService.CheckThread();
            _spriteBatch = new SpriteBatch(Game.GraphicsDeviceService.GraphicsDevice);
            _typingFont = Game.Content.Load<SpriteFont>("MenuFontBig");
        }

        public override void Update()
        {
            if (_chatControl.Pulse && ChatPlayer != null)
            {
                if (IsTyping)
                {
                    _game.SendMessageToAllPlayers(_message.Content, ChatPlayer);
                    StopWritingMessage();
                }
                else
                    StartWritingMessage();
            }
            if (IsTyping)
            {
                if (_escapeControl.Pulse) StopWritingMessage();
            }
        }

        public override void Draw()
        {
            Game.GraphicsDeviceService.CheckThread();
            if (!IsTyping) return;
            var chatName = ChatPlayer != null ? ChatPlayer.Name : "???";
            var text = string.Format("{0}>{1}<", chatName, _message.Content);
            _spriteBatch.Begin();
            _spriteBatch.DrawString(_typingFont, text, GetTypingPos(text).Round(), TypingColor);
            _spriteBatch.End();
        }

        private Vector2 GetTypingPos(string text)
        {
            var viewport = Game.Window.ClientBounds;
            var textSize = _typingFont.MeasureString(text);
            return new Vector2(viewport.Width / 2, viewport.Height / 2 + 300 - _typingFont.LineSpacing * 3) - textSize / 2;
        }

        private void StartWritingMessage()
        {
            if (_message != null) throw new InvalidOperationException("Already writing a message");
            _game.UIEngine.PushExclusiveControls(new[] { _chatControl, _escapeControl });
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
