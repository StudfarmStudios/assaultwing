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
        private const float BLINK_TARGET_HIT_RANGE_SQUARED = 1f * 1f;

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

        private Tuple<Vector2, Vector2> _queriedTargetPosAndMove;
        private Vector2? _targetPos;
        private Vector2 _startPos;
        private TimeSpan _safetyTimeout;
        private Vector2 _originalOwnerMove;

        private TimeSpan SafetyTimeoutInterval { get { return TimeSpan.FromSeconds(0.01f + _blinkDistance / _blinkMoveSpeed); } }
        private bool BlinkTargetReached
        {
            get
            {
                return Vector2.DistanceSquared(Owner.Pos, _targetPos.Value) < BLINK_TARGET_HIT_RANGE_SQUARED
                    || _safetyTimeout < Owner.Arena.TotalTime;
            }
        }

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
                if (Owner.Game.NetworkMode != NetworkMode.Client)
                {
                    // Due to slight add-up errors on game servers and standalone games,
                    // correct blink movement every frame so that Owner will eventually hit _targetPos.
                    var targetFPS = Owner.Game.TargetFPS;
                    Owner.Move = (AWMathHelper.InterpolateTowards(Owner.Pos, _targetPos.Value, _blinkMoveSpeed / targetFPS) - Owner.Pos) * targetFPS;
                }
                if (BlinkTargetReached) FinishBlink();
            }
        }

        public override void Dispose()
        {
            if (PlayerOwner != null) PlayerOwner.PostprocessEffectNames.Remove(EFFECT_NAME);
            base.Dispose();
        }

        protected override bool PermissionToFire()
        {
            _queriedTargetPosAndMove = GetBlinkTargetAndMove();
            return Arena.IsFreePosition(new Circle(_queriedTargetPosAndMove.Item1, Gob.SMALL_GOB_PHYSICAL_RADIUS),
                area => area.Owner.MoveType != MoveType.Dynamic);
        }

        protected override void ShootImpl()
        {
            // Client tries to guess where blink is going
            if (Owner.Game.NetworkMode == NetworkMode.Client) _queriedTargetPosAndMove = GetBlinkTargetAndMove();
            _originalOwnerMove = Owner.Move;
            Owner.Disable(); // re-enabled in Update()
            Owner.Body.BodyType = FarseerPhysics.Dynamics.BodyType.Kinematic;
            _targetPos = _queriedTargetPosAndMove.Item1;
            Owner.Move = _queriedTargetPosAndMove.Item2;
            _safetyTimeout = Owner.Arena.TotalTime + SafetyTimeoutInterval;
            _startPos = Owner.Pos;
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
                var pengMove = GetBlinkTargetAndMove().Item2 / _blinkMoveSpeed * _blinkFailTravelEffectSpeed;
                peng.ResetPos(Owner.Pos, pengMove, Owner.Rotation);
                var flyTime = (_blinkDistance - 50) / _blinkFailTravelEffectSpeed; // It looks better if it doesn't travel the full blink distance.
                peng.Emitter.NumberToCreate = (int)Math.Round(flyTime * peng.Emitter.EmissionFrequency);
                peng.MoveType = MoveType.Kinematic;
                peng.VisibilityLimitedTo = PlayerOwner;
                Owner.Arena.Gobs.Add(peng);
            });
            Gob.CreateGob<Gobs.Peng>(Owner.Game, _blinkFailTargetEffect, peng =>
            {
                peng.ResetPos(GetBlinkTargetAndMove().Item1, Vector2.Zero, Owner.Rotation);
                peng.VisibilityLimitedTo = PlayerOwner;
                Owner.Arena.Gobs.Add(peng);
            });
        }

        private Tuple<Vector2, Vector2> GetBlinkTargetAndMove()
        {
            return GetBlinkTargetAndMove(Owner.Pos, AWMathHelper.GetUnitVector2(Owner.Rotation));
        }

        private Tuple<Vector2, Vector2> GetBlinkTargetAndMove(Vector2 from, Vector2 direction)
        {
            var directionUnit = Vector2.Normalize(direction);
            var target = from + _blinkDistance * directionUnit;
            var move = _blinkMoveSpeed * directionUnit;
            return Tuple.Create(target, move);
        }

        private void FinishBlink()
        {
            Owner.Move = _originalOwnerMove;
            Owner.Body.BodyType = FarseerPhysics.Dynamics.BodyType.Dynamic;
            Owner.Enable();
            _targetPos = null;
            _safetyTimeout = TimeSpan.Zero;
            if (PlayerOwner != null) PlayerOwner.PostprocessEffectNames.Remove(EFFECT_NAME);
        }
    }
}
