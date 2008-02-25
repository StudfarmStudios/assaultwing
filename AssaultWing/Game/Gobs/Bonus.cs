using System;
using System.Collections.Generic;
using System.Text;
using AW2.Helpers;
using Microsoft.Xna.Framework;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A bonus that can be collected by a player.
    /// </summary>
    public class Bonus : Gob, ISolid, IDamageable, IConsistencyCheckable
    {
        /// <summary>
        /// Lifetime of the bonus, in seconds.
        /// </summary>
        [TypeParameter]
        float lifetime;

        /// <summary>
        /// Time at which the bonus dies, in game time.
        /// </summary>
        TimeSpan deathTime;

        /// <summary>
        /// Creates an uninitialised bonus.
        /// </summary>
        /// This constructor is only for serialisation.
        public Bonus()
            : base()
        {
            this.lifetime = 10;
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
                switch (RandomHelper.GetRandomInt(2))
                {
                    case 0: // Explosion
                        DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                        Gob explosion = Gob.CreateGob("bomb explosion");
                        explosion.Pos = this.Pos;
                        data.AddGob(explosion);
                        break;
                    case 1: // Special weapon upgrade
                        gobShip.Owner.UpgradeWeapon2();
                        break;
                    default:
                        Log.Write("Bonus didn't do anything, programmer's mistake");
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