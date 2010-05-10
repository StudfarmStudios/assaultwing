using System;
using System.Linq;
using AW2.Helpers;

namespace AW2.Game.Gobs.Bonus
{


    /// <summary>
    /// A bonus that can be collected by a player.
    /// </summary>
    public class RandomBonus : Bonus
    {
        /*TODO: Randon bonus needs to be refactored*/
        #region Bonus fields
/*
        /// <summary>
        /// The possibile bonus actions that collecting the bonus can activate.
        /// </summary>
        [TypeParameter, ShallowCopy]
        BonusActionPossibility[] possibilities;
*/        
        #endregion Bonus fields

        /// This constructor is only for serialisation.
        public RandomBonus()
        {
            lifetime = 10;
            deathTime = new TimeSpan(0, 1, 20);
        }

        /// <summary>
        /// Creates a new bonus.
        /// </summary>
        /// <param name="typeName">Type of the bonus.</param>
        public RandomBonus(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            base.Activate();
            deathTime = AssaultWing.Instance.GameTime.TotalArenaTime + TimeSpan.FromSeconds(lifetime);
        }

        public override void Update()
        {
            base.Update();
            if (deathTime <= AssaultWing.Instance.GameTime.TotalArenaTime)
                Die(new DeathCause());
        }

        protected override void DoBonusAction(Player player)
        {
            /*if (possibilities.Length == 0) 
                throw new InvalidOperationException("Bonus has no possible bonus actions");
            */
            // Pick our choice from the combined probability mass 
            // and then find out which possibility we hit.
            /* TODO: Refactor
             * float massTotal = possibilities.Sum(possibility => possibility.weight);
            float choice = RandomHelper.GetRandomFloat(0, massTotal);
            massTotal = 0;
            //BonusActionPossibility poss = new BonusActionPossibility();
            for (int i = 0; i < possibilities.Length && choice >= massTotal; ++i)
            {
                poss = possibilities[i];
                massTotal += poss.weight;
            }
            */
            // Perform the bonus action.

        }

        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public new void MakeConsistent(Type limitationAttribute)
        {
            // NOTE: This method is meant to re-implement the interface member
            // IConsistencyCheckable.MakeConsistent(Type) that is already implemented
            // in the base class Gob. According to the C# Language Specification 1.2
            // (and not corrected in the specification version 2.0), adding the 'new'
            // keyword to this re-implementation would make this code
            // 
            //      Wall wall;
            //      ((IConsistencyCheckable)wall).MakeConsistent(type)
            //
            // call Gob.MakeConsistent(Type). However, debugging reveals this is not the
            // case. By leaving out the 'new' keyword, the semantics stays the same, as
            // seen by debugging, but the compiler produces a warning.
            base.MakeConsistent(limitationAttribute);
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                lifetime = Math.Max(0.5f, lifetime);
            }
        }

        #endregion
    }
}