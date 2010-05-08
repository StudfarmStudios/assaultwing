using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.Sound;

namespace AW2.Game.Weapons
{
    public class SelfDestruct : Weapon
    {
        #region ForwardShot fields

        [TypeParameter, ShallowCopy]
        CanonicalString[] deathGobTypes;
        
        #endregion SelfDestruct fields

        /// This constructor is only for serialisation.
        public SelfDestruct()
        {
            deathGobTypes = new CanonicalString[0];
        }

        public SelfDestruct(CanonicalString typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// Fires the weapon.
        /// </summary>
        protected override void FireImpl(AW2.UI.ControlState triggerState)
        {
            owner.SelfDestruct(deathGobTypes);
            owner.DamageLevel = owner.MaxDamageLevel * 10;
            owner.Die(new DeathCause(owner, DeathCauseType.Damage));
        }

        public override void Activate()
        {
            FireMode = FireModeType.Single;
        }

        public override void Update()
        {
        }

        /// <summary>
        /// Releases all resources allocated by the weapon.
        /// </summary>
        public override void Dispose()
        {
        }
    }
}
