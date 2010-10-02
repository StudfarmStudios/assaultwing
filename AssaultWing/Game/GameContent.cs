using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;

namespace AW2.Game
{
    public class GameContent
    {
        private AWGame _game;

        public BasicEffect WallSilhouetteEffect { get; private set; }

        public GameContent(AWGame game)
        {
            _game = game;
        }

        public void LoadContent()
        {
            WallSilhouetteEffect = new BasicEffect(_game.GraphicsDeviceService.GraphicsDevice, null);
            WallSilhouetteEffect.World = Matrix.Identity;
            WallSilhouetteEffect.VertexColorEnabled = false;
            WallSilhouetteEffect.LightingEnabled = false;
            WallSilhouetteEffect.TextureEnabled = false;
            WallSilhouetteEffect.FogEnabled = false;
        }

        public void UnloadContent()
        {
            if (WallSilhouetteEffect != null)
            {
                WallSilhouetteEffect.Dispose();
                WallSilhouetteEffect = null;
            }
        }
    }
}
