using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.UI;
using AW2.Net.Messages;
using AW2.Game;

namespace AW2.Core.GameComponents
{
    /// <summary>
    /// In-game chat functionality.
    /// </summary>
    public class PlayerChat : AWGameComponent
    {
        private Control _chatControl;
        private EditableText _message;
        private SpriteBatch _spriteBatch;
        private SpriteFont _typingFont;

        private Vector2 TypingPos { get { return new Vector2(10, 250); } }
        private Color TypingColor { get { return Color.White; } }
        private bool IsTyping { get { return _message != null; } }

        public PlayerChat(AssaultWing game)
            : base(game)
        {
            _chatControl = new KeyboardKey(Keys.Enter);
        }

        public override void Dispose()
        {
            _chatControl.Dispose();
            base.Dispose();
        }

        public override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(Game.GraphicsDeviceService.GraphicsDevice);
            _typingFont = Game.Content.Load<SpriteFont>("MenuFontBig");
        }

        public override void Update()
        {
            if (_chatControl.Pulse)
            {
                if (IsTyping)
                    SendMessage();
                else
                    _message = new EditableText("", 40, EditableText.Keysets.All);
            }
            if (IsTyping) _message.Update(() => { });
        }

        public override void Draw()
        {
            if (!IsTyping) return;
            var text = string.Format("{0}>{1}<", Game.DataEngine.Players.First(plr => !plr.IsRemote).Name, _message.Content);
            _spriteBatch.Begin();
            _spriteBatch.DrawString(_typingFont, text, TypingPos, TypingColor);
            _spriteBatch.End();
        }

        private void SendMessage()
        {
            var messageContent = _message.Content.Trim();
            _message = null;
            if (messageContent == "") return;
            var text = string.Format("{0}: {1}",
                Game.DataEngine.Players.First(plr => !plr.IsRemote).Name,
                messageContent);
            switch (Game.NetworkMode)
            {
                case NetworkMode.Server:
                    foreach (var plr in Game.DataEngine.Players) plr.SendMessage(text, Player.PLAYER_MESSAGE_COLOR);
                    break;
                case NetworkMode.Client:
                    foreach (var plr in Game.DataEngine.Players.Where(plr => !plr.IsRemote))
                        plr.SendMessage(text, Player.PLAYER_MESSAGE_COLOR);
                    Game.NetworkEngine.GameServerConnection.Send(new PlayerMessageMessage
                    {
                        PlayerID = -1,
                        Color = Player.PLAYER_MESSAGE_COLOR,
                        Text = text,
                    });
                    break;
                default: throw new InvalidOperationException("Text messages not supported in mode " + Game.NetworkMode);
            }
        }
    }
}
