using System;
using System.Linq;
using AW2.Helpers;
using Microsoft.Xna.Framework.Graphics;
using AW2.Net.Messages;

namespace AW2.Game.Gobs.Bonus
{
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

        /// <summary>
        /// What happens when the bonus is collected.
        /// </summary>
        [TypeParameter]
        protected GameAction gameAction;

        protected string bonusText;
        protected string bonusIconName;
        protected Texture2D bonusIcon;

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

        public override void Activate()
        {
            base.Activate();
            deathTime = AssaultWing.Instance.GameTime.TotalGameTime + TimeSpan.FromSeconds(lifetime);
        }

        public override void Update()
        {
            base.Update();
            if (deathTime <= AssaultWing.Instance.GameTime.TotalGameTime)
                Die(new DeathCause());
        }

        #endregion Methods related to gobs' functionality in the game world

        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            // We assume we have only one receptor area and that's the one for
            // bonus collection. That means that the other gob is a ship.
            if (myArea.Type == CollisionAreaType.Receptor)
            {
                if (AssaultWing.Instance.NetworkMode != NetworkMode.Client)
                    DoBonusAction(theirArea.Owner.Owner);
                AssaultWing.Instance.SoundEngine.PlaySound("BonusCollection");
                Die(new DeathCause());
            }
        }

        /// <summary>
        /// Perform on a player a bonus action (either a player bonus or some other thing such as an explosion).
        /// </summary>
        /// <param name="player">The player to receive the bonus action.</param>
        protected abstract void DoBonusAction(Player player);

        /// <summary>
        /// Displays the BonusMessage on BonusGob collision position
        /// </summary>
        protected void DisplayMessage()
        {
            Gob.CreateGob((CanonicalString)"bonusmessage", gob =>
            {
                gob.ResetPos(Pos, gob.Move, gob.Rotation);
                ((BonusMessage)gob).Message = bonusText;
                ((BonusMessage)gob).IconName = bonusIconName;
                AssaultWing.Instance.DataEngine.Arena.Gobs.Add(gob);
            });
        }
    }
}
