using System;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;

namespace AW2.Menu
{
    public class MenuContent
    {
        public SpriteFont FontBig { get; private set; }
        public Texture2D MainCursor { get; private set; }
        public Texture2D MainBackground { get; private set; }
        public Texture2D MainHighlight { get; private set; }

        public void LoadContent()
        {
            var content = AssaultWingCore.Instance.Content;
            FontBig = content.Load<SpriteFont>("MenuFontBig");
            MainCursor = content.Load<Texture2D>("menu_main_cursor");
            MainBackground = content.Load<Texture2D>("menu_main_bg");
            MainHighlight = content.Load<Texture2D>("menu_main_hilite");
        }
    }
}
