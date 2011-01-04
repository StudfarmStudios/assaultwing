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

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            DrawLargeStatusBackground(view, spriteBatch);
            DrawPlayerListDisplay(view, spriteBatch);
            DrawChatMessages(view, spriteBatch);
            DrawChatTextInputBox(view, spriteBatch);
        }

        private void DrawChatMessages(Vector2 view, SpriteBatch spriteBatch)
        {
        }

        private void DrawChatTextInputBox(Vector2 view, SpriteBatch spriteBatch)
        {
            
        }
    }
}
