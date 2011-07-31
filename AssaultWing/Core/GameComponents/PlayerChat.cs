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
        private Control _chatSendControl, _escapeControl;
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
            _chatSendControl = new KeyboardKey(Keys.Enter);
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
            if (!IsTyping) return;
            var chatName = ChatPlayer != null ? ChatPlayer.Name : "???";
            var text = string.Format("{0}>{1}<", chatName, _message.Content);
            _spriteBatch.Begin();
            _spriteBatch.DrawString(_typingFont, text, GetTypingPos(text).Round(), TypingColor);
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
            var textY = Math.Min(viewport.Height / 2 + 300 - _typingFont.LineSpacing * 3, viewport.Height - textSize.Y);
            return new Vector2(viewport.Width / 2, textY) - textSize / 2;
        }

        private void StartWritingMessage()
        {
            if (_message != null) throw new InvalidOperationException("Already writing a message");
            _game.UIEngine.PushExclusiveControls(new[] { _chatSendControl, _escapeControl });
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
