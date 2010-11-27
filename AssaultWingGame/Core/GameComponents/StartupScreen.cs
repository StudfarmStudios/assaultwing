using Microsoft.Xna.Framework;

namespace AW2.Core.GameComponents
{
    public class StartupScreen : AWGameComponent
    {
        public StartupScreen(AssaultWing game)
            : base(game)
        {
        }

        public override void Draw()
        {
            Game.GraphicsDeviceService.GraphicsDevice.Clear(Color.Black);
            base.Draw();
        }
    }
}
