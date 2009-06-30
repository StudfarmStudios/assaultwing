// marked in as DEBUG because we don't want NUnit framework to release builds
#if DEBUG
using NUnit.Framework;
#endif
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
            data.ForEachViewport(viewport => viewport.LoadContent());
            data.LoadContent();
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
            data.ForEachViewport(viewport => viewport.UnloadContent());
            if (data.Arena != null)
                foreach (var gob in data.Arena.Gobs) gob.UnloadContent();
            data.UnloadContent();

            base.UnloadContent();
        }

        /// <summary>
        /// Draws players' views to game world and static graphics around them.
        /// </summary>
        /// <param name="gameTime">Time passed since the last call to Draw.</param>
        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;

            Viewport screen = gfx.Viewport;
            screen.X = 0;
            screen.Y = 0;
            screen.Width = AssaultWing.Instance.ClientBounds.Width;
            screen.Height = AssaultWing.Instance.ClientBounds.Height;
            gfx.Viewport = screen;
            gfx.Clear(new Color(0x40, 0x40, 0x40));

            // Draw all viewports.
            AssaultWing.Instance.DataEngine.ForEachViewport(delegate(AWViewport viewport) { viewport.Draw(); });

            // Restore viewport to the whole client window.
            gfx.Viewport = screen;

            // Draw viewport separators.
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            AssaultWing.Instance.DataEngine.ForEachViewportSeparator(delegate(ViewportSeparator separator)
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
            });
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
            RearrangeViewports();
        }

        /// <summary>
        /// Rearranges player viewports.
        /// </summary>
        public void RearrangeViewports()
        {
            var data = AssaultWing.Instance.DataEngine;
            data.ClearViewports();
            int localPlayers = data.Players.Count(player => !player.IsRemote);
            if (localPlayers == 0) return;

            // Find out an optimal arrangement of viewports.
            // These conditions are required:
            // - they are all equal in size (give or take a pixel),
            // - they fill up the whole system window.
            // This condition is preferable:
            // - each viewport is as wide as tall.
            // We do this by going through viewport arrangements in
            // different NxM grids.
            Rectangle window = AssaultWing.Instance.ClientBounds;
            float bestAspectRatio = Single.MaxValue;
            int bestRows = 1;
            for (int rows = 1; rows <= localPlayers; ++rows)
            {
                // Only check out grids with cells as many as players.
                if (localPlayers % rows != 0) continue;
                int columns = localPlayers / rows;
                int viewportWidth = window.Width / columns;
                int viewportHeight = window.Height / rows;
                float aspectRatio = (float)viewportHeight / (float)viewportWidth;
                if (CompareAspectRatios(aspectRatio, bestAspectRatio) < 0)
                {
                    bestAspectRatio = aspectRatio;
                    bestRows = rows;
                }
            }
            int bestColumns = localPlayers / bestRows;

            // Assign the viewports to players.
            int playerI = 0;
            foreach (var player in data.Players)
            {
                if (player.IsRemote) return;
                int viewportX = playerI % bestColumns;
                int viewportY = playerI / bestColumns;
                int onScreenX1 = window.Width * viewportX / bestColumns;
                int onScreenY1 = window.Height * viewportY / bestRows;
                int onScreenX2 = window.Width * (viewportX + 1) / bestColumns;
                int onScreenY2 = window.Height * (viewportY + 1) / bestRows;
                Rectangle onScreen = new Rectangle(onScreenX1, onScreenY1,
                    onScreenX2 - onScreenX1, onScreenY2 - onScreenY1);
                AWViewport viewport = new PlayerViewport(player, onScreen);
                data.AddViewport(viewport);
                ++playerI;
            }

            // Register all needed viewport separators.
            for (int i = 1; i < bestColumns; ++i)
                data.AddViewportSeparator(new ViewportSeparator(true, window.Width * i / bestColumns));
            for (int i = 1; i < bestRows; ++i)
                data.AddViewportSeparator(new ViewportSeparator(false, window.Height * i / bestRows));
        }

        /// <summary>
        /// Rearranges player viewports so that one player gets all screen space
        /// and the others get nothing.
        /// </summary>
        /// <param name="privilegedPlayer">The player who gets all the screen space.</param>
        public void RearrangeViewports(int privilegedPlayer)
        {
            AssaultWing.Instance.DataEngine.ClearViewports();
            Rectangle window = AssaultWing.Instance.ClientBounds;
            Rectangle onScreen = new Rectangle(0, 0, window.Width, window.Height);
            AWViewport viewport = new PlayerViewport(AssaultWing.Instance.DataEngine.Players[privilegedPlayer], onScreen);
            AssaultWing.Instance.DataEngine.AddViewport(viewport);
        }

        /// <summary>
        /// Compares aspect ratios based on visual appropriateness.
        /// </summary>
        /// In C sense, this method defines an order on aspect ratios, 
        /// where more preferable aspect ratios come before less 
        /// preferable aspect ratios.
        /// <param name="aspectRatio1">One aspect ratio.</param>
        /// <param name="aspectRatio2">Another aspect ratio.</param>
        /// <returns><b>-1</b> if <b>aspectRatio1</b> is more preferable;
        /// <b>0</b> if <b>aspectRatio1</b> is as preferable as <b>aspectRatio2</b>;
        /// <b>1</b> if <b>aspectRatio2</b> is more preferable.</returns>
        private static int CompareAspectRatios(float aspectRatio1, float aspectRatio2)
        {
            float badness1 = aspectRatio1 >= 1.0f
                ? aspectRatio1 - 1.0f
                : 1.0f / aspectRatio1 - 1.0f;
            float badness2 = aspectRatio2 >= 1.0f
                ? aspectRatio2 - 1.0f
                : 1.0f / aspectRatio2 - 1.0f;
            if (badness1 < badness2) return -1;
            if (badness1 > badness2) return 1;
            return 0;
        }

        #region Unit tests
#if DEBUG
        /// <summary>
        /// Test class for graphics engine.
        /// </summary>
        [TestFixture]
        public class GraphicsEngineTest
        {
            /// <summary>
            /// Sets up the test.
            /// </summary>
            [SetUp]
            public void SetUp()
            {
            }

            /// <summary>
            /// Comparing aspect ratios
            /// </summary>
            [Test]
            public void AspectRatioComparison()
            {
                Assert.AreEqual(0, CompareAspectRatios(1.0f, 1.0f));
                Assert.AreEqual(0, CompareAspectRatios(0.5f, 0.5f));
                Assert.AreEqual(0, CompareAspectRatios(2.0f, 2.0f));

                Assert.AreEqual(1, CompareAspectRatios(0.5f, 1.0f));
                Assert.AreEqual(-1, CompareAspectRatios(1.0f, 2.0f));
                Assert.AreEqual(-1, CompareAspectRatios(1.0f, 0.5f));
                Assert.AreEqual(1, CompareAspectRatios(2.0f, 1.0f));

                Assert.AreEqual(-1, CompareAspectRatios(0.5f, Single.MaxValue));
                Assert.AreEqual(1, CompareAspectRatios(Single.MaxValue, 0.5f));
                Assert.AreEqual(1, CompareAspectRatios(Single.Epsilon, 2.0f));
                Assert.AreEqual(-1, CompareAspectRatios(2.0f, Single.Epsilon));

                Assert.AreEqual(1, CompareAspectRatios(0.9f, 1.1f));
                Assert.AreEqual(-1, CompareAspectRatios(0.9f, 1.2f));
            }
        }
#endif
        #endregion // Unit tests

    }
}
