using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Graphics
{
    internal class EditorViewport : AWViewport
    {
        private const float SMALL_GOB_RADIUS = 20;

        private EditorSpectator _spectator;

        public bool IsCirclingSmallAndInvisibleGobs { get; set; }

        public EditorViewport(EditorSpectator spectator, Rectangle onScreen, Func<IEnumerable<CanonicalString>> getPostprocessEffectNames)
            : base(onScreen, getPostprocessEffectNames)
        {
            _spectator = spectator;
        }

        protected override void RenderGameWorld()
        {
            base.RenderGameWorld();
            if (IsCirclingSmallAndInvisibleGobs) CircleSmallAndInvisibleGobs();
        }

        protected override Vector2 GetLookAtPos()
        {
            return _spectator.LookAtPos;
        }

        private void CircleSmallAndInvisibleGobs()
        {
            var view = ViewMatrix;
            foreach (var layer in AssaultWingCore.Instance.DataEngine.Arena.Layers)
            {
                var projection = GetProjectionMatrix(layer.Z);
                foreach (var gob in layer.Gobs)
                    if (gob.DrawBounds.Radius < SMALL_GOB_RADIUS)
                    {
                        Graphics3D.DebugDraw(new BoundingSphere(new Vector3(gob.Pos, 0), SMALL_GOB_RADIUS), view, projection, Matrix.Identity);
                        Graphics3D.DebugDraw(gob.Pos, gob.Pos + SMALL_GOB_RADIUS * AWMathHelper.GetUnitVector2(gob.Rotation), view, projection, Matrix.Identity);
                    }
            }
        }
    }
}
