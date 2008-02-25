using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;

namespace AW2.Graphics
{
    /// <summary>
    /// An view on the display that looks into the game world.
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
    }
    
    /// <summary>
    /// An viewport that follows a player.
    /// </summary>
    class PlayerViewport : AWViewport
    {
        /// <summary>
        /// The player we are following.
        /// </summary>
        Player player;

        /// <summary>
        /// The area of the display to draw on.
        /// </summary>
        Viewport viewport;

        /// <summary>
        /// Last returned view matrix.
        /// </summary>
        protected Matrix view;

        /// <summary>
        /// Last returned projection matrix.
        /// </summary>
        protected Matrix projection;

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
            view = Matrix.CreateLookAt(Vector3.Backward, Vector3.Zero, Vector3.Up);
            projection = Matrix.Identity;
        }

        public Viewport InternalViewport { get { return viewport; } }
        public Player Player { get { return player; } }

        public Vector2 ScreenUpperLeft
        {
            get
            {
                return new Vector2(player.Ship.Pos.X - viewport.Width / 2, - player.Ship.Pos.Y - viewport.Height / 2);
            }
        }
      
        /// <summary>
        /// The minimum X and Y coordinates of the game world this viewport is viewing.
        /// </summary>
        public Vector2 WorldAreaMin { get { return new Vector2(
            player.Ship.Pos.X - viewport.Width / 2, 
            player.Ship.Pos.Y - viewport.Height / 2); } }

        /// <summary>
        /// The maximum X and Y coordinates of the game world this viewport is viewing.
        /// </summary>
        public Vector2 WorldAreaMax { get { return new Vector2(
            player.Ship.Pos.X + viewport.Width / 2, 
            player.Ship.Pos.Y + viewport.Height / 2); } }

        public Matrix ViewMatrix
        {
            get
            {
                Gob ship = player.Ship;
                if (ship != null)
                    view = Matrix.CreateLookAt(new Vector3(ship.Pos, 500f), new Vector3(ship.Pos, 0f), Vector3.Up);
                return view;
            }
        }
        public Matrix ProjectionMatrix
        {
            get
            {
                projection = Matrix.CreateOrthographic(viewport.Width, viewport.Height, 1f, 10000f);
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
    }
}
