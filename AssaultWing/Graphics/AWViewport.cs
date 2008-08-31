using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Game.Particles;
using AW2.Helpers;

namespace AW2.Graphics
{
    /// <summary>
    /// A view on the display that looks into the game world.
    /// </summary>
    public interface AWViewport
    {
        /// <summary>
        /// The location of the view on screen.
        /// </summary>
        Viewport InternalViewport { get; }

        /// <summary>
        /// The view matrix of the viewport.
        /// </summary>
        Matrix ViewMatrix { get; }

        /// <summary>
        /// The projection matrix of the viewport.
        /// </summary>
        Matrix ProjectionMatrix { get; }

        /// <summary>
        /// Checks if a bounding volume might be visible in the viewport.
        /// </summary>
        /// <param name="volume">The bounding volume.</param>
        /// <returns><b>false</b> if the bounding volume definitely cannot be seen in the viewport;
        /// <b>true</b> otherwise.</returns>
        bool Intersects(BoundingSphere volume);

        /// <summary>
        /// Checks if a bounding volume might be visible in the viewport.
        /// </summary>
        /// <param name="volume">The bounding volume.</param>
        /// <returns><b>false</b> if the bounding volume definitely cannot be seen in the viewport;
        /// <b>true</b> otherwise.</returns>
        bool Intersects(BoundingBox volume);

        /// <summary>
        /// Draws the viewport's contents.
        /// </summary>
        void Draw();
    }

    /// <summary>
    /// A visual separator between viewports.
    /// </summary>
    public struct ViewportSeparator
    {
        /// <summary>
        /// If <b>true</b>, the separator is vertical;
        /// if <b>false</b>, the separator is horizontal.
        /// </summary>
        public bool vertical;

        /// <summary>
        /// The X coordinate of a vertical separator, or
        /// the Y coordinate of a horizontal separator.
        /// </summary>
        public int coordinate;

        /// <summary>
        /// Creates a new viewport separator.
        /// </summary>
        /// <param name="vertical">Is the separator vertical.</param>
        /// <param name="coordinate">The X or Y coordinate of the separator.</param>
        public ViewportSeparator(bool vertical, int coordinate)
        {
            this.vertical = vertical;
            this.coordinate = coordinate;
        }
    }
    
    /// <summary>
    /// A viewport that follows a player.
    /// </summary>
    class PlayerViewport : AWViewport
    {
        #region PlayerViewport fields

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
        /// Sprite batch to use for drawing sprites.
        /// </summary>
        SpriteBatch spriteBatch;

        /// <summary>
        /// The player we are following.
        /// </summary>
        Player player;

        /// <summary>
        /// The area of the display to draw on.
        /// </summary>
        Viewport viewport;

        /// <summary>
        /// Last point we looked at.
        /// </summary>
        Vector2 lookAt;

        /// <summary>
        /// Last returned view matrix.
        /// </summary>
        protected Matrix view;

        /// <summary>
        /// Last returned projection matrix.
        /// </summary>
        protected Matrix projection;

        /// <summary>
        /// The minimum X and Y coordinates of the game world this viewport is viewing.
        /// </summary>
        Vector2 worldAreaMin;

        /// <summary>
        /// The maximum X and Y coordinates of the game world this viewport is viewing.
        /// </summary>
        Vector2 worldAreaMax;

        /// <summary>
        /// Times, in game time, at which the player's bonus boxes started
        /// sliding in to the player's viewport overlay or out of it.
        /// </summary>
        PlayerBonusItems<TimeSpan> bonusEntryTimeins;

        /// <summary>
        /// Start position relative X and Y adjustments for sliding bonus boxes, 
        /// usually between 0 and 1. The adjustment is the relative coordinate
        /// at which the box was when it started its current movement.
        /// Normally this is 0 for entering boxes and 1 for exiting boxes.
        /// </summary>
        PlayerBonusItems<Vector2> bonusEntryPosAdjustments;

        /// <summary>
        /// Which directions the player's bonus boxes are moving in.
        /// <b>true</b> means entry movement;
        /// <b>false</b> means exit movement.
        /// </summary>
        PlayerBonusItems<bool> bonusEntryDirections;

        #endregion PlayerViewport fields

        /// <summary>
        /// Creates a new player viewport.
        /// </summary>
        /// <param name="player">Which player the viewport will follow.</param>
        /// <param name="onScreen">Where on screen is the viewport located.</param>
        public PlayerViewport(Player player, Rectangle onScreen)
        {
            this.player = player;
            spriteBatch = new SpriteBatch(AssaultWing.Instance.GraphicsDevice);
            viewport = new Viewport();
            viewport.X = onScreen.X;
            viewport.Y = onScreen.Y;
            viewport.Width = onScreen.Width;
            viewport.Height = onScreen.Height;
            viewport.MinDepth = 0f;
            viewport.MaxDepth = 1f;
            lookAt = Vector2.Zero;
            view = Matrix.CreateLookAt(Vector3.Backward, Vector3.Zero, Vector3.Up);
            projection = Matrix.CreateOrthographic(viewport.Width, viewport.Height, 1f, 10000f);
            worldAreaMin = Vector2.Zero;
            worldAreaMax = new Vector2(viewport.Width, viewport.Height);
            bonusEntryTimeins = new PlayerBonusItems<TimeSpan>();
            bonusEntryPosAdjustments = new PlayerBonusItems<Vector2>();
            bonusEntryDirections = new PlayerBonusItems<bool>();

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

        #region PlayerViewport properties

        public Player Player { get { return player; } }
      
        /// <summary>
        /// The minimum X and Y coordinates of the game world this viewport is viewing.
        /// </summary>
        public Vector2 WorldAreaMin
        {
            get
            {
                if (player.Ship != null)
                    worldAreaMin = new Vector2(
                        player.Ship.Pos.X - viewport.Width / 2,
                        player.Ship.Pos.Y - viewport.Height / 2);
                return worldAreaMin;
            }
        }

        /// <summary>
        /// The maximum X and Y coordinates of the game world this viewport is viewing.
        /// </summary>
        public Vector2 WorldAreaMax { 
            get { 
                if (player.Ship != null)
                    worldAreaMax = new Vector2(
                        player.Ship.Pos.X + viewport.Width / 2, 
                        player.Ship.Pos.Y + viewport.Height / 2);
                return worldAreaMax;
            }
        }


        /// <summary>
        /// Times, in game time, at which the player's bonus boxes started
        /// sliding in to the player's viewport overlay or out of it.
        /// </summary>
        public PlayerBonusItems<TimeSpan> BonusEntryTimeins { get { return bonusEntryTimeins; } set { bonusEntryTimeins = value; } }

        /// <summary>
        /// Start position adjustments for sliding bonus boxes, 
        /// usually between 0 and 1. Value 0, the usual case, means that
        /// the bonus box started sliding from the very beginning.
        /// Value 0.5 means that the bonus started sliding midway between
        /// the expected and the resulting position.
        /// </summary>
        public PlayerBonusItems<Vector2> BonusEntryPosAdjustments { get { return bonusEntryPosAdjustments; } set { bonusEntryPosAdjustments = value; } }

        /// <summary>
        /// Which directions the player's bonus boxes are moving in.
        /// <b>true</b> means entry movement;
        /// <b>false</b> means exit movement.
        /// </summary>
        public PlayerBonusItems<bool> BonusEntryDirections { get { return bonusEntryDirections; } set { bonusEntryDirections = value; } }

        #endregion PlayerViewport properties

        #region AWViewport implementation

        public Viewport InternalViewport { get { return viewport; } }

        public Matrix ViewMatrix
        {
            get
            {
                Gob ship = player.Ship;
                if (ship != null)
                    lookAt = ship.Pos;
                int sign = Helpers.RandomHelper.GetRandomInt(2) * 2 - 1; // -1 or +1
                float viewShake = sign * player.Shake;
                view = Matrix.CreateLookAt(new Vector3(lookAt, 500f), new Vector3(lookAt, 0f),
                    new Vector3((float)Math.Cos(MathHelper.PiOver2 + viewShake),
                                (float)Math.Sin(MathHelper.PiOver2 + viewShake),
                                0));
                return view;
            }
        }
        public Matrix ProjectionMatrix
        {
            get
            {
                return projection;
            }
        }

        /// <summary>
        /// Checks if a bounding volume might be visible in the viewport.
        /// </summary>
        /// <param name="volume">The bounding volume.</param>
        /// <returns><b>false</b> if the bounding volume definitely cannot be seen in the viewport;
        /// <b>true</b> otherwise.</returns>
        public bool Intersects(BoundingSphere volume)
        {
            // We add one unit to the bounding sphere to account for rounding of floating-point
            // world coordinates to integer-valued screen pixels.
            if (volume.Center.X + volume.Radius + 1f < WorldAreaMin.X)
                return false;
            if (volume.Center.Y + volume.Radius + 1f < WorldAreaMin.Y)
                return false;
            if (WorldAreaMax.X < volume.Center.X - volume.Radius - 1f)
                return false;
            if (WorldAreaMax.Y < volume.Center.Y - volume.Radius - 1f)
                return false;
            return true;
        }

        /// <summary>
        /// Checks if a bounding volume might be visible in the viewport.
        /// </summary>
        /// <param name="volume">The bounding volume.</param>
        /// <returns><b>false</b> if the bounding volume definitely cannot be seen in the viewport;
        /// <b>true</b> otherwise.</returns>
        public bool Intersects(BoundingBox volume)
        {
            // We add one unit to the bounding box to account for rounding of floating-point
            // world coordinates to integer-valued screen pixels.
            if (volume.Max.X + 1f < WorldAreaMin.X)
                return false;
            if (volume.Max.Y + 1f < WorldAreaMin.Y)
                return false;
            if (WorldAreaMax.X < volume.Min.X - 1f)
                return false;
            if (WorldAreaMax.Y < volume.Min.Y - 1f)
                return false;
            return true;
        }

        /// <summary>
        /// Draws the viewport's contents.
        /// </summary>
        public void Draw()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            gfx.Viewport = InternalViewport;
            gfx.Clear(Color.Black);
            view = ViewMatrix; // TODO: Remove ViewMatrix and ProjectionMatrix from AWViewport. Make this more sensible

            // 2D graphics
            data.Arena.DrawParallaxes(spriteBatch, this);
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);

            // Restore renderstate for 3D graphics.
            gfx.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            gfx.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            gfx.RenderState.DepthBufferEnable = true;
            gfx.RenderState.DepthBufferWriteEnable = true;

            // 3D graphics
            data.ForEachGob(delegate(Gob gob)
            {
                if (!(gob is ParticleEngine)) // HACK: Should implement draw order to Gob
                    gob.Draw(view, projection, spriteBatch);
            });
            spriteBatch.End();

            // particles
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.SaveState);
            data.ForEachParticleEngine(delegate(ParticleEngine pEng)
            {
                pEng.Draw(view, projection, spriteBatch);
            });
            spriteBatch.End();

            // overlay components
            DrawPlayerOverlay(this);
            Player.AttenuateShake();
        }

        #endregion AWViewport implementation


        /// <summary>
        /// Draws overlay graphics into a player viewport.
        /// </summary>
        /// <param name="viewport">The player viewport.</param>
        private void DrawPlayerOverlay(PlayerViewport viewport)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);

            // Status display background
            Texture2D statusDisplayTexture = data.GetTexture(TextureName.StatusDisplay);
            spriteBatch.Draw(statusDisplayTexture,
                new Vector2(viewport.InternalViewport.Width, 0) / 2,
                null, Color.White, 0,
                new Vector2(statusDisplayTexture.Width, 0) / 2,
                1, SpriteEffects.None, 0);

            // Damage meter
            Texture2D barShipTexture = data.GetTexture(TextureName.BarShip);
            if (viewport.Player.Ship != null)
            {
                Rectangle damageBarRect = new Rectangle(0, 0,
                    (int)Math.Ceiling((1 - viewport.Player.Ship.DamageLevel / viewport.Player.Ship.MaxDamageLevel)
                    * barShipTexture.Width),
                    barShipTexture.Height);
                Color damageBarColor = Color.White;
                if (viewport.Player.Ship.DamageLevel / viewport.Player.Ship.MaxDamageLevel >= 0.8f)
                {
                    float seconds = (float)AssaultWing.Instance.GameTime.TotalRealTime.TotalSeconds;
                    if (seconds % 0.5f < 0.25f)
                        damageBarColor = Color.Red;
                }
                spriteBatch.Draw(barShipTexture,
                    new Vector2(viewport.InternalViewport.Width, 8 * 2) / 2,
                    damageBarRect, damageBarColor, 0,
                    new Vector2(barShipTexture.Width, 0) / 2,
                    1, SpriteEffects.None, 0);
            }

            // Player lives left
            Texture2D iconShipTexture = data.GetTexture(TextureName.IconShipsLeft);
            for (int i = 0; i < viewport.Player.Lives; ++i)
                spriteBatch.Draw(iconShipTexture,
                    new Vector2(viewport.InternalViewport.Width +
                                barShipTexture.Width + (8 + i * 10) * 2,
                                barShipTexture.Height + 8 * 2) / 2,
                    null,
                    Color.White,
                    0,
                    new Vector2(0, iconShipTexture.Height) / 2,
                    1, SpriteEffects.None, 0);

            // Primary weapon charge
            Texture2D barMainTexture = data.GetTexture(TextureName.BarMain);
            if (viewport.Player.Ship != null)
            {
                Rectangle charge1BarRect = new Rectangle(0, 0,
                    (int)Math.Ceiling(viewport.Player.Ship.Weapon1Charge / viewport.Player.Ship.Weapon1ChargeMax
                    * barMainTexture.Width),
                    barMainTexture.Height);
                spriteBatch.Draw(barMainTexture,
                    new Vector2(viewport.InternalViewport.Width, 24 * 2) / 2,
                    charge1BarRect, Color.White, 0,
                    new Vector2(barMainTexture.Width, 0) / 2,
                    1, SpriteEffects.None, 0);
            }

            // Primary weapon loadedness
            if (viewport.Player.Ship != null)
            {
                if (viewport.Player.Ship.Weapon1Loaded)
                {
                    float seconds = (float)(AssaultWing.Instance.GameTime.TotalGameTime - viewport.Player.Ship.Weapon1.LoadedTime).TotalSeconds;
                    Texture2D texture = data.GetTexture(TextureName.IconWeaponLoad);
                    float scale = 1;
                    Color color = Color.White;
                    if (seconds < 0.2f)
                    {
                        scale = MathHelper.Lerp(3, 1, seconds / 0.2f);
                        color = new Color(Vector4.Lerp(new Vector4(0, 1, 0, 0.1f), Vector4.One, seconds / 0.2f));
                    }
                    spriteBatch.Draw(texture,
                        new Vector2(viewport.InternalViewport.Width + texture.Width +
                                    barMainTexture.Width + 8 * 2,
                                    barMainTexture.Height + 24 * 2) / 2,
                        null, color, 0,
                        new Vector2(texture.Width, texture.Height) / 2,
                        scale, SpriteEffects.None, 0);
                }
            }

            // Secondary weapon charge
            Texture2D barSpecialTexture = data.GetTexture(TextureName.BarSpecial);
            if (viewport.Player.Ship != null)
            {
                Rectangle charge2BarRect = new Rectangle(0, 0,
                    (int)Math.Ceiling(viewport.Player.Ship.Weapon2Charge / viewport.Player.Ship.Weapon2ChargeMax
                    * barSpecialTexture.Width),
                    barSpecialTexture.Height);
                spriteBatch.Draw(barSpecialTexture,
                    new Vector2(viewport.InternalViewport.Width, 40 * 2) / 2,
                    charge2BarRect, Color.White, 0,
                    new Vector2(barSpecialTexture.Width, 0) / 2,
                    1, SpriteEffects.None, 0);
            }

            // Secondary weapon loadedness
            if (viewport.Player.Ship != null)
            {
                if (viewport.Player.Ship.Weapon2Loaded)
                {
                    float seconds = (float)(AssaultWing.Instance.GameTime.TotalGameTime - viewport.Player.Ship.Weapon2.LoadedTime).TotalSeconds;
                    Texture2D texture = data.GetTexture(TextureName.IconWeaponLoad);
                    float scale = 1;
                    Color color = Color.White;
                    if (seconds < 0.2f)
                    {
                        scale = MathHelper.Lerp(3, 1, seconds / 0.2f);
                        color = new Color(Vector4.Lerp(new Vector4(0, 1, 0, 0.2f), Vector4.One, seconds / 0.2f));
                    }
                    spriteBatch.Draw(texture,
                        new Vector2(viewport.InternalViewport.Width + texture.Width +
                                    barSpecialTexture.Width + 8 * 2,
                                    barSpecialTexture.Height + 40 * 2) / 2,
                        null, color, 0,
                        new Vector2(texture.Width, texture.Height) / 2,
                        scale, SpriteEffects.None, 0);
                }
            }

            // Draw bonus display.
            Texture2D bonusBackgroundTexture = data.GetTexture(TextureName.BonusBackground);
            Vector2 bonusBoxSize = new Vector2(bonusBackgroundTexture.Width,
                                               bonusBackgroundTexture.Height);

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
            spriteBatch.Draw(data.GetTexture(TextureName.Radar),
                Vector2.Zero, Color.White);

            // Arena walls on radar
            Vector2 radarDisplayTopLeft = new Vector2(0, 1); // TODO: Make this constant configurable
            spriteBatch.Draw(data.ArenaRadarSilhouette, radarDisplayTopLeft, Color.White);

            // Ships on radar
            Matrix arenaToRadarTransform = data.ArenaToRadarTransform;
            Texture2D shipOnRadarTexture = data.GetTexture(TextureName.RadarShip);
            Vector2 shipOnRadarTextureCenter = new Vector2(shipOnRadarTexture.Width, shipOnRadarTexture.Height) / 2;
            data.ForEachPlayer(delegate(Player player)
            {
                if (player.Ship == null) return;
                Vector2 posInArena = player.Ship.Pos;
                Vector2 posOnRadar = radarDisplayTopLeft + Vector2.Transform(posInArena, arenaToRadarTransform);
                spriteBatch.Draw(shipOnRadarTexture, posOnRadar, null, Color.White, 0,
                    shipOnRadarTextureCenter, 1, SpriteEffects.None, 0);
            });

            // Chat box
            Texture2D chatBoxTexture = data.GetTexture(TextureName.ChatBox);
            Vector2 chatBoxPos = new Vector2(0, viewport.InternalViewport.Height - chatBoxTexture.Height);
            spriteBatch.Draw(chatBoxTexture, chatBoxPos, Color.White);
            spriteBatch.DrawString(data.GetFont(FontName.Overlay), "Testing, testing...", chatBoxPos + new Vector2(8, 16), Color.White);

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
            GraphicsEngine gfx = (GraphicsEngine)AssaultWing.Instance.Services.GetService(typeof(GraphicsEngine));

            // Figure out what to draw for this bonus.
            string bonusText;
            Texture2D bonusIcon;
            switch (playerBonus)
            {
                case PlayerBonus.Weapon1LoadTime:
                    bonusText = player.Weapon1Name + "\nspeedloader";
                    bonusIcon = data.GetTexture(TextureName.BonusIconWeapon1LoadTime);
                    break;
                case PlayerBonus.Weapon2LoadTime:
                    bonusText = player.Weapon2Name + "\nspeedloader";
                    bonusIcon = data.GetTexture(TextureName.BonusIconWeapon2LoadTime);
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
                    bonusIcon = data.GetTexture(TextureName.IconWeaponLoad);
                    Log.Write("Warning: Don't know how to draw player bonus box " + playerBonus);
                    break;
            }

            // Draw bonus box background.
            Texture2D bonusBackgroundTexture = data.GetTexture(TextureName.BonusBackground);
            Vector2 backgroundOrigin = new Vector2(0,
                bonusBackgroundTexture.Height) / 2;
            spriteBatch.Draw(bonusBackgroundTexture,
                bonusPos, null, Color.White, 0, backgroundOrigin, 1, SpriteEffects.None, 0);

            // Draw bonus icon.
            Vector2 iconPos = bonusPos - backgroundOrigin + new Vector2(112, 9);
            spriteBatch.Draw(bonusIcon,
                iconPos, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            // Draw bonus duration meter.
            Texture2D bonusDurationTexture = data.GetTexture(TextureName.BonusDuration);
            float startSeconds = (float)player.BonusTimeins[playerBonus].TotalSeconds;
            float endSeconds = (float)player.BonusTimeouts[playerBonus].TotalSeconds;
            float nowSeconds = (float)AssaultWing.Instance.GameTime.TotalGameTime.TotalSeconds;
            float duration = (endSeconds - nowSeconds) / (endSeconds - startSeconds);
            int durationHeight = (int)Math.Round(duration * bonusDurationTexture.Height);
            int durationY = bonusDurationTexture.Height - durationHeight;
            Rectangle durationClip = new Rectangle(0, durationY,
                bonusDurationTexture.Width, durationHeight);
            Vector2 durationPos = bonusPos - backgroundOrigin + new Vector2(14, 8 + durationY);
            spriteBatch.Draw(bonusDurationTexture,
                durationPos, durationClip, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            // Draw bonus text.
            // Round coordinates for beautiful text.
            SpriteFont overlayFont = data.GetFont(FontName.Overlay);
            Vector2 textSize = overlayFont.MeasureString(bonusText);
            Vector2 textPos = bonusPos - backgroundOrigin + new Vector2(32, 23.5f - textSize.Y / 2);
            textPos.X = (float)Math.Round(textPos.X);
            textPos.Y = (float)Math.Round(textPos.Y);
            spriteBatch.DrawString(overlayFont, bonusText, textPos, Color.White);
        }
    }
}
