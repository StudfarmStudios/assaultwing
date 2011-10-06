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
            int parseIndex = 0;
            int textStartIndex = 0;
            var textPos = origin;
            while (parseIndex < rawName.Length)
            {
                var tabIndex = rawName.IndexOf('\t', parseIndex);
                int textLength = (tabIndex == -1 ? rawName.Length : tabIndex) - textStartIndex;
                draw(textPos.Round(), rawName.Substring(textStartIndex, textLength));
                if (tabIndex == -1)
                    parseIndex = rawName.Length;
                else
                {
                    parseIndex = tabIndex + 1;
                    int enCount = rawName[parseIndex];
                    textStartIndex = parseIndex + 1;
                    textPos = origin + new Vector2(enWidth * enCount, 0);
                }
            }
        }

        public override void LoadContent()
        {
            if (Game.CommandLineOptions.DedicatedServer) return;
            Log.Write("Graphics engine loading graphics content.");
            Game.Content.LoadAllGraphicsContent();
            var data = Game.DataEngine;
            _spriteBatch = new SpriteBatch(Game.GraphicsDeviceService.GraphicsDevice);
            GameContent.LoadContent();

            // Loop through gob types and load all the 3D models and textures they need.
            // The purpose of this is to load from disk here and cache the content for fast access later.
            foreach (var gobTemplate in data.GetTypeTemplates<Gob>())
            {
                foreach (var modelName in gobTemplate.ModelNames)
                    Game.Content.Load<Model>(modelName);
                foreach (var textureName in gobTemplate.TextureNames)
                    Game.Content.Load<Texture2D>(textureName);
            }

            // Load all textures that each weapon needs.
            // The purpose of this is to load from disk here and cache the content for fast access later.
            foreach (var weaponTemplate in data.GetTypeTemplates<Weapon>())
            {
                foreach (var textureName in weaponTemplate.TextureNames)
                    Game.Content.Load<Texture2D>(textureName);
            }

            // Load arena previews.
            // The purpose of this is to load from disk here and cache the content for fast access later.
            Game.Content.Load<Texture2D>("no_preview");
            foreach (var arenaInfo in data.GetTypeTemplates<Arena>().Select(a => a.Info))
                try { Game.Content.Load<Texture2D>(arenaInfo.PreviewName); }
                catch (Microsoft.Xna.Framework.Content.ContentLoadException) { }

            // Load arena related content if an arena is being played right now.
            if (data.Arena != null)
                LoadArenaContent(data.Arena);

            // Propagate LoadContent to other components that are known to
            // contain references to graphics content.
            foreach (var viewport in data.Viewports) viewport.LoadContent();
        }

        /// <summary>
        /// Loads the graphical content required by an arena.
        /// </summary>
        /// <param name="arenaTemplate">The arena whose graphical content to load.</param>
        public void LoadArenaContent(Arena arenaTemplate)
        {
            // NOTE !!! This method has very little to do with GraphicsEngineImpl. Refactor into Arena.LoadContent() !!!
            var data = Game.DataEngine;

            foreach (var gob in arenaTemplate.Gobs)
            {
                // Load the layer's gob types.
                foreach (var modelName in gob.ModelNames)
                    Game.Content.Load<Model>(modelName);

                // Load the layer's gobs' textures.
                foreach (var textureName in gob.TextureNames)
                    Game.Content.Load<Texture2D>(textureName);
            }

            foreach (var layer in arenaTemplate.Layers)
                if (layer.ParallaxName != "")
                    Game.Content.Load<Texture2D>(layer.ParallaxName);
        }

        public override void UnloadContent()
        {
            Log.Write("Graphics engine unloading graphics content.");
            var data = Game.DataEngine;
            GameContent.UnloadContent();

            if (_spriteBatch != null)
            {
                _spriteBatch.Dispose();
                _spriteBatch = null;
            }

            // Propagate UnloadContent to other components that are known to
            // contain references to graphics content.
            foreach (var viewport in data.Viewports) viewport.UnloadContent();
            if (data.Arena != null)
                foreach (var gob in data.Arena.Gobs) gob.UnloadContent();
            data.UnloadContent();

            base.UnloadContent();
        }

        public override void Update()
        {
            foreach (var viewport in Game.DataEngine.Viewports) viewport.Update();
        }

        public override void Draw()
        {
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
            gfx.Clear(Color.Black);
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
