using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Helpers.Geometric;

namespace AW2.Graphics.OverlayComponents
{
    public class GobTrackerItem
    {
        public static readonly string PLAYER_TEXTURE = "gui_tracker_player";
        public static readonly string DOCK_TEXTURE = "gui_tracker_dock";
        public static readonly string BONUS_TEXTURE = "gui_tracker_bonus";
        public static readonly string ROCKET_TARGET_TEXTURE = "gui_tracker_rockettarget";

        public Gob Gob { get; private set; }
        public Gob TrackerGob { get; private set; }
        public bool StickToBorders { get; set; }
        public bool RotateTowardsTarget { get; set; }
        public bool ShowWhileTargetOnScreen { get; set; }
        public bool ScaleByDistance { get; set; }
        public string Texture { get; private set; }
        private Color DrawColor
        {
            get
            {
                if (TrackerGob != null && TrackerGob.Owner != null) return TrackerGob.Owner.PlayerColor;
                if (Gob.Owner != null) return Gob.Owner.PlayerColor;
                return Color.White;
            }
        }

        public GobTrackerItem(Gob gob, Gob trackerGob, string texture)
        {
            if (gob == null) throw new ArgumentNullException("GobTrackerItem.Gob must not be null");
            Gob = gob;
            StickToBorders = true;
            RotateTowardsTarget = true;
            ShowWhileTargetOnScreen = false;
            ScaleByDistance = true;
            Texture = texture;
            TrackerGob = trackerGob;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 trackerPos, Func<float, Matrix> getGameToScreenTransform)
        {
            if (Gob.IsHidden) return;

            var gameToScreenTransform = getGameToScreenTransform(Gob.Layer.Z);
            var gobDrawPos = Gob.Pos + Gob.DrawPosOffset;
            var gobPosOnScreen = Vector2.Transform(gobDrawPos, gameToScreenTransform);
            var origGobPosOnScreen = gobPosOnScreen;
            var trackerPosOnScreen = Vector2.Transform(trackerPos, gameToScreenTransform);
            float rotation = 0f;
            float scale = 1f;

            if (ScaleByDistance)
            {
                var farDistance = 4000;
                var distance = Vector2.Distance(gobDrawPos, trackerPos);
                scale = MathHelper.Max(0, (farDistance - distance) / farDistance);
            }
            if (RotateTowardsTarget)
            {
                rotation = -AW2.Helpers.AWMathHelper.Angle(gobDrawPos - trackerPos);
            }
            if (StickToBorders)
            {
                var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
                var max = new Vector2(gfx.Viewport.Width, gfx.Viewport.Height);
                if (Geometry.IsPointInsideRectangle(trackerPosOnScreen, Vector2.Zero, max))
                    gobPosOnScreen = Geometry.CropLineSegment(trackerPosOnScreen, gobPosOnScreen, Vector2.Zero, max);
            }
            if ((gobPosOnScreen != origGobPosOnScreen && StickToBorders) || (gobPosOnScreen == origGobPosOnScreen && ShowWhileTargetOnScreen))
            {
                var texture = AssaultWingCore.Instance.Content.Load<Texture2D>(Texture);
                spriteBatch.Draw(texture, gobPosOnScreen, null, DrawColor, rotation, new Vector2(texture.Width, texture.Height) / 2, scale, SpriteEffects.None, 0);
            }
        }

    }
}
