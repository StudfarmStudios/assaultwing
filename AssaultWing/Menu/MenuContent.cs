using System;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;

namespace AW2.Menu
{
    public class MenuContent
    {
        public SpriteFont FontHuge { get; private set; }
        public SpriteFont FontBig { get; private set; }
        public SpriteFont FontSmall { get; private set; }
        public Texture2D MainCursor { get; private set; }
        public Texture2D MainBackground { get; private set; }
        public Texture2D MainHighlight { get; private set; }

        public Texture2D ListCursorTexture { get; private set; }
        public Texture2D ListHiliteTexture { get; private set; }
        public Texture2D PlayerNameBackground { get; private set; }

        public Texture2D TabEquipmentTexture { get; private set; }
        public Texture2D TabPlayersTexture { get; private set; }
        public Texture2D TabMatchTexture { get; private set; }
        public Texture2D TabChatTexture { get; private set; }
        public Texture2D TabHilite { get; private set; }

        #region Equip tab

        public Texture2D StatusPaneTexture { get; private set; }
        public Texture2D PlayerPaneTexture { get; private set; }
        public Texture2D PlayerNameBackgroundTexture { get; private set; }
        public Texture2D PlayerNameBorderTexture { get; private set; }
        public Texture2D HighlightMainTexture { get; private set; }
        public Texture2D WeaponHeaders { get; private set; }
        public Texture2D ListTextCursorTexture { get; private set; }
        public Texture2D CursorMainTexture{ get; private set; }

        #endregion

        public void LoadContent()
        {
            var content = AssaultWingCore.Instance.Content;

            FontHuge = content.Load<SpriteFont>("MenuFontHuge");
            FontBig = content.Load<SpriteFont>("MenuFontBig");
            FontSmall = content.Load<SpriteFont>("MenuFontSmall");
            MainCursor = content.Load<Texture2D>("menu_main_cursor");
            MainBackground = content.Load<Texture2D>("menu_main_bg");
            MainHighlight = content.Load<Texture2D>("menu_main_hilite");

            ListCursorTexture = content.Load<Texture2D>("menu_equip_player_name_cursor");
            ListHiliteTexture = content.Load<Texture2D>("menu_equip_player_name_hilite");
            PlayerNameBackground = content.Load<Texture2D>("menu_equip_player_name_bg_empty");

            TabEquipmentTexture = content.Load<Texture2D>("menu_equip_tab_equipment");
            TabPlayersTexture = content.Load<Texture2D>("menu_equip_tab_players");
            TabMatchTexture = content.Load<Texture2D>("menu_equip_tab_gamesettings");
            TabChatTexture = content.Load<Texture2D>("menu_equip_tab_chat");
            TabHilite = content.Load<Texture2D>("menu_equip_tab_hilite");

            StatusPaneTexture = content.Load<Texture2D>("menu_equip_status_display");
            PlayerPaneTexture = content.Load<Texture2D>("menu_equip_player_bg");
            PlayerNameBackgroundTexture = content.Load<Texture2D>("menu_equip_player_name_bg");
            PlayerNameBorderTexture = content.Load<Texture2D>("menu_equip_player_name_border");
            HighlightMainTexture = content.Load<Texture2D>("menu_equip_hilite_large");
            WeaponHeaders = content.Load<Texture2D>("menu_equip_weaponinfo_headers");
            ListTextCursorTexture = content.Load<Texture2D>("menu_equip_player_name_textcursor");
            CursorMainTexture = content.Load<Texture2D>("menu_equip_cursor_large");
        }
    }
}
