using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;

namespace AW2.Menu.Equip
{
    public class MatchTab : EquipMenuTab
    {
        private enum MatchTabItem { Type, Arena }

        private MatchTabItem _tabItem;

        public override Texture2D TabTexture { get { return Content.TabMatchTexture; } }

        public MatchTab(EquipMenuComponent menuComponent)
            : base(menuComponent)
        {
        }

        public override void Update()
        {
            int itemIndexDelta = Controls.ListDown.Pulse ? 1 : Controls.ListUp.Pulse ? -1 : 0;
            if (itemIndexDelta != 0)
            {
                int matchTabItemCount = Enum.GetValues(typeof(MatchTabItem)).Length;
                _tabItem = (MatchTabItem)(((int)_tabItem + itemIndexDelta + matchTabItemCount) % matchTabItemCount);
                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");
                MenuComponent.ListCursorFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
            }
            if (Controls.Activate.Pulse && MenuEngine.Game.NetworkMode != NetworkMode.Client)
            {
                if (_tabItem == MatchTabItem.Arena)
                {
                    MenuComponent.IsTemporarilyInactive = true;
                    MenuEngine.ActivateComponent(MenuComponentType.Arena);
                }
            }
        }

        public override void Draw(Microsoft.Xna.Framework.Vector2 view, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            DrawLargeStatusBackground(view, spriteBatch);
            DrawGameSettingsList(view, spriteBatch);
            switch (_tabItem)
            {
                case MatchTabItem.Type:
                    DrawGameModeInfo(view, spriteBatch);
                    break;
                case MatchTabItem.Arena:
                    DrawArenaInfo(view, spriteBatch);
                    break;
                default: throw new ApplicationException("Unexpected EquipMenuGameSettings " + _tabItem);
            }
        }

        private void DrawGameSettingsList(Vector2 view, SpriteBatch spriteBatch)
        {
            var listPos = MenuComponent.Pos - view + new Vector2(360, 201);
            var currentPos = listPos;
            var lineHeight = new Vector2(0, 56);
            var cursorPos = currentPos + (lineHeight * (int)_tabItem) + new Vector2(-27, -17);

            spriteBatch.Draw(Content.PlayerNameBackground, LeftPanePos - view, Color.White);

            spriteBatch.DrawString(Content.FontSmall, MatchTabItem.Type.ToString(), currentPos, Color.GreenYellow);
            spriteBatch.DrawString(Content.FontSmall, "Mayhem", currentPos + new Vector2(0, 20), Color.White);
            currentPos += lineHeight;

            var arenaName = MenuEngine.Game.SelectedArenaName;
            spriteBatch.DrawString(Content.FontSmall, MatchTabItem.Arena.ToString(), currentPos, Color.GreenYellow);
            spriteBatch.DrawString(Content.FontSmall, arenaName, currentPos + new Vector2(0, 20), Color.White);

            float cursorTime = (float)(MenuEngine.Game.GameTime.TotalRealTime - MenuComponent.ListCursorFadeStartTime).TotalSeconds;
            spriteBatch.Draw(Content.ListCursorTexture, cursorPos, Color.White);
            spriteBatch.Draw(Content.ListHiliteTexture, cursorPos, Color.Multiply(Color.White, EquipMenuComponent.CursorFade.Evaluate(cursorTime)));
        }

        private void DrawGameModeInfo(Vector2 view, SpriteBatch spriteBatch)
        {
            var infoDisplayPos = MenuComponent.Pos - view + new Vector2(595, 220);
            var lineHeight = new Vector2(0, 20);
            var infoWidth = new Vector2(320, 0);
            var currentPos = infoDisplayPos;
            var arenaName = MenuEngine.Game.SelectedArenaName;

            spriteBatch.DrawString(Content.FontBig, "Gametype Settings", currentPos, Color.White);
            currentPos += new Vector2(0, 50);

            spriteBatch.DrawString(Content.FontSmall, "Enemy", currentPos, Color.White);
            spriteBatch.DrawString(Content.FontSmall, "Everyone", currentPos + (infoWidth - new Vector2(Content.FontSmall.MeasureString("Everyone").X, 0)), Color.GreenYellow);
            currentPos += lineHeight;

            spriteBatch.DrawString(Content.FontSmall, "Players", currentPos, Color.White);
            spriteBatch.DrawString(Content.FontSmall, "max 16", currentPos + (infoWidth - new Vector2(Content.FontSmall.MeasureString("max 16").X, 0)), Color.GreenYellow);
            currentPos += lineHeight;

            spriteBatch.DrawString(Content.FontSmall, "Time limit", currentPos, Color.White);
            spriteBatch.DrawString(Content.FontSmall, "none", currentPos + (infoWidth - new Vector2(Content.FontSmall.MeasureString("none").X, 0)), Color.GreenYellow);
            currentPos += lineHeight;

            spriteBatch.DrawString(Content.FontSmall, "Life limit", currentPos, Color.White);
            spriteBatch.DrawString(Content.FontSmall, "none", currentPos + (infoWidth - new Vector2(Content.FontSmall.MeasureString("none").X, 0)), Color.GreenYellow);
            currentPos += lineHeight;

            spriteBatch.DrawString(Content.FontSmall, "Score limit", currentPos, Color.White);
            spriteBatch.DrawString(Content.FontSmall, "none", currentPos + (infoWidth - new Vector2(Content.FontSmall.MeasureString("none").X, 0)), Color.GreenYellow);
            currentPos += lineHeight;

            spriteBatch.DrawString(Content.FontSmall, "Arena count", currentPos, Color.White);
            spriteBatch.DrawString(Content.FontSmall, "1", currentPos + (infoWidth - new Vector2(Content.FontSmall.MeasureString("1").X, 0)), Color.GreenYellow);
            currentPos += lineHeight;

            spriteBatch.DrawString(Content.FontSmall, "Arenas", currentPos, Color.White);
            spriteBatch.DrawString(Content.FontSmall, "Selectable <" + arenaName + ">", currentPos + (infoWidth - new Vector2(Content.FontSmall.MeasureString("Selectable <" + arenaName + ">").X, 0)), Color.GreenYellow);
            currentPos += new Vector2(0, 72);

            spriteBatch.DrawString(Content.FontSmall, "If you want to change these gametype\n" +
                                                   "settings, please create a pilot in\n" +
                                                   "Assault Wing website which will allow\n" +
                                                   "you to create your own gametype settings'.", currentPos, new Color(218, 159, 33));
        }

        private void DrawArenaInfo(Vector2 view, SpriteBatch spriteBatch)
        {
            var infoDisplayPos = MenuComponent.Pos - view + new Vector2(595, 220);
            var currentPos = infoDisplayPos;
            var lineHeight = new Vector2(0, 20);
            var infoWidth = new Vector2(320, 0);
            var arenaName = MenuEngine.Game.SelectedArenaName;
            var arenaInfo = MenuEngine.Game.DataEngine.ArenaInfos.FirstOrDefault(info => info.Name == arenaName);
            var content = MenuEngine.Game.Content;
            string previewName = content.Exists<Texture2D>(arenaInfo.PreviewName) ? arenaInfo.PreviewName : "no_preview";
            var previewTexture = content.Load<Texture2D>(previewName);

            spriteBatch.DrawString(Content.FontBig, "Arena info", currentPos, Color.White);
            currentPos += new Vector2(0, 50);
            spriteBatch.DrawString(Content.FontSmall, "Current arena:", currentPos, Color.White);
            spriteBatch.DrawString(Content.FontSmall, arenaName, currentPos + new Vector2(Content.FontSmall.MeasureString("Current arena:  ").X, 0), Color.GreenYellow);
            spriteBatch.DrawString(Content.FontSmall, "Arena list", currentPos + infoWidth + new Vector2(10, 0), Color.White);
            currentPos += lineHeight;
            spriteBatch.DrawString(Content.FontSmall, "Gametype settings don't\n" +
                                                      "contain a list of arenas\n" +
                                                      "so the game host can\n" +
                                                      "change the arena.", currentPos + infoWidth + new Vector2(10, 0), new Color(218, 159, 33));
            spriteBatch.Draw(previewTexture, currentPos, null, Color.White, 0,
                new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
        }
    }
}
