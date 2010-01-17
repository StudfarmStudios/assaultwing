using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.Sound;
using AW2.Game.Particles;

namespace AW2.Game.Weapons
{

    /// <summary>
    /// A weapon that shoots gobs forward.
    /// Each firing can consist of a number of shots being fired.
    /// The shots are shot at even temporal intervals in a random
    /// angle. The shot angles distribute evenly in an angular fan
    /// whose center is directed at the direction of the weapon's owner.
    /// </summary>
    public class SelfDestruct : Weapon, IConsistencyCheckable
    {
        #region ForwardShot fields

        [TypeParameter, ShallowCopy]
        CanonicalString[] deathGobTypes;
        
        #endregion SelfDestruct fields

        /// <summary>
        /// Creates an uninitialised forward shooting weapon.
        /// </summary>
        /// This constructor is only for serialisation.
        public SelfDestruct()
            : base()
        {
 
        }

        public SelfDestruct(CanonicalString typeName)
            : base(typeName)
        {
 
        }

        /// <summary>
        /// Fires the weapon.
        /// </summary>
        public override void Fire(AW2.UI.ControlState triggerState)
        {
            foreach (CanonicalString name in deathGobTypes)
            {
                Log.Write(name);
            }

            owner.selfDestruct(deathGobTypes);
            owner.DamageLevel = owner.MaxDamageLevel*10;
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

        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public void MakeConsistent(Type limitationAttribute)
        {
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                /*
                shotCount = Math.Max(1, shotCount);
                shotSpacing = Math.Max(0, shotSpacing);
                 * */
            }
        }

        #endregion
    }
}
