using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Menu.Equip
{
    public class ChatTab : EquipMenuTab
    {
        public override Texture2D TabTexture { get { return Content.TabChatTexture; } }

        public ChatTab(EquipMenuComponent menuComponent)
            : base(menuComponent)
        {
        }

        public override void Update()
        {
        }

        public override void Draw(Microsoft.Xna.Framework.Vector2 view, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            DrawLargeStatusBackground(view, spriteBatch);
            DrawChatTextInputBox(view, spriteBatch);
        }

        private void DrawChatTextInputBox(Vector2 view, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Content.PlayerNameBackground, LeftPanePos - view, Color.White);
        }
    }
}
