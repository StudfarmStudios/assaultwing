using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.Helpers.Serialization;

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

        private Peng[] _exhaustEngines;
        private bool _exhaustEffectsEnabled = true;

        public float MaxSpeed { get { return _maxSpeed; } }
        public Gob Owner { get; private set; }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Thruster()
        {
            _maxForce = 50000;
            _maxSpeed = 200;
            _exhaustEngineNames = new CanonicalString[0];
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
            SetExhaustEffectsEnabled(enable);
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
                    exhaustEngine.Emitter.Resume();
                else
                    exhaustEngine.Emitter.Pause();
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
            foreach (var boneIndex in Owner.GetNamedPositions("Thruster"))
                foreach (var engineName in _exhaustEngineNames)
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
    }
}
