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

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Repulsor()
        {
            _radialFlow = new RadialFlow();
        }

        public Repulsor(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void ShootImpl()
        {
            _radialFlow.Activate(Owner.Game.PhysicsEngine, Arena.TotalTime);
        }

        protected override void CreateVisualsImpl()
        {
        }

        public override void Update()
        {
            base.Update();
            foreach (var gob in Arena.Gobs.GameplayLayer.Gobs)
                _radialFlow.Apply(Owner.Pos, gob);
        }
    }
}
