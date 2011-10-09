using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Game;
using AW2.Graphics.OverlayComponents;
using AW2.Helpers;

namespace AW2.Graphics
{
    /// <summary>
    /// A viewport that follows a player.
    /// </summary>
    public class PlayerViewport : AWViewport
    {
        private Player _player;
        private GobTrackerOverlay _gobTrackerOverlay;
        private PIDController _lookAtController;
        private Vector2 _lastLookAtTarget;

        /// <summary>
        /// Last used sign of player's shake angle. Either 1 or -1.
        /// </summary>
        private float _shakeSign;

        public static List<Func<PlayerViewport, OverlayComponent>> CustomOverlayCreators = new List<Func<PlayerViewport, OverlayComponent>>();

        public GobTrackerOverlay GobTracker { get { return _gobTrackerOverlay; } set { _gobTrackerOverlay = value; } }

        private Vector2 LookAtTarget
        {
            get
            {
                if (_player.Ship != null) _lastLookAtTarget = _player.Ship.Pos + _player.Ship.DrawPosOffset;
                return _lastLookAtTarget;
            }
        }

        /// <param name="player">Which player the viewport will follow.</param>
        /// <param name="onScreen">Where on screen is the viewport located.</param>
        /// <param name="getPostprocessEffectNames">Provider of names of postprocess effects.</param>
        public PlayerViewport(Player player, Rectangle onScreen, Func<IEnumerable<CanonicalString>> getPostprocessEffectNames)
            : base(player.Game, onScreen, getPostprocessEffectNames)
        {
            _player = player;
            _shakeSign = -1;
            _lookAtController = new PIDController(() => LookAtTarget, () => CurrentLookAt)
            {
                ProportionalGain = 0.11f,
                IntegralGain = 0.0002f,
                DerivativeGain = 0.0f,
            };
            AddOverlayComponent(new MiniStatusOverlay(this));
            AddOverlayComponent(new CombatLogOverlay(this));
            AddOverlayComponent(new RadarOverlay(this));
            AddOverlayComponent(new BonusListOverlay(this));
            AddOverlayComponent(new PlayerStatusOverlay(this));
            AddOverlayComponent(new ScoreOverlay(this));
            GobTracker = new GobTrackerOverlay(this);
            AddOverlayComponent(GobTracker);
            foreach (var customOverlayCreator in CustomOverlayCreators) AddOverlayComponent(customOverlayCreator(this));
        }

        public Player Player { get { return _player; } }

        public override void Update()
        {
            base.Update();
            _lookAtController.Compute();
            CurrentLookAt += _lookAtController.Output;
        }

        public override void Reset(Vector2 lookAtPos)
        {
            CurrentLookAt = lookAtPos;
            _lastLookAtTarget = lookAtPos;
            _lookAtController.Reset();
        }

        protected override Matrix ViewMatrix
        {
            get
            {
                // TODO: Shake only if gameplay is on. Otherwise freeze because shake won't be attenuated either.
                _shakeSign = -_shakeSign;

                float viewShake = _shakeSign * _player.Shake;
                return Matrix.CreateLookAt(new Vector3(CurrentLookAt, 1000), new Vector3(CurrentLookAt, 0),
                    new Vector3(AWMathHelper.GetUnitVector2(MathHelper.PiOver2 + viewShake), 0));
            }
        }

        protected override bool IsBlockedFromView(Gob gob)
        {
            return gob.VisibilityLimitedTo != null && gob.VisibilityLimitedTo != _player;
        }
    }
}
