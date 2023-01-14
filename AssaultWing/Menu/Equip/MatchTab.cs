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
        public override string HelpText { get { return "Arrows/Enter select, " + BasicHelpText; } }

        private bool IsArenaSelectable { get { return MenuEngine.Game.NetworkMode != NetworkMode.Client; } }

        public MatchTab(EquipMenuComponent menuComponent)
            : base(menuComponent)
        {
        }

        public override void Update()
        {
            int itemIndexDelta = Controls.Dirs.Down.Pulse ? 1 : Controls.Dirs.Up.Pulse ? -1 : 0;
            if (itemIndexDelta != 0)
            {
                int matchTabItemCount = Enum.GetValues(typeof(MatchTabItem)).Length;
                _tabItem = (MatchTabItem)(((int)_tabItem + itemIndexDelta + matchTabItemCount) % matchTabItemCount);
                MenuEngine.Game.SoundEngine.PlaySound("menuBrowseItem");
                MenuComponent.ListCursorFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
            }
            if (Controls.Activate.Pulse && _tabItem == MatchTabItem.Type)
            {
                var modes = MenuEngine.Game.DataEngine.GetTypeTemplates<AW2.Game.Logic.GameplayMode>().ToArray();
                var oldIndex = Array.FindIndex(modes, mode => mode.Name == MenuEngine.Game.DataEngine.GameplayMode.Name);
                MenuEngine.Game.DataEngine.GameplayMode = modes[(oldIndex + 1) % modes.Length];
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

            spriteBatch.DrawString(Content.FontSmall, MatchTabItem.Type.ToString(), Vector2.Round(currentPos), Color.GreenYellow);
            spriteBatch.DrawString(Content.FontSmall, MenuEngine.Game.DataEngine.GameplayMode.Name, Vector2.Round(currentPos + new Vector2(0, 20)), Color.White);
            currentPos += lineHeight;

            var arenaName = MenuEngine.Game.SelectedArenaName;
            spriteBatch.DrawString(Content.FontSmall, MatchTabItem.Arena.ToString(), Vector2.Round(currentPos), Color.GreenYellow);
            spriteBatch.DrawString(Content.FontSmall, arenaName, Vector2.Round(currentPos + new Vector2(0, 20)), Color.White);

            float cursorTime = (float)(MenuEngine.Game.GameTime.TotalRealTime - MenuComponent.ListCursorFadeStartTime).TotalSeconds;
            spriteBatch.Draw(Content.ListCursorTexture, cursorPos, Color.White);
            spriteBatch.Draw(Content.ListHiliteTexture, cursorPos, Color.Multiply(Color.White, EquipMenuComponent.CursorFade.Evaluate(cursorTime)));
        }

        private void DrawGameModeInfo(Vector2 view, SpriteBatch spriteBatch)
        {
            var infoDisplayPos = MenuComponent.Pos - view + new Vector2(595, 220);
            var firstInfoLinePos = infoDisplayPos + new Vector2(0, 50);

            spriteBatch.DrawString(Content.FontBig, "Gametype Settings", Vector2.Round(infoDisplayPos), Color.White);

            Action<int, string, string> drawInfoLine = (line, item, value) =>
            {
                var lineHeight = new Vector2(0, 20);
                var infoWidth = new Vector2(320, 0);
                var itemPos = firstInfoLinePos + lineHeight * line;
                var valuePos = itemPos + infoWidth - new Vector2(Content.FontSmall.MeasureString(value).X, 0);
                spriteBatch.DrawString(Content.FontSmall, item, Vector2.Round(itemPos), Color.White);
                spriteBatch.DrawString(Content.FontSmall, value, Vector2.Round(valuePos), Color.GreenYellow);
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
            spriteBatch.DrawString(Content.FontSmall, explanation, Vector2.Round(explanationPos), new Color(218, 159, 33));
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

            spriteBatch.DrawString(Content.FontBig, "Arena info", Vector2.Round(currentPos), Color.White);
            currentPos += new Vector2(0, 50);
            spriteBatch.DrawString(Content.FontSmall, "Current arena:", Vector2.Round(currentPos), Color.White);
            spriteBatch.DrawString(Content.FontSmall, arenaName, Vector2.Round(currentPos + new Vector2(Content.FontSmall.MeasureString("Current arena:  ").X, 0)), Color.GreenYellow);
            if (IsArenaSelectable) spriteBatch.DrawString(Content.FontSmall, "Press Enter to change.", Vector2.Round(currentPos + infoWidth + new Vector2(10, 0)), new Color(218, 159, 33));
            currentPos += new Vector2(0, Content.FontSmall.LineSpacing);
            spriteBatch.Draw(previewTexture, currentPos, null, Color.White, 0, new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
        }
    }
}
