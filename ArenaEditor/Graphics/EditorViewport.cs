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
                    if (gob.DrawBounds.Radius < 20)
                        Graphics3D.DebugDraw(new BoundingSphere(new Vector3(gob.Pos, 0), 20), view, projection, Matrix.Identity);
            }
        }
    }
}
