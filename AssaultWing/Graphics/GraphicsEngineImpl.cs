using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Graphics
{
    /// <summary>
    /// Basic graphics engine.
    /// </summary>
    class GraphicsEngineImpl : DrawableGameComponent
    {
        SpriteBatch spriteBatch;

        /// <summary>
        /// Creates a new graphics engine.
        /// </summary>
        /// <param name="game">The Game to add the component to.</param>
        public GraphicsEngineImpl(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
        }

        /// <summary>
        /// Called when the component needs to load graphics resources.
        /// </summary>
        protected override void LoadContent()
        {
            Log.Write("Graphics engine loading graphics content.");
            var data = AssaultWing.Instance.DataEngine;
            spriteBatch = new SpriteBatch(this.GraphicsDevice);

            // Loop through gob types and load all the 3D models and textures they need.
            // The purpose of this is to load from disk here and cache the content for fast access later.
            data.ForEachTypeTemplate<Gob>(gobTemplate =>
            {
                foreach (var modelName in gobTemplate.ModelNames)
                    AssaultWing.Instance.Content.Load<Model>(modelName);
                foreach (var textureName in gobTemplate.TextureNames)
                    AssaultWing.Instance.Content.Load<Texture2D>(textureName);
            });

            // Load all textures that each weapon needs.
            // The purpose of this is to load from disk here and cache the content for fast access later.
            data.ForEachTypeTemplate<Weapon>(weaponTemplate =>
            {
                foreach (var textureName in weaponTemplate.TextureNames)
                    AssaultWing.Instance.Content.Load<Texture2D>(textureName);
            });

            // Load arena previews.
            // The purpose of this is to load from disk here and cache the content for fast access later.
            AssaultWing.Instance.Content.Load<Texture2D>("no_preview");
            foreach (var name in data.ArenaPlaylist)
                try { AssaultWing.Instance.Content.Load<Texture2D>(name.ToLower() + "_preview"); }
                catch (Microsoft.Xna.Framework.Content.ContentLoadException) { }

            // Load arena related content if an arena is being played right now.
            if (data.Arena != null)
                LoadArenaContent(data.Arena);

            // Propagate LoadContent to other components that are known to
            // contain references to graphics content.
            foreach (var viewport in data.Viewports) viewport.LoadContent();
            //TODO: FIX THIS
            //PlayerBonus.LoadContent();
        }

        /// <summary>
        /// Loads the graphical content required by an arena.
        /// </summary>
        /// <param name="arenaTemplate">The arena whose graphical content to load.</param>
        public void LoadArenaContent(Arena arenaTemplate)
        {
            // NOTE !!! This method has very little to do with GraphicsEngineImpl. Refactor into Arena.LoadContent() !!!
            var data = AssaultWing.Instance.DataEngine;

            foreach (var gob in arenaTemplate.Gobs)
            {
                // Load the layer's gob types.
                foreach (var modelName in gob.ModelNames)
                    AssaultWing.Instance.Content.Load<Model>(modelName);

                // Load the layer's gobs' textures.
                foreach (var textureName in gob.TextureNames)
                    AssaultWing.Instance.Content.Load<Texture2D>(textureName);

                gob.LoadContent();
            }

            foreach (ArenaLayer layer in arenaTemplate.Layers)
                if (layer.ParallaxName != "")
                    AssaultWing.Instance.Content.Load<Texture2D>(layer.ParallaxName);
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        protected override void UnloadContent()
        {
            Log.Write("Graphics engine unloading graphics content.");
            var data = AssaultWing.Instance.DataEngine;

            if (spriteBatch != null)
            {
                spriteBatch.Dispose();
                spriteBatch = null;
            }

            // Propagate UnloadContent to other components that are known to
            // contain references to graphics content.
            foreach (var viewport in data.Viewports) viewport.UnloadContent();
            if (data.Arena != null)
                foreach (var gob in data.Arena.Gobs) gob.UnloadContent();
            data.UnloadContent();
            //TODO: FIX THIS
            //PlayerBonus.UnloadContent();

            base.UnloadContent();
        }

        /// <summary>
        /// Draws players' views to game world and static graphics around them.
        /// </summary>
        /// <param name="gameTime">Time passed since the last call to Draw.</param>
        public override void Draw(GameTime gameTime)
        {
            AssaultWing.Instance.GobsDrawnPerFrameAvgPerSecondBaseCounter.Increment();
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;

            Viewport screen = gfx.Viewport;
            screen.X = 0;
            screen.Y = 0;
            screen.Width = AssaultWing.Instance.ClientBounds.Width;
            screen.Height = AssaultWing.Instance.ClientBounds.Height;
            gfx.Viewport = screen;
            gfx.Clear(new Color(0x40, 0x40, 0x40));

            foreach (var viewport in AssaultWing.Instance.DataEngine.Viewports) viewport.Draw();

            // Restore viewport to the whole client window.
            gfx.Viewport = screen;

            // Draw viewport separators.
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            foreach (var separator in AssaultWing.Instance.DataEngine.Viewports.Separators)
            {
                Texture2D separatorTexture = AssaultWing.Instance.Content.Load<Texture2D>("viewport_border_vertical");
                Vector2 separatorOrigin = new Vector2(separatorTexture.Width, 0) / 2;
                if (separator.vertical)
                {
                    // Loop the texture vertically, centered on the screen.
                    // 'extraLength' is how many pixels more is the least sufficiently long 
                    // multiple of a pair of the separator texture than the screen height;
                    // it helps us center the looping separator texture.
                    int extraLength = 2 * separatorTexture.Height - screen.Height % (2 * separatorTexture.Height);
                    for (Vector2 pos = new Vector2(separator.coordinate, -extraLength / 2);
                        pos.Y < screen.Height; pos.Y += separatorTexture.Height)
                        spriteBatch.Draw(separatorTexture, pos, null, Color.White, 0,
                            separatorOrigin, 1, SpriteEffects.None, 0);
                }
                else
                {
                    // Loop the texture horizontally, centered on the screen.
                    // We use the vertical texture rotated 90 degrees to the left.
                    // 'extraLength' is how many pixels more is the least sufficiently long 
                    // multiple of a pair of the separator texture than the screen width;
                    // it helps us center the looping separator texture.
                    int extraLength = 2 * separatorTexture.Height - screen.Width % (2 * separatorTexture.Height);
                    for (Vector2 pos = new Vector2(-extraLength / 2, separator.coordinate);
                        pos.X < screen.Width; pos.X += separatorTexture.Height)
                        spriteBatch.Draw(separatorTexture, pos, null, Color.White, -MathHelper.PiOver2,
                            separatorOrigin, 1, SpriteEffects.None, 0);
                }
            }
            spriteBatch.End();
        }

        /// <summary>
        /// Reacts to a system window resize.
        /// </summary>
        /// This method should be called after the window size changes in windowed mode,
        /// or after the screen resolution changes in fullscreen mode,
        /// or after switching between windowed and fullscreen mode.
        public void WindowResize()
        {
            AssaultWing.Instance.DataEngine.RearrangeViewports();
        }
    }
}
