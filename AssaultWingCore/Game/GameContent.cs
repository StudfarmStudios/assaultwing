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
        public BasicEffect LightningEffect { get; private set; }

        public GameContent(AWGame game)
        {
            _game = game;
        }

        public void LoadContent()
        {
            var gfx = _game.GraphicsDeviceService.GraphicsDevice;

            WallSilhouetteEffect = new BasicEffect(gfx);
            WallSilhouetteEffect.World = Matrix.Identity;
            WallSilhouetteEffect.VertexColorEnabled = false;
            WallSilhouetteEffect.LightingEnabled = false;
            WallSilhouetteEffect.TextureEnabled = false;
            WallSilhouetteEffect.FogEnabled = false;

            LightningEffect = LightningEffect ?? new BasicEffect(gfx);
            LightningEffect.World = Matrix.Identity;
            LightningEffect.TextureEnabled = true;
            LightningEffect.VertexColorEnabled = false;
            LightningEffect.LightingEnabled = false;
            LightningEffect.FogEnabled = false;
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
