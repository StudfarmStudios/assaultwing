using System.Linq;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// Repels gobs away from the shooter.
    /// </summary>
    public class Repulsor : ShipDevice
    {
        [TypeParameter]
        private RadialFlow _radialFlow;
        [TypeParameter, ShallowCopy]
        private CanonicalString[] _particleEngineNames;

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

        public override void Update()
        {
            base.Update();
            _radialFlow.Update();
        }

        protected override void ShootImpl()
        {
            _radialFlow.Activate(Owner);
        }

        protected override void CreateVisuals()
        {
            GobHelper.CreatePengs(_particleEngineNames, Owner);
        }
    }
}
