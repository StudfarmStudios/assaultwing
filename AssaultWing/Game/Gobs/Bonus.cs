using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Events;
using AW2.Helpers;

namespace AW2.Game.Gobs
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
        /// The duration of the bonus action, in seconds.
        /// </summary>
        /// Bonus actions that don't have a meaningful duration
        /// leave this field uninterpreted.
        public float duration;

        /// <summary>
        /// Creates a new bonus action possibility.
        /// </summary>
        /// <param name="weight">The probability weight of this possibility 
        /// relative to other possibilities.</param>
        /// <param name="action">The bonus action to perform in case this possibility is chosen.</param>
        /// <param name="duration">The duration of the bonus action, in seconds.</param>
        public BonusActionPossibility(float weight, BonusAction action, float duration)
        {
            this.weight = weight;
            this.action = action;
            this.duration = duration;
        }
    }

    /// <summary>
    /// A bonus that can be collected by a player.
    /// </summary>
    public class Bonus : Gob, ISolid, IDamageable, IConsistencyCheckable
    {
        #region Bonus fields

        /// <summary>
        /// Lifetime of the bonus, in seconds.
        /// </summary>
        [TypeParameter]
        float lifetime;

        /// <summary>
        /// Time at which the bonus dies, in game time.
        /// </summary>
        [RuntimeState]
        TimeSpan deathTime;

        /// <summary>
        /// The possibile bonus actions that collecting the bonus can activate.
        /// </summary>
        [TypeParameter]
        BonusActionPossibility[] possibilities;

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
            this.possibilities = new BonusActionPossibility[] {
                new BonusActionPossibility(1, BonusAction.Explode, 0),
                new BonusActionPossibility(2, BonusAction.UpgradeWeapon2, 10),
                new BonusActionPossibility(1.5f, BonusAction.UpgradeWeapon1LoadTime, 15),
            };
        }

        /// <summary>
        /// Creates a new bonus.
        /// </summary>
        /// <param name="typeName">Type of the bonus.</param>
        public Bonus(string typeName)
            : base(typeName)
        {
            long ticks = (long)(10 * 1000 * 1000 * lifetime);
            deathTime = physics.TimeStep.TotalGameTime.Add(new TimeSpan(ticks));
        }

        #region Methods related to gobs' functionality in the game world

        /// <summary>
        /// Updates the bonus's internal state.
        /// </summary>
        public override void Update()
        {
            base.Update();
            if (deathTime <= physics.TimeStep.TotalGameTime)
            {
                Die();
            }
        }

        #endregion Methods related to gobs' functionality in the game world

        #region ICollidable Members
        // Some members are implemented in class Gob.

        /// <summary>
        /// Performs collision operations with a gob whose general collision area
        /// has collided with one of our receptor areas.
        /// </summary>
        /// <param name="gob">The gob we collided with.</param>
        /// <param name="receptorName">The name of our colliding receptor area.</param>
        public override void Collide(ICollidable gob, string receptorName)
        {
            Ship gobShip = gob as Ship;
            if (gobShip != null)
            {
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                EventEngine eventer = (EventEngine)AssaultWing.Instance.Services.GetService(typeof(EventEngine));

                // Calculate probability mass total.
                float massTotal = 0;
                foreach (BonusActionPossibility poss in possibilities)
                    massTotal += poss.weight;

                // Pick our choice from the combined probability mass 
                // and then find out which possibility we hit.
                float choice = RandomHelper.GetRandomFloat(0, massTotal);
                massTotal = 0;
                foreach (BonusActionPossibility poss in possibilities)
                {
                    massTotal += poss.weight;
                    if (choice > massTotal) continue;

                    // Perform the bonus action.
                    PlayerBonus playerBonus = PlayerBonus.None; // which bonus to undo later, or None
                    switch (poss.action)
                    {
                        case BonusAction.Explode:
                            Gob explosion = Gob.CreateGob("bomb explosion");
                            explosion.Pos = this.Pos;
                            data.AddGob(explosion);
                            break;
                        case BonusAction.UpgradeWeapon1:
                            gobShip.Owner.UpgradeWeapon1();
                            playerBonus = PlayerBonus.Weapon1Upgrade;
                            break;
                        case BonusAction.UpgradeWeapon2:
                            gobShip.Owner.UpgradeWeapon2();
                            playerBonus = PlayerBonus.Weapon2Upgrade;
                            break;
                        case BonusAction.UpgradeWeapon1LoadTime:
                            gobShip.Owner.UpgradeWeapon1LoadTime();
                            playerBonus = PlayerBonus.Weapon1LoadTime;
                            break;
                        case BonusAction.UpgradeWeapon2LoadTime:
                            gobShip.Owner.UpgradeWeapon2LoadTime();
                            playerBonus = PlayerBonus.Weapon2LoadTime;
                            break;
                        default:
                            Log.Write("Bonus didn't do anything, programmer's mistake");
                            break;
                    }

                    if (playerBonus != PlayerBonus.None)
                    {
                        // Send the timed event for undoing the bonus, if required.
                        TimeSpan expiryTime = AssaultWing.Instance.GameTime.TotalGameTime
                            + new TimeSpan((long)(poss.duration * 10 * 1000 * 1000));
                        BonusExpiryEvent bonusEve = new BonusExpiryEvent(gobShip.Owner.Name,
                            playerBonus, expiryTime);
                        eventer.SendEvent(bonusEve);

                        // Set the player's bonus timing.
                        gobShip.Owner.BonusTimeins[playerBonus] = AssaultWing.Instance.GameTime.TotalGameTime;
                        gobShip.Owner.BonusTimeouts[playerBonus] = expiryTime;
                    }

                    // We found the action, break out of the search.
                    break;
                }
                Die();
            }
        }

        #endregion

        #region IDamageable Members
        // Some members are implemented in class Gob.

        #endregion

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
                lifetime = Math.Max(0.5f, lifetime);
            }
        }

        #endregion
    }
}