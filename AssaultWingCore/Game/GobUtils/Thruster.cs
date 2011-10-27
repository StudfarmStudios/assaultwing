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

        private Peng[] _exhaustEngines;
        private bool _exhaustEffectsEnabled = true;
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
        /// <param name="enable">If true, the thruster effects will be enabled right away.</param>
        public void Activate(Gob owner, bool enable)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            Owner = owner;
            _exhaustEngines = CreateExhaustEngines();
            if (HasSound)
            {
                _thrusterSound = Owner.Game.SoundEngine.CreateSound(_runningSound, Owner);
                _thrusterTurnSound = Owner.Game.SoundEngine.CreateSound("LowEngine", Owner);
            }
            SetExhaustEffectsEnabled(enable);
        }

        public void Update()
        {
            UpdateThrusterSound();
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

        /// <param name="proportionalThrust">Proportional amount of thrust, between -1 (full thrust backward)
        /// and 1 (full thrust forward).</param>
        /// <param name="direction">Direction of thrust in radians.</param>
        public void Thrust(float proportionalThrust, float direction)
        {
            ThrustImpl(proportionalThrust, AWMathHelper.GetUnitVector2(direction));
        }

        /// <param name="proportionalThrust">Proportional amount of thrust, between -1 (full thrust backward)
        /// and 1 (full thrust forward).</param>
        /// <param name="direction">Direction of thrust. Amplitude is irrelevant.</param>
        public void Thrust(float proportionalThrust, Vector2 direction)
        {
            ThrustImpl(proportionalThrust, Vector2.Normalize(direction));
        }

        public void SetExhaustEffectsEnabled(bool active)
        {
            if (active == _exhaustEffectsEnabled) return;
            _exhaustEffectsEnabled = active;
            foreach (var exhaustEngine in _exhaustEngines)
                if (active)
                {
                    exhaustEngine.Emitter.Resume();
                    if (HasSound)
                    {
                        _thrusterSound.EnsureIsPlaying();
                        _thrusterTurnSound.EnsureIsPlaying();
                    }
                }
                else
                {
                    exhaustEngine.Emitter.Pause();
                    if (HasSound)
                    {
                        _thrusterSound.Stop();
                        _thrusterTurnSound.Stop();
                    }
                }
        }

        private void ThrustImpl(float proportionalThrust, Vector2 unitDirection)
        {
            if (proportionalThrust < -1 || proportionalThrust > 1) throw new ArgumentOutOfRangeException("proportionalThrust");
            var force = _maxForce * proportionalThrust * unitDirection;
            Owner.Game.PhysicsEngine.ApplyLimitedForce(Owner, force, _maxSpeed, Owner.Game.GameTime.ElapsedGameTime);
        }

        private Peng[] CreateExhaustEngines()
        {
            var exhaustEngineList = new List<Peng>();
            foreach (var engineName in _exhaustEngineNames)
                foreach (var boneIndex in Owner.GetNamedPositions("Thruster"))
                    Gob.CreateGob<Peng>(Owner.Game, engineName, peng =>
                    {
                        peng.Leader = Owner;
                        peng.LeaderBone = boneIndex.Item2;
                        if (!_exhaustEffectsEnabled) peng.Emitter.Pause();
                        Owner.Arena.Gobs.Add(peng);
                        exhaustEngineList.Add(peng);
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
    }
}
