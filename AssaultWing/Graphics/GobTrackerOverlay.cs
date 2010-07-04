using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;

namespace AW2.Graphics
{
    /// <summary>
    /// Overlay graphics component pointing to a few selected gobs on top of a player's arena view.
    /// </summary>
    public class GobTrackerOverlay : OverlayComponent
    {
        private Player _player;

        public PlayerViewport Viewport { get; set; }

        public override Point Dimensions
        {
            get
            {
                if (Viewport != null)
                {
                    var x = Viewport.OnScreen.Right - Viewport.OnScreen.Left;
                    var y = Viewport.OnScreen.Bottom - Viewport.OnScreen.Top;
                    return new Point(x, y);
                }
                return new Point(0, 0);
            }
        }

        public GobTrackerOverlay(Player player)
            : base(HorizontalAlignment.Stretch, VerticalAlignment.Stretch)
        {
            _player = player;
        }

        private void SetViewport()
        {
            if (Viewport == null)
            {
                foreach (PlayerViewport viewport in AssaultWing.Instance.DataEngine.Viewports)
                {
                    if (viewport != null && viewport.Player.ID == _player.ID)
                    {
                        Viewport = viewport;
                    }
                }
            }
        }

        private Vector2 GetTrackerPos(Gob trackerGob, Player trackerPlayer)
        {
            if (trackerGob != null) return trackerGob.Pos;
            if (trackerPlayer != null) return trackerPlayer.LookAtPos;
            throw new ApplicationException("Null tracker");
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            SetViewport();

            if (Viewport != null)
            {
                RemoveOutdatedItems();

                // Then draw
                if (_player.Ship != null)
                {
                    foreach (var gobtracker in Viewport.Player.GobTrackerItems)
                    {
                        var pos = Vector2.Transform(gobtracker.Gob.Pos, Viewport.GetGameToScreenMatrix(0));
                        var origPos = pos;
                        var trackerPos = GetTrackerPos(gobtracker.TrackerGob, _player);
                        float rotation = 0f;
                        float scale = 1f;

                        if (gobtracker.ScaleByDistance)
                        {
                            var farDistance = AssaultWing.Instance.DataEngine.Arena.Dimensions.Length();
                            var distance = Vector2.Distance(gobtracker.Gob.Pos, trackerPos);
                            scale = MathHelper.Max(0, (farDistance - distance) / farDistance);
                        }
                        if (gobtracker.RotateTowardsTarget)
                        {
                            rotation = -AW2.Helpers.AWMathHelper.Angle(gobtracker.Gob.Pos - trackerPos);
                        }
                        if (gobtracker.StickToBorders)
                        {
                            pos = AW2.Helpers.Geometric.Geometry.CropLineSegment(_player.Ship.Pos, gobtracker.Gob.Pos, Viewport.WorldAreaMin(0), Viewport.WorldAreaMax(0));
                            pos = Vector2.Transform(pos, Viewport.GetGameToScreenMatrix(0));
                        }
                        if ((pos != origPos && gobtracker.StickToBorders) || (pos == origPos && gobtracker.ShowWhileTargetOnScreen))
                        {
                            Texture2D texture = AssaultWing.Instance.Content.Load<Texture2D>(gobtracker.Texture);
                            spriteBatch.Draw(texture, pos, null, gobtracker.DrawColor, rotation, new Vector2(texture.Width, texture.Height) / 2, scale, SpriteEffects.None, 0);
                        }
                    }
                }
            }
        }

        private void RemoveOutdatedItems()
        {
            Viewport.Player.GobTrackerItems.RemoveAll(IsItemOutdated);
        }

        private bool IsItemOutdated(GobTrackerItem item)
        {
            if (item.Gob.Dead || item.Gob.IsDisposed) return true;
            if (item.TrackerGob != null)
            {
                if (item.TrackerGob.Dead || item.TrackerGob.IsDisposed) return true;
            }
            return false;
        }
    }
}
