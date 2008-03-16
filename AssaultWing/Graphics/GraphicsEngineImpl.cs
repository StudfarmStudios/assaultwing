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
                "gui_playerinfo_bg",
                "gui_playerinfo_bar_ship",
                "gui_playerinfo_bar_main",
                "gui_playerinfo_bar_special",
                "gui_playerinfo_ship",
                "gui_playerinfo_white_ball",
                "gui_playerinfo_white_rect",
                "gui_bonus_bg",
                "gui_bonus_duration",
                "b_icon_general",
                "b_icon_general",
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

            AssaultWing game = (AssaultWing)Game;
            DataEngine data = (DataEngine)game.Services.GetService(typeof(DataEngine));

            spriteBatch = new SpriteBatch(this.GraphicsDevice);

            // Load fonts.
            overlayFont = this.Game.Content.Load<SpriteFont>(System.IO.Path.Combine("fonts", "ConsoleFont"));

            // Loop through gob types and load all the 3D models and textures they need.
            data.ForEachTypeTemplate<Gob>(delegate(Gob gobTemplate)
            {
                List<string> modelNames = gobTemplate.ModelNames;
                foreach (string modelName in modelNames)
                {
                    try
                    {
                        string modelNamePath = System.IO.Path.Combine("models", modelName);
                        Model model = game.Content.Load<Model>(modelNamePath);
                        data.AddModel(modelName, model);
                    }
                    catch (Microsoft.Xna.Framework.Content.ContentLoadException e)
                    {
                        Log.Write("Error loading model " + modelName + " (" + e.Message + ")");
                    }
                }
                List<string> textureNames = gobTemplate.TextureNames;
                foreach (string textureName in textureNames)
                {
                    try
                    {
                        data.AddTexture(textureName, LoadTexture(game, textureName));
                    }
                    catch (Microsoft.Xna.Framework.Content.ContentLoadException e)
                    {
                        Log.Write("Error loading texture " + textureName + " (" + e.Message + ")");
                    }
                }
            });
            
            // HACK: These model names are known only runtime. FIX THIS SOON
            foreach (string modelName in new string[] { "wall_1", "wall_2", "wall_3", "wall_4",
                "wall_5", "wall_6", "wall_7", "wall_8", "wall_9", "wall_10", "wall_11", "wall_12", "shield", "demonskull", "greendiamond", "orangediamond", "bluediamond", "spear", "bones", "gravestone", })
            {
                Model wallModel = game.Content.Load<Model>(System.IO.Path.Combine("models", modelName));
                data.AddModel(modelName, wallModel);
            }

            data.ForEachArena(delegate(Arena arenaTemplate)
            {
                Log.Write("Loading textures for arena: " + arenaTemplate.Name);
                foreach (string textureName in arenaTemplate.ParallaxNames) 
                {
                    try
                    {
                        data.AddTexture(textureName, LoadTexture(game, textureName));
                    }
                    catch (Microsoft.Xna.Framework.Content.ContentLoadException e)
                    {
                        Log.Write("Error loading texture " + textureName + " (" + e.Message + ")");
                    }
                }
            });

            // Load overlay graphics.
            foreach (ViewportOverlay overlay in Enum.GetValues(typeof(ViewportOverlay)))
            {
                try
                {
                    overlays[(int)overlay] = LoadTexture(game, overlayNames[(int)overlay]);
                }
                catch (Exception e)
                {
                    Log.Write("Error loading texture " + overlayNames[(int)overlay] + " (" + e.Message + ")");
                }
            }
        }

        private Texture2D LoadTexture(AssaultWing game, string name)
        {
            string textureNamePath = System.IO.Path.Combine("textures", name);
            return game.Content.Load<Texture2D>(textureNamePath);
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
            screen.Width = graphics.GraphicsDevice.DisplayMode.Width;
            screen.Height = graphics.GraphicsDevice.DisplayMode.Height;
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
                }

                #endregion

            };
            data.ForEachViewport(drawViewport);

            graphics.GraphicsDevice.Viewport = screen; // return back to original

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
            int bestRows = 0;
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
                spriteBatch.Draw(overlays[(int)ViewportOverlay.BarShip],
                    new Vector2(viewport.InternalViewport.Width, 8 * 2) / 2,
                    damageBarRect, Color.White, 0,
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
                    spriteBatch.Draw(overlays[(int)ViewportOverlay.IconWeaponLoad],
                        new Vector2(viewport.InternalViewport.Width +
                                    overlays[(int)ViewportOverlay.BarMain].Width + 8 * 2,
                                    overlays[(int)ViewportOverlay.BarMain].Height + 24 * 2) / 2,
                        null,
                        //plrViewport.Player.Ship.Weapon1Loaded ? Color.Green : Color.Red,
                        Color.White,
                        0,
                        new Vector2(0, overlays[(int)ViewportOverlay.IconWeaponLoad].Height) / 2,
                        1, SpriteEffects.None, 0);
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
                    spriteBatch.Draw(overlays[(int)ViewportOverlay.IconWeaponLoad],
                        new Vector2(viewport.InternalViewport.Width +
                                    overlays[(int)ViewportOverlay.BarSpecial].Width + 8 * 2,
                                    overlays[(int)ViewportOverlay.BarSpecial].Height + 40 * 2) / 2,
                        null,
                        //plrViewport.Player.Ship.Weapon2Loaded ? Color.Green : Color.Red,
                        Color.White,
                        0,
                        new Vector2(0, overlays[(int)ViewportOverlay.IconWeaponLoad].Height) / 2,
                        1, SpriteEffects.None, 0);
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
            // Figure out what to draw for this bonus.
            string bonusText;
            ViewportOverlay bonusIcon;
            switch (playerBonus)
            {
                case PlayerBonus.Weapon1LoadTime:
                    bonusText = player.Weapon1Name + "\nspeedloader";
                    bonusIcon = ViewportOverlay.BonusIconWeapon1LoadTime;
                    break;
                case PlayerBonus.Weapon2LoadTime:
                    bonusText = player.Weapon2Name + "\nspeedloader";
                    bonusIcon = ViewportOverlay.BonusIconWeapon2LoadTime;
                    break;
                case PlayerBonus.Weapon1Upgrade:
                    bonusText = player.Weapon1Name;
                    bonusIcon = ViewportOverlay.BonusIconWeapon1LoadTime; // TODO: Weapon icons
                    break;
                case PlayerBonus.Weapon2Upgrade:
                    bonusText = player.Weapon2Name;
                    bonusIcon = ViewportOverlay.BonusIconWeapon1LoadTime; // TODO: Weapon icons
                    break;
                default:
                    bonusText = "<unknown>";
                    bonusIcon = ViewportOverlay.IconWeaponLoad;
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
            spriteBatch.Draw(overlays[(int)bonusIcon],
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
