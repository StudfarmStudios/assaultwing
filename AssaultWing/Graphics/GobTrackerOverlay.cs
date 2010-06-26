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
        public static readonly string PLAYER_TEXTURE = "gui_playerinfo_white_ball";
        private Gob _gob;
        private bool _stickToViewportBorders;
        private bool _rotateTowardsTarget;
        private bool _showWhileTargetOnScreen;
        private string _texture;
        private Color _drawColor;

        public Gob Gob { get { return _gob; } set { _gob = value; } }
        public bool StickToBorders { get { return _stickToViewportBorders; } set { _stickToViewportBorders = value; } }
        public bool RotateTowardsTarget { get { return _rotateTowardsTarget; } set { _rotateTowardsTarget = value; } }
        public bool ShowWhileTargetOnScreen { get { return _showWhileTargetOnScreen; } set { _showWhileTargetOnScreen = value; } }
        public string Texture { get { return _texture; } set { _texture = value; } }
        public Color DrawColor { get { return _drawColor; } set { _drawColor = value; } }

        public GobTrackerItem(Gob gob, string texture,  bool stickToViewportBorders, bool rotateTowardsTarget, bool showWhileTargetOnScreen, Color drawColor)
        {
            _gob = gob;
            _stickToViewportBorders = stickToViewportBorders;
            _rotateTowardsTarget = rotateTowardsTarget;
            _showWhileTargetOnScreen = showWhileTargetOnScreen;
            _texture = texture;
            _drawColor = drawColor;
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
                foreach (GobTrackerItem gobtracker in Viewport.Player.GobTrackerItems)
                {
                    Vector2 pos = Vector2.Transform(gobtracker.Gob.Pos, Viewport.GetGameToScreenMatrix(0));
                    Vector2 origPos = Vector2.Transform(gobtracker.Gob.Pos, Viewport.GetGameToScreenMatrix(0));

                    if (gobtracker.StickToBorders)
                    {
                        pos = AW2.Helpers.Geometric.Geometry.CropLineSegment(_player.Ship.Pos, gobtracker.Gob.Pos, Viewport.WorldAreaMin(0), Viewport.WorldAreaMax(0));
                        pos = Vector2.Transform(pos, Viewport.GetGameToScreenMatrix(0));
                    }

                    if ((pos != origPos && gobtracker.StickToBorders) || (pos == origPos && gobtracker.ShowWhileTargetOnScreen))
                    {
                        Texture2D texture = AssaultWing.Instance.Content.Load<Texture2D>(gobtracker.Texture);
                        spriteBatch.Draw(texture, pos, null, gobtracker.DrawColor, 0, new Vector2(texture.Width, texture.Height) / 2, 1.5f, SpriteEffects.None, 0);
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
