using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Game.Gobs;

namespace AW2.Graphics
{
    public class GobTrackerItem
    {
        public static readonly string PLAYER_TEXTURE = "gui_tracker_player";

        public Gob Gob { get; set; }
        public bool StickToBorders { get; set; }
        public bool RotateTowardsTarget { get; set; }
        public bool ShowWhileTargetOnScreen { get; set; }
        public string Texture { get; set; }
        public Color DrawColor { get; set; }

        public GobTrackerItem(Gob gob, string texture,  bool stickToViewportBorders, bool rotateTowardsTarget, bool showWhileTargetOnScreen, Color drawColor)
        {
            Gob = gob;
            StickToBorders = stickToViewportBorders;
            RotateTowardsTarget = rotateTowardsTarget;
            ShowWhileTargetOnScreen = showWhileTargetOnScreen;
            Texture = texture;
            DrawColor = drawColor;
        }
    }

    class GobTrackerOverlay : OverlayComponent
    {
        private Player _player;
        private PlayerViewport _viewport;

        public PlayerViewport Viewport { get { return _viewport; } set { _viewport = value; } }

        public override Point Dimensions
        {
            get {
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
            : base(HorizontalAlignment.Center, VerticalAlignment.Center)
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

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            SetViewport();

            if (Viewport != null)
            {
                // First remove
                Viewport.Player.GobTrackerItems.RemoveAll(item => item.Gob.Dead);

                // Then draw
                if (_player.Ship != null)
                {
                    foreach (GobTrackerItem gobtracker in Viewport.Player.GobTrackerItems)
                    {
                        Vector2 pos = Vector2.Transform(gobtracker.Gob.Pos, Viewport.GetGameToScreenMatrix(0));
                        float rotation = 0f;
                        Vector2 origPos = Vector2.Transform(gobtracker.Gob.Pos, Viewport.GetGameToScreenMatrix(0));

                        if (gobtracker.RotateTowardsTarget)
                        {
                            rotation = -AW2.Helpers.AWMathHelper.Angle(gobtracker.Gob.Pos - _player.Ship.Pos);
                        }

                        if (gobtracker.StickToBorders)
                        {
                            pos = AW2.Helpers.Geometric.Geometry.CropLineSegment(_player.Ship.Pos, gobtracker.Gob.Pos, Viewport.WorldAreaMin(0), Viewport.WorldAreaMax(0));
                            pos = Vector2.Transform(pos, Viewport.GetGameToScreenMatrix(0));
                        }

                        if ((pos != origPos && gobtracker.StickToBorders) || (pos == origPos && gobtracker.ShowWhileTargetOnScreen))
                        {
                            Texture2D texture = AssaultWing.Instance.Content.Load<Texture2D>(gobtracker.Texture);
                            spriteBatch.Draw(texture, pos, null, gobtracker.DrawColor, rotation, new Vector2(texture.Width, texture.Height) / 2, 1.23f, SpriteEffects.None, 0);
                        }
                    }
                }
            }
        }

        public override void LoadContent()
        {
            var content = AssaultWing.Instance.Content;
        }

        public override void UnloadContent()
        {
        }
    }
}
