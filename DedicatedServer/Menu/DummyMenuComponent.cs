using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Menu
{
    /// <summary>
    /// A dummy menu component that doesn't do anything or look like anything.
    /// </summary>
    public class DummyMenuComponent : MenuComponent
    {
        public override Vector2 Center { get { return new Vector2(0, 698) + new Vector2(700, 455); } }
        public override string HelpText { get { return ""; } }

        public DummyMenuComponent(MenuEngineImpl menuEngine)
            : base(menuEngine)
        {
        }

        public override void Update() { }
        public override void Draw(Vector2 view, SpriteBatch spriteBatch) { }
    }
}
