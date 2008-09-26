using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;

namespace AW2.Graphics
{
    /// <summary>
    /// Overlay graphics component displaying a box with a player's chat and gameplay messages.
    /// The messages are stored in <c>DataEngine</c> from where this component reads them.
    /// </summary>
    class ChatBoxOverlay : OverlayComponent
    {
        Player player;
        Texture2D chatBoxTexture;
        SpriteFont chatBoxFont;

        /// <summary>
        /// The dimensions of the component in pixels.
        /// </summary>
        /// The return value field <c>Point.X</c> is the width of the component,
        /// and the field <c>Point.Y</c> is the height of the component,
        public override Point Dimensions
        {
            get { return new Point(chatBoxTexture.Width, chatBoxTexture.Height); }
        }

        /// <summary>
        /// Creates a chat box.
        /// </summary>
        /// <param name="player">The player whose chat messages to display.</param>
        public ChatBoxOverlay(Player player)
            : base(HorizontalAlignment.Left, VerticalAlignment.Bottom)
        {
            this.player = player;
        }

        /// <summary>
        /// Draws the overlay graphics component using the guarantee that the
        /// graphics device's viewport is set to the exact area needed by the component.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to use. <c>Begin</c> is assumed
        /// to have been called and <c>End</c> is assumed to be called after this
        /// method returns.</param>
        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));

            // Chat box background
            spriteBatch.Draw(chatBoxTexture, Vector2.Zero, Color.White);

            // Chat messages
            Vector2 messagePos = new Vector2(10, chatBoxTexture.Height - chatBoxFont.LineSpacing);
            for (int i = player.Messages.Count - 1; i >= 0 && messagePos.Y > 16; --i, messagePos.Y -= chatBoxFont.LineSpacing)
                spriteBatch.DrawString(chatBoxFont, player.Messages[i], messagePos, Color.White);
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public override void LoadContent()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            chatBoxTexture = data.GetTexture(TextureName.ChatBox);
            chatBoxFont = data.GetFont(FontName.Overlay);
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public override void UnloadContent()
        {
            // Our textures and fonts are disposed by the graphics engine.
        }
    }
}
