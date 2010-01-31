using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;
using Microsoft.Xna.Framework;
using AW2.Helpers.Geometric;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// Blinking device for a ship. Blinking is teleportation to a nearby location.
    /// </summary>
    class Blink : ShipDevice
    {
        /// <summary>
        /// Target area of blink, relative to ship position and rotation.
        /// </summary>
        [TypeParameter]
        IGeomPrimitive blinkArea;

        Vector2? _targetPos;

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Blink()
            : base()
        {
            blinkArea = new Circle(Vector2.UnitX * 500, 50);
        }

        public Blink(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            FireMode = FireModeType.Single;
        }

        protected override void FireImpl(AW2.UI.ControlState triggerState)
        {
            if (!CanFire) return;
            if (triggerState.pulse)
            {
                var transform =
                    Matrix.CreateRotationZ(owner.Rotation) *
                    Matrix.CreateTranslation(new Vector3(owner.Pos, 0));
                var targetArea = blinkArea.Transform(transform);
                Vector2 newPos;
                if (Arena.GetFreePosition(owner, targetArea, out newPos))
                {
                    StartFiring();
                    owner.Disable(); // re-enabled in Update()
                    _targetPos = newPos;
                    if (!owner.Owner.PostprocessEffectNames.Contains("gaussian_blur"))
                        owner.Owner.PostprocessEffectNames.Add("gaussian_blur");
                }
            }
        }

        public override void Update()
        {
            if (_targetPos.HasValue)
            {
                var pos = AWMathHelper.InterpolateTowards(owner.Pos, _targetPos.Value, 20);
                owner.ResetPos(pos, owner.Move, owner.Rotation);
                if (pos == _targetPos.Value)
                {
                    owner.Enable();
                    _targetPos = null;
                    owner.Owner.PostprocessEffectNames.Remove("gaussian_blur");
                    DoneFiring();
                }
            }
        }

        public override void Dispose()
        {
        }
    }
}
