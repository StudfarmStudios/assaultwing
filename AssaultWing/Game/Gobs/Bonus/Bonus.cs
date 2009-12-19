using System;
using System.Linq;
using AW2.Helpers;

namespace AW2.Game.Gobs.Bonus
{
    /// <summary>
    /// What can happen when a bonus is activated.
    /// </summary>
    /// This enum is closely related to the enum PlayerBonus which lists
    /// what bonuses a player can have.
    /// <seealso cref="AW2.Game.PlayerBonus"/>
    public enum BonusAction
    {
        /// <summary>
        /// Create an explosion.
        /// </summary>
        Explode,

        /// <summary>
        /// Upgrade primary weapon's load time.
        /// </summary>
        UpgradeWeapon1LoadTime,

        /// <summary>
        /// Upgrade secondary weapon's load time.
        /// </summary>
        UpgradeWeapon2LoadTime,

        /// <summary>
        /// Upgrade primary weapon.
        /// </summary>
        UpgradeWeapon1,

        /// <summary>
        /// Upgrade secondary weapon.
        /// </summary>
        UpgradeWeapon2,
    }

    /// <summary>
    /// A bonus action as one of many possible choices.
    /// </summary>
    public struct BonusActionPossibility
    {
        /// <summary>
        /// The probability weight of this possibility 
        /// relative to other possibilities.
        /// </summary>
        public float weight;

        /// <summary>
        /// The bonus action to perform in case this possibility is chosen.
        /// </summary>
        public BonusAction action;

        /// <summary>
        /// Creates a new bonus action possibility.
        /// </summary>
        /// <param name="weight">The probability weight of this possibility 
        /// relative to other possibilities.</param>
        /// <param name="action">The bonus action to perform in case this possibility is chosen.</param>
        /// <param name="duration">The duration of the bonus action, in seconds.</param>
        public BonusActionPossibility(float weight, BonusAction action)
        {
            this.weight = weight;
            this.action = action;
        }
    }

    /// <summary>
    /// A bonus that can be collected by a player.
    /// </summary>
    public abstract class Bonus : Gob, IConsistencyCheckable
    {
        #region Bonus fields

        /// <summary>
        /// Lifetime of the bonus, in seconds.
        /// </summary>
        [TypeParameter]
        protected float lifetime;

        /// <summary>
        /// Time at which the bonus dies, in game time.
        /// </summary>
        [RuntimeState]
        protected TimeSpan deathTime;

        /// <summary>
        /// The duration of the bonus, in seconds.
        /// </summary>
        /// Bonus that don't have a meaningful duration
        /// leave this field uninterpreted.
        [TypeParameter]
        protected float duration;

        #endregion Bonus fields

        /// <summary>
        /// Creates an uninitialised bonus.
        /// </summary>
        /// This constructor is only for serialisation.
        public Bonus()
            : base()
        {
            this.lifetime = 10;
            this.deathTime = new TimeSpan(0, 1, 20);
        }

        /// <summary>
        /// Creates a new bonus.
        /// </summary>
        /// <param name="typeName">Type of the bonus.</param>
        public Bonus(CanonicalString typeName)
            : base(typeName)
        {
        }

        #region Methods related to gobs' functionality in the game world

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            
            deathTime = AssaultWing.Instance.GameTime.TotalGameTime + TimeSpan.FromSeconds(lifetime);
        }

        /// <summary>
        /// Updates the bonus's internal state.
        /// </summary>
        public override void Update()
        {
            base.Update();
            if (deathTime <= AssaultWing.Instance.GameTime.TotalGameTime)
                Die(new DeathCause());
        }

        #endregion Methods related to gobs' functionality in the game world

        /// <summary>
        /// Performs collision operations for the case when one of this gob's collision areas
        /// is overlapping one of another gob's collision areas.
        /// </summary>
        /// <param name="myArea">The collision area of this gob.</param>
        /// <param name="theirArea">The collision area of the other gob.</param>
        /// <param name="stuck">If <b>true</b> then the gob is stuck, i.e.
        /// <b>theirArea.Type</b> matches <b>myArea.CannotOverlap</b> and it's not possible
        /// to backtrack out of the overlap. It is then up to this gob and the other gob 
        /// to resolve the overlap.</param>
        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            // We assume we have only one receptor area and that's the one for
            // bonus collection. That means that the other gob is a ship.
            if (myArea.Type == CollisionAreaType.Receptor)
            {
                if (AssaultWing.Instance.NetworkMode != NetworkMode.Client)
                    DoBonusAction(theirArea.Owner.Owner);
                Die(new DeathCause());
            }
        }

        /// <summary>
        /// Perform a bonus action on a player.
        /// </summary>
        /// <param name="player">The player to receive the bonus action.</param>
        protected abstract void DoBonusAction(Player player);

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