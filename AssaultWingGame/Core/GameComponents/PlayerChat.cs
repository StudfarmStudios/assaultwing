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
        private AssaultWing _game;
        private Control _chatControl;
        private EditableText _message;
        private SpriteBatch _spriteBatch;
        private SpriteFont _typingFont;

        private Color TypingColor { get { return Color.White; } }
        private bool IsTyping { get { return _message != null; } }

        public PlayerChat(AssaultWing game, int updateOrder)
            : base(game, updateOrder)
        {
            _game = game;
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
                {
                    _game.SendMessageToAllPlayers(_message.Content, _game.DataEngine.Players.First(plr => !plr.IsRemote));
                    _message = null;
                }
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
            _spriteBatch.DrawString(_typingFont, text, GetTypingPos(text), TypingColor);
            _spriteBatch.End();
        }

        private Vector2 GetTypingPos(string text)
        {
            var viewport = Game.Window.ClientBounds;
            var textSize = _typingFont.MeasureString(text);
            return new Vector2(viewport.Width / 2, viewport.Height / 2 + 300 - _typingFont.LineSpacing * 3) - textSize / 2;
        }
    }
}
