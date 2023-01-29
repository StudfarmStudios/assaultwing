using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Game.GobUtils;
using AW2.Helpers;

namespace AW2.Graphics
{
    /// <summary>
    /// Gameplay graphics implementation.
    /// </summary>
    public class GraphicsEngineImpl : AWGameComponent
    {
        private SpriteBatch _spriteBatch;

        public static BlendState AdditiveBlendPremultipliedAlpha { get; private set; }
        public static BlendState SubtractiveBlend { get; private set; }

        public GameContent GameContent { get; private set; }

        static GraphicsEngineImpl()
        {
            AdditiveBlendPremultipliedAlpha = new BlendState
            {
                ColorDestinationBlend = Blend.One,
                AlphaDestinationBlend = Blend.One,
                ColorSourceBlend = Blend.One,
                AlphaSourceBlend = Blend.One,
                Name = "Additive blend for colors with premultiplied alpha",
            };
            SubtractiveBlend = new BlendState
            {
                ColorBlendFunction = BlendFunction.ReverseSubtract,
                ColorDestinationBlend = Blend.One,
                AlphaDestinationBlend = Blend.One,
                ColorSourceBlend = Blend.SourceAlpha,
                AlphaSourceBlend = Blend.SourceAlpha,
                Name = "Subtractive blend",
            };
        }

        public GraphicsEngineImpl(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
            GameContent = new GameContent(game);
        }

        /// <summary>
        /// Draws text with formatting commands. E.g. "\t\xa" resets X coordinate to 10 N widths from the origin.
        /// </summary>
        public static void DrawFormattedText(Vector2 origin, float enWidth, string text, Action<Vector2, string> draw)
        {
            var rawName = text;
            var textStartIndex = 0;
            var textPos = origin;
            while (true)
            {
                var tabIndex = rawName.IndexOf('\t', textStartIndex);
                int textLength = (tabIndex == -1 ? rawName.Length : tabIndex) - textStartIndex;
                draw(Vector2.Round(textPos), rawName.Substring(textStartIndex, textLength));
                if (tabIndex == -1) break;
                var enCount = rawName[tabIndex + 1];
                textStartIndex = tabIndex + 2;
                textPos = origin + new Vector2(enWidth * enCount, 0);
            }
        }

        public override void LoadContent()
        {
            if (Game.CommandLineOptions.DedicatedServer) return;
            Game.Content.LoadAllGraphicsContent();
            _spriteBatch = new SpriteBatch(Game.GraphicsDeviceService.GraphicsDevice);
            GameContent.LoadContent();
            foreach (var viewport in Game.DataEngine.Viewports) viewport.LoadContent();
        }

        public override void UnloadContent()
        {
            GameContent.UnloadContent();
            if (_spriteBatch != null) _spriteBatch.Dispose();
            _spriteBatch = null;
            foreach (var viewport in Game.DataEngine.Viewports) viewport.UnloadContent();
            Game.DataEngine.UnloadContent();
        }

        public override void Draw()
        {
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
            gfx.Clear(Color.Black);
            Game.DataEngine.ArenaSilhouette.EnsureUpdated();
            foreach (var viewport in Game.DataEngine.Viewports) viewport.PrepareForDraw();
            foreach (var viewport in Game.DataEngine.Viewports) viewport.Draw();
            DrawViewportSeparators();
        }

        private void DrawViewportSeparators()
        {
            int viewportHeight = Game.GraphicsDeviceService.GraphicsDevice.Viewport.Height;
            int viewportWidth = Game.GraphicsDeviceService.GraphicsDevice.Viewport.Width;
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            foreach (var separator in Game.DataEngine.Viewports.Separators)
            {
                var separatorTexture = Game.Content.Load<Texture2D>("viewport_border_vertical");
                var separatorOrigin = new Vector2(separatorTexture.Width, 0) / 2;
                if (separator.Vertical)
                {
                    // Loop the texture vertically, centered on the screen.
                    // 'extraLength' is how many pixels more is the least sufficiently long 
                    // multiple of a pair of the separator texture than the screen height;
                    // it helps us center the looping separator texture.
                    int extraLength = 2 * separatorTexture.Height - viewportHeight % (2 * separatorTexture.Height);
                    for (Vector2 pos = new Vector2(separator.Coordinate, -extraLength / 2);
                        pos.Y < viewportHeight; pos.Y += separatorTexture.Height)
                        _spriteBatch.Draw(separatorTexture, pos, null, Color.White, 0,
                            separatorOrigin, 1, SpriteEffects.None, 0);
                }
                else
                {
                    // Loop the texture horizontally, centered on the screen.
                    // We use the vertical texture rotated 90 degrees to the left.
                    // 'extraLength' is how many pixels more is the least sufficiently long 
                    // multiple of a pair of the separator texture than the screen width;
                    // it helps us center the looping separator texture.
                    int extraLength = 2 * separatorTexture.Height - viewportWidth % (2 * separatorTexture.Height);
                    for (Vector2 pos = new Vector2(-extraLength / 2, separator.Coordinate);
                        pos.X < viewportWidth; pos.X += separatorTexture.Height)
                        _spriteBatch.Draw(separatorTexture, pos, null, Color.White, -MathHelper.PiOver2,
                            separatorOrigin, 1, SpriteEffects.None, 0);
                }
            }
            _spriteBatch.End();
        }
    }
}
