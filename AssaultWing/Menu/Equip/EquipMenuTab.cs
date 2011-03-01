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
        private static readonly Vector2 LEFT_PANE_POS = new Vector2(334, 164);

        private int _playerListIndex; // access through property PlayerListIndex

        public EquipMenuComponent MenuComponent { get; private set; }
        public MenuEngineImpl MenuEngine { get { return MenuComponent.MenuEngine; } }
        public MenuContent Content { get { return MenuComponent.Content; } }
        public EquipMenuControls Controls { get { return MenuComponent.Controls; } }
        public Vector2 LeftPanePos { get { return MenuComponent.Pos + LEFT_PANE_POS; } }
        public abstract Texture2D TabTexture { get; }

        protected Vector2 StatusPanePos { get { return MenuComponent.Pos + new Vector2(537, 160); } }
        protected int PlayerListCursorIndex
        {
            get
            {
                _playerListIndex = _playerListIndex.Clamp(0, Math.Max(0, MenuEngine.Game.DataEngine.Players.Count() - 1));
                return _playerListIndex;
            }
            set { _playerListIndex = value; }
        }

        private Vector2 PlayerListLineHeight { get { return new Vector2(0, 30); } }
        private Vector2 GetPlayerListPos(Vector2 view)
        {
            return MenuComponent.Pos - view + new Vector2(360, 201);
        }

        protected EquipMenuTab(EquipMenuComponent menuComponent)
        {
            MenuComponent = menuComponent;
        }

        public virtual void Update() { }
        public virtual void Draw(Vector2 view, SpriteBatch spriteBatch) { }

        protected void DrawLargeStatusBackground(Vector2 view, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Content.StatusPaneTexture, StatusPanePos - view, Color.White);
        }

        protected void DrawPlayerListDisplay(Vector2 view, SpriteBatch spriteBatch)
        {
            var currentPlayerPos = GetPlayerListPos(view);
            spriteBatch.Draw(Content.PlayerNameBackground, LeftPanePos - view, Color.White);
            foreach (var plr in MenuEngine.Game.DataEngine.Players)
            {
                spriteBatch.DrawString(Content.FontSmall, plr.Name, currentPlayerPos.Round(), plr.PlayerColor);
                currentPlayerPos += PlayerListLineHeight;
            }
        }

        protected void DrawPlayerListCursor(Vector2 view, SpriteBatch spriteBatch)
        {
            var cursorPos = GetPlayerListPos(view) + (PlayerListLineHeight * PlayerListCursorIndex) + new Vector2(-27, -37);
            float cursorTime = (float)(MenuEngine.Game.GameTime.TotalRealTime - MenuComponent.ListCursorFadeStartTime).TotalSeconds;
            spriteBatch.Draw(Content.ListCursorTexture, cursorPos, Color.White);
            spriteBatch.Draw(Content.ListHiliteTexture, cursorPos, Color.Multiply(Color.White, EquipMenuComponent.CursorFade.Evaluate(cursorTime)));
        }
    }
}
