using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;

namespace AW2.Graphics.OverlayComponents
{
    /// <summary>
    /// Overlay graphics component pointing to a few selected gobs on top of a player's arena view.
    /// </summary>
    public class GobTrackerOverlay : OverlayComponent
    {
        private PlayerViewport _viewport;
        private Player Player { get { return _viewport.Player; } }

        /// <summary>
        /// Dimensions are meaningless because our alignments are Stretch.
        /// </summary>
        public override Point Dimensions { get { return new Point(1, 1); } }

        public GobTrackerOverlay(PlayerViewport viewport)
            : base(viewport, HorizontalAlignment.Stretch, VerticalAlignment.Stretch)
        {
            _viewport = viewport;
        }

        private Vector2 GetTrackerPos(Gob trackerGob)
        {
            if (trackerGob != null) return trackerGob.Pos + trackerGob.DrawPosOffset;
            return _viewport.LookAtPos;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            RemoveOutdatedItems();
            foreach (var gobTracker in Player.GobTrackerItems)
            {
                var trackerPos = GetTrackerPos(gobTracker.TrackerGob);
                gobTracker.Draw(spriteBatch, trackerPos, z => Viewport.GetGameToScreenMatrix(z));
            }
        }

        private void RemoveOutdatedItems()
        {
            Player.GobTrackerItems.RemoveAll(IsItemOutdated);
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
