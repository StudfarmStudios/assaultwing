using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Helpers;

namespace AW2.Menu.Equip
{
    public class MatchTab : EquipMenuTab
    {
        private enum MatchTabItem { Type, Arena }

        private MatchTabItem _tabItem;

        public override Texture2D TabTexture { get { return Content.TabMatchTexture; } }
        public override string HelpText { get { return "Arrows/Enter select, Tab changes tab, F10 starts game, Esc exits"; } }

        private bool IsArenaSelectable { get { return MenuEngine.Game.NetworkMode != NetworkMode.Client; } }

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
            if (Controls.Activate.Pulse && IsArenaSelectable && _tabItem == MatchTabItem.Arena)
            {
                MenuComponent.IsTemporarilyInactive = true;
                MenuEngine.Activate(MenuComponentType.Arena);
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

            spriteBatch.DrawString(Content.FontSmall, MatchTabItem.Type.ToString(), currentPos.Round(), Color.GreenYellow);
            spriteBatch.DrawString(Content.FontSmall, "Mayhem", (currentPos + new Vector2(0, 20)).Round(), Color.White);
            currentPos += lineHeight;

            var arenaName = MenuEngine.Game.SelectedArenaName;
            spriteBatch.DrawString(Content.FontSmall, MatchTabItem.Arena.ToString(), currentPos.Round(), Color.GreenYellow);
            spriteBatch.DrawString(Content.FontSmall, arenaName, (currentPos + new Vector2(0, 20)).Round(), Color.White);

            float cursorTime = (float)(MenuEngine.Game.GameTime.TotalRealTime - MenuComponent.ListCursorFadeStartTime).TotalSeconds;
            spriteBatch.Draw(Content.ListCursorTexture, cursorPos, Color.White);
            spriteBatch.Draw(Content.ListHiliteTexture, cursorPos, Color.Multiply(Color.White, EquipMenuComponent.CursorFade.Evaluate(cursorTime)));
        }

        private void DrawGameModeInfo(Vector2 view, SpriteBatch spriteBatch)
        {
            var infoDisplayPos = MenuComponent.Pos - view + new Vector2(595, 220);
            var firstInfoLinePos = infoDisplayPos + new Vector2(0, 50);

            spriteBatch.DrawString(Content.FontBig, "Gametype Settings", infoDisplayPos.Round(), Color.White);

            Action<int, string, string> drawInfoLine = (line, item, value) =>
            {
                var lineHeight = new Vector2(0, 20);
                var infoWidth = new Vector2(320, 0);
                var itemPos = firstInfoLinePos + lineHeight * line;
                var valuePos = itemPos + infoWidth - new Vector2(Content.FontSmall.MeasureString(value).X, 0);
                spriteBatch.DrawString(Content.FontSmall, item, itemPos.Round(), Color.White);
                spriteBatch.DrawString(Content.FontSmall, value, valuePos.Round(), Color.GreenYellow);
            };
            drawInfoLine(0, "Enemy", "Everyone");
            drawInfoLine(1, "Players", "Max 16");
            drawInfoLine(2, "Time limit", "None");
            drawInfoLine(3, "Life limit", "None");
            drawInfoLine(4, "Score limit", "None");
            drawInfoLine(5, "Arena count", "Unlimited");
            var arenaChoice = IsArenaSelectable ? "Selectable" : "Random";
            drawInfoLine(6, "Arena", arenaChoice + " <" + MenuEngine.Game.SelectedArenaName + ">");

            var explanation = "Currently gametype settings cannot be changed.";
            var explanationPos = firstInfoLinePos + new Vector2(0, 192);
            spriteBatch.DrawString(Content.FontSmall, explanation, explanationPos.Round(), new Color(218, 159, 33));
        }

        private void DrawArenaInfo(Vector2 view, SpriteBatch spriteBatch)
        {
            var infoDisplayPos = MenuComponent.Pos - view + new Vector2(595, 220);
            var currentPos = infoDisplayPos;
            var lineHeight = new Vector2(0, 20);
            var infoWidth = new Vector2(320, 0);
            var arenaName = MenuEngine.Game.SelectedArenaName;
            var arenaInfo = ((AW2.Game.Arena)MenuEngine.Game.DataEngine.GetTypeTemplate((CanonicalString)arenaName)).Info;
            var content = MenuEngine.Game.Content;
            string previewName = content.Exists<Texture2D>(arenaInfo.PreviewName) ? arenaInfo.PreviewName : "no_preview";
            var previewTexture = content.Load<Texture2D>(previewName);

            spriteBatch.DrawString(Content.FontBig, "Arena info", currentPos.Round(), Color.White);
            currentPos += new Vector2(0, 50);
            spriteBatch.DrawString(Content.FontSmall, "Current arena:", currentPos.Round(), Color.White);
            spriteBatch.DrawString(Content.FontSmall, arenaName, (currentPos + new Vector2(Content.FontSmall.MeasureString("Current arena:  ").X, 0)).Round(), Color.GreenYellow);
            if (IsArenaSelectable) spriteBatch.DrawString(Content.FontSmall, "Press Enter to change.", (currentPos + infoWidth + new Vector2(10, 0)).Round(), new Color(218, 159, 33));
            currentPos += new Vector2(0, Content.FontSmall.LineSpacing);
            spriteBatch.Draw(previewTexture, currentPos, null, Color.White, 0, new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
        }
    }
}
