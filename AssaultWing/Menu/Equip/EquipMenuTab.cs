using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Graphics;
using AW2.Helpers;

namespace AW2.Menu.Equip
{
    public abstract class EquipMenuTab
    {
        private const int PLAYER_NAMES_VISIBLE = 12;
        private static readonly Vector2 LEFT_PANE_POS = new Vector2(334, 164);

        public EquipMenuComponent MenuComponent { get; private set; }
        public MenuEngineImpl MenuEngine { get { return MenuComponent.MenuEngine; } }
        public MenuContent Content { get { return MenuComponent.Content; } }
        public MenuControls Controls { get { return MenuComponent.Controls; } }
        public Vector2 LeftPanePos { get { return MenuComponent.Pos + LEFT_PANE_POS; } }
        public abstract Texture2D TabTexture { get; }
        public abstract string HelpText { get; }
        protected string BasicHelpText { get { return "Tab changes tab, F10 enters game, Esc exits"; } }

        protected Vector2 StatusPanePos { get { return MenuComponent.Pos + new Vector2(537, 160); } }
        protected ScrollableList PlayerList { get; set; }

        private Vector2 PlayerListLineHeight { get { return new Vector2(0, 30); } }
        private Vector2 GetPlayerListPos(Vector2 view)
        {
            return MenuComponent.Pos - view + new Vector2(360, 201);
        }

        protected EquipMenuTab(EquipMenuComponent menuComponent)
        {
            MenuComponent = menuComponent;
            PlayerList = new ScrollableList(PLAYER_NAMES_VISIBLE, () => MenuEngine.Game.DataEngine.Players.Count());
        }

        public virtual void Update() { }
        public virtual void Draw(Vector2 view, SpriteBatch spriteBatch) { }

        protected void DrawLargeStatusBackground(Vector2 view, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Content.StatusPaneTexture, StatusPanePos - view, Color.White);
        }

        protected void DrawPlayerListDisplay(Vector2 view, SpriteBatch spriteBatch, bool drawCursor)
        {
            var currentPlayerPos = GetPlayerListPos(view);
            var scrollUpPos = currentPlayerPos + new Vector2(29, -37);
            var scrollDownPos = currentPlayerPos + new Vector2(29, 336);
            spriteBatch.Draw(Content.PlayerNameBackground, LeftPanePos - view, Color.White);
            var playersArray = MenuEngine.Game.DataEngine.Players.ToArray();
            PlayerList.ForEachVisible((realIndex, visibleInidex, isSelected) =>
            {
                var plr = playersArray[realIndex];
                spriteBatch.DrawString(Content.FontSmall, plr.Name, currentPlayerPos.Round(), plr.Color);
                currentPlayerPos += PlayerListLineHeight;
                if (drawCursor && isSelected)
                {
                    var cursorPos = GetPlayerListPos(view) + (PlayerListLineHeight * visibleInidex) + new Vector2(-27, -37);
                    var cursorTime = (float)(MenuEngine.Game.GameTime.TotalRealTime - MenuComponent.ListCursorFadeStartTime).TotalSeconds;
                    spriteBatch.Draw(Content.ListCursorTexture, cursorPos, Color.White);
                    spriteBatch.Draw(Content.ListHiliteTexture, cursorPos, Color.Multiply(Color.White, EquipMenuComponent.CursorFade.Evaluate(cursorTime)));
                }
            });
            if (PlayerList.IsScrollableUp) spriteBatch.Draw(Content.ScrollUpTexture, scrollUpPos, Color.White);
            if (PlayerList.IsScrollableDown) spriteBatch.Draw(Content.ScrollDownTexture, scrollDownPos, Color.White);
        }
    }
}
