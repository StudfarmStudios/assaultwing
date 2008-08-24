// marked in as DEBUG because we don't want NUnit framework to release builds
#if DEBUG
using NUnit.Framework;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Helpers;
using AW2.Game.Particles;

namespace AW2.Graphics
{
    /// <summary>
    /// Basic graphics engine.
    /// </summary>
    class GraphicsEngineImpl : DrawableGameComponent, GraphicsEngine
    {
        SpriteBatch spriteBatch;

        /// <summary>
        /// The font to use for player view overlay text.
        /// </summary>
        SpriteFont overlayFont;

        /// <summary>
        /// Type of player viewport overlay graphics.
        /// </summary>
        enum ViewportOverlay
        {
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
        }

        /// <summary>
        /// Names of overlay graphics.
        /// </summary>
        string[] overlayNames;

        /// <summary>
        /// Overlay graphics.
        /// </summary>
        Texture2D[] overlays;

        /// <summary>
        /// The X-movement curve of a bonux box that enters a player's
        /// viewport overlay.
        /// </summary>
        /// The curve defines the relative X-coordinate (between 0 and 1)
        /// of the bonus box in respect of time in seconds. 0 means the box is
        /// not visible; 1 means the box is fully visible.
        Curve bonusBoxEntry;

        /// <summary>
        /// The X-movement curve of a bonux box that leaves a player's
        /// viewport overlay.
        /// </summary>
        /// The curve defines the relative X-coordinate (between 0 and 1)
        /// of the bonus box in respect of time in seconds. 0 means the box is
        /// not visible; 1 means the box is fully visible.
        Curve bonusBoxExit;

        /// <summary>
        /// The Y-movement curve of a bonux box that is giving space for another
        /// bonus box that is entering a player's viewport overlay.
        /// </summary>
        /// The curve defines the relative Y-coordinate (between 0 and 1)
        /// of the bonus box in respect of time in seconds. 0 means the box is
        /// still blocking the other box; 1 means the box has moved totally aside.
        Curve bonusBoxAvoid;

        /// <summary>
        /// The Y-movement curve of a bonux box that is closing in space from another
        /// bonus box that is leaving a player's viewport overlay.
        /// </summary>
        /// The curve defines the relative Y-coordinate (between 0 and 1)
        /// of the bonus box in respect of time in seconds. 0 means the box is
        /// still blocking the other box; 1 means the box has moved totally aside.
        Curve bonusBoxClose;

        /// <summary>
        /// Creates a new graphics engine.
        /// </summary>
        /// <param name="game">The Game to add the component to.</param>
        public GraphicsEngineImpl(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
            overlayNames = new string[] {
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
            };
            overlays = new Texture2D[Enum.GetValues(typeof(ViewportOverlay)).Length];
            bonusBoxEntry = new Curve();
            bonusBoxEntry.Keys.Add(new CurveKey(0, 0));
            bonusBoxEntry.Keys.Add(new CurveKey(0.3f, 1));
            bonusBoxEntry.Keys.Add(new CurveKey(0.9f, 1));
            bonusBoxEntry.Keys.Add(new CurveKey(2.0f, 1));
            bonusBoxEntry.ComputeTangents(CurveTangent.Smooth);
            bonusBoxEntry.PostLoop = CurveLoopType.Constant;
            bonusBoxExit = new Curve();
            bonusBoxExit.Keys.Add(new CurveKey(0, 1));
            bonusBoxExit.Keys.Add(new CurveKey(0.4f, 0.2f));
            bonusBoxExit.Keys.Add(new CurveKey(1.0f, 0));
            bonusBoxExit.ComputeTangents(CurveTangent.Smooth);
            bonusBoxExit.PostLoop = CurveLoopType.Constant;
            bonusBoxAvoid = new Curve();
            bonusBoxAvoid.Keys.Add(new CurveKey(0, 0));
            bonusBoxAvoid.Keys.Add(new CurveKey(0.2f, 0.8f));
            bonusBoxAvoid.Keys.Add(new CurveKey(0.5f, 1));
            bonusBoxAvoid.Keys.Add(new CurveKey(1.0f, 1));
            bonusBoxAvoid.ComputeTangents(CurveTangent.Smooth);
            bonusBoxAvoid.PostLoop = CurveLoopType.Constant;
            bonusBoxClose = new Curve();
            bonusBoxClose.Keys.Add(new CurveKey(0, 1));
            bonusBoxClose.Keys.Add(new CurveKey(0.4f, 0.8f));
            bonusBoxClose.Keys.Add(new CurveKey(1.0f, 0));
            bonusBoxClose.ComputeTangents(CurveTangent.Smooth);
            bonusBoxClose.PostLoop = CurveLoopType.Constant;
        }

        /// <summary>
        /// Called when the component needs to load graphics resources.
        /// </summary>
        protected override void LoadContent()
        {
            Log.Write("Graphics engine loading graphics content.");
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            spriteBatch = new SpriteBatch(this.GraphicsDevice);

            // Load fonts.
            overlayFont = this.Game.Content.Load<SpriteFont>(System.IO.Path.Combine("fonts", "ConsoleFont"));

            // Loop through gob types and load all the 3D models and textures they need.
            data.ForEachTypeTemplate<Gob>(delegate(Gob gobTemplate)
            {
                foreach (string modelName in gobTemplate.ModelNames)
                {
                    if (data.HasModel(modelName)) continue;
                    Model model = LoadModel(modelName);
                    if (model != null)
                        data.AddModel(modelName, model);
                }
                foreach (string textureName in gobTemplate.TextureNames)
                {
                    if (data.HasTexture(textureName)) continue;
                    Texture2D texture = LoadTexture(textureName);
                    if (texture != null)
                        data.AddTexture(textureName, texture);
                }
            });
            
            // Loop through arenas and load all the 3D models and textures they need.
            data.ForEachArena(delegate(Arena arenaTemplate)
            {
                foreach (Gob gob in arenaTemplate.Gobs)
                    foreach (string modelName in gob.ModelNames)
                    {
                        if (data.HasModel(modelName)) continue;
                        Model model = LoadModel(modelName);
                        if (model != null)
                            data.AddModel(modelName, model);
                    }
                foreach (string textureName in arenaTemplate.ParallaxNames) 
                {
                    if (data.HasTexture(textureName)) continue;
                    Texture2D texture = LoadTexture(textureName);
                    if (texture != null)
                        data.AddTexture(textureName, texture);
                }
            });

            // Load all textures that each weapon needs.
            data.ForEachTypeTemplate<Weapon>(delegate(Weapon weapon)
            {
                foreach (string textureName in weapon.TextureNames)
                {
                    if (data.HasTexture(textureName)) continue;
                    Texture2D texture = LoadTexture(textureName);
                    if (texture != null)
                        data.AddTexture(textureName, texture);
                }
            });

            // Load overlay graphics.
            foreach (ViewportOverlay overlay in Enum.GetValues(typeof(ViewportOverlay)))
            {
                overlays[(int)overlay] = LoadTexture(overlayNames[(int)overlay]);
            }
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
                string textureNamePath = System.IO.Path.Combine("textures", name);
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
        /// Loads a 3D model by name and manages errors.
        /// </summary>
        /// <param name="name">The name of the 3D model.</param>
        /// <returns>The loaded 3D model, or <b>null</b> on error.</returns>
        private Model LoadModel(string name)
        {
            try
            {
                string modelNamePath = System.IO.Path.Combine("models", name);
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
            DataEngine data = (DataEngine)Game.Services.GetService(typeof(DataEngine));
            GraphicsDeviceManager graphics = (GraphicsDeviceManager)Game.Services.GetService(typeof(IGraphicsDeviceManager));

            Viewport screen = graphics.GraphicsDevice.Viewport;
            screen.X = 0;
            screen.Y = 0;
            screen.Width = AssaultWing.Instance.ClientBounds.Width;
            screen.Height = AssaultWing.Instance.ClientBounds.Height;
            graphics.GraphicsDevice.Viewport = screen;
            graphics.GraphicsDevice.Clear(new Color(0x40, 0x40, 0x40));

            // Draw all viewports.
            Action<AWViewport> drawViewport = delegate(AWViewport viewport)
            {
                GraphicsDevice.Viewport = viewport.InternalViewport;
                graphics.GraphicsDevice.Clear(Color.Chocolate);
                
                #region 2D graphics

                data.Arena.DrawParallaxes(spriteBatch, viewport);
                // TODO: Make DrawParallaxes not force texture looping to Clamp.
                AssaultWing.Instance.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
                AssaultWing.Instance.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;

                #endregion 2D graphics

                // Restore renderstate for 3D graphics.
                graphics.GraphicsDevice.RenderState.DepthBufferEnable = true;
                graphics.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
                
                #region 3D graphics
                
                Action<Gob> drawGob = delegate(Gob gob)
                {
                    if (!(gob is ParticleEngine)) // HACK: Should implement draw order to Gob
                        gob.Draw(viewport.ViewMatrix, viewport.ProjectionMatrix, spriteBatch);
                };
                data.ForEachGob(drawGob);
                
                #endregion 3D graphics
                
                # region particles

                Action<ParticleEngine> drawParticles = delegate(ParticleEngine pEng)
                {
                    pEng.Draw(viewport.ViewMatrix, viewport.ProjectionMatrix, spriteBatch);
                };
                spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.SaveState);
                data.ForEachParticleEngine(drawParticles);
                spriteBatch.End();

                #endregion

                #region 2D overlay graphics

                if (viewport is PlayerViewport)
                {
                    PlayerViewport plrViewport = (PlayerViewport)viewport;
                    DrawPlayerOverlay(plrViewport);
                    plrViewport.Player.AttenuateShake();
                }

                #endregion

            };
            data.ForEachViewport(drawViewport);

            // Restore viewport to the whole client window.
            graphics.GraphicsDevice.Viewport = screen;

            // Draw viewport separators.
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            data.ForEachViewportSeparator(delegate(ViewportSeparator separator)
            {
                Texture2D separatorTexture = overlays[(int)ViewportOverlay.ViewportSeparatorVertical];
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
            DataEngine data = (DataEngine)Game.Services.GetService(typeof(DataEngine));
            data.ClearViewports();
            int players = 0;
            data.ForEachPlayer(delegate(Player player) { ++players; });
            if (players == 0) return;

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
            for (int rows = 1; rows <= players; ++rows)
            {
                // Only check out grids with cells as many as players.
                if (players % rows != 0) continue;
                int columns = players / rows;
                int viewportWidth = window.Width / columns;
                int viewportHeight = window.Height / rows;
                float aspectRatio = (float)viewportHeight / (float)viewportWidth;
                if (CompareAspectRatios(aspectRatio, bestAspectRatio) < 0)
                {
                    bestAspectRatio = aspectRatio;
                    bestRows = rows;
                }
            }
            int bestColumns = players / bestRows;

            // Assign the viewports to players.
            int playerI = 0;
            data.ForEachPlayer(delegate(Player player)
            {
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
            });

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
            DataEngine data = (DataEngine)Game.Services.GetService(typeof(DataEngine));
            data.ClearViewports();
            Rectangle window = AssaultWing.Instance.ClientBounds;
            int playerI = 0;
            data.ForEachPlayer(delegate(Player player) {
                if (playerI == privilegedPlayer)
                {
                    Rectangle onScreen = new Rectangle(0, 0, window.Width, window.Height);
                    AWViewport viewport = new PlayerViewport(player, onScreen);
                    data.AddViewport(viewport);
                }
                ++playerI;
            });
        }

        /// <summary>
        /// Draws overlay graphics into a player viewport.
        /// </summary>
        /// <param name="viewport">The player viewport.</param>
        private void DrawPlayerOverlay(PlayerViewport viewport)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);

            // Status display background
            spriteBatch.Draw(overlays[(int)ViewportOverlay.StatusDisplay],
                new Vector2(viewport.InternalViewport.Width, 0) / 2,
                null, Color.White, 0,
                new Vector2(overlays[(int)ViewportOverlay.StatusDisplay].Width, 0) / 2,
                1, SpriteEffects.None, 0);

            // Damage meter
            if (viewport.Player.Ship != null)
            {
                Rectangle damageBarRect = new Rectangle(0, 0,
                    (int)Math.Ceiling((1 - viewport.Player.Ship.DamageLevel / viewport.Player.Ship.MaxDamageLevel)
                    * overlays[(int)ViewportOverlay.BarShip].Width),
                    overlays[(int)ViewportOverlay.BarShip].Height);
                Color damageBarColor = Color.White;
                if (viewport.Player.Ship.DamageLevel / viewport.Player.Ship.MaxDamageLevel >= 0.8f)
                {
                    float seconds = (float)AssaultWing.Instance.GameTime.TotalRealTime.TotalSeconds;
                    if (seconds % 0.5f < 0.25f)
                        damageBarColor = Color.Red;
                }
                spriteBatch.Draw(overlays[(int)ViewportOverlay.BarShip],
                    new Vector2(viewport.InternalViewport.Width, 8 * 2) / 2,
                    damageBarRect, damageBarColor, 0,
                    new Vector2(overlays[(int)ViewportOverlay.BarShip].Width, 0) / 2,
                    1, SpriteEffects.None, 0);
            }

            // Player lives left
            for (int i = 0; i < viewport.Player.Lives; ++i)
                spriteBatch.Draw(overlays[(int)ViewportOverlay.IconShipsLeft],
                    new Vector2(viewport.InternalViewport.Width +
                                overlays[(int)ViewportOverlay.BarShip].Width + (8 + i * 10) * 2,
                                overlays[(int)ViewportOverlay.BarShip].Height + 8 * 2) / 2,
                    null,
                    Color.White,
                    0,
                    new Vector2(0, overlays[(int)ViewportOverlay.IconShipsLeft].Height) / 2,
                    1, SpriteEffects.None, 0);

            // Primary weapon charge
            if (viewport.Player.Ship != null)
            {
                Rectangle charge1BarRect = new Rectangle(0, 0,
                    (int)Math.Ceiling(viewport.Player.Ship.Weapon1Charge / viewport.Player.Ship.Weapon1ChargeMax
                    * overlays[(int)ViewportOverlay.BarMain].Width),
                    overlays[(int)ViewportOverlay.BarMain].Height);
                spriteBatch.Draw(overlays[(int)ViewportOverlay.BarMain],
                    new Vector2(viewport.InternalViewport.Width, 24 * 2) / 2,
                    charge1BarRect, Color.White, 0,
                    new Vector2(overlays[(int)ViewportOverlay.BarMain].Width, 0) / 2,
                    1, SpriteEffects.None, 0);
            }

            // Primary weapon loadedness
            if (viewport.Player.Ship != null)
            {
                if (viewport.Player.Ship.Weapon1Loaded)
                {
                    float seconds = (float)(AssaultWing.Instance.GameTime.TotalGameTime - viewport.Player.Ship.Weapon1.LoadedTime).TotalSeconds;
                    Texture2D texture = overlays[(int)ViewportOverlay.IconWeaponLoad];
                    float scale = 1;
                    Color color = Color.White;
                    if (seconds < 0.2f)
                    {
                        scale = MathHelper.Lerp(3, 1, seconds / 0.2f);
                        color = new Color(Vector4.Lerp(new Vector4(0, 1, 0, 0.1f), Vector4.One, seconds / 0.2f));
                    }
                    spriteBatch.Draw(texture,
                        new Vector2(viewport.InternalViewport.Width + texture.Width +
                                    overlays[(int)ViewportOverlay.BarMain].Width + 8 * 2,
                                    overlays[(int)ViewportOverlay.BarMain].Height + 24 * 2) / 2,
                        null, color, 0,
                        new Vector2(texture.Width, texture.Height) / 2,
                        scale, SpriteEffects.None, 0);
                }
            }

            // Secondary weapon charge
            if (viewport.Player.Ship != null)
            {
                Rectangle charge2BarRect = new Rectangle(0, 0,
                    (int)Math.Ceiling(viewport.Player.Ship.Weapon2Charge / viewport.Player.Ship.Weapon2ChargeMax
                    * overlays[(int)ViewportOverlay.BarSpecial].Width),
                    overlays[(int)ViewportOverlay.BarSpecial].Height);
                spriteBatch.Draw(overlays[(int)ViewportOverlay.BarSpecial],
                    new Vector2(viewport.InternalViewport.Width, 40 * 2) / 2,
                    charge2BarRect, Color.White, 0,
                    new Vector2(overlays[(int)ViewportOverlay.BarSpecial].Width, 0) / 2,
                    1, SpriteEffects.None, 0);
            }

            // Secondary weapon loadedness
            if (viewport.Player.Ship != null)
            {
                if (viewport.Player.Ship.Weapon2Loaded)
                {
                    float seconds = (float)(AssaultWing.Instance.GameTime.TotalGameTime - viewport.Player.Ship.Weapon2.LoadedTime).TotalSeconds;
                    Texture2D texture = overlays[(int)ViewportOverlay.IconWeaponLoad];
                    float scale = 1;
                    Color color = Color.White;
                    if (seconds < 0.2f)
                    {
                        scale = MathHelper.Lerp(3, 1, seconds / 0.2f);
                        color = new Color(Vector4.Lerp(new Vector4(0, 1, 0, 0.2f), Vector4.One, seconds / 0.2f));
                    }
                    spriteBatch.Draw(texture,
                        new Vector2(viewport.InternalViewport.Width + texture.Width +
                                    overlays[(int)ViewportOverlay.BarSpecial].Width + 8 * 2,
                                    overlays[(int)ViewportOverlay.BarSpecial].Height + 40 * 2) / 2,
                        null, color, 0,
                        new Vector2(texture.Width, texture.Height) / 2,
                        scale, SpriteEffects.None, 0);
                }
            }

            // Draw bonus display.
            Vector2 bonusBoxSize = new Vector2(overlays[(int)ViewportOverlay.BonusBackground].Width,
                                               overlays[(int)ViewportOverlay.BonusBackground].Height);

            // 'bonusPos' lists bottom left coordinates of areas reserved for
            // displayed bonus boxes relative to bonus box area top right corner,
            // with the exception that the first coordinates are (0,0).
            // Bonus boxes are then drawn vertically centered in these areas.
            // The last Y coordinate will state the height of the whole reserved bonus box area.
            // 'bonusBonus' lists the types of bonuses whose coordinates are in 'bonusPos'.
            List<Vector2> bonusPos = new List<Vector2>();
            List<PlayerBonus> bonusBonus = new List<PlayerBonus>();
            bonusPos.Add(Vector2.Zero);
            bonusBonus.Add(PlayerBonus.None);
            foreach (PlayerBonus bonus in Enum.GetValues(typeof(PlayerBonus)))
            {
                if (bonus == PlayerBonus.None) continue;

                // Calculate bonus box position.
                float slideTime = (float)(AssaultWing.Instance.GameTime.TotalGameTime.TotalSeconds
                    - viewport.BonusEntryTimeins[bonus].TotalSeconds);
                Vector2 adjustment = viewport.BonusEntryPosAdjustments[bonus];
                Vector2 curvePos, shift, scale;
                if (viewport.BonusEntryDirections[bonus])
                {   
                    curvePos = new Vector2(bonusBoxEntry.Evaluate(slideTime), bonusBoxAvoid.Evaluate(slideTime));
                    shift = new Vector2(adjustment.X - bonusBoxEntry.Evaluate(0),
                        adjustment.Y - bonusBoxAvoid.Evaluate(0));
                    scale = new Vector2((adjustment.X - bonusBoxEntry.Keys[bonusBoxEntry.Keys.Count - 1].Value)
                        / (bonusBoxEntry.Evaluate(0) - bonusBoxEntry.Keys[bonusBoxEntry.Keys.Count - 1].Value),
                        (adjustment.Y - bonusBoxAvoid.Keys[bonusBoxAvoid.Keys.Count - 1].Value)
                        / (bonusBoxAvoid.Evaluate(0) - bonusBoxAvoid.Keys[bonusBoxAvoid.Keys.Count - 1].Value));
                }
                else
                {
                    curvePos = new Vector2(bonusBoxExit.Evaluate(slideTime), bonusBoxClose.Evaluate(slideTime));
                    shift = new Vector2(adjustment.Y - bonusBoxExit.Evaluate(0),
                        adjustment.Y - bonusBoxClose.Evaluate(0));
                    scale = new Vector2((adjustment.X - bonusBoxExit.Keys[bonusBoxExit.Keys.Count - 1].Value)
                        / (bonusBoxExit.Evaluate(0) - bonusBoxExit.Keys[bonusBoxExit.Keys.Count - 1].Value),
                        (adjustment.Y - bonusBoxClose.Keys[bonusBoxClose.Keys.Count - 1].Value)
                        / (bonusBoxClose.Evaluate(0) - bonusBoxClose.Keys[bonusBoxClose.Keys.Count - 1].Value));
                }
                Vector2 relativePos = new Vector2(
                    (curvePos.X + shift.X) * scale.X,
                    (curvePos.Y + shift.Y) * scale.Y);
                    //adjustment.X + (1 - adjustment.X) * curvePos.X,
                    //adjustment.Y + (1 - adjustment.Y) * curvePos.Y);
                Vector2 newBonusPos = new Vector2(-bonusBoxSize.X * relativePos.X,
                        bonusPos[bonusPos.Count - 1].Y + bonusBoxSize.Y * relativePos.Y);

                // React to changes in player's bonuses.
                if ((viewport.Player.Bonuses & bonus) != 0 &&
                    !viewport.BonusEntryDirections[bonus])
                {
                    viewport.BonusEntryDirections[bonus] = true;
                    viewport.BonusEntryPosAdjustments[bonus] = relativePos;
                    viewport.BonusEntryTimeins[bonus] = AssaultWing.Instance.GameTime.TotalGameTime;
                }
                if ((viewport.Player.Bonuses & bonus) == 0 &&
                    viewport.BonusEntryDirections[bonus])
                {
                    viewport.BonusEntryDirections[bonus] = false;
                    viewport.BonusEntryPosAdjustments[bonus] = relativePos;
                    viewport.BonusEntryTimeins[bonus] = AssaultWing.Instance.GameTime.TotalGameTime;
                }

                // Draw the box only if it's visible.
                if (newBonusPos.X < 0)
                {
                    bonusBonus.Add(bonus);
                    bonusPos.Add(newBonusPos);
                }
            }
            Vector2 bonusBoxAreaTopRight = new Vector2(viewport.InternalViewport.Width * 2,
                viewport.InternalViewport.Height - bonusPos[bonusPos.Count - 1].Y) / 2;
            for (int i = 1; i < bonusBonus.Count; ++i)
            {
                Vector2 leftMiddlePoint = new Vector2(bonusPos[i].X + bonusBoxAreaTopRight.X,
                    MathHelper.Lerp(bonusPos[i].Y + bonusBoxAreaTopRight.Y, bonusPos[i - 1].Y + bonusBoxAreaTopRight.Y, 0.5f));
                DrawBonusBox(leftMiddlePoint, bonusBonus[i], viewport.Player);
            }

            // Radar background
            spriteBatch.Draw(overlays[(int)ViewportOverlay.Radar],
                Vector2.Zero, Color.White);

            // Ships on radar
            Vector2 radarDisplayDimensions = new Vector2(162, 150); // TODO: Make this constant configurable
            Vector2 radarDisplayTopLeft = new Vector2(0, 1); // TODO: Make this constant configurable
            Vector2 arenaDimensions = data.Arena.Dimensions;
            float arenaToRadarScale = Math.Min(
                radarDisplayDimensions.X / arenaDimensions.X,
                radarDisplayDimensions.Y / arenaDimensions.Y);
            float arenaHeightOnRadar = arenaDimensions.Y * arenaToRadarScale;
            Vector2 arenaToRadarScaleAndFlip = new Vector2(arenaToRadarScale, -arenaToRadarScale);
            Matrix arenaToRadarTransform = 
                Matrix.CreateScale(arenaToRadarScale, -arenaToRadarScale, 1) *
                Matrix.CreateTranslation(radarDisplayTopLeft.X, radarDisplayTopLeft.Y + arenaHeightOnRadar, 0);
            Texture2D shipOnRadarTexture = overlays[(int)ViewportOverlay.RadarShip];
            Vector2 shipOnRadarTextureCenter = new Vector2(shipOnRadarTexture.Width, shipOnRadarTexture.Height) / 2;
            data.ForEachPlayer(delegate(Player player)
            {
                if (player.Ship == null) return;
                Vector2 posInArena = player.Ship.Pos;
                Vector2 posOnRadar = Vector2.Transform(posInArena, arenaToRadarTransform);
                spriteBatch.Draw(shipOnRadarTexture, posOnRadar, null, Color.White, 0,
                    shipOnRadarTextureCenter, 1, SpriteEffects.None, 0);
            });

            spriteBatch.End();
        }

        /// <summary>
        /// Draws a player bonus box.
        /// </summary>
        /// <param name="bonusPos">The position at which to draw 
        /// the background's left middle point.</param>
        /// <param name="playerBonus">Which player bonus it is.</param>
        /// <param name="player">The player whose bonus it is.</param>
        private void DrawBonusBox(Vector2 bonusPos, PlayerBonus playerBonus, Player player)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));

            // Figure out what to draw for this bonus.
            string bonusText;
            Texture2D bonusIcon;
            switch (playerBonus)
            {
                case PlayerBonus.Weapon1LoadTime:
                    bonusText = player.Weapon1Name + "\nspeedloader";
                    bonusIcon = overlays[(int)ViewportOverlay.BonusIconWeapon1LoadTime];
                    break;
                case PlayerBonus.Weapon2LoadTime:
                    bonusText = player.Weapon2Name + "\nspeedloader";
                    bonusIcon = overlays[(int)ViewportOverlay.BonusIconWeapon2LoadTime];
                    break;
                case PlayerBonus.Weapon1Upgrade:
                    {
                        Weapon weapon1 = player.Ship != null
                            ? player.Ship.Weapon1
                            : (Weapon)data.GetTypeTemplate(typeof(Weapon), player.Weapon1Name);
                        bonusText = player.Weapon1Name;
                        bonusIcon = data.GetTexture(weapon1.IconName);
                    }
                    break;
                case PlayerBonus.Weapon2Upgrade:
                    {
                        Weapon weapon2 = player.Ship != null
                            ? player.Ship.Weapon2
                            : (Weapon)data.GetTypeTemplate(typeof(Weapon), player.Weapon2Name);
                        bonusText = player.Weapon2Name;
                        bonusIcon = data.GetTexture(weapon2.IconName);
                    }
                    break;
                default:
                    bonusText = "<unknown>";
                    bonusIcon = overlays[(int)ViewportOverlay.IconWeaponLoad];
                    Log.Write("Warning: Don't know how to draw player bonus box " + playerBonus);
                    break;
            }

            // Draw bonus box background.
            Vector2 backgroundOrigin = new Vector2(0,
                overlays[(int)ViewportOverlay.BonusBackground].Height) / 2;
            spriteBatch.Draw(overlays[(int)ViewportOverlay.BonusBackground],
                bonusPos, null, Color.White, 0, backgroundOrigin, 1, SpriteEffects.None, 0);

            // Draw bonus icon.
            Vector2 iconPos = bonusPos - backgroundOrigin + new Vector2(112, 9);
            spriteBatch.Draw(bonusIcon,
                iconPos, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            // Draw bonus duration meter.
            float startSeconds = (float)player.BonusTimeins[playerBonus].TotalSeconds;
            float endSeconds = (float)player.BonusTimeouts[playerBonus].TotalSeconds;
            float nowSeconds = (float)AssaultWing.Instance.GameTime.TotalGameTime.TotalSeconds;
            float duration = (endSeconds - nowSeconds) / (endSeconds - startSeconds);
            int durationHeight = (int)Math.Round(duration * overlays[(int)ViewportOverlay.BonusDuration].Height);
            int durationY = overlays[(int)ViewportOverlay.BonusDuration].Height - durationHeight;
            Rectangle durationClip = new Rectangle(0, durationY,
                overlays[(int)ViewportOverlay.BonusDuration].Width, durationHeight);
            Vector2 durationPos = bonusPos - backgroundOrigin + new Vector2(14, 8 + durationY);
            spriteBatch.Draw(overlays[(int)ViewportOverlay.BonusDuration],
                durationPos, durationClip, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            // Draw bonus text.
            // Round coordinates for beautiful text.
            Vector2 textSize = overlayFont.MeasureString(bonusText);
            Vector2 textPos = bonusPos - backgroundOrigin + new Vector2(32, 23.5f - textSize.Y / 2);
            textPos.X = (float)Math.Round(textPos.X);
            textPos.Y = (float)Math.Round(textPos.Y);
            spriteBatch.DrawString(overlayFont, bonusText, textPos, Color.White);
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
