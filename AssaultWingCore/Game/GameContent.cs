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
        public Texture2D RadarDisplayTexture { get; private set; }
        public Texture2D ShipOnRadarTexture { get; private set; }
        public Texture2D DockOnRadarTexture { get; private set; }
        public Effect BasicShaders { get; private set; }
        public SpriteBatch ViewportSpriteBatch { get; private set; }
        public SpriteBatch RadarSilhouetteSpriteBatch { get; private set; }

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

            RadarDisplayTexture = _game.Content.Load<Texture2D>("gui_radar_bg");
            ShipOnRadarTexture = _game.Content.Load<Texture2D>("gui_playerinfo_white_ball");
            DockOnRadarTexture = _game.Content.Load<Texture2D>("p_green_box");

            BasicShaders = _game.Content.Load<Effect>("basicshaders");
            ViewportSpriteBatch = new SpriteBatch(gfx);
            RadarSilhouetteSpriteBatch = new SpriteBatch(gfx);
        }

        public void UnloadContent()
        {
            if (WallSilhouetteEffect != null)
            {
                WallSilhouetteEffect.Dispose();
                WallSilhouetteEffect = null;
            }
            if (ViewportSpriteBatch != null)
            {
                ViewportSpriteBatch.Dispose();
                ViewportSpriteBatch = null;
            }
            if (RadarSilhouetteSpriteBatch != null)
            {
                RadarSilhouetteSpriteBatch.Dispose();
                RadarSilhouetteSpriteBatch = null;
            }
        }
    }
}
