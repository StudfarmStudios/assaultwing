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
    /// Type of texture used in static game graphics.
    /// </summary>
    public enum TextureName
    {
        #region Player viewport graphics

        /// <summary>
        /// Vertical viewport separator.
        /// </summary>
        ViewportSeparatorVertical,

        /// <summary>
        /// Player status display background.
        /// </summary>
        StatusDisplay,

        /// <summary>
        /// Ship damage bar.
        /// </summary>
        BarShip,

        /// <summary>
        /// Primary weapon load bar.
        /// </summary>
        BarMain,

        /// <summary>
        /// Secondary weapon load bar.
        /// </summary>
        BarSpecial,

        /// <summary>
        /// Player ships left icon.
        /// </summary>
        IconShipsLeft,

        /// <summary>
        /// Weapon load icon.
        /// </summary>
        IconWeaponLoad,

        /// <summary>
        /// Load bar marker.
        /// </summary>
        IconBarMarker,

        /// <summary>
        /// Player bonus box background.
        /// </summary>
        BonusBackground,

        /// <summary>
        /// Player bonus duration meter.
        /// </summary>
        BonusDuration,

        /// <summary>
        /// Player bonus icon for primary weapon load time upgrade.
        /// </summary>
        BonusIconWeapon1LoadTime,

        /// <summary>
        /// Player bonus icon for secondary weapon load time upgrade.
        /// </summary>
        BonusIconWeapon2LoadTime,

        /// <summary>
        /// Player bonus icon for secondary weapon upgrade Berserkers.
        /// </summary>
        Weapon2Berserkers,

        /// <summary>
        /// Player bonus icon for secondary weapon upgrade Bouncegun.
        /// </summary>
        Weapon2Bouncegun,

        /// <summary>
        /// Player radar background.
        /// </summary>
        Radar,

        /// <summary>
        /// Ship on radar
        /// </summary>
        RadarShip,

        /// <summary>
        /// Chat box background.
        /// </summary>
        ChatBox,

        /// <summary>
        /// Mini status display damage bar background.
        /// </summary>
        MiniDamageBackground,

        /// <summary>
        /// Mini status display damage bar fill.
        /// </summary>
        MiniDamageFill,

        #endregion Player viewport graphics

        /// <summary>
        /// Game overlay dialog background.
        /// </summary>
        OverlayDialogBackground,

        /// <summary>
        /// Progress bar background.
        /// </summary>
        ProgressBarBackground,

        /// <summary>
        /// Progress bar filling.
        /// </summary>
        ProgressBarFill,

        /// <summary>
        /// Progress bar flow effect.
        /// </summary>
        ProgressBarFlow,

        /// <summary>
        /// Menu system background.
        /// </summary>
        MenuBackground,

        #region Main menu graphics

        /// <summary>
        /// Main menu component background.
        /// </summary>
        MainMenuBackground,

        /// <summary>
        /// Main menu cursor.
        /// </summary>
        MainMenuCursor,

        /// <summary>
        /// Main menu highlight.
        /// </summary>
        MainMenuHighlight,

        #endregion Main menu graphics

        #region Equip menu graphics

        /// <summary>
        /// Equip menu component background.
        /// </summary>
        EquipMenuBackground,

        /// <summary>
        /// Equip menu background for network game status display.
        /// </summary>
        EquipMenuStatusDisplay,

        /// <summary>
        /// Equip menu background for one player pane.
        /// </summary>
        EquipMenuPlayerBackground,

        /// <summary>
        /// Equip menu player 1 pane top.
        /// </summary>
        EquipMenuPlayerTop1,

        /// <summary>
        /// Equip menu player 2 pane top.
        /// </summary>
        EquipMenuPlayerTop2,

        /// <summary>
        /// Equip menu cursor for player pane main display.
        /// </summary>
        EquipMenuCursorMain,

        /// <summary>
        /// Equip menu highlight for player pane main display.
        /// </summary>
        EquipMenuHighlightMain,

        #endregion Equip menu graphics

        #region Arena menu graphics

        /// <summary>
        /// Arena selection menu component background.
        /// </summary>
        ArenaMenuBackground,

        /// <summary>
        /// Arena selection menu cursor.
        /// </summary>
        ArenaMenuCursor,

        /// <summary>
        /// Arena selection menu highlight.
        /// </summary>
        ArenaMenuHighlight,

        /// <summary>
        /// Arena selection menu checkbox tag.
        /// </summary>
        ArenaMenuCheckboxTag,

        #endregion Arena menu graphics
    }

    /// <summary>
    /// Type of font used in static game graphics.
    /// </summary>
    public enum FontName
    {
        /// <summary>
        /// Font used in overlay graphics.
        /// </summary>
        Overlay,

        /// <summary>
        /// Huge font used in the menu system.
        /// </summary>
        MenuFontHuge,

        /// <summary>
        /// Big font used in the menu system.
        /// </summary>
        MenuFontBig,

        /// <summary>
        /// Small font used in the menu system.
        /// </summary>
        MenuFontSmall,
    }

    /// <summary>
    /// Basic graphics engine.
    /// </summary>
    class GraphicsEngineImpl : DrawableGameComponent
    {
        SpriteBatch spriteBatch;

        /// <summary>
        /// Names of overlay graphics.
        /// </summary>
        string[] overlayNames;

        /// <summary>
        /// Names of static fonts.
        /// </summary>
        string[] fontNames;

        /// <summary>
        /// Creates a new graphics engine.
        /// </summary>
        /// <param name="game">The Game to add the component to.</param>
        public GraphicsEngineImpl(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
            overlayNames = new string[] {
                // Player viewport
                "viewport_border_vertical",
                "gui_playerinfo_bg",
                "gui_playerinfo_bar_ship",
                "gui_playerinfo_bar_main",
                "gui_playerinfo_bar_special",
                "gui_playerinfo_ship",
                "gui_playerinfo_white_ball",
                "gui_playerinfo_white_rect",
                "gui_bonus_bg",
                "gui_bonus_duration",
                "b_icon_rapid_fire_1",
                "b_icon_rapid_fire_1",
                "b_icon_berserkers",
                "b_icon_bouncegun",
                "gui_radar_bg",
                "gui_playerinfo_white_ball", // HACK: Ship sprite on radar display
                "gui_console_bg",
                "mini_hpbar_bg",
                "mini_hpbar_fill",
                // General
                "ingame_dialog",
                "menu_progressbar_bg",
                "menu_progressbar_fill",
                "menu_progressbar_advancer",
                "menu_rustywall_bg",
                // Main menu
                "menu_main_bg",
                "menu_main_cursor",
                "menu_main_hilite",
                // Equip menu
                "menu_equip_bg",
                "menu_equip_status_display",
                "menu_equip_player_bg",
                "menu_equip_player_color_green",
                "menu_equip_player_color_red",
                "menu_equip_cursor_large",
                "menu_equip_hilite_large",
                // Arena menu
                "menu_levels_bg",
                "menu_levels_cursor",
                "menu_levels_hilite",
                "menu_levels_tag",
            };
            fontNames = new string[] {
                "ConsoleFont",
                "MenuFontHuge",
                "MenuFontBig",
                "MenuFontSmall",
            };
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
            data.ForEachTypeTemplate<Gob>(gobTemplate =>
            {
                foreach (var modelName in gobTemplate.ModelNames)
                {
                    if (data.Models.ContainsKey(modelName)) continue;
                    Model model = LoadModel(modelName);
                    if (model != null)
                        data.Models.Add(modelName, model);
                }
                foreach (var textureName in gobTemplate.TextureNames)
                {
                    if (data.Textures.ContainsKey(textureName)) continue;
                    Texture2D texture = LoadTexture(textureName);
                    if (texture != null)
                        data.Textures.Add(textureName, texture);
                }
            });

            // Load all textures that each weapon needs.
            data.ForEachTypeTemplate<Weapon>(weaponTemplate =>
            {
                foreach (var textureName in weaponTemplate.TextureNames)
                {
                    if (data.Textures.ContainsKey(textureName)) continue;
                    Texture2D texture = LoadTexture(textureName);
                    if (texture != null)
                        data.Textures.Add(textureName, texture);
                }
            });

            // Load static graphics.
            foreach (TextureName overlay in Enum.GetValues(typeof(TextureName)))
                data.AddTexture(overlay, LoadTexture(overlayNames[(int)overlay]));

            data.ArenaPreviews.Add((CanonicalString)"noPreview", LoadTexture("no_preview"));            
            foreach (var name in data.ArenaPlaylist)
            {
                string arenaName = name;
                Texture2D preview = LoadTexture(arenaName.ToLower() + "_preview");
                if (preview != null)
                    data.ArenaPreviews.Add((CanonicalString)arenaName, preview);
            }

            // Load static fonts.
            foreach (FontName font in Enum.GetValues(typeof(FontName)))
                data.AddFont(font, LoadFont(fontNames[(int)font]));

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
            var data = AssaultWing.Instance.DataEngine;

            foreach (var gob in arenaTemplate.Gobs)
            {
                // Load the layer's gob types.
                foreach (var modelName in gob.ModelNames)
                {
                    if (data.Models.ContainsKey(modelName)) continue;
                    Model model = LoadModel(modelName);
                    if (model != null)
                        data.Models.Add(modelName, model);
                }

                // Load the layer's gobs' textures.
                foreach (var textureName in gob.TextureNames)
                {
                    if (data.Textures.ContainsKey(textureName)) continue;
                    Texture2D texture = LoadTexture(textureName);
                    if (texture != null)
                        data.Textures.Add(textureName, texture);
                }

                gob.LoadContent();
            }

            foreach (ArenaLayer layer in arenaTemplate.Layers)
            {
                // Load the layer's parallax texture.
                var parallaxName = layer.ParallaxName;
                if (parallaxName != "" && !data.Textures.ContainsKey(parallaxName))
                {
                    Texture2D parallaxTexture = LoadTexture(parallaxName);
                    if (parallaxTexture != null)
                        data.Textures.Add(parallaxName, parallaxTexture);
                }
            }
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
            data.Textures.Clear();
            data.ArenaPreviews.Clear();
            data.Models.Clear();
            data.ClearFonts();

            // Propagate UnloadContent to other components that are known to
            // contain references to graphics content.
            data.ForEachViewport(viewport => viewport.UnloadContent());
            foreach (var gob in data.Arena.Gobs) gob.UnloadContent();
            data.UnloadContent();

            base.UnloadContent();
        }

        /// <summary>
        /// Loads a texture by name and manages errors.
        /// </summary>
        /// <param name="name">The names of the textures.</param>
        /// <returns>The loaded texture, or <b>null</b> on error.</returns>
        private Texture2D LoadTexture(string name)
        {
            try
            {
                string textureNamePath = System.IO.Path.Combine(Paths.Textures, name);
                Texture2D texture = AssaultWing.Instance.Content.Load<Texture2D>(textureNamePath);
                return texture;
            }
            catch (Microsoft.Xna.Framework.Content.ContentLoadException e)
            {
                Log.Write("Error loading texture " + name + " (" + e.Message + ")");
            }
            return null;
        }

        /// <summary>
        /// Loads a font by name and manages errors.
        /// </summary>
        /// <param name="name">The name of the font.</param>
        /// <returns>The loaded font, or <b>null</b> on error.</returns>
        private SpriteFont LoadFont(string name)
        {
            try
            {
                string fontNamePath = System.IO.Path.Combine(Paths.Fonts, name);
                SpriteFont font = AssaultWing.Instance.Content.Load<SpriteFont>(fontNamePath);
                return font;
            }
            catch (Microsoft.Xna.Framework.Content.ContentLoadException e)
            {
                Log.Write("Error loading font " + name + " (" + e.Message + ")");
            }
            return null;
        }

        /// <summary>
        /// Loads a 3D model by name and manages errors.
        /// </summary>
        /// <param name="name">The name of the 3D model.</param>
        /// <returns>The loaded 3D model, or <b>null</b> on error.</returns>
        private Model LoadModel(string name)
        {
            try
            {
                string modelNamePath = System.IO.Path.Combine(Paths.Models, name);
                Model model = AssaultWing.Instance.Content.Load<Model>(modelNamePath);
                return model;
            }
            catch (Microsoft.Xna.Framework.Content.ContentLoadException e)
            {
                Log.Write("Error loading 3D model " + name + " (" + e.Message + ")");
            }
            return null;
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
                Texture2D separatorTexture = AssaultWing.Instance.DataEngine.GetTexture(TextureName.ViewportSeparatorVertical);
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
