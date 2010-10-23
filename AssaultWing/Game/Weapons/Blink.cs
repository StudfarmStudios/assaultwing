using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// Blinking device for a ship. Blinking is teleportation to a nearby location.
    /// </summary>
    public class Blink : ShipDevice
    {
        private static readonly CanonicalString EFFECT_NAME = (CanonicalString)"gaussian_blur";

        /// <summary>
        /// Target area of blink, relative to ship position and rotation.
        /// </summary>
        [TypeParameter]
        private IGeomPrimitive blinkArea;

        /// <summary>
        /// Move speed during blink, in meters per second.
        /// </summary>
        [TypeParameter]
        private float blinkMoveSpeed;

        private Vector2? _targetPos;

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Blink()
        {
            blinkArea = new Circle(Vector2.UnitX * 500, 50);
            blinkMoveSpeed = 1200;
        }

        public Blink(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Update()
        {
            base.Update();
            if (_targetPos.HasValue)
            {
                float blinkMoveStep = Owner.Game.PhysicsEngine.ApplyChange(blinkMoveSpeed, Owner.Game.GameTime.ElapsedGameTime);
                var pos = AWMathHelper.InterpolateTowards(Owner.Pos, _targetPos.Value, blinkMoveStep);
                Owner.ResetPos(pos, Owner.Move, Owner.Rotation);
                if (pos == _targetPos.Value)
                {
                    Owner.Enable();
                    _targetPos = null;
                    Owner.Owner.PostprocessEffectNames.Remove(EFFECT_NAME);
                }
            }
        }

        public override void Dispose()
        {
            Owner.Owner.PostprocessEffectNames.Remove(EFFECT_NAME);
            base.Dispose();
        }

        protected override bool PermissionToFire(bool canFire)
        {
            // Blink is totally controlled by the server because of complex visual effects.
            if (Owner.Game.NetworkMode == NetworkMode.Client) return false;

            if (!canFire) return false;
            var transform =
                Matrix.CreateRotationZ(Owner.Rotation) *
                Matrix.CreateTranslation(new Vector3(Owner.Pos, 0));
            var targetArea = blinkArea.Transform(transform);
            Vector2 newPos;
            bool success = Arena.GetFreePosition(Owner, targetArea, out newPos);
            if (success)
            {
                _targetPos = newPos;
                Owner.Disable(); // re-enabled in Update()
            }
            return success;
        }

        protected override void ShootImpl()
        {
        }

        protected override void CreateVisualsImpl()
        {
            Owner.Owner.PostprocessEffectNames.EnsureContains(EFFECT_NAME);
        }
    }
}
