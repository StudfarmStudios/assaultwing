using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Game
{
    public class GameContent
    {
        public BasicEffect WallSilhouetteEffect { get; private set; }

        public void LoadContent()
        {
            var gfx = AssaultWingCore.Instance.GraphicsDevice;
            WallSilhouetteEffect = new BasicEffect(gfx, null);
            WallSilhouetteEffect.World = Matrix.Identity;
            WallSilhouetteEffect.VertexColorEnabled = false;
            WallSilhouetteEffect.LightingEnabled = false;
            WallSilhouetteEffect.TextureEnabled = false;
            WallSilhouetteEffect.FogEnabled = false;
        }

        public void UnloadContent()
        {
            WallSilhouetteEffect.Dispose();
        }

    }
}
