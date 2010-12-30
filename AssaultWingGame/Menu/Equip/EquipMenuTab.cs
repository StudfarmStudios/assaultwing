using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Graphics;

namespace AW2.Menu.Equip
{
    public abstract class EquipMenuTab
    {
        private static readonly Vector2 LEFT_PANE_POS = new Vector2(334, 164);

        public EquipMenuComponent MenuComponent { get; private set; }
        public MenuEngineImpl MenuEngine { get { return MenuComponent.MenuEngine; } }
        public MenuContent Content { get { return MenuComponent.Content; } }
        public EquipMenuControls Controls { get { return MenuComponent.Controls; } }
        public Vector2 LeftPanePos { get { return MenuComponent.Pos + LEFT_PANE_POS; } }
        public abstract Texture2D TabTexture { get; }

        protected EquipMenuTab(EquipMenuComponent menuComponent)
        {
            MenuComponent = menuComponent;
        }

        public virtual void Update() { }
        public virtual void Draw(Vector2 view, SpriteBatch spriteBatch) { }

        protected void DrawLargeStatusBackground(Vector2 view, SpriteBatch spriteBatch)
        {
            var data = MenuEngine.Game.DataEngine;
            var statusPanePos = MenuComponent.Pos - view + new Vector2(537, 160);
            spriteBatch.Draw(Content.StatusPaneTexture, statusPanePos, Color.White);
        }
    }
}
