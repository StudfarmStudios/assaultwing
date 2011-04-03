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
        /// Distance of blink in meters (pixels).
        /// </summary>
        [TypeParameter]
        private float _blinkDistance;

        /// <summary>
        /// Move speed during blink, in meters per second.
        /// </summary>
        [TypeParameter]
        private float _blinkMoveSpeed;

        private Vector2 _queriedTargetPos;
        private Vector2? _targetPos;

        private Vector2 BlinkTarget { get { return Owner.Pos + AWMathHelper.GetUnitVector2(Owner.Rotation) * _blinkDistance; } }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Blink()
        {
            _blinkDistance = 500;
            _blinkMoveSpeed = 1200;
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
                float blinkMoveStep = Owner.Game.PhysicsEngine.ApplyChange(_blinkMoveSpeed, Owner.Game.GameTime.ElapsedGameTime);
                var pos = AWMathHelper.InterpolateTowards(Owner.Pos, _targetPos.Value, blinkMoveStep);
                Owner.ResetPos(pos, Owner.Move, Owner.Rotation);
                if (pos == _targetPos.Value)
                {
                    Owner.Enable();
                    _targetPos = null;
                    if (Owner.Owner != null) Owner.Owner.PostprocessEffectNames.Remove(EFFECT_NAME);
                }
            }
        }

        public override void Dispose()
        {
            if (Owner.Owner != null) Owner.Owner.PostprocessEffectNames.Remove(EFFECT_NAME);
            base.Dispose();
        }

        protected override bool PermissionToFire()
        {
            _queriedTargetPos = BlinkTarget;
            return Arena.IsFreePosition(Owner, _queriedTargetPos);
        }

        protected override void ShootImpl()
        {
            _targetPos = _queriedTargetPos;
            Owner.Disable(); // re-enabled in Update()
        }

        protected override void CreateVisualsImpl()
        {
            if (Owner.Owner != null) Owner.Owner.PostprocessEffectNames.EnsureContains(EFFECT_NAME);
        }
    }
}
