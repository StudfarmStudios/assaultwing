using System;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;

namespace AW2.Graphics
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
    }
}
