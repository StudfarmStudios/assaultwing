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
    public abstract class AWViewport
    {
        /// <summary>
        /// Sprite batch to use for drawing sprites.
        /// </summary>
        protected SpriteBatch spriteBatch;

        /// <summary>
        /// Overlay graphics components to draw in this viewport.
        /// </summary>
        List<OverlayComponent> overlayComponents;

        /// <summary>
        /// Creates a viewport.
        /// </summary>
        public AWViewport()
        {
            overlayComponents = new List<OverlayComponent>();
            LoadContent();
        }

        /// <summary>
        /// Adds an overlay graphics component to the viewport.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddOverlayComponent(OverlayComponent component)
        {
            overlayComponents.Add(component);
        }

        /// <summary>
        /// Removes all overlay graphics components from the viewport.
        /// </summary>
        public void ClearOverlayComponents()
        {
            overlayComponents.Clear();
        }

        /// <summary>
        /// Checks if a bounding volume might be visible in the viewport.
        /// </summary>
        /// <param name="volume">The bounding volume.</param>
        /// <returns><b>false</b> if the bounding volume definitely cannot be seen in the viewport;
        /// <b>true</b> otherwise.</returns>
        public abstract bool Intersects(BoundingSphere volume);

        /// <summary>
        /// Checks if a bounding volume might be visible in the viewport.
        /// </summary>
        /// <param name="volume">The bounding volume.</param>
        /// <returns><b>false</b> if the bounding volume definitely cannot be seen in the viewport;
        /// <b>true</b> otherwise.</returns>
        public abstract bool Intersects(BoundingBox volume);

        /// <summary>
        /// Draws the viewport's overlay graphics components.
        /// </summary>
        public virtual void Draw()
        {
            foreach (OverlayComponent component in overlayComponents)
                component.Draw(spriteBatch);
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public void LoadContent()
        {
            spriteBatch = new SpriteBatch(AssaultWing.Instance.GraphicsDevice);
            foreach (OverlayComponent component in overlayComponents)
                component.LoadContent();
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public void UnloadContent()
        {
            foreach (OverlayComponent component in overlayComponents)
                component.UnloadContent();
            spriteBatch.Dispose();
        }
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
        /// The minimum X and Y coordinates of the game world this viewport is viewing.
        /// </summary>
        Vector2 worldAreaMin;

        /// <summary>
        /// The maximum X and Y coordinates of the game world this viewport is viewing.
        /// </summary>
        Vector2 worldAreaMax;

        /// <summary>
        /// Last used sign of player's shake angle. Either 1 or -1.
        /// </summary>
        float shakeSign;

        #endregion PlayerViewport fields

        /// <summary>
        /// Creates a new player viewport.
        /// </summary>
        /// <param name="player">Which player the viewport will follow.</param>
        /// <param name="onScreen">Where on screen is the viewport located.</param>
        public PlayerViewport(Player player, Rectangle onScreen)
        {
            this.player = player;
            viewport = new Viewport();
            viewport.X = onScreen.X;
            viewport.Y = onScreen.Y;
            viewport.Width = onScreen.Width;
            viewport.Height = onScreen.Height;
            viewport.MinDepth = 0f;
            viewport.MaxDepth = 1f;
            lookAt = Vector2.Zero;
            worldAreaMin = Vector2.Zero;
            worldAreaMax = new Vector2(viewport.Width, viewport.Height);
            shakeSign = -1;

            // Create overlay graphics components.
            AddOverlayComponent(new ChatBoxOverlay(player));
            AddOverlayComponent(new RadarOverlay(player));
            AddOverlayComponent(new BonusListOverlay(player));
            AddOverlayComponent(new PlayerStatusOverlay(player));
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
        /// The view matrix for drawing 3D content into the viewport.
        /// </summary>
        private Matrix ViewMatrix
        {
            get
            {
                Gob ship = player.Ship;
                if (ship != null)
                    lookAt = ship.Pos;
                shakeSign = -shakeSign;
                float viewShake = shakeSign * player.Shake;
                return Matrix.CreateLookAt(new Vector3(lookAt, 1000), new Vector3(lookAt, 0),
                    new Vector3((float)Math.Cos(MathHelper.PiOver2 + viewShake),
                                (float)Math.Sin(MathHelper.PiOver2 + viewShake),
                                0));
            }
        }

        #endregion PlayerViewport properties

        #region AWViewport implementation

        /// <summary>
        /// Checks if a bounding volume might be visible in the viewport.
        /// </summary>
        /// <param name="volume">The bounding volume.</param>
        /// <returns><b>false</b> if the bounding volume definitely cannot be seen in the viewport;
        /// <b>true</b> otherwise.</returns>
        public override bool Intersects(BoundingSphere volume)
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
        public override bool Intersects(BoundingBox volume)
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
        public override void Draw()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            gfx.Viewport = viewport;
            Matrix view = ViewMatrix;

            data.ForEachArenaLayer(delegate(ArenaLayer layer)
            {
                gfx.Clear(ClearOptions.DepthBuffer, Color.Pink, 1, 0);
                float layerScale = 1000 / (1000 - layer.Z);
                Matrix projection = Matrix.CreateOrthographic(
                    viewport.Width / layerScale, viewport.Height / layerScale,
                    1f, 11000f);

                // Layer parallax
                if (layer.ParallaxName != null)
                {
                    spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
                    Vector2 pos = WorldAreaMin * -layerScale;
                    pos.Y = -pos.Y;
                    Vector2 fillPos = new Vector2();
                    Texture2D tex = data.GetTexture(layer.ParallaxName);
                    int mult = (int)Math.Ceiling(pos.X / (float)tex.Width);
                    pos.X = pos.X - mult * tex.Width;
                    mult = (int)Math.Ceiling(pos.Y / (float)tex.Height);
                    pos.Y = pos.Y - mult * tex.Height;

                    int loopX = (int)Math.Ceiling((-pos.X + viewport.Width) / tex.Width);
                    int loopY = (int)Math.Ceiling((-pos.Y + viewport.Height) / tex.Height);
                    fillPos.Y = pos.Y;
                    for (int y = 0; y < loopY; y++)
                    {
                        fillPos.X = pos.X;
                        for (int x = 0; x < loopX; x++)
                        {
                            spriteBatch.Draw(tex, fillPos, Color.White);
                            fillPos.X += tex.Width;
                        }
                        fillPos.Y += tex.Height;
                    }
                    spriteBatch.End();
                }

                // Modify renderstate for 3D graphics.
                gfx.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
                gfx.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
                gfx.RenderState.DepthBufferEnable = true;
                gfx.RenderState.DepthBufferWriteEnable = true;

                // 3D graphics
                // We assume that all gobs but particle engines draw only 3D graphics
                // and that particle engines don't draw any 3D graphics.
                // To enforce this, we pass 'null' as the sprite batch.
                foreach (Gob gob in layer.Gobs)
                {
                    if (!(gob is ParticleEngine))
                        gob.Draw(view, projection, null);
                }

                // 2D graphics
                // We assume that particle engines draw only 2D graphics and that
                // no other gob draws any 2D graphics.
                spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.None);
                foreach (Gob gob in layer.Gobs)
                {
                    if (gob is ParticleEngine)
                        gob.Draw(view, projection, spriteBatch);
                }
                spriteBatch.End();
            });

            // Overlay components
            base.Draw();
        }

        #endregion AWViewport implementation
    }
}
