using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// Repels gobs away from the shooter.
    /// </summary>
    public class Repulsor : Weapon
    {
        [TypeParameter]
        private RadialFlow _radialFlow;
        [TypeParameter, ShallowCopy]
        private CanonicalString[] _particleEngineNames;

        /// <summary>
        /// Game time when repelling ends.
        /// </summary>
        private TimeSpan _repelEndTime;

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Repulsor()
        {
            _radialFlow = new RadialFlow();
            _particleEngineNames = new[] { (CanonicalString)"dummypeng" };
        }

        public Repulsor(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void ShootImpl()
        {
            _radialFlow.Activate(Owner, Arena.TotalTime);
        }

        protected override void CreateVisualsImpl()
        {
            GobHelper.CreatePengs(_particleEngineNames, Owner);
        }

        public override void Update()
        {
            base.Update();
            if (!_radialFlow.IsFinished(Arena.TotalTime))
                foreach (var gob in Arena.Gobs.GameplayLayer.Gobs.Where(g => g.Movable))
                    _radialFlow.Apply(gob);
        }
    }
}
