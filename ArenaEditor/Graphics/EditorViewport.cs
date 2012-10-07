using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Game;
using AW2.Game.Gobs;
using AW2.Game.Players;
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
                if (IsCirclingSmallAndInvisibleGobs && !(gob is Wall || gob is PropModel))
                    CircleGob(gob);
            };
        }

        public override void Update()
        {
            base.Update();
            CurrentLookAt = _spectator.LookAtPos;
        }

        public override void Reset(Vector2 lookAtPos)
        {
            _spectator.LookAtPos = lookAtPos;
        }

        private void CircleGob(Gob gob)
        {
            var context = new Graphics3D.DebugDrawContext(ViewMatrix, GetProjectionMatrix(gob.Layer.Z));
            Graphics3D.DebugDrawCircle(context, new BoundingSphere(new Vector3(gob.Pos, 0), SMALL_GOB_RADIUS));
            Graphics3D.DebugDrawPolyline(context, gob.Pos, gob.Pos + SMALL_GOB_RADIUS * AWMathHelper.GetUnitVector2(gob.Rotation));
        }

        private Graphics3D.DebugDrawContext GetDebugDrawContext(Gob gob)
        {
            return new Graphics3D.DebugDrawContext(ViewMatrix, GetProjectionMatrix(gob.Layer.Z));
        }
    }
}
