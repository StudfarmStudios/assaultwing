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

        [TypeParameter]
        private float _blinkFailTravelEffectSpeed;
        [TypeParameter]
        private CanonicalString _blinkFailTravelEffect;
        [TypeParameter]
        private CanonicalString _blinkFailTargetEffect;

        private Vector2 _queriedTargetPos;
        private Vector2? _targetPos;
        private Vector2 _startPos;
        private TimeSpan _safetyTimeout;

        private TimeSpan SafetyTimeoutInterval { get { return TimeSpan.FromSeconds(0.1f + _blinkDistance / _blinkMoveSpeed); } }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Blink()
        {
            _blinkDistance = 500;
            _blinkMoveSpeed = 1200;
            _blinkFailTravelEffectSpeed = 2400;
            _blinkFailTravelEffect = (CanonicalString)"dummypeng";
            _blinkFailTargetEffect = (CanonicalString)"dummypeng";
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
                // Client corrects its blink target estimate based on the most recent Owner update
                if (Owner.Game.NetworkMode == NetworkMode.Client && Owner.Pos != _startPos)
                    _targetPos = GetBlinkTarget(_startPos, Owner.Pos - _startPos);
                float blinkMoveStep = Owner.Game.PhysicsEngine.ApplyChange(_blinkMoveSpeed, Owner.Game.GameTime.ElapsedGameTime);
                var pos = AWMathHelper.InterpolateTowards(Owner.Pos, _targetPos.Value, blinkMoveStep);
                Owner.ResetPos(pos, Owner.Move, Owner.Rotation);
                if (pos == _targetPos.Value || _safetyTimeout < Owner.Arena.TotalTime)
                {
                    Owner.Enable();
                    _targetPos = null;
                    _safetyTimeout = TimeSpan.Zero;
                    if (PlayerOwner != null) PlayerOwner.PostprocessEffectNames.Remove(EFFECT_NAME);
                }
            }
        }

        public override void Dispose()
        {
            if (PlayerOwner != null) PlayerOwner.PostprocessEffectNames.Remove(EFFECT_NAME);
            base.Dispose();
        }

        protected override bool PermissionToFire()
        {
            _queriedTargetPos = GetBlinkTarget();
            return Arena.IsFreePosition(Owner, _queriedTargetPos);
        }

        protected override void ShootImpl()
        {
            // Client tries to guess where blink is going
            _targetPos = Owner.Game.NetworkMode == NetworkMode.Client
                ? GetBlinkTarget()
                : _targetPos = _queriedTargetPos;
            _safetyTimeout = Owner.Arena.TotalTime + SafetyTimeoutInterval;
            _startPos = Owner.Pos;
            Owner.Disable(); // re-enabled in Update()
        }

        protected override void CreateVisuals()
        {
            if (PlayerOwner != null) PlayerOwner.PostprocessEffectNames.EnsureContains(EFFECT_NAME);
        }

        protected override void ShowFiringFailedEffect()
        {
            if (Owner == null) return;
            Gob.CreateGob<Gobs.Peng>(Owner.Game, _blinkFailTravelEffect, peng =>
            {
                var pengMove = _blinkFailTravelEffectSpeed * Vector2.Normalize(GetBlinkTarget() - Owner.Pos);
                peng.ResetPos(Owner.Pos, pengMove, Owner.Rotation);
                var flyTime = _blinkDistance / _blinkFailTravelEffectSpeed;
                peng.Emitter.NumberToCreate = (int)Math.Round(flyTime * peng.Emitter.EmissionFrequency);
                peng.IsMovable = true;
                peng.VisibilityLimitedTo = PlayerOwner;
                Owner.Arena.Gobs.Add(peng);
            });
            Gob.CreateGob<Gobs.Peng>(Owner.Game, _blinkFailTargetEffect, peng =>
            {
                peng.ResetPos(GetBlinkTarget(), Vector2.Zero, Owner.Rotation);
                peng.VisibilityLimitedTo = PlayerOwner;
                Owner.Arena.Gobs.Add(peng);
            });
        }

        private Vector2 GetBlinkTarget()
        {
            return GetBlinkTarget(Owner.Pos, AWMathHelper.GetUnitVector2(Owner.Rotation));
        }

        private Vector2 GetBlinkTarget(Vector2 from, Vector2 direction)
        {
            return from + Vector2.Normalize(direction) * _blinkDistance;
        }
    }
}
