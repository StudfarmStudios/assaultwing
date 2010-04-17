using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Graphics
{
    internal class EditorViewport : AWViewport
    {
        public bool IsCirclingSmallAndInvisibleGobs { get; set; }

        public EditorViewport(Rectangle onScreen, ILookAt lookAt, Func<IEnumerable<CanonicalString>> getPostprocessEffectNames)
            : base(onScreen, lookAt, getPostprocessEffectNames)
        {
        }

        protected override void RenderGameWorld()
        {
            base.RenderGameWorld();
            if (IsCirclingSmallAndInvisibleGobs) CircleSmallAndInvisibleGobs();
        }

        private void CircleSmallAndInvisibleGobs()
        {
            var view = ViewMatrix;
            foreach (var layer in AssaultWing.Instance.DataEngine.Arena.Layers)
            {
                var projection = GetProjectionMatrix(layer.Z);
                foreach (var gob in layer.Gobs)
                    if (gob.DrawBounds.Radius < 20)
                        Graphics3D.DebugDraw(new BoundingSphere(new Vector3(gob.Pos, 0), 20), view, projection, Matrix.Identity);
            }
        }
    }
}
