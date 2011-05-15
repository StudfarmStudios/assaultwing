using System;
using System.Collections.Generic;
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
            : base(spectator.Game, onScreen, getPostprocessEffectNames)
        {
            _spectator = spectator;
            GobDrawn += gob =>
            {
                if (IsCirclingSmallAndInvisibleGobs &&
                    (gob.DrawBounds.Radius < SMALL_GOB_RADIUS || !gob.IsVisible))
                    CircleGob(gob);
            };
        }

        protected override Vector2 GetLookAtPos()
        {
            return _spectator.LookAtPos;
        }

        private void CircleGob(Gob gob)
        {
            var projection = GetProjectionMatrix(gob.Layer.Z);
            Graphics3D.DebugDraw(new BoundingSphere(new Vector3(gob.Pos, 0), SMALL_GOB_RADIUS), ViewMatrix, projection, Matrix.Identity);
            Graphics3D.DebugDraw(gob.Pos, gob.Pos + SMALL_GOB_RADIUS * AWMathHelper.GetUnitVector2(gob.Rotation), ViewMatrix, projection, Matrix.Identity);
        }
    }
}
