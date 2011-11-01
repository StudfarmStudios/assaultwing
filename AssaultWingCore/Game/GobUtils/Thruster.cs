using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Sound;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Propels a gob forward.
    /// </summary>
    [LimitedSerialization]
    public class Thruster
    {
        private class ExhaustEngine
        {
            private Peng _peng;
            private float _directionRelativeToOwner;
            private bool _paused;

            public ExhaustEngine(Peng peng, float directionRelativetoOwner)
            {
                _peng = peng;
                _directionRelativeToOwner = directionRelativetoOwner;
                EnsurePaused(true);
            }

            public void EnsurePaused(bool toBePaused)
            {
                var isOn = !_paused;
                if (!toBePaused && !isOn) _peng.Emitter.Resume();
                if (toBePaused && isOn) _peng.Emitter.Pause();
                _paused = toBePaused;
            }

            public void EnableForDirection(float directionRelativeToOwner)
            {
                EnsurePaused(AWMathHelper.AbsoluteAngleDifference(_directionRelativeToOwner, directionRelativeToOwner) > MathHelper.PiOver2);
            }
        }

        /// <summary>
        /// Maximum force of thrust, measured in Newtons.
        /// </summary>
        [TypeParameter]
        private float _maxForce;

        /// <summary>
        /// Maximum speed reachable by thrust, measured in meters per second.
        /// </summary>
        [TypeParameter]
        private float _maxSpeed;

        /// <summary>
        /// Names of exhaust engine types.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private CanonicalString[] _exhaustEngineNames;

        [TypeParameter]
        private string _runningSound;

        private ExhaustEngine[] _exhaustEngines;
        private bool _exhaustAmountUpdated;
        private SoundInstance _thrusterSound;
        private SoundInstance _thrusterTurnSound;
        private float _turnSoundBlend;

        public float MaxSpeed { get { return _maxSpeed; } }
        public float MaxForce { get { return _maxForce; } }
        public Gob Owner { get; private set; }

        private bool HasSound { get { return !string.IsNullOrEmpty(_runningSound); } }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Thruster()
        {
            _maxForce = 50000;
            _maxSpeed = 200;
            _exhaustEngineNames = new CanonicalString[0];
            _runningSound = "";
        }

        /// <summary>
        /// Attaches the thruster to a gob. Call this method right after creating the gob.
        /// </summary>
        public void Activate(Gob owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            Owner = owner;
            _exhaustEngines = CreateExhaustEngines();
            if (HasSound)
            {
                _thrusterSound = Owner.Game.SoundEngine.CreateSound(_runningSound, Owner);
                _thrusterTurnSound = Owner.Game.SoundEngine.CreateSound("LowEngine", Owner);
            }
        }

        public void Update()
        {
            UpdateThrusterSound();
            UpdateExhaustEngines();
        }

        public void Dispose()
        {
            if (HasSound)
            {
                _thrusterSound.Dispose();
                _thrusterSound = null;
                _thrusterTurnSound.Dispose();
                _thrusterTurnSound = null;
            }
        }

        /// <summary>
        /// Thrust owner to where it is heading.
        /// </summary>
        /// <param name="proportionalThrust">Proportional amount of thrust, between -1 (full thrust backward)
        /// and 1 (full thrust forward).</param>
        public void Thrust(float proportionalThrust)
        {
            ThrustImpl(proportionalThrust, MathHelper.Pi, AWMathHelper.GetUnitVector2(Owner.Rotation));
        }

        /// <param name="proportionalThrust">Proportional amount of thrust, between -1 (full thrust backward)
        /// and 1 (full thrust forward).</param>
        /// <param name="thrustDirection">Amplitude is irrelevant.</param>
        public void Thrust(float proportionalThrust, Vector2 thrustDirection)
        {
            var exhaustDirection = (-thrustDirection).Angle() - Owner.Rotation;
            ThrustImpl(proportionalThrust, exhaustDirection, Vector2.Normalize(thrustDirection));
        }

        private void EnableExhaustEffects(float exhaustDirectionRelativeToOwner)
        {
            foreach (var exhaustEngine in _exhaustEngines) exhaustEngine.EnableForDirection(exhaustDirectionRelativeToOwner);
            if (HasSound)
            {
                _thrusterSound.EnsureIsPlaying();
                _thrusterTurnSound.EnsureIsPlaying();
            }
        }

        private void DisableExhaustEffects()
        {
            foreach (var exhaustEngine in _exhaustEngines) exhaustEngine.EnsurePaused(true);
            if (HasSound)
            {
                _thrusterSound.Stop();
                _thrusterTurnSound.Stop();
            }
        }

        private void ThrustImpl(float proportionalThrust, float exhaustDirectionRelativeToOwner, Vector2 thrustDirectionUnit)
        {
            if (proportionalThrust < -1 || proportionalThrust > 1) throw new ArgumentOutOfRangeException("proportionalThrust");
            var force = _maxForce * proportionalThrust * thrustDirectionUnit;
            Owner.Game.PhysicsEngine.ApplyLimitedForce(Owner, force, _maxSpeed, Owner.Game.GameTime.ElapsedGameTime);
            if (proportionalThrust >= 0.4f)
                EnableExhaustEffects(exhaustDirectionRelativeToOwner);
            else if (proportionalThrust <= -0.4f)
                EnableExhaustEffects(exhaustDirectionRelativeToOwner + MathHelper.Pi);
            else
                DisableExhaustEffects();
            _exhaustAmountUpdated = true;
        }

        private ExhaustEngine[] CreateExhaustEngines()
        {
            var exhaustEngineList = new List<ExhaustEngine>();
            foreach (var engineName in _exhaustEngineNames)
                foreach (var boneIndex in Owner.GetNamedPositions("Thruster"))
                    Gob.CreateGob<Peng>(Owner.Game, engineName, peng =>
                    {
                        peng.Leader = Owner;
                        peng.LeaderBone = boneIndex.Item2;
                        Owner.Arena.Gobs.Add(peng);
                        exhaustEngineList.Add(new ExhaustEngine(peng, Owner.GetBoneRotationRelativeToGob(boneIndex.Item2)));
                    });
            return exhaustEngineList.ToArray();
        }

        private void UpdateThrusterSound()
        {
            if (!HasSound) return;
            var turnBlendTarget = MathHelper.Clamp(Owner.Move.Length() / _maxSpeed, 0, 1);
            _turnSoundBlend = AWMathHelper.InterpolateTowards(_turnSoundBlend, turnBlendTarget, (float)Owner.Game.GameTime.ElapsedGameTime.TotalSeconds);
            _thrusterSound.SetVolume(_turnSoundBlend);
            _thrusterTurnSound.SetVolume(1 - _turnSoundBlend);
        }

        private void UpdateExhaustEngines()
        {
            if (!_exhaustAmountUpdated) DisableExhaustEffects();
            _exhaustAmountUpdated = false;
        }
    }
}
