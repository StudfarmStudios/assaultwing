using System;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using Microsoft.Xna.Framework;

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
        public bool StickToBorders { get; private set; }
        public bool RotateTowardsTarget { get; private set; }
        public bool ShowWhileTargetOnScreen { get; private set; }
        public bool ScaleByDistance { get; private set; }
        public string Texture { get; private set; }
        public Color DrawColor { get; private set; }

        public GobTrackerItem(Gob gob, Gob trackerGob, string texture, bool stickToViewportBorders, bool rotateTowardsTarget, bool showWhileTargetOnScreen, bool scaleByDistance, Color drawColor)
        {
            if (gob == null) throw new ArgumentNullException("GobTrackerItem.Gob must not be null");
            Gob = gob;
            StickToBorders = stickToViewportBorders;
            RotateTowardsTarget = rotateTowardsTarget;
            ShowWhileTargetOnScreen = showWhileTargetOnScreen;
            Texture = texture;
            DrawColor = drawColor;
            ScaleByDistance = scaleByDistance;
            TrackerGob = trackerGob;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 trackerPos, Func<float, Matrix> getGameToScreenTransform)
        {
            var gameToScreenTransform = getGameToScreenTransform(Gob.Layer.Z);
            var gobPosOnScreen = Vector2.Transform(Gob.Pos, gameToScreenTransform);
            var origGobPosOnScreen = gobPosOnScreen;
            var trackerPosOnScreen = Vector2.Transform(trackerPos, gameToScreenTransform);
            float rotation = 0f;
            float scale = 1f;

            if (ScaleByDistance)
            {
                var farDistance = AssaultWing.Instance.DataEngine.Arena.Dimensions.Length();
                var distance = Vector2.Distance(Gob.Pos, trackerPos);
                scale = MathHelper.Max(0, (farDistance - distance) / farDistance);
            }
            if (RotateTowardsTarget)
            {
                rotation = -AW2.Helpers.AWMathHelper.Angle(Gob.Pos - trackerPos);
            }
            if (StickToBorders)
            {
                var gfx = AssaultWing.Instance.GraphicsDevice;
                var max = new Vector2(gfx.Viewport.Width, gfx.Viewport.Height);
                gobPosOnScreen = AW2.Helpers.Geometric.Geometry.CropLineSegment(trackerPosOnScreen, gobPosOnScreen, Vector2.Zero, max);
            }
            if ((gobPosOnScreen != origGobPosOnScreen && StickToBorders) || (gobPosOnScreen == origGobPosOnScreen && ShowWhileTargetOnScreen))
            {
                Texture2D texture = AssaultWing.Instance.Content.Load<Texture2D>(Texture);
                spriteBatch.Draw(texture, gobPosOnScreen, null, DrawColor, rotation, new Vector2(texture.Width, texture.Height) / 2, scale, SpriteEffects.None, 0);
            }
        }

    }
}
